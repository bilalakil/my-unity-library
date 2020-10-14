using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

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

[AddComponentMenu("")] // To prevent it from showing up in the Add Component list
[DefaultExecutionOrder(-1000)]
public class SoundController : MonoBehaviour
{
    const string PP_VOLUME_ON = "_ml_soundOn";
    const float VOLUME_ON = 0f;
    const float VOLUME_OFF = -100f;

    static Regex _nameEndPattern = new Regex("[-_]*[0-9]+$");

    static SoundController _i
    {
        get
        {
            if (!_haveInstantiated)
            {
                var obj = new GameObject("SoundController");
                DontDestroyOnLoad(obj);
                obj.AddComponent<SoundController>();
            }
            return __i;
        }
    }
    static SoundController __i;
    static bool _haveInstantiated;

    public static bool VolumeOn
    {
        get => _i._volumeOn;
        set
        {
            Assert.IsTrue(_i._mixer != null);
            
            _i._volumeOn = value;
            PlayerPrefs.SetInt(PP_VOLUME_ON, value ? 1 : 0);

            _i._mixer.SetFloat(_i._volumeKey, value ? VOLUME_ON : VOLUME_OFF);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var defaultSounds = Resources.Load<GameObject>("DefaultSounds");
        if (defaultSounds == null) return;

        DontDestroyOnLoad(Instantiate(defaultSounds));
    }

    public static void RegisterSound(AudioSource sound)
        => _i.RegisterSound_(sound);
    public static void DeregisterSound(AudioSource sound)
        => _i?.DeregisterSound_(sound);
    /// <summary>See "Sound naming convention" in this file's notes.</summary>
    public static AudioSource Get(string name) => _i.Get_(name);
    /// <summary>See "Sound naming convention" in this file's notes.</summary>
    public static void Play(string name) => _i.Get_(name).Play();

    Dictionary<string, List<AudioSource>> _sounds;
    AudioMixer _mixer;
    string _volumeKey;
    bool _volumeOn;

    void OnEnable()
    {
        if (__i != null)
        {
            Destroy(gameObject);
            return;
        }

        __i = this;
        _haveInstantiated = true;

        _sounds = new Dictionary<string, List<AudioSource>>();
    }

    void Start()
    {
        var config = Resources.Load<MyLibraryConfig>("MyLibraryConfig");
        if (config == null) return;

        _mixer = config.soundMixer;
        _volumeKey = config.soundMasterVolumeKey;

        if (_i._mixer != null)
            VolumeOn = PlayerPrefs.GetInt(PP_VOLUME_ON, 1) == 1;
    }

    void OnDisable()
    {
        if (__i == this) __i = null;
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
        if (!_sounds.ContainsKey(name))
            throw new InvalidOperationException("Attempted to play non-existant sound: " + name);

        return _sounds[name][UnityEngine.Random.Range(0, _sounds[name].Count)];
    }

    string GetName(AudioSource sound)
    {
        var baseName = sound.gameObject.StandardName();
        var minusNumbers = _nameEndPattern.Replace(baseName, "");

        return minusNumbers;
    }
}