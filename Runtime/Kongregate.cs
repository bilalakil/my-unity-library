using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Kongregate : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")] static extern bool KongregateInit();
    [DllImport("__Internal")] static extern void KongregateSubmitStat(string stat, int value);
#else
    static bool KongregateInit() { return false; }
    static void KongregateSubmitStat(string stat, int value) {}
#endif

    static Kongregate _i;

    public static void SetStat(string name, int value)
    {
        if (_i == null || !_i._connected) return;
        KongregateSubmitStat(name, value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;

        var obj = new GameObject();
        obj.name = "Kongregate";
        DontDestroyOnLoad(obj);
        obj.AddComponent<Kongregate>();

        var exists = false;
        try { exists = KongregateInit(); }
        catch (EntryPointNotFoundException) { }

        if (!exists) Destroy(obj);
    }

    bool _connected;

    void OnEnable() => _i = this;

    void OnKongregateAPILoaded() => _connected = true;
}
