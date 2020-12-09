#define WS_DEBUG
//#define WS_DEBUG_INFO

using System;
using System.Threading;

#if UNITY_WEBGL && !UNITY_EDITOR

public class WebSocket : IWebSocket
{
    public WebSocket(string url, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public event Action<string> onMessageReceived;

    public bool IsConnected => throw new NotImplementedException();

    public Task Connect(TimeSpan timeout) => throw new NotImplementedException();

    public Task ReceiveLoop() => throw new NotImplementedException();

    public void Send(string msg, TimeSpan timeout) => throw new NotImplementedException();

    public void ClearSendQueue() => throw new NotImplementedException();
    
    public void Dispose() => throw new NotImplementedException();
}

#else // !(UNITY_WEBGL && !UNITY_EDITOR)

using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

public class WebSocket : IWebSocket, IDisposable
{
    public static void IDebugInfo(string msg)
    {
#if WS_DEBUG && WS_DEBUG_INFO
        UnityEngine.Debug.Log("INFO WebSocket: " + msg);
#endif
    }

    ClientWebSocket _ws;
    Uri _uri;
    Queue<(string, TimeSpan)> _messages = new Queue<(string, TimeSpan)>();
    bool _sendingMessages;
    CancellationToken _token;

    public WebSocket(string url, CancellationToken cancellationToken)
    {
        _token = cancellationToken;
        _ws = new ClientWebSocket();
        _uri = new Uri(url);
    }

    public event Action<string> onMessageReceived;

    public bool IsConnected => _ws.State == WebSocketState.Open;

    public async Task Connect(TimeSpan timeout)
    {
        try { await LimitTaskTimeOrDie(_ws.ConnectAsync(_uri, _token), timeout); }
        catch (WebSocketException) { return; }
    }

    // Initially based off of: https://thecodegarden.net/websocket-client-dotnet
    public async Task ReceiveLoop()
    {
        var buffer = new ArraySegment<byte>(new byte[2048]);

        while (!_token.IsCancellationRequested)
        {
            WebSocketReceiveResult result;

            using (var memory = new MemoryStream())
            {
                do
                {
                    try
                    {
                        result = await _ws.ReceiveAsync(buffer, _token);
                    }
                    catch (OperationCanceledException) { return; }
                    catch (WebSocketException)
                    {
                        IDebugInfo("Died");
                        return;
                    }

                    if (_token.IsCancellationRequested) return;

                    memory.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!_token.IsCancellationRequested && !result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close) break;

                memory.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(memory, Encoding.UTF8))
                    onMessageReceived?.Invoke(await reader.ReadToEndAsync());
            }
        }
    }

    public void Send(string msg, TimeSpan timeout)
    {
        _messages.Enqueue((msg, timeout));

        if (!_sendingMessages) RunSendLoop();
    }

    public void ClearSendQueue() => _messages.Clear();

    public void Dispose()
    {
        IDebugInfo("Dispose");

        try { _ws.Dispose(); }
        catch {}
    }

    // For some reason this succeeds even if the internet is disconnected :(
    async void RunSendLoop()
    {
        _sendingMessages = true;

        while (_messages.Count != 0)
        {
            var (msg, timeout) = _messages.Dequeue();
            IDebugInfo("Sending message: " + msg);

            var bytes = Encoding.UTF8.GetBytes(msg);
            var buffer = new ArraySegment<Byte>(bytes, 0, bytes.Length);

            try
            {
                await LimitTaskTimeOrDie(
                    _ws.SendAsync(buffer, WebSocketMessageType.Text, true, _token),
                    timeout
                );
            }
            catch (WebSocketException) { return; }

            if (_token.IsCancellationRequested) _messages.Clear();
        }

        _sendingMessages = false;
    }

    async Task LimitTaskTimeOrDie(Task task, TimeSpan timeout)
    {
        var complete = false;
        var mainTask = task.ContinueWith((_) => complete = true);

        var giveUpTask = Task.Run(() => _token.WaitHandle.WaitOne(timeout))
            .ContinueWith((_) =>
            {
                if (complete) return;

                IDebugInfo("Operation timed out, self-destructing");
                _ws.Dispose();
            });
        
        await Task.WhenAny(mainTask, giveUpTask);
    }
}

#endif

public static class WebSocketFactory
{
    public static IWebSocket Get(string url, CancellationToken cancellationToken) =>
        new WebSocket(url, cancellationToken);
}

public interface IWebSocket : IDisposable
{
    event Action<string> onMessageReceived;

    bool IsConnected { get; }

    Task Connect(TimeSpan timeout);
    Task ReceiveLoop();
    void Send(string msg, TimeSpan timeout);
    void ClearSendQueue();
}