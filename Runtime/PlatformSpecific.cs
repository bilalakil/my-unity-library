using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// On awake, checks if the game is running on a platform matching supplied flags,
/// and destroys the game object if there is no match.
///
/// Note that in the editor, instead of destroying the object, it just disables it
/// to allow testing via Other > Test > Invert Platform Match.
/// </summary>
public class PlatformSpecific : MonoBehaviour
{
#if UNITY_EDITOR
    static event Action onTestChanged;

    static bool _testInvert;

    [MenuItem("Other/Play Mode/PlatformSpecific: Test Invert")]
    static void TestInvert()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("PlatformSpecific: Test Invert - Must be used in play mode!");
            return;
        }

        _testInvert = !_testInvert;
        onTestChanged?.Invoke();
    }

    [MenuItem("Other/Play Mode/PlatformSpecific: Reset Test Invert")]
    static void ResetTestInvert()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("PlatformSpecific: Reset Test Invert - Must be used in play mode!");
            return;
        }

        _testInvert = false;
        onTestChanged?.Invoke();
    }
#endif

    static RuntimePlatform[] _phones = new RuntimePlatform[]
    {
        RuntimePlatform.IPhonePlayer,
        RuntimePlatform.Android
    };

    [Tooltip("Enables child game objects if there is a successful match.")]
    public bool enableChildren;
    [Space]
    [Tooltip("If invert is true, then the object will be enabled if it DOESN'T match the selected platform.")]
    public bool invert;
    public bool windows;
    public bool mac;
    public bool linux;
    public bool phone;
    public bool web;

#if UNITY_EDITOR
    [NonSerialized] bool _watchingTestChanged;
#endif

    void OnEnable() 
    {
#if UNITY_EDITOR
        if (_watchingTestChanged) return;
        _watchingTestChanged = true;

        onTestChanged += Do;
#endif

        Do();
    }

#if UNITY_EDITOR
    void OnDestroy() => onTestChanged -= Do;
#endif

    void Do()
    {
        var enable = ShouldEnable();

#if UNITY_EDITOR
        gameObject.SetActive(enable);
#endif

        if (enable)
        {
            if (enableChildren)
                foreach (Transform child in transform)
                    child.gameObject.SetActive(true);
        }
#if !UNITY_EDITOR
        else Destroy(gameObject);
        
        Destroy(this);
#endif
    }

    bool ShouldEnable()
    {
        var plat = Application.platform;
        var match =
            (windows && (plat == RuntimePlatform.WindowsPlayer || plat == RuntimePlatform.WindowsEditor))
            || (mac && (plat == RuntimePlatform.OSXPlayer || plat == RuntimePlatform.OSXEditor))
            || (linux && (plat == RuntimePlatform.LinuxPlayer || plat == RuntimePlatform.LinuxEditor))
            || (phone && Array.IndexOf(_phones, plat) != -1)
            || (web && plat == RuntimePlatform.WebGLPlayer);

        var inverted = invert ? !match : match;

#if UNITY_EDITOR
        return _testInvert ? !inverted : inverted;
#else
        return inverted;
#endif
    }
}