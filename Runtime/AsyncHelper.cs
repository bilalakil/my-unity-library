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

    // ## Coroutines

    // #### Then
    
    public Async Then(Action cb)
    {
        _steps.Add(() => {
            cb?.Invoke();
            return null;
        });
        MaybeTakeStep();
        return this;
    }


    // #### Lerp
    
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


    // #### Wait
    
    public Async Wait(float secs, bool realtime = false)
    {
        _steps.Add(() => WaitCoroutine(secs, realtime));
        MaybeTakeStep();
        return this;
    }

    IEnumerator WaitCoroutine(float secs, bool realtime)
    {
        if (realtime) yield return new WaitForSecondsRealtime(secs);
        else yield return new WaitForSeconds(secs);
        FinishedStep();
    }


    // #### Every
    
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


    // #### LoadScene
    
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
/// <summary>Exists to be the default host for coroutines started from the `Async` class.</summary>
internal class AsyncHelper : MonoBehaviour
{
    internal static AsyncHelper I
    {
        get
        {
            if (!_haveInstantiated)
            {
                var obj = new GameObject("AsyncHelper");
                obj.AddComponent<AsyncHelper>();
                DontDestroyOnLoad(obj);
            }
            return _i;
        }
    }
    static AsyncHelper _i;
    static bool _haveInstantiated;

    void OnEnable()
    {
        if (_i != null)
        {
            Destroy(gameObject);
            return;
        }

        _i = this;
        _haveInstantiated = true;
    }
    void OnDisable()
    {
        if (_i == this) _i = null;
    }
}