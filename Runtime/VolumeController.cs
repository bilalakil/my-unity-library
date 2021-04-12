using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyLibrary
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-5000)] // After KVS, before other plugin/user-land code
    public class VolumeController : MonoBehaviour
    {
        public static string KEY_VOLUME_TEMPLATE = "myLibrary_volume_{0}";
        public const float VOLUME_MAX = 0f;
        public const float VOLUME_MIN = -50f;
        public const float VOLUME_DIFF = VOLUME_MAX - VOLUME_MIN;

        static VolumeController _iBacking;
        static VolumeController I
        {
            get
            {
                if (_iBacking == null)
                {
                    Debug.LogError("Cannot change volume without setting MyLibraryConfig.volumeConfigs");
                    return null;
                }

                return _iBacking;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            if (_iBacking != null)
                return;

            var config = MyLibraryConfig.Load();
            if (config?.volumeConfigs == null || config.volumeConfigs.Length == 0)
                return;
            
            var obj = new GameObject("VolumeController");
            obj.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(obj);

            _iBacking = obj.AddComponent<VolumeController>();
            _iBacking.Use(config.volumeConfigs);

            Action quittingHandler = null;
            quittingHandler = () => {
                Application.quitting -= quittingHandler;
                Deinit();
            };

            Application.quitting += quittingHandler;
        }

        public static void Deinit()
        {
            if (_iBacking == null)
                return;

            Destroy(_iBacking.gameObject);
            _iBacking = null;
        }

        public static void SetUserVolume(string @ref, float val) =>
            I?.I_SetUserVolume(@ref, val);
        
        public static float GetUserVolume(string @ref)
        {
            if (_iBacking == null)
                throw new InvalidOperationException("Cannot get volume without setting MyLibraryConfig.volumeConfigs");

            return _iBacking.I_GetUserVolume(@ref);
        }

        public static void SetDynamicVolume(string @ref, float val) =>
            I?.I_SetDynamicVolume(@ref, val);
        
        public static float GetDynamicVolume(string @ref)
        {
            if (_iBacking == null)
                throw new InvalidOperationException("Cannot get volume without setting MyLibraryConfig.volumeConfigs");

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

        void Use(MyLibraryConfig.VolumeConfig[] configs)
        {
            _configArray = configs.Select(
                c =>
                {
                    var userVolume = KVS.GetFloat(GetKeyForRef(c.@ref), 1f);

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
            if (!CheckRefExists(@ref, false))
                return;

            KVS.SetFloat(GetKeyForRef(@ref), val);
            _configs[@ref].userVolume = val;

            _isDirty = true;
        }

        float I_GetUserVolume(string @ref)
        {
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
            VOLUME_MIN + VOLUME_DIFF * (userVolume * dynamicVolume);

        [Serializable]
        class VolumeConfig
        {
            public MyLibraryConfig.VolumeConfig raw;
            public float userVolume;
            public float dynamicVolume;
        }
    }
}