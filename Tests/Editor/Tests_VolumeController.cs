using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyLibrary
{
    /**
     * NOTE: These tests assume the DefaultMusic resource DOES NOT exist.
     * NOTE: These tests assume a MyLibraryConfig resource DOES NOT exist.
     * WARNING: Running these tests will nuke the default KVS data!
     */
    public class Tests_VolumeController : UnityEditorBDD
    {
        const string ASSET_DIR = UnityEditorBDD.TEST_ASSET_DIR + "/VolumeController";

        static string[] _configsToCleanUp = new string[]
        {
            ASSET_DIR + "/TestConfigStandard",
        };

        [SetUp]
        public void ResetAndInitVolumeController()
        {
            ResetVolumeController();

            KVS.Init();
            VolumeController.Init();
        }

        [TearDown]
        public void ResetVolumeController()
        {
            VolumeController.Deinit();
            KVS.Deinit();

            DestroyKVSOnDisk(_configsToCleanUp);
        }

        [UnityTest]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public IEnumerator VolumeChangeWorksWithMultipleMixers()
        {
            // GIVEN a blank slate
            ThenUserVolumeIs("music", 1f);
            ThenDynamicVolumeIs("music", 1f);
            ThenMixerVolumeIs("music", 0);
            ThenUserVolumeIs("sfx", 1f);
            ThenDynamicVolumeIs("sfx", 1f);
            ThenMixerVolumeIs("sfx", 0);

            WhenUserVolumeSetTo("music", 0.5f);
            WhenDynamicVolumeSetTo("sfx", 0.5f);
            ThenUserVolumeIs("music", 0.5f);
            ThenDynamicVolumeIs("sfx", 0.5f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f));
            ThenMixerVolumeIs("sfx", VolumeController.GetDecibels(0.5f));

            WhenDynamicVolumeSetTo("music", 0.5f);
            WhenUserVolumeSetTo("sfx", 0.5f);
            ThenDynamicVolumeIs("music", 0.5f);
            ThenUserVolumeIs("sfx", 0.5f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f * 0.5f));
            ThenMixerVolumeIs("sfx", VolumeController.GetDecibels(0.5f * 0.5f));

            WhenUserVolumeSetTo("music", 1f);
            WhenDynamicVolumeSetTo("sfx", 1f);
            ThenUserVolumeIs("music", 1f);
            ThenDynamicVolumeIs("sfx", 1f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f));
            ThenMixerVolumeIs("sfx", VolumeController.GetDecibels(0.5f));

            WhenDynamicVolumeSetTo("music", 1f);
            WhenUserVolumeSetTo("sfx", 1f);
            ThenDynamicVolumeIs("music", 1f);
            ThenUserVolumeIs("sfx", 1f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", 0);
            ThenMixerVolumeIs("sfx", 0);

            WhenDynamicVolumeSetTo("music", 1.2f);
            WhenUserVolumeSetTo("sfx", 1.2f);
            ThenDynamicVolumeIs("music", 1.2f);
            ThenUserVolumeIs("sfx", 1.2f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(1.2f));
            ThenMixerVolumeIs("sfx", VolumeController.GetDecibels(1.2f));

            WhenUserVolumeSetTo("music", 0.2f);
            WhenDynamicVolumeSetTo("sfx", 0.2f);
            ThenUserVolumeIs("music", 0.2f);
            ThenDynamicVolumeIs("sfx", 0.2f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.2f * 1.2f));
            ThenMixerVolumeIs("sfx", VolumeController.GetDecibels(0.2f * 1.2f));
        }

        [UnityTest]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public IEnumerator VolumeChangeAppliesToMixerNextFrame()
        {
            // GIVEN a blank slate
            ThenMixerVolumeIs("music", 0);
            ThenUserVolumeIs("music", 1f);
            ThenDynamicVolumeIs("music", 1f);

            WhenUserVolumeSetTo("music", 0.5f);
            WhenDynamicVolumeSetTo("music", 0.5f);
            ThenUserVolumeIs("music", 0.5f);
            ThenDynamicVolumeIs("music", 0.5f);
            ThenMixerVolumeIs("music", 0);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f * 0.5f));
        }

        [UnityTest]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public IEnumerator UserVolumeLoadsFromKVS()
        {
            {
                VolumeController.Deinit();
                GivenKVSUserVolume("music", 0.5f);
                VolumeController.Init();
            }

            ThenUserVolumeIs("music", 0.5f);
            ThenDynamicVolumeIs("music", 1f);

            yield return WhenPostUpdate();
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f));
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(1.1f)]
        public void UserVolumeSavesToKVS(float userVolume)
        {
            WhenUserVolumeSetTo("music", userVolume);
            ThenUserVolumeIs("music", userVolume);
        }

        [Test]
        public void SetMethodsLogErrorIfNotConfigured()
        {
            // GIVEN a blank slate WITH NO CONFIG SET
            var expected = "Cannot change volume without setting up MyLibraryConfig.volumes";
            ThenSettingUserVolumeLogsError("music", expected);
            ThenSettingDynamicVolumeLogsError("music", expected);
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        [TestCase("foo")]
        [TestCase("bar")]
        public void SetMethodsLogErrorForWrongRef(string @ref)
        {
            // GIVEN a blank slate
            var expected = $"Tried to deal with volume for non-existing mixer: {@ref}";
            ThenSettingUserVolumeLogsError(@ref, expected);
            ThenSettingDynamicVolumeLogsError(@ref, expected);
        }

        [Test]
        public void GetMethodsThrowErrorIfNotConfigured()
        {
            // GIVEN a blank slate WITH NO CONFIG SET
            var expected = "Cannot get volume without setting up MyLibraryConfig.volumes";
            ThenGettingUserVolumeThrowsError("music", expected);
            ThenGettingDynamicVolumeThrowsError("music", expected);
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        [TestCase("foo")]
        [TestCase("bar")]
        public void GetMethodsThrowErrorForWrongRef(string @ref)
        {
            // GIVEN a blank slate
            var expected = $"Tried to deal with volume for non-existing mixer: {@ref}";
            ThenGettingUserVolumeThrowsError(@ref, expected);
            ThenGettingDynamicVolumeThrowsError(@ref, expected);
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigNoKVS")]
        public void SetUserVolumeLogsErrorIfKVSNotConfigured() =>
            ThenSettingUserVolumeLogsError("music", "User volume cannot be used without setting up MyLibraryConfig.kvs");

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigNoKVS")]
        public void GetUserVolumeThrowsErrorIfKVSNotConfigured() =>
            ThenGettingUserVolumeThrowsError("music", "User volume cannot be used without setting up MyLibraryConfig.kvs");

        [UnityTest]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigNoKVS")]
        public IEnumerator DynamicVolumeStillWorksIfKVSNotConfigured()
        {
            WhenDynamicVolumeSetTo("music", 0.5f);
            yield return WhenPostUpdate();
            ThenDynamicVolumeIs("music", 0.5f);
            ThenMixerVolumeIs("music", VolumeController.GetDecibels(0.5f));
        }

        [Test]
        public void DecibelsCalculationWorksFineWith0Volume() =>
            VolumeController.GetDecibels(0f);

        void GivenKVSUserVolume(string @ref, float val) =>
            KVS.SetFloat(
                string.Format(VolumeController.KEY_VOLUME_TEMPLATE, @ref),
                val
            );

        void WhenUserVolumeSetTo(string @ref, float val) =>
            VolumeController.SetUserVolume(@ref, val);

        void WhenDynamicVolumeSetTo(string @ref, float val) =>
            VolumeController.SetDynamicVolume(@ref, val);

        void ThenUserVolumeIs(string @ref, float expected)
        {
            Assert.IsTrue(Mathf.Approximately(
                expected,
                VolumeController.GetUserVolume(@ref)
            ));
            Assert.IsTrue(Mathf.Approximately(
                expected,
                KVS.GetFloat(string.Format(VolumeController.KEY_VOLUME_TEMPLATE, @ref), 1f)
            ));
        }

        void ThenDynamicVolumeIs(string @ref, float expected) =>
            Assert.IsTrue(Mathf.Approximately(
                expected,
                VolumeController.GetDynamicVolume(@ref)
            ));

        void ThenMixerVolumeIs(string @ref, float expected)
        {
            var volConfig = MyLibraryConfig.I.volumes
                .Where(_ => _.@ref == @ref)
                .First();

            float actual;
            volConfig.mixer.GetFloat(volConfig.mixerVolumeKey, out actual);

            Assert.IsTrue(Mathf.Approximately(expected, actual));
        }

        void ThenSettingUserVolumeLogsError(string @ref, string expected)
        {
            LogAssert.Expect(LogType.Error, expected);
            VolumeController.SetUserVolume(@ref, 0.5f);
        }

        void ThenSettingDynamicVolumeLogsError(string @ref, string expected)
        {
            LogAssert.Expect(LogType.Error, expected);
            VolumeController.SetDynamicVolume(@ref, 0.5f);
        }

        void ThenGettingUserVolumeThrowsError(string @ref, string expected)
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => VolumeController.GetUserVolume(@ref)
            );
            Assert.AreEqual(expected, ex.Message);
        }

        void ThenGettingDynamicVolumeThrowsError(string @ref, string expected)
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => VolumeController.GetDynamicVolume(@ref)
            );
            Assert.AreEqual(expected, ex.Message);
        }
    }
}