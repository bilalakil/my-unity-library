// Compatible with SimpleRelay v0.x.x

#define SR_DEBUG
//#define SR_DEBUG_INFO

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleRelay : MonoBehaviour
{
    const string DEFAULT_LOCAL_ID = "1";
    const float HEARTBEAT_PERIOD_STABLE = 10f;
    const float HEARTBEAT_PERIOD_UNSTABLE = 2f;
    const int HEARTBEAT_COUNT_UNTIL_UNSTABLE = 3;
    const int HEARTBEAT_COUNT_UNTIL_RECONNECT = 6;
    const float PING_FREQUENCY = 3f;
    const string PP_SAVED_CONFIG_TEMPLATE = "SimpleRelay_SavedConfig_{0}";
    const int WS_CONNECTION_RETRIES = 2;
    
    static readonly HttpClient HttpClient;
    static readonly TimeSpan WSTimeout = TimeSpan.FromSeconds(3f);
    static readonly Regex OutgoingMessageStripPattern = new Regex("\\s*,?\\s*\"pinned\":\\s*false\\s*");

    static readonly HashSet<string> _liveSRIDs = new HashSet<string>();
    
    static void IDebugInfoS(string msg)
    {
#if SR_DEBUG && SR_DEBUG_INFO
        Debug.Log(string.Format("INFO SimpleRelay: {0}", msg));
#endif
    }

    static SimpleRelay()
    {
        HttpClient = new HttpClient();
        HttpClient.Timeout = TimeSpan.FromSeconds(PING_FREQUENCY);
    }

    public static bool CanTryReconnecting(string localId = DEFAULT_LOCAL_ID) =>
        !string.IsNullOrEmpty(PlayerPrefs.GetString(PPForLocalId(localId)));
    
    public static SimpleRelay Rejoin(string localId = DEFAULT_LOCAL_ID)
    {
        var ppKey = PPForLocalId(localId);
        var json = PlayerPrefs.GetString(ppKey);
        IDebugInfoS("Loading config: " + json);

        Config config;
        try { config = JsonUtility.FromJson<Config>(json); }
        catch (Exception e)
        {
            IDebugInfoS("Failed to load config, clearing it: " + e.ToString());
            PlayerPrefs.DeleteKey(ppKey);
            throw e;
        }

        return Spawn().Init(config);
    }

    public static SimpleRelay JoinRandom(string sessionType, int targetNumMembers, string localId = DEFAULT_LOCAL_ID) =>
        Spawn().Init(new Config {
            sessionType = sessionType,
            numMembers = targetNumMembers,
            localId = localId
        });

    public static SimpleRelay HostPrivate(string sessionType, int targetNumMembers, string localId = DEFAULT_LOCAL_ID) =>
        Spawn().Init(new Config {
            sessionType = sessionType,
            numMembers = targetNumMembers,
            isPrivate = true,
            localId = localId
        });

    public static SimpleRelay JoinPrivate(string sessionType, string sessionId, string localId = DEFAULT_LOCAL_ID) =>
        Spawn().Init(new Config {
            sessionType = sessionType,
            isPrivate = true,
            sessionId = sessionId,
            localId = localId
        });

    static SimpleRelay Spawn()
    {
        var obj = new GameObject("SimpleRelay");
        var simpleRelay = obj.AddComponent<SimpleRelay>();
        DontDestroyOnLoad(obj);
        return simpleRelay;
    }

    static string PPForLocalId(string localId) => string.Format(PP_SAVED_CONFIG_TEMPLATE, localId);

    public event Action onStateChanged;
    public event Action<SRMessage> onMessageReceived;

    public SRState State => _state;

    string PPString => PPForLocalId(_config.localId);
    bool IsWaitingForPassword => _config.isPrivate && string.IsNullOrEmpty(_config.sessionId);

    MyLibraryConfig _libConfig;
    Config _config;
    SRState _state = new SRState();
    long _lastMessageTime;
    float _lastHeartbeatAt;
    int _heartbeatsPending;
    bool _waitingToDisconnect;

    [NonSerialized] IWebSocket _ws;
    [NonSerialized] CancellationTokenSource _cancellation;

    void IDebugError(string msg)
    {
#if SR_DEBUG
        Debug.LogError(string.Format("ERROR SimpleRelay {0}: {1}", _config.localId, msg));
#endif
    }
    
    void IDebugInfo(string msg)
    {
#if SR_DEBUG && SR_DEBUG_INFO
        Debug.Log(string.Format("INFO SimpleRelay {0}: {1}", _config.localId, msg));
#endif
    }

    void Awake() => _libConfig = Resources.Load<MyLibraryConfig>("MyLibraryConfig");

    void OnEnable()
    {
        if (_config.initd)
        {
            IDebugInfo("Re-enabled - reconnecting");
            Reconnect();

            _liveSRIDs.Add(_config.localId);
        }
    }

    void Update() => TickHeartbeatLoop();

    void OnDisable()
    {
        if (    
            _state.ConnStatus == SRState.ConnectionStatus.Paused ||
            _state.ConnStatus == SRState.ConnectionStatus.Closed
        ) return;

        IDebugInfo("Disabled - pausing");
        Pause();
    }

    void OnDestroy()
    {
        if (_state.ConnStatus == SRState.ConnectionStatus.Closed) return;

        var shouldClearConfig = !_config.isPrivate && _state.CurStage == SRState.Stage.WaitingForMoreMembers;

        IDebugInfo("Destroyed - tearing down" + (shouldClearConfig ? " and clearing config" : ""));
        Teardown(
            SRState.CloseReason.ConnectionDied,
            shouldClearConfig,
            false
        );
    }

    public void Send(string payload, bool pinned = false)
    {
        if (_state.ConnStatus != SRState.ConnectionStatus.Connected) return;

        var msg = new OutgoingMessage { payload = payload, pinned = pinned };
        var json = JsonUtility.ToJson(msg);
        var strippedJson = OutgoingMessageStripPattern.Replace(json, "");

        Debug.Log(strippedJson);

        _ws.Send(strippedJson, WSTimeout);
    }

    public void Reconnect()
    {
        if (
            _state.ConnStatus == SRState.ConnectionStatus.Connecting ||
            _state.ConnStatus == SRState.ConnectionStatus.Closed
        ) return;

        KillWS();
        RunWS();

        _state.ready = false;
        _state.connStatus = SRState.ConnectionStatus.Reconnecting;
        onStateChanged?.Invoke();
    }

    public void Pause()
    {
        if (    
            _state.ConnStatus == SRState.ConnectionStatus.Paused ||
            _state.ConnStatus == SRState.ConnectionStatus.Closed
        ) return;

        KillWS();

        _state.connStatus = SRState.ConnectionStatus.Paused;
        onStateChanged?.Invoke();
    }

    public void Disconnect(bool endSession)
    {
        if (endSession && _state.CurStage == SRState.Stage.SessionInProgress)
        {
            IDebugInfo("End session requested. Will try to send end session instruction");

            _config.tryingToEndSession = true;
            SaveConfig();
            
            SendHeartbeat();
            return;
        }

        if (
            _state.ConnStatus == SRState.ConnectionStatus.Connecting ||
            _state.ConnStatus == SRState.ConnectionStatus.Reconnecting
        )
        {
            IDebugInfo("Disconnection requested. Waiting until connected for clean disconnection");

            _waitingToDisconnect = true;

            _liveSRIDs.Remove(_config.localId);
            ClearConfig();
            _config.localId = "";

            return;
        }

        IDebugInfo("Disconnection request, tearing down and clearing config");
        Teardown(SRState.CloseReason.DisconnectRequested, true, true);
    }

    SimpleRelay Init(Config config)
    {
        if (string.IsNullOrEmpty(config.localId))
            throw new InvalidOperationException("Invalid local ID");

        if (_liveSRIDs.Contains(config.localId))
            throw new NotSupportedException(
                "Duplicate SimpleRelay ID used. " +
                "Either terminate the existing SimpleRelay, " +
                "or create a new one with a different local ID"
            );

        config.initd = true;
        _config = config;
        _liveSRIDs.Add(_config.localId);

        if (!string.IsNullOrEmpty(_config.memberId))
            _state.stage = SRState.Stage.SessionInProgress;
        
        RunWS();
        
        return this;
    }

    async void RunWS() => await RunWSAsync();
    async Task RunWSAsync()
    {
        var wsConnectionAttemptNum = 0;
        
        while (true)
        {
            _cancellation = new CancellationTokenSource();
            var localCancellation = _cancellation;

            IDebugInfo("Testing web connectivity via HTTPS ping");
            while (true)
            {
                HttpResponseMessage res = null;
                try { res = await HttpClient.GetAsync(GetPingURL(), localCancellation.Token); }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }

                if (localCancellation.IsCancellationRequested) return;

                if (res != null && res.StatusCode == HttpStatusCode.OK) break;

                if (
                    _state.CurStage == SRState.Stage.WaitingForMoreMembers &&
                    _state.ConnStatus == SRState.ConnectionStatus.Connecting
                )
                {
                    IDebugInfo(
                        "Failed to ping, aborting initial connection. Ping URL: " + 
                        GetPingURL()
                    );
                    Teardown(SRState.CloseReason.InitialConnectionFailed, true, true);
                    return;
                }

                await Task.Run(
                    () => localCancellation.Token.WaitHandle.WaitOne(
                        HttpClient.Timeout
                    )
                );
                if (localCancellation.IsCancellationRequested) return;
            }
            IDebugInfo("Ping successful");

            var url = GetConnectionURL(_config);
            _ws = WebSocketFactory.Get(url, localCancellation.Token);
            _ws.onMessageReceived += HandleWSMessageReceived;

            IDebugInfo("WS attempting connection to: " + url);

            await _ws.Connect(WSTimeout);
            if (localCancellation.IsCancellationRequested) return;

            if (!_ws.IsConnected)
            {
                if (wsConnectionAttemptNum != WS_CONNECTION_RETRIES)
                {
                    ++wsConnectionAttemptNum;
                    IDebugInfo("WS failed to connect - retrying");
                    continue;
                }

                _ws = null;

                IDebugInfo(
                    "WS failed to connect - tearing down and clearing config. Connection URL: " +
                    GetConnectionURL(_config)
                );
                Teardown(
                    _state.ConnStatus == SRState.ConnectionStatus.Reconnecting
                        ? SRState.CloseReason.ConnectionDied
                        : SRState.CloseReason.InitialConnectionFailed,
                    true,
                    true
                );
                return;
            }

            if (_waitingToDisconnect)
            {
                IDebugInfo("Disconnecting now that WS is properly connected");
                Teardown(SRState.CloseReason.DisconnectRequested, false, true);
                return;
            }

            IDebugInfo("WS successfully connected. Entering message loop, waiting for intro message");
            _state.connIsStable = true;
            _state.connStatus = SRState.ConnectionStatus.Connected;
            onStateChanged?.Invoke();
            
            wsConnectionAttemptNum = 0;
            _lastHeartbeatAt = Time.unscaledTime;
            _heartbeatsPending = 0;

            await _ws.ReceiveLoop();
            if (localCancellation.IsCancellationRequested) return;

            IDebugInfo("WS receive loop died, will retry");

            _state.connStatus = SRState.ConnectionStatus.Reconnecting;
            onStateChanged?.Invoke();
        }
    }

    void KillWS()
    {
        if (_ws != null)
        {
            _ws.onMessageReceived -= HandleWSMessageReceived;
            _ws.Dispose();
            _ws = null;
        }

        if (_cancellation != null)
        {
            _cancellation.Cancel(false);
            _cancellation = null;
        }
    }

    void HandleWSMessageReceived(string json)
    {
#if SR_DEBUG_INFO
        if (json != "[{\"type\":\"HEARTBEAT\"}]")
            IDebugInfo("Received message: " + json);
#endif

        Messages messages;
        try
        {
            messages = JsonUtility.FromJson<Messages>("{\"messages\":"+json+"}");
        }
        catch (Exception e)
        {
            IDebugError("Failed to parse raw message: " + e.ToString());
            return;
        }

        foreach (var message in messages.messages)
            RouteMessage(message);
    }

    void RouteMessage(Message message)
    {
        if (message.type == "CONNECTION_OVERWRITE") HandleConnectionOverwrite();
        else if (message.type == "HEARTBEAT") HandleHeartbeat();
        else if (message.type == "INVALID_CONNECTION") HandleInvalidConnection();
        else if (message.type == "MEMBER_DISCONNECT") HandleMemberDisconnect(message);
        else if (message.type == "MEMBER_RECONNECT") HandleMemberReconnect(message);
        else if (message.type == "MESSAGE") HandleMessage(message);
        else if (message.type == "PRIVATE_SESSION_PENDING") HandlePrivateSessionPending(message);
        else if (message.type == "SESSION_END") HandleSessionEnd();
        else if (message.type == "SESSION_RECONNECT") HandleSessionReconnect(message);
        else if (message.type == "SESSION_START") HandleSessionStart(message);
        else IDebugError("Unhandled message type: " + message.type);
    }

    void HandleConnectionOverwrite()
    {
        IDebugInfo("CONNECTION_OVERWRITE received - tearing down and clearing config");
        Teardown(SRState.CloseReason.ConnectionOverwritten, true, true);
    }

    void HandleHeartbeat()
    {
        _heartbeatsPending = 0;

        if (!_state.ConnIsStable)
        {
            _state.connIsStable = true;
            onStateChanged?.Invoke();
        }
    }

    void HandleInvalidConnection()
    {
        IDebugInfo("INVALID_CONNECTION received - reconnecting");
        Reconnect();
    }

    void HandleMemberDisconnect(Message message)
    {
        _state.memberPresence[message.memberNum] = false;
        onStateChanged?.Invoke();
    }

    void HandleMemberReconnect(Message message)
    {
        _state.memberPresence[message.memberNum] = true;
        onStateChanged?.Invoke();
    }

    void HandleMessage(IMessage message)
    {
        SRMessage srMessage;
        if (message is SRMessage) srMessage = message as SRMessage;
        else srMessage = new SRMessage(message);

        _lastMessageTime = srMessage.Time;

        onMessageReceived?.Invoke(srMessage);

        if (message.Pinned)
        {
            _state.pinnedMessage = srMessage;
            onStateChanged?.Invoke();
        }
    }

    void HandlePrivateSessionPending(Message message)
    {
        // Could be received multiple times due to heartbeat
        if (_state.Ready) return;

        _config.sessionId = message.sessionId;
        
        _state.password = message.sessionId;

        SaveConfig();
        onStateChanged?.Invoke();
    }

    void HandleSessionEnd()
    {
        IDebugInfo("SESSION_END received - tearing down and clearing config");
        Teardown(SRState.CloseReason.SessionEnded, true, true);
    }

    void HandleSessionReconnect(Message message)
    {
        // Could be received multiple times due to heartbeat
        if (_state.Ready) return;

        _state.memberNum = message.memberNum;
        _state.memberPresence = message.members;
        _state.pinnedMessage = message.pinnedMessage;
        _state.ready = true;
        _state.stage = SRState.Stage.SessionInProgress;

        if (_state.PinnedMessage != null)
            _lastMessageTime = _state.PinnedMessage.Time;

        onStateChanged?.Invoke();
    }

    void HandleSessionStart(Message message)
    {
        // Could be received multiple times due to heartbeat
        if (_state.Ready) return;

        _config.sessionType = message.sessionType;
        _config.sessionId = message.sessionId;
        _config.numMembers = message.numMembers;
        _config.memberId = message.memberId;

        _state.memberNum = message.memberNum;
        _state.ready = true;
        _state.stage = SRState.Stage.SessionInProgress;
        _state.password = null;

        _state.memberPresence = new bool[_config.numMembers];
        for (var i = 0; i != _config.numMembers; ++i)
            _state.memberPresence[i] = true;

        SaveConfig();
        onStateChanged?.Invoke();
    }

    string GetConnectionURL(Config config)
    {
        var parts = new List<string>();

        var haveID = !string.IsNullOrEmpty(config.sessionId);
        if (haveID)
            parts.Add("sessionId=" + WebUtility.UrlEncode(config.sessionId));

        if (!string.IsNullOrEmpty(config.memberId))
            parts.Add("memberId=" + WebUtility.UrlEncode(config.memberId));
        else
        {
            if (!string.IsNullOrEmpty(config.sessionType))
                parts.Add("sessionType=" + WebUtility.UrlEncode(config.sessionType));

            if (!haveID)
            {
                if (config.numMembers != 0)
                    parts.Add("targetNumMembers=" + config.numMembers);
                if (config.isPrivate)
                    parts.Add("private=true");
            }
        }

        return _libConfig.simpleRelayWSSURL + "?" + string.Join("&", parts);
    }

    string GetPingURL() => _libConfig.simpleRelayHTTPSURL + "/ping";

    void SaveConfig()
    {
        IDebugInfo("Saving config");
        PlayerPrefs.SetString(PPString, JsonUtility.ToJson(_config));
    }

    void Teardown(SRState.CloseReason reason, bool clearConfig, bool selfDestruct)
    {
        KillWS();

        _liveSRIDs.Remove(_config.localId);

        if (clearConfig) ClearConfig();

        _state.connStatus = SRState.ConnectionStatus.Closed;
        _state.closeStageReason = reason;
        onStateChanged?.Invoke();

        if (selfDestruct) DestroyImmediate(gameObject);
    }

    void ClearConfig() => PlayerPrefs.DeleteKey(PPString);

    void TickHeartbeatLoop()
    {
        if (
            _state.ConnStatus != SRState.ConnectionStatus.Connected ||
            _cancellation.IsCancellationRequested
        ) return;
        
        var heartbeatDelay =
            IsWaitingForPassword ||
            (_state.CurStage == SRState.Stage.SessionInProgress && !_state.Ready) ||
            _heartbeatsPending != 0
                ? HEARTBEAT_PERIOD_UNSTABLE
                : HEARTBEAT_PERIOD_STABLE;
            
        if (Time.unscaledTime < _lastHeartbeatAt + heartbeatDelay) return;

        SendHeartbeat();
    }

    void SendHeartbeat()
    {
        if (_state.ConnStatus != SRState.ConnectionStatus.Connected) return;

        var waitingFor = new List<string>();
        if (!_state.ready)
        {
            if (IsWaitingForPassword)
                waitingFor.Add("\"PRIVATE_SESSION_PENDING\"");

            if (string.IsNullOrEmpty(_config.memberId)) waitingFor.Add("\"SESSION_START\"");
            else waitingFor.Add("\"SESSION_RECONNECT\"");
        }

        _ws.Send(
            _config.tryingToEndSession
                ? "{\"action\":\"END_SESSION\"}"
                : string.Format(
                    "{{" +
                        "\"action\":\"HEARTBEAT\"" +
                        ",\"inclMessagesAfter\":{0}" +
                        (
                            waitingFor.Count == 0 ? ""
                                : ",\"waitingFor\":[" + string.Join(",", waitingFor) + "]"
                        ) +
                        "}}",
                    _lastMessageTime
            ),
            WSTimeout
        );

        _lastHeartbeatAt = Time.unscaledTime;
        ++_heartbeatsPending;

        if (_heartbeatsPending == HEARTBEAT_COUNT_UNTIL_RECONNECT)
        {
            IDebugInfo("Too many missed heartbeats - reconnecting");
            Reconnect();
            return;
        }

        if (
            _heartbeatsPending >= HEARTBEAT_COUNT_UNTIL_UNSTABLE &&
            _state.ConnIsStable
        )
        {
            _state.connIsStable = false;
            onStateChanged?.Invoke();
        }
    }

    [Serializable]
    struct Config
    {
        public bool initd;
        public string localId;
        public string sessionType;
        public int numMembers;
        public bool isPrivate;
        public string sessionId;
        public string memberId;
        public bool tryingToEndSession;
    }

    public interface IMessage
    {
        int MemberNum { get; }
        string Payload { get; }
        bool Pinned { get; }
        ///<summary>Milliseconds since epoch</summary>
        long Time { get; }
    }

    [Serializable]
    struct Messages
    {
#pragma warning disable CS0649
        public List<Message> messages;
#pragma warning restore CS0649
    }

    [Serializable]
    struct Message : SimpleRelay.IMessage
    {
        // SimpleRelay.IMessage
        public int MemberNum => memberNum;
        public String Payload => payload;
        public bool Pinned => pinned;
        public long Time => time;


#pragma warning disable CS0649
        public string type;

        // MESSAGE, SESSION_RECONNECT
        public int memberNum;

        // MESSAGE
        public string payload;
        public bool pinned;
        public long time;

        // SESSION_RECONNECT
        public bool[] members;
        public SRMessage pinnedMessage;

        // SESSION_START
        public string sessionType;
        public string sessionId;
        public string memberId;
        public int numMembers;
#pragma warning restore CS0649
    }

    class OutgoingMessage
    {
        public string action = "SEND_MESSAGE";
        public string payload;
        public bool pinned;
    }
}

[Serializable]
public class SRState
{
    public Stage CurStage => stage;
    public ConnectionStatus ConnStatus => connStatus;
    /// <summary>Only relevant during `ConnectionStatus.Connected`</summary>
    public bool ConnIsStable => connIsStable;
    /// <summary>Even when connected, state may be out of date while `!Ready`</summary>
    public bool Ready => ready;
    public CloseReason CloseStageReason => closeStageReason;
    public int MemberNum => memberNum;
    public IReadOnlyList<bool> MemberPresence => memberPresence;
    public string Password => password == "" ? null : password;

    // Odd check is to avoid deserialisation issues,
    // specifically that missing values are still constructed during deserialisation 
    public SRMessage PinnedMessage =>
        (pinnedMessage?.Time ?? 0) == 0 ? null : pinnedMessage;

    [SerializeField] internal Stage stage =
        Stage.WaitingForMoreMembers;
    [SerializeField] internal ConnectionStatus connStatus =
        ConnectionStatus.Connecting;
    [SerializeField] internal bool connIsStable = true;
    [SerializeField] internal bool ready;
    [SerializeField] internal CloseReason closeStageReason;
    [SerializeField] internal int memberNum = -1;
    [SerializeField] internal bool[] memberPresence;
    [SerializeField] internal SRMessage pinnedMessage;
    [SerializeField] internal string password;

    public enum Stage
    {
        WaitingForMoreMembers,
        SessionInProgress,
        ConnectionClosed
    }

    public enum ConnectionStatus
    {
        Connecting,
        Connected,
        Paused,
        Reconnecting,
        Closed
    }

    public enum CloseReason
    {
        InitialConnectionFailed,
        SessionEnded,
        ConnectionOverwritten,
        ConnectionDied,
        DisconnectRequested
    }
}

[Serializable]
public class SRMessage : SimpleRelay.IMessage
{
    // SimpleRelay.IMessage
    public int MemberNum => memberNum;
    public String Payload => payload;
    public bool Pinned => pinned;
    public long Time => time;


#pragma warning disable CS0649
    [SerializeField] internal int memberNum;
    [SerializeField] internal string payload;
    [SerializeField] internal bool pinned;
    [SerializeField] internal long time;
#pragma warning restore CS0649

    internal SRMessage(SimpleRelay.IMessage message)
    {
        memberNum = message.MemberNum;
        payload = message.Payload;
        pinned = message.Pinned;
        time = message.Time;
    }
}
