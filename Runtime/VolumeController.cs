using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyLibrary
{
    /**
     * ## Notes
     * Deals in percentages (e.g. 0.5 user volume and 0.5 dynamic volume results in 0.25 final).
     * Only final volumes between 0 and 1 have been properly considered.
     *
     * ### REQUIREMENTS
     * - MyLibraryConfig.volumes set up
     * - To use user volume, KVS setup (see KVS.cs)
     *
     * ### Persistence
     * Uses the default KVS to store user volume settings. 
     */

    [AddComponentMenu("")]
    [DefaultExecutionOrder(-5000)] // After KVS, before other plugin/user-land code
    public class VolumeController : MonoBehaviour
    {
        public static string KEY_VOLUME_TEMPLATE = "myLibrary_volume_{0}";

        // https://www.desmos.com/calculator/2xkckawwxt (~-80 at 0, 1 at 1)
        public static float GetDecibels(float volPct) =>
            Mathf.Log(volPct * (1f - 0.0182f) + 0.0182f) * 20f;

        static VolumeController _iBacking;
        static VolumeController I
        {
            get
            {
                if (_iBacking == null)
                {
                    Debug.LogError("Cannot change volume without setting up MyLibraryConfig.volumes");
                    return null;
                }

                return _iBacking;
            }
        }

        static Action _quittingHandler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (_quittingHandler == null)
            {
                _quittingHandler = Deinit;
                Application.quitting += _quittingHandler;
            }

            if (_iBacking != null)
                return;

            if ((MyLibraryConfig.I?.volumes?.Length ?? 0) == 0)
                return;
            
            var obj = new GameObject("VolumeController");
            obj.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(obj);

            _iBacking = obj.AddComponent<VolumeController>();
            _iBacking.Use(MyLibraryConfig.I.volumes);
        }

        public static void Deinit()
        {
            if (_quittingHandler != null)
            {
                Application.quitting -= _quittingHandler;
                _quittingHandler = null;
            }

            if (_iBacking != null)
            {
                Destroy(_iBacking.gameObject);
                _iBacking = null;
            }
        }

        public static void SetUserVolume(string @ref, float val) =>
            I?.I_SetUserVolume(@ref, val);
        
        public static float GetUserVolume(string @ref)
        {
            if (_iBacking == null)
                throw new InvalidOperationException("Cannot get volume without setting up MyLibraryConfig.volumes");

            return _iBacking.I_GetUserVolume(@ref);
        }

        public static void SetDynamicVolume(string @ref, float val) =>
            I?.I_SetDynamicVolume(@ref, val);
        
        public static float GetDynamicVolume(string @ref)
        {
            if (_iBacking == null)
                throw new InvalidOperationException("Cannot get volume without setting up MyLibraryConfig.volumes");

            return _iBacking.I_GetDynamicVolume(@ref);
        }
        
        VolumeConfig[] _configArray = new VolumeConfig[0];
        Dictionary<string, VolumeConfig> _configs;

        bool _isDirty;
        
        void OnEnable()
        {
            if (_iBacking != null && _iBacking != this)
            {
                Debug.LogWarning("VolumeController duplicated, self-destructing");
                Destroy(gameObject);
                return;
            }

            _iBacking = this;
            PopulateNonSerializables();
        }

        void Update()
        {
            if (_isDirty)
                SetMixerVolumes();
        }

        void OnDisable()
        {
            if (_iBacking != this)
                return;
            
            _iBacking = null;
        }

        void Use(MyLibraryConfig.Volume[] configs)
        {
            _configArray = configs.Select(
                c =>
                {
                    var userVolume = KVS.Configured
                        ? KVS.GetFloat(GetKeyForRef(c.@ref), 1f)
                        : 1f;

                    return new VolumeConfig {
                        raw=c,
                        userVolume=userVolume,
                        dynamicVolume=1f
                    };
                }
            ).ToArray();

            PopulateNonSerializables();

            _isDirty = true;
        }

        string GetKeyForRef(string @ref) =>
            string.Format(KEY_VOLUME_TEMPLATE, @ref);
        
        bool CheckRefExists(string @ref, bool shouldThrow)
        {
            if (!_configs.ContainsKey(@ref))
            {
                var msg = $"Tried to deal with volume for non-existing mixer: {@ref}";
                if (shouldThrow)
                    throw new InvalidOperationException(msg);
                else
                    Debug.LogError(msg);

                return false;
            }
            return true;
        }

        void I_SetUserVolume(string @ref, float val)
        {
            if (!KVS.Configured)
            {
                Debug.LogError("User volume cannot be used without setting up MyLibraryConfig.kvs");
                return;
            }

            if (!CheckRefExists(@ref, false))
                return;

            KVS.SetFloat(GetKeyForRef(@ref), val);
            _configs[@ref].userVolume = val;

            _isDirty = true;
        }

        float I_GetUserVolume(string @ref)
        {
            if (!KVS.Configured)
                throw new InvalidOperationException("User volume cannot be used without setting up MyLibraryConfig.kvs");

            CheckRefExists(@ref, true);
            return _configs[@ref].userVolume;
        }

        void I_SetDynamicVolume(string @ref, float val)
        {
            if (!CheckRefExists(@ref, false))
                return;

            _configs[@ref].dynamicVolume = val;

            _isDirty = true;
        }

        float I_GetDynamicVolume(string @ref)
        {
            CheckRefExists(@ref, true);
            return _configs[@ref].dynamicVolume;
        }

        void PopulateNonSerializables()
        {
            _configs = new Dictionary<string, VolumeConfig>();
            foreach (var config in _configArray)
                _configs[config.raw.@ref] = config;
        }

        void SetMixerVolumes()
        {
            _isDirty = false;

            foreach (var config in _configArray)
                config.raw.mixer.SetFloat(
                    config.raw.mixerVolumeKey,
                    GetActualVolume(config.userVolume, config.dynamicVolume)
                );
        }

        float GetActualVolume(float userVolume, float dynamicVolume) =>
            GetDecibels(userVolume * dynamicVolume);

        [Serializable]
        class VolumeConfig
        {
            public MyLibraryConfig.Volume raw;
            public float userVolume;
            public float dynamicVolume;
        }
    }
}