using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SearchForComponentInAllPrefabs : EditorWindow
{
    MonoScript _script;
    string _prefabToFind;
    Object[] _results;

    [MenuItem("Search/Search for component in all prefabs")]
    public static void ShowThis() => EditorWindow.GetWindow(
        typeof(SearchForComponentInAllPrefabs),
        false,
        "Search for Component in All Prefabs",
        true
    );
    
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

                if (obj?.GetComponentInChildren(_script.GetClass()) == null)
                    continue;
                
                prefabsWithMatch.Add(obj);
            }

            _results = prefabsWithMatch.ToArray();
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

