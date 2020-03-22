using System;
using UnityEngine;

/// <summary>
/// On Awake, checks if the game is running on a platform matching supplied flags,
/// and either destroys the gameObject or this component itself depending on if there was a match.
/// </summary>
public class DestroyOnPlatforms : MonoBehaviour
{
    static RuntimePlatform[] _phones = new RuntimePlatform[]
    {
        RuntimePlatform.IPhonePlayer,
        RuntimePlatform.Android
    };

    [Tooltip("Destroy child game objects instead of this game object.")]
    public bool destroyChildren;
    [Space]
    [Tooltip("Instead of destroying the game object on platform match, only destroy it if it doesn't.")]
    public bool invert;
    public bool windows;
    public bool mac;
    public bool linux;
    public bool phone;
    public bool web;

    void Awake()
    {
        var destroy = ShouldDestroyGameObject();

        if (destroyChildren)
        {
            foreach (Transform child in transform)
                if (destroy) Destroy(child.gameObject);
                else child.gameObject.SetActive(true);
        }
        else if (destroy) Destroy(gameObject);

        Destroy(this);
    }

    bool ShouldDestroyGameObject()
    {
        var plat = Application.platform;
        var match =
            (windows && (plat == RuntimePlatform.WindowsPlayer || plat == RuntimePlatform.WindowsEditor))
            || (mac && (plat == RuntimePlatform.OSXPlayer || plat == RuntimePlatform.OSXEditor))
            || (linux && (plat == RuntimePlatform.LinuxPlayer || plat == RuntimePlatform.LinuxEditor))
            || (phone && Array.IndexOf(_phones, plat) != -1)
            || (web && plat == RuntimePlatform.WebGLPlayer);

        return invert ? !match : match;
    }
}