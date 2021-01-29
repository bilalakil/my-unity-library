using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")] // To prevent it from showing up in the Add Component list
[DefaultExecutionOrder(-1000)]
public class FlagController : MonoBehaviour
{
    const float FPS_CHECK_DURATION_SECS = 5f;

    static int[] FPS_STEPS = { 30, 60, 90, 120 };

    static FlagController _i
    {
        get
        {
            if (!_haveInstantiated)
            {
                var obj = new GameObject("FlagController");
                obj.AddComponent<FlagController>();
                DontDestroyOnLoad(obj);
            }
            return _iBacking;
        }
    }
    static FlagController _iBacking;
    static bool _haveInstantiated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _iBacking = null;
        _haveInstantiated = false;
    }

    public static void Add(string flag) => _i.Add_(flag);
    public static void Remove(string flag) => _i?.Remove_(flag);
    public static bool Check(string flag) => _i.Check_(flag);
    public static void RegisterListener(string flag, Action cb) =>
        _i.RegisterListener_(flag, cb);
    public static void DeregisterListener(string flag, Action cb) =>
        _i?.DeregisterListener_(flag, cb);

    [NonSerialized] HashSet<string> _flags = new HashSet<string>();
    [NonSerialized] Dictionary<string, HashSet<Action>> _listeners =
        new Dictionary<string, HashSet<Action>>();

#if UNITY_EDITOR
    MyLibraryConfig _config;

    void Awake() =>
        _config = Resources.Load<MyLibraryConfig>("MyLibraryConfig");
#endif

    float _sessionFPS;

    void OnEnable()
    {
        if (_iBacking != null)
        {
            Destroy(gameObject);
            return;
        }

        _iBacking = this;
        _haveInstantiated = true;
        PrepareStaticFlags();
    }
    void OnDisable()
    {
        if (_iBacking == this)
            _iBacking = null;
    }

    void Add_(string flag)
    {
        if (_flags.Contains(flag)) return;
        _flags.Add(flag);

        ExecuteListeners(flag);
    }

    void Remove_(string flag)
    {
        if (!_flags.Contains(flag)) return;
        _flags.Remove(flag);

        ExecuteListeners(flag);
    }

    bool Check_(string flag) => _flags.Contains(flag);

    void RegisterListener_(string flag, Action cb)
    {
        if (!_listeners.ContainsKey(flag))
            _listeners[flag] = new HashSet<Action>();

        if (_listeners[flag].Contains(cb))
            throw new NotSupportedException();

        _listeners[flag].Add(cb);
    }

    void DeregisterListener_(string flag, Action cb)
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
                Add_("platform_android");
                Add_("platform_phone");
                break;
            case RuntimePlatform.IPhonePlayer:
                Add_("platform_ios");
                Add_("platform_phone");
                break;
            case RuntimePlatform.LinuxEditor:
                Add_("platform_linux");
                Add_("platform_pc");
                Add_("platform_editor");
                break;
            case RuntimePlatform.LinuxPlayer:
                Add_("platform_linux");
                Add_("platform_pc");
                break;
            case RuntimePlatform.OSXEditor:
                Add_("platform_macos");
                Add_("platform_pc");
                Add_("platform_editor");
                break;
            case RuntimePlatform.OSXPlayer:
                Add_("platform_macos");
                Add_("platform_pc");
                break;
            case RuntimePlatform.WebGLPlayer:
                Add_("platform_web");
                break;
            case RuntimePlatform.WindowsEditor:
                Add_("platform_windows");
                Add_("platform_pc");
                Add_("platform_editor");
                break;
            case RuntimePlatform.WindowsPlayer:
                Add_("platform_windows");
                Add_("platform_pc");
                break;
            default:
                Debug.LogWarning("Unsupported platform");
                break;
        }
    }

    void PrepareFPSFlags()
    {
#if UNITY_EDITOR
        if (_config?.testConfig.useFlagFPSOverride ?? false)
            _sessionFPS = _config.testConfig.flagFPSOverride;
#endif

        if (_sessionFPS == 0f)
        {
            var startingFrameCount = Time.frameCount;

            new Async(this)
                .Wait(FPS_CHECK_DURATION_SECS, TimeMode.Unscaled)
                .Then(() => {
                    var curFrameCount = Time.frameCount;
                    _sessionFPS = curFrameCount / FPS_CHECK_DURATION_SECS;
                    PrepareFPSFlags();
                });
            
            return;
        }

        foreach (var step in FPS_STEPS) Add_(string.Format(
            "fps_{0}{1}",
            _sessionFPS > step ? ">" : "<",
            step
        ));
    }
}
