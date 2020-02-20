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

    /// <summary>
    /// Instead of destroying the game object if there is a match,
    /// only destroy it if there is not a match.
    /// </summary>
    public bool invert;
    public bool phone;

    void Awake()
    {
        if (ShouldDestroyGameObject()) Destroy(gameObject);
        else Destroy(this);
    }

    bool ShouldDestroyGameObject()
    {
        var plat = Application.platform;
        var match =
            (phone && Array.IndexOf(_phones, phone) != -1);

        return invert ? !match : match;
    }
}