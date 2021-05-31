using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

namespace MyLibrary
{
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

    [AddComponentMenu("")]
    [DefaultExecutionOrder(-1000)]
    public class SoundController : MonoBehaviour
    {
        static Regex _nameEndPattern = new Regex("[-_]*[0-9]+$");

        static SoundController _i
        {
            get
            {
                if (!_haveInstantiated)
                {
                    var obj = new GameObject("SoundController");
                    DontDestroyOnLoad(obj);
                    _iBacking = obj.AddComponent<SoundController>();

                    var defaultSounds = Resources.Load<GameObject>("DefaultSounds");
                    if (defaultSounds != null)
                        DontDestroyOnLoad(Instantiate(defaultSounds));
                }

                return _iBacking;
            }
        }
        static SoundController _iBacking;
        static bool _haveInstantiated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Reset()
        {
            _iBacking = null;
            _haveInstantiated = false;
        }

        public static void RegisterSound(AudioSource sound) => _i.RegisterSound_(sound);
        public static void DeregisterSound(AudioSource sound) => _i?.DeregisterSound_(sound);
        public static AudioSource Get(string name) => _i.Get_(name);
        public static void Play(string name) => _i?.Get_(name).Play();
        public static void PlayAtLocation(string name, Vector3 location)
        {
            if (_i == null)
                return;

            var sound = _i.Get_(name);
            var obj = Instantiate(sound.gameObject, location, Quaternion.identity);
            var newSound = obj.GetComponent<AudioSource>();

            newSound.Play();

            Destroy(obj, newSound.clip.length);
        }

        Dictionary<string, List<AudioSource>> _sounds;

        void OnEnable()
        {
            if (
                _iBacking != null &&
                _iBacking != this
            )
            {
                Destroy(gameObject);
                return;
            }
            _iBacking = this;
            _haveInstantiated = true;
            _sounds = new Dictionary<string, List<AudioSource>>();
        }

        void OnDisable()
        {
            if (_iBacking == this)
                _iBacking = null;
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

            if (_sounds[name].Count == 0)
                _sounds.Remove(name);
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
}