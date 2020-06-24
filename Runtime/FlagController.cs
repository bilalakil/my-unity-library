using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class FlagController : MonoBehaviour
{
    const string PP_FPS_CHECK = "fpsCheck";
    const float FPS_CHECK_DURATION_SECS = 5f;

    static int[] FPS_STEPS = { 30, 60, 90, 120 };

    public static FlagController i
    {
        get
        {
            if (_i == null)
            {
                var obj = new GameObject("FlagController");
                obj.AddComponent<FlagController>();
                DontDestroyOnLoad(obj);
            }

            return _i;
        }
    }
    static FlagController _i;

    public static bool exists => _i != null;

    [NonSerialized] HashSet<string> _flags = new HashSet<string>();
    [NonSerialized] Dictionary<string, HashSet<Action>> _listeners =
        new Dictionary<string, HashSet<Action>>();

#if UNITY_EDITOR
    MyLibraryConfig _config;

    void Awake() =>
        _config = Resources.Load<MyLibraryConfig>("MyLibraryConfig");
#endif

    void OnEnable()
    {
        _i = this;
        PrepareStaticFlags();
    }

    void OnDisable() => _i = null;

    public void Add(string flag)
    {
        if (_flags.Contains(flag)) return;
        _flags.Add(flag);

        ExecuteListeners(flag);
    }

    public void Remove(string flag)
    {
        if (!_flags.Contains(flag)) return;
        _flags.Remove(flag);

        ExecuteListeners(flag);
    }

    public bool Check(string flag) => _flags.Contains(flag);

    public void RegisterListener(string flag, Action cb)
    {
        if (!_listeners.ContainsKey(flag))
            _listeners[flag] = new HashSet<Action>();

        if (_listeners[flag].Contains(cb))
            throw new NotSupportedException();

        _listeners[flag].Add(cb);
    }

    public void DeregisterListener(string flag, Action cb)
    {
        if (!_listeners.ContainsKey(flag) || !_listeners[flag].Contains(cb))
            throw new NotSupportedException();
        
        if (_listeners[flag].Count == 1) _listeners.Remove(flag);
        else _listeners[flag].Remove(cb);
    }

    void ExecuteListeners(string flag)
    {
        if (_listeners.ContainsKey(flag))
            foreach (var cb in _listeners[flag]) cb();
    }

    void PrepareStaticFlags()
    {
        PreparePlatformFlags();
        PrepareFPSFlags();
    }

    void PreparePlatformFlags()
    {
        var platform = Application.platform;

#if UNITY_EDITOR
        if (_config?.testConfig.useFlagPlatformOverride ?? false)
            platform = _config.testConfig.flagPlatformOverride;
#endif

        switch (platform)
        {
            case RuntimePlatform.Android:
                Add("platform_android");
                Add("platform_phone");
                break;
            case RuntimePlatform.IPhonePlayer:
                Add("platform_ios");
                Add("platform_phone");
                break;
            case RuntimePlatform.LinuxEditor:
                Add("platform_linux");
                Add("platform_pc");
                Add("platform_editor");
                break;
            case RuntimePlatform.LinuxPlayer:
                Add("platform_linux");
                Add("platform_pc");
                break;
            case RuntimePlatform.OSXEditor:
                Add("platform_macos");
                Add("platform_pc");
                Add("platform_editor");
                break;
            case RuntimePlatform.OSXPlayer:
                Add("platform_macos");
                Add("platform_pc");
                break;
            case RuntimePlatform.WebGLPlayer:
                Add("platform_web");
                break;
            case RuntimePlatform.WindowsEditor:
                Add("platform_windows");
                Add("platform_pc");
                Add("platform_editor");
                break;
            case RuntimePlatform.WindowsPlayer:
                Add("platform_windows");
                Add("platform_pc");
                break;
            default:
                Debug.LogWarning("Unsupported platform");
                break;
        }
    }

    void PrepareFPSFlags()
    {
        var recordedFPS = PlayerPrefs.GetFloat(PP_FPS_CHECK, -1f);

#if UNITY_EDITOR
        if (_config?.testConfig.useFlagFPSOverride ?? false)
            recordedFPS = _config.testConfig.flagFPSOverride;
#endif

        if (recordedFPS == -1f)
        {
            var startingFrameCount = Time.frameCount;

            new Async(this)
                .Wait(FPS_CHECK_DURATION_SECS)
                .Then(() => {
                    var curFrameCount = Time.frameCount;
                    var avg = curFrameCount / FPS_CHECK_DURATION_SECS;
                    PlayerPrefs.SetFloat(PP_FPS_CHECK, avg);
                    PrepareFPSFlags();
                });
            
            return;
        }

        foreach (var step in FPS_STEPS) Add(string.Format(
            "fps_{0}{1}",
            recordedFPS > step ? ">" : "<",
            step
        ));
    }
}
