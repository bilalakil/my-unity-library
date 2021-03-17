using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyLibrary
{
    public class Tests_FindPoorlySizedImages
    {
        const string ASSET_DIR = UnityEditorBDD.TEST_ASSET_DIR + "/FindPoorlySizedImages";

        [Test]
        public void PositiveWithNoScaling()
        {
            var texturePath = ASSET_DIR + "/no-scaling-4x4.png";
            Assert.IsTrue(FindPoorlySizedImages.Check(texturePath));
        }

        [Test]
        public void NegativeWithNoScaling()
        {
            var texturePath = ASSET_DIR + "/no-scaling-3x3.png";
            Assert.IsFalse(FindPoorlySizedImages.Check(texturePath));
        }

        [Test]
        public void PositiveWithDefaultScaling()
        {
            var texturePath = ASSET_DIR + "/default-scaling-256-257x257.png";
            Assert.IsTrue(FindPoorlySizedImages.Check(texturePath));
        }

        [Test]
        public void NegativeWithDefaultScaling()
        {
            var texturePath = ASSET_DIR + "/default-scaling-256-512x36.png";
            Assert.IsFalse(FindPoorlySizedImages.Check(texturePath));
        }

        // Only testing Android negative scaling below because I can't be bothered creating so many test files,
        // but the idea is that the same logic applies to all other potential platforms.

        [Test]
        public void NegativeWithAndroidScaling()
        {
            var texturePath = ASSET_DIR + "/android-scaling-256-512x36.png";
            Assert.IsFalse(FindPoorlySizedImages.Check(texturePath));
        }

        [Test]
        public void ScanDirectoriesWorksOnTestAssets()
        {
            var expected = new Texture2D[]
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(ASSET_DIR + "/android-scaling-256-512x36.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(ASSET_DIR + "/default-scaling-256-512x36.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(ASSET_DIR + "/no-scaling-3x3.png"),
            };

            Assert.AreEqual(
                expected,
                FindPoorlySizedImages.ScanDirectories(new string[] { ASSET_DIR })
            );
        }
    }
}