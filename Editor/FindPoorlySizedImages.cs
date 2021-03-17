using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MyLibrary
{
    public class FindPoorlySizedImages : EditorWindow
    {
        // https://docs.unity3d.com/ScriptReference/TextureImporter.GetPlatformTextureSettings.html
        static string[] _textureImporterPlatformList = new string[]
        {
            "Default", // Adding my own one here
            "Standalone", "Web", "iPhone", "Android", "WebGL",
            "Windows Store Apps", "PS4", "XboxOne", "Nintendo Switch", "tvOS",
        };

        [MenuItem("Search/Find poorly sized images")]
        public static void ShowThis() => EditorWindow.GetWindow(
            typeof(FindPoorlySizedImages),
            false,
            "Find Poorly Sized Images",
            true
        );

        public static bool Check(string path)
        {
            var texture = new Texture2D(1, 1);
            var bytes = File.ReadAllBytes(path);
            texture.LoadImage(bytes);

            var largest = Mathf.Max(texture.width, texture.height);

            var textureImporter = TextureImporter.GetAtPath(path) as TextureImporter;

            // Smile and nod for unsupported texture types (i.e. *.ttf) to keep `ScanDirectories` simple
            if (textureImporter == null)
                return true;

            foreach (var platform in _textureImporterPlatformList)
            {
                TextureImporterPlatformSettings platformSettings = null;
                if (platform != "Default")
                {
                    platformSettings = textureImporter.GetPlatformTextureSettings(platform);
                    if (!platformSettings.overridden)
                        platformSettings = null;
                }
                // Fallback for "Default" or non-overridden platforms
                if (platformSettings == null)
                    platformSettings = textureImporter.GetDefaultPlatformTextureSettings();

                var width = texture.width;
                var height = texture.height;

                if (largest > platformSettings.maxTextureSize)
                {
                    /* Inspector preview shows Unity rounding does NOT conform to `Mathf.Round`'s odd/even up/down behaviour:
                    * - 1024 x 5  reduced to max 256 = 256 x 1.25 = 256 x 1
                    * - 1024 x 6  reduced to max 256 = 256 x 1.5  = 256 x 2
                    * - 1024 x 7  reduced to max 256 = 256 x 1.75 = 256 x 2
                    * - 1024 x 10 reduced to max 256 = 256 x 2.5  = 256 x 3
                    */
                    var mult = (double)platformSettings.maxTextureSize / largest;
                    width = (int)Math.Round(width * mult, MidpointRounding.AwayFromZero);
                    height = (int)Math.Round(height * mult, MidpointRounding.AwayFromZero);
                }

                if (
                    width % 4 != 0 ||
                    height % 4 != 0
                ) return false;
            }

            return true;
        }

        public static Texture2D[] ScanDirectories(string[] dirs) =>
            AssetDatabase.FindAssets("t:Texture2D", dirs)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(_ => !Check(_))
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .ToArray();
        
        Vector2 _scrollPos;
        Texture2D[] _results;
        
        void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Checks the Assets folder for textures whose resolution, "
                    + "once scaled per ANY platform's max texture size setting, "
                    + "ends up not being a multiple of 4 in either dimension.",
                EditorStyles.wordWrappedLabel
            );

            if (GUILayout.Button("Scan"))
                _results = ScanDirectories(new string[] { "Assets" });

            EditorGUILayout.LabelField("Results");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            ++EditorGUI.indentLevel;

            if (_results != null)
            {
                if (_results.Length != 0)
                    foreach (var scene in _results)
                        EditorGUILayout.ObjectField(scene, typeof(UnityEngine.Object), false);
                else EditorGUILayout.LabelField("Nothing found!");
            }

            --EditorGUI.indentLevel;
            EditorGUILayout.EndScrollView();
        }
    }
}