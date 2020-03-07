using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Async
{
    MonoBehaviour _owner;
    bool _inStep;
    List<Func<IEnumerator>> _steps = new List<Func<IEnumerator>>();

    public Async(MonoBehaviour owner) => _owner = owner;
    public Async() : this(AsyncHelper.I) { }

    void MaybeTakeStep()
    {
        if (_steps.Count == 0 || _inStep) return;
        _inStep = true;

        var enumerator = _steps[0]();
        if (enumerator != null) _owner.StartCoroutine(enumerator);

        _steps.RemoveAt(0);

        if (enumerator == null) FinishedStep();
    }

    void FinishedStep()
    {
        _inStep = false;
        MaybeTakeStep();
    }

    // ======== Coroutines

    // ==== Then
    
    public Async Then(Action cb)
    {
        _steps.Add(() => {
            cb?.Invoke();
            return null;
        });
        return this;
    }


    // ==== Lerp
    
    public Async Lerp(float from, float to, float over, Action<float> step)
    {
        _steps.Add(() => LerpCoroutine(from, to, over, step));
        MaybeTakeStep();
        return this;
    }
    public Async Lerp(float over, Action<float> step) => Lerp(0, 1, over, step);

    IEnumerator LerpCoroutine(float from, float to, float over, Action<float> cb)
    {
        var d = 0f;
        var change = to - from;

        while (d < over)
        {
            d = Mathf.Min(d + Time.deltaTime, over);
            var val = from + change * (d / over);
            cb?.Invoke(val);

            yield return null;
        }

        cb?.Invoke(to);
        FinishedStep();
    }


    // ==== Wait
    
    public Async Wait(float secs)
    {
        _steps.Add(() => WaitCoroutine(secs));
        MaybeTakeStep();
        return this;
    }

    IEnumerator WaitCoroutine(float secs)
    {
        yield return new WaitForSeconds(secs);
        FinishedStep();
    }


    // ==== Every
    
    public Async Every(float secs, Action cb)
    {
        _steps.Add(() => EveryCoroutine(secs, cb));
        MaybeTakeStep();
        return this;
    }

    IEnumerator EveryCoroutine(float secs, Action cb)
    {
        while (true)
        {
            yield return new WaitForSeconds(secs);
            cb?.Invoke();
        }
    }


    // ==== LoadScene
    
    public Async LoadScene(string path)
    {
        _steps.Add(() => LoadSceneCoroutine(path));
        MaybeTakeStep();
        return this;
    }

    IEnumerator LoadSceneCoroutine(string path)
    {
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(path);
        while(!op.isDone) yield return null;
        
        FinishedStep();
    }
}

[AddComponentMenu("")] // To prevent it from showing up in the Add Component list
[DefaultExecutionOrder(-10000)]
/// <summary>Exists to be the default host for coroutines started from the Async class.</summary>
public class AsyncHelper : MonoBehaviour
{
    public static AsyncHelper I { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var o = new GameObject();
        o.name = "AsyncHelper";
        o.AddComponent<AsyncHelper>();
        DontDestroyOnLoad(o);
    }

    void OnEnable() => I = this;
}

