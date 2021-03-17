using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MyLibrary
{
    public class SearchForComponentInAllPrefabs : EditorWindow
    {
        [MenuItem("Search/Search for Component in All Prefabs")]
        public static void ShowThis() => EditorWindow.GetWindow(
            typeof(SearchForComponentInAllPrefabs),
            false,
            "Search for Component in All Prefabs",
            true
        );
        
        Vector2 _scrollPos;
        MonoScript _script;
        string _prefabToFind;
        Object[] _results;

        void OnGUI()
        {
            _script = (MonoScript)EditorGUILayout.ObjectField("Component", _script, typeof(MonoScript), false);

            if (GUILayout.Button("Search") && _script != null)
            {
                List<Object> prefabsWithMatch = new List<Object>();

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (obj?.GetComponentInChildren(_script.GetClass(), true) == null)
                        continue;
                    
                    prefabsWithMatch.Add(obj);
                }

                _results = prefabsWithMatch.ToArray();
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