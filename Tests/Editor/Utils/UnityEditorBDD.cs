using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyLibrary
{
    public class UnityEditorBDD : BDD
    {
        public const string TEST_ASSET_DIR = "Packages/me.bilalakil.my-unity-library/Tests/Editor/Assets";

        protected GameObject GivenTestGameObject(string path)
        {
            var fullPath = TEST_ASSET_DIR + "/" + path;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            var obj = GameObject.Instantiate(asset);
            return obj;
        }

        protected void DestroyKVSOnDisk(string[] configPaths)
        {
            foreach (var path in configPaths)
            {
                var config = AssetDatabase.LoadAssetAtPath<MyLibraryConfig>(path + ".asset");
                File.Delete(Application.persistentDataPath + "/" + config.kvs.defaultFilename);
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        protected class GivenMyLibraryConfigAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            string _configPath;

            MyLibraryConfig _prevConfig;

            public GivenMyLibraryConfigAttribute(string configPath) =>
                _configPath = configPath + ".asset";
            
            public IEnumerator BeforeTest(ITest test)
            {
                _prevConfig = MyLibraryConfig.loadOverride;
                MyLibraryConfig.loadOverride = AssetDatabase.LoadAssetAtPath<MyLibraryConfig>(_configPath);
                yield break;
            }

            public IEnumerator AfterTest(ITest test)
            {
                MyLibraryConfig.loadOverride = _prevConfig;
                yield break;
            }
        }
    }
}
