using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyLibrary
{
    public class SearchForPrefabInAllScenes : EditorWindow
    {
        [MenuItem("Search/Search for Prefab in All Scenes")]
        public static void ShowThis() => EditorWindow.GetWindow(
            typeof(SearchForPrefabInAllScenes),
            false,
            "Search for Prefab in All Scenes",
            true
        );
        
        Vector2 _scrollPos;
        Object _obj;
        string _prefabToFind;
        SceneAsset[] _results;

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

                        foreach (var tfm in rootObj.GetComponentsInChildren<Transform>(true))
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

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            ++EditorGUI.indentLevel;

            if (_results != null)
            {
                if (_results.Length != 0)
                    foreach (var scene in _results)
                        EditorGUILayout.ObjectField(scene, typeof(Object), false);
                else EditorGUILayout.LabelField("Nothing found!");
            }

            --EditorGUI.indentLevel;
            EditorGUILayout.EndScrollView();
        }
    }
}