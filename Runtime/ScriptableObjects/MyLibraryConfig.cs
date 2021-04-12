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
        public static MyLibraryConfig Load()
        {
            if (loadOverride != null)
                return loadOverride;
            return Resources.Load<MyLibraryConfig>("MyLibraryConfig");
        }

        public VolumeConfig[] volumeConfigs;

        [Serializable]
        public struct VolumeConfig
        {
            public string @ref;
            public AudioMixer mixer;
            public string mixerVolumeKey;
        }

        public TestConfig testConfig;

        [Serializable]
        public struct TestConfig
        {
            public bool useFlagPlatformOverride;
            public RuntimePlatform flagPlatformOverride;
            public bool useFlagFPSOverride;
            public float flagFPSOverride;
        }
    }
}