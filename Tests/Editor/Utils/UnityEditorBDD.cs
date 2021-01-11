using UnityEditor;
using UnityEngine;

public class UnityEditorBDD : BDD
{
    const string TEST_ASSET_DIRECTORY_PATH = "Packages/me.bilalakil.my-unity-library/Tests/Editor/Assets/";

    protected GameObject GivenTestGameObject(string path)
    {
        var fullPath = TEST_ASSET_DIRECTORY_PATH + path;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        var obj = GameObject.Instantiate(asset);
        return obj;
    }
}
