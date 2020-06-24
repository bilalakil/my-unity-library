using System;
using UnityEngine;

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
