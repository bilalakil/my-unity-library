using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyLibrary
{
    /**
     * ## Notes
     * Replacement for Unity's PlayerPrefs (has the same set of static methods),
     * but saves to a file in Application.persistentDataPath
     * that is suitable in file-based cloud-save solutions (e.g. Steam).
     *
     * WARNING: Trivially edit-able by players. No encryption or in-memory protection
     * (as is also the case with Unity's PlayerPrefs).
     *
     * ### REQUIREMENTS
     * - MyLibraryConfig.kvs set up
     *
     * ### Automatic saving
     * Attempts to save to disk during teardown.
     * This has been tested in a rudimentary manner on Windows in a built application,
     * successfully saving after pressing ALT+F4 or ending the task via Task Manager.
     *
     * However I assume it may not work in more complex teardown cases.
     * Be sure to call Save sometime after modifying critical data.
     *
     * ### Complexity
     * - GetX: Dictionary<,>[] + List<>[] (O(1))
     * - HasKey: Dictionary<,>[] (O(1))
     * - SetX: Dictionary<,>.Add + List<>.Add (O(1) or O(n) if resize is needed)
     * - DeleteAll: O(1)
     * - DeleteKey: O(n)
     */

    [AddComponentMenu("")]
    [DefaultExecutionOrder(-10000)]
    public class KVS : MonoBehaviour
    {
        public static event Action onSave;

        static KVS _iBacking;
        static KVS I
        {
            get
            {
                if (_iBacking == null)
                    throw new InvalidOperationException("Cannot use KVS without setting up MyLibraryConfig.kvs");

                return _iBacking;
            }
        }

        static Action _quittingHandler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (_quittingHandler == null)
            {
                _quittingHandler = Deinit;
                Application.quitting += _quittingHandler;
            }

            if (_iBacking != null)
                return;

            if (string.IsNullOrWhiteSpace(MyLibraryConfig.I?.kvs?.defaultFilename))
                return;

            var obj = new GameObject("KVS");
            obj.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(obj);

            _iBacking = obj.AddComponent<KVS>();

            _iBacking.I_onSave += TriggerStaticOnSave;
        }

        public static void Deinit()
        {
            if (_quittingHandler != null)
            {
                Application.quitting -= _quittingHandler;
                _quittingHandler = null;
            }

            if (_iBacking != null)
            {
                Destroy(_iBacking.gameObject);

                // Apparently this isn't needed here o.O
                // _iBacking.I_onSave -= TriggerStaticOnSave;

                _iBacking = null;
            }
        }

#if UNITY_EDITOR
        [MenuItem("Edit/Clear All KVS")]
        public static void DeleteAll_Editor()
        {
            if (Application.isPlaying)
                throw new NotSupportedException("KVS: Cannot delete via menu while in play mode");

            var config = Resources.Load<MyLibraryConfig>("MyLibraryConfig");
            if (string.IsNullOrEmpty(config?.kvs.defaultFilename))
                throw new InvalidOperationException("Cannot use KVS without setting up MyLibraryConfig.kvs");

            var path = Path.Combine(
                Application.persistentDataPath,
                config.kvs.defaultFilename
            );

            File.Delete(path);
            Debug.Log("Deleted stored KVS");
        }
#endif

        static void TriggerStaticOnSave() => onSave?.Invoke();

        /// <summary>If false after initialisation then trying to use KVS will throw an error</summary>
        public static bool Configured => _iBacking != null;

        public static string FilePath =>
            Path.Combine(
                Application.persistentDataPath,
                MyLibraryConfig.I.kvs.defaultFilename
            );
        
        /// <summary>WARNING: Direct modification is dangerous!</summary>
        public static Data RawData
        {
            get => I._data;
            set => I.UseData(value);
        }

#region PlayerPrefs interface
        public static void DeleteAll() =>
            I.I_DeleteAll();
        
        public static void DeleteKey(string key) =>
            I.I_DeleteKey(key);

        public static float GetFloat(string key, float defaultValue=0.0f) =>
            I.I_GetFloat(key, defaultValue);
        
        public static int GetInt(string key, int defaultValue=0) =>
            I.I_GetInt(key, defaultValue);
        
        public static string GetString(string key, string defaultValue="") =>
            I.I_GetString(key, defaultValue);
        
        public static bool HasKey(string key) =>
            I.I_HasKey(key);
        
        public static void Save() =>
            I.I_Save();
        
        public static void SetFloat(string key, float val) =>
            I.I_SetFloat(key, val);
        
        public static void SetInt(string key, int val) =>
            I.I_SetInt(key, val);
        
        public static void SetString(string key, string val) =>
            I.I_SetString(key, val);
#endregion

        public event Action I_onSave;

        bool _hasLoadedFromDisk;
        Data _data = new Data();

        Dictionary<string, int> _floatIndices;
        Dictionary<string, int> _intIndices;
        Dictionary<string, int> _strIndices;

        void OnEnable()
        {
            if (_iBacking != null && _iBacking != this)
            {
                Debug.LogWarning("KVS duplicated, self-destructing");
                Destroy(gameObject);
                return;
            }

            _iBacking = this;

            if (_hasLoadedFromDisk)
                SetupIndices();
            else
                LoadFromDisk();
        }

        void OnDisable()
        {
            if (_iBacking != this)
                return;

            _iBacking = null;

            I_Save();
        }

        void LoadFromDisk()
        {
            _hasLoadedFromDisk = true;

            var data = new Data();

            if (File.Exists(FilePath))
            {

                try
                {
                    var json = File.ReadAllText(FilePath);
                    data = JsonUtility.FromJson<Data>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to convert saved data from JSON, nuking! Exception:\n{e}");
                    File.Delete(FilePath);
                }
            }

            UseData(data);
        }

        void UseData(Data data)
        {
            _data = data;
            SetupIndices();
        }

        void SetupIndices()
        {
            _floatIndices = new Dictionary<string, int>();
            for (var i = 0; i != _data.floats.Count; ++i)
                _floatIndices[_data.floats[i].key] = i;
            
            _intIndices = new Dictionary<string, int>();
            for (var i = 0; i != _data.ints.Count; ++i)
                _intIndices[_data.ints[i].key] = i;
            
            _strIndices = new Dictionary<string, int>();
            for (var i = 0; i != _data.strs.Count; ++i)
                _strIndices[_data.strs[i].key] = i;
        }

        public void I_DeleteAll()
        {
            File.Delete(FilePath);

            UseData(new Data());
        }

        public void I_DeleteKey(string key)
        {
            int i;
            Dictionary<string, int> dict;

            if (_floatIndices.ContainsKey(key))
            {
                dict = _floatIndices;
                i = dict[key];

                _data.floats.RemoveAt(i);
            }
            else if (_intIndices.ContainsKey(key))
            {
                dict = _intIndices;
                i = dict[key];

                _data.ints.RemoveAt(i);
            }
            else if (_strIndices.ContainsKey(key))
            {
                dict = _strIndices;
                i = dict[key];

                _data.strs.RemoveAt(i);
            }
            else
                return;
            
            dict.Remove(key);

            var keys = new List<string>(dict.Keys);
            foreach (var k in keys)
                if (dict[k] > i)
                    --dict[k];
        }

        public float I_GetFloat(string key, float defaultValue=0.0f) =>
            _floatIndices.ContainsKey(key)
                ? _data.floats[_floatIndices[key]].val
                : defaultValue;

        public int I_GetInt(string key, int defaultValue=0) =>
            _intIndices.ContainsKey(key)
                ? _data.ints[_intIndices[key]].val
                : defaultValue;
        
        public bool I_HasKey(string key) =>
            _floatIndices.ContainsKey(key) ||
            _intIndices.ContainsKey(key) ||
            _strIndices.ContainsKey(key);
        
        public string I_GetString(string key, string defaultValue="") =>
            _strIndices.ContainsKey(key)
                ? _data.strs[_strIndices[key]].val
                : defaultValue;
        
        public void I_Save()
        {
            var json = JsonUtility.ToJson(_data);
            File.WriteAllText(FilePath, json);

            I_onSave?.Invoke();
        }

        public void I_SetFloat(string key, float val)
        {
            var kv = new KVFloat { key=key, val=val };

            if (_floatIndices.ContainsKey(key))
                _data.floats[_floatIndices[key]] = kv;
            else
            {
                _floatIndices[key] = _data.floats.Count;
                _data.floats.Add(kv);
            }
        }

        public void I_SetInt(string key, int val)
        {
            var kv = new KVInt { key=key, val=val };

            if (_intIndices.ContainsKey(key))
                _data.ints[_intIndices[key]] = kv;
            else
            {
                _intIndices[key] = _data.ints.Count;
                _data.ints.Add(kv);
            }
        }

        public void I_SetString(string key, string val)
        {
            var kv = new KVString { key=key, val=val };

            if (_strIndices.ContainsKey(key))
                _data.strs[_strIndices[key]] = kv;
            else
            {
                _strIndices[key] = _data.strs.Count;
                _data.strs.Add(kv);
            }
        }
        
        [Serializable]
        public class Data
        {
            public List<KVFloat> floats = new List<KVFloat>();
            public List<KVInt> ints = new List<KVInt>();
            public List<KVString> strs = new List<KVString>();
        }

        [Serializable]
        public struct KVFloat
        {
            public string key;
            public float val;
        }

        [Serializable]
        public struct KVInt
        {
            public string key;
            public int val;
        }

        [Serializable]
        public struct KVString
        {
            public string key;
            public string val;
        }
    }
}
