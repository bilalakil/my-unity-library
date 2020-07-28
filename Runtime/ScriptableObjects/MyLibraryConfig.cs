using System;
using UnityEngine;
using UnityEngine.Audio;

/**
 * ## Notes
 *
 * Put one of these in a Resources folder, named "MyLibraryConfig",
 * to globally configure this library.
 *
 * All settings under `testConfig` are only relevant in-editor, for testing.
 */

[CreateAssetMenu(menuName = "MyLibrary/MyLibraryConfig", fileName = "MyLibraryConfig")]
public class MyLibraryConfig : ScriptableObject
{
    public AudioMixer musicMixer;
    public string musicMasterVolumeKey = "MasterVolume";
    public AudioMixer soundMixer;
    public string soundMasterVolumeKey = "MasterVolume";
    public string simpleRelayHTTPSURL; // https://abc.def.ghi.com:1234/Prod"
    public string simpleRelayWSSURL; // "wss://abc.def.ghi.com:1234/Prod"

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
