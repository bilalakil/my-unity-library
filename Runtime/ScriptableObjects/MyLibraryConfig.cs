using System;
using UnityEngine;
using UnityEngine.Audio;

namespace MyLibrary
{
    /**
    * ## Notes
    * Put one of these in a Resources folder, named "MyLibraryConfig" to globally configure this library.
    * Alternatively, one can be set with code via MyLibraryConfig.loadOverride.
    *
    * The library does not support these values being changed during play (it probably caches the values where needed).
    *
    * All settings under `testConfig` are only relevant in-editor, for testing.
    */

    [CreateAssetMenu(menuName = "Config/MyLibraryConfig", fileName = "MyLibraryConfig")]
    public class MyLibraryConfig : ScriptableObject
    {
        public static MyLibraryConfig loadOverride;
        static MyLibraryConfig _iBacking;
        public static MyLibraryConfig I
        {
            get
            {
                if (loadOverride != null)
                    return loadOverride;
                if (_iBacking == null)
                    _iBacking = Resources.Load<MyLibraryConfig>("MyLibraryConfig");
                return _iBacking;
            }
        }

        static Action _quittingHandler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            if (_quittingHandler == null)
            {
                _quittingHandler = Deinit;
                Application.quitting += _quittingHandler;
            }
        }

        public static void Deinit()
        {
            _iBacking = null;

            if (_quittingHandler != null)
            {
                Application.quitting -= _quittingHandler;
                _quittingHandler = null;
            }
        }

        public KVS kvs;
        public Volume[] volumes;

        [Serializable]
        public class KVS
        {
            public string defaultFilename = "kvs.dat";
        }

        [Serializable]
        public struct Volume
        {
            public string @ref;
            public AudioMixer mixer;
            public string mixerVolumeKey;
        }

        public Testing testing;

        [Serializable]
        public struct Testing
        {
            public FlagOverrides flagOverrides;

            [Serializable]
            public struct FlagOverrides
            {
                public bool overridePlatform;
                public RuntimePlatform platform;
                public bool overrideFPS;
                public float fps;
            }
        }
    }
}