using UnityEditor;
using UnityEngine;

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
}
