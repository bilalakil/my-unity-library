using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/**
 * ## Notes
 *
 * #### Sound naming convention
 * Registered sounds can be accessed via the name of their game object,
 * ignoring "(Clone)" and any ending numbers.
 * i.e. "explosion1(Clone)" and "explosion2" will be named "explosion".
 *
 * #### Sound randomisation
 * If multiple sounds are registered with the same name (see above),
 * requests with that name will choose among them at random.
 *
 * #### Auto-initialised sound library
 * If prefab with the name "DefaultSounds" exists in a Resources folder,
 * it will be automatically initialised and made `DontDestroyOnLoad`.
 */

[DefaultExecutionOrder(-1000)]
public class SoundController : MonoBehaviour
{
    static char[] _digits
        = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

    static SoundController _i;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var obj = new GameObject();
        obj.name = "SoundController";
        DontDestroyOnLoad(obj);
        obj.AddComponent<SoundController>();

        var defaultSounds = Resources.Load<GameObject>("DefaultSounds");
        if (defaultSounds != null)
            DontDestroyOnLoad(Instantiate(defaultSounds));
    }

    public static void RegisterSound(AudioSource sound)
        => _i.RegisterSound_(sound);
    public static void DeregisterSound(AudioSource sound)
        => _i.DeregisterSound_(sound);
    /// <summary>See "Sound naming convention" in this file's notes.</summary>
    public static AudioSource Get(string name) => _i.Get_(name);
    /// <summary>See "Sound naming convention" in this file's notes.</summary>
    public static void Play(string name) => _i.Get_(name).Play();

    Dictionary<string, List<AudioSource>> _sounds;

    void OnEnable()
    {
        _i = this;
        _sounds = new Dictionary<string, List<AudioSource>>();
    }
    
    void RegisterSound_(AudioSource sound)
    {
        var name = GetName(sound);

        if (!_sounds.ContainsKey(name))
            _sounds[name] = new List<AudioSource>();
        _sounds[name].Add(sound);
    }

    void DeregisterSound_(AudioSource sound)
    {
        var name = GetName(sound);

        Assert.IsTrue(
            _sounds.ContainsKey(name)
            && _sounds[name].Contains(sound)
        );
        _sounds[name].Remove(sound);
        if (_sounds[name].Count == 0) _sounds.Remove(name);
    }

    AudioSource Get_(string name)
    {
        Assert.IsTrue(_sounds.ContainsKey(name));
        return _sounds[name][Random.Range(0, _sounds[name].Count)];
    }

    string GetName(AudioSource sound)
    {
        var baseName = sound.gameObject.StandardName();
        var minusNumbers = baseName.TrimEnd(_digits);

        return minusNumbers;
    }
}