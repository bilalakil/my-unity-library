using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SearchForPrefabInAllScenes : EditorWindow
{
    Object _obj;
    string _prefabToFind;
    SceneAsset[] _results;

    [MenuItem("Search/Search for prefab in all scenes")]
    public static void ShowThis() => EditorWindow.GetWindow(
        typeof(SearchForPrefabInAllScenes),
        false,
        "Search for Prefab in All Scenes",
        true
    );
    
    void OnGUI()
    {
        _obj = EditorGUILayout.ObjectField("Prefab", _obj, typeof(GameObject), false);
        _prefabToFind = _obj == null ? null : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_obj);

        if (GUILayout.Button("Search") && _prefabToFind != null)
        {
            var curScene = SceneManager.GetActiveScene().path;
            var scenesWithMatch = new List<SceneAsset>();

            foreach (var sceneGUID in AssetDatabase.FindAssets("t:Scene"))
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                EditorSceneManager.OpenScene(scenePath);
                var scene = SceneManager.GetActiveScene();
                
                var sceneHasMatch = false;

                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    if (sceneHasMatch) break;

                    foreach (var tfm in rootObj.GetComponentsInChildren<Transform>())
                    {
                        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tfm.gameObject) == _prefabToFind)
                        {
                            sceneHasMatch = true;
                            scenesWithMatch.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));

                            break;
                        }
                    }
                }
            }

            _results = scenesWithMatch.ToArray();

            EditorSceneManager.OpenScene(curScene);
        }

        EditorGUILayout.LabelField("Results");
        ++EditorGUI.indentLevel;

        if (_results != null)
        {
            if (_results.Length != 0)
                foreach (var scene in _results)
                    EditorGUILayout.ObjectField(scene, typeof(Object), false);
            else EditorGUILayout.LabelField("Nothing found!");
        }

        --EditorGUI.indentLevel;
    }
}
