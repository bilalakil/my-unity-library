using MyLibrary;
using UnityEngine;

/**
 * ## Sample Notes
 * 
 * This drop-in static class can be used in projects that used Unity's PlayerPrefs in the past,
 * and are looking to transition to usage of KVS instead.
 *
 * Copy/paste the script into your project and replace all `PlayerPrefs.Get` calls.
 *
 * It will clear the old PlayerPrefs as things are transferred over.
 */

public static class KVSBridge
{
    public static float GetFloat(string key, float defaultValue=0)
    {
        var ppVal = PlayerPrefs.GetFloat(key, float.MaxValue);
        if (ppVal != float.MaxValue)
        {
            PlayerPrefs.DeleteKey(key);
            if (KVS.GetFloat(key, float.MaxValue) == float.MaxValue)
                KVS.SetFloat(key, ppVal);
        }

        return KVS.GetFloat(key, defaultValue);
    }

    public static int GetInt(string key, int defaultValue=0)
    {
        var ppVal = PlayerPrefs.GetInt(key, int.MaxValue);
        if (ppVal != int.MaxValue)
        {
            PlayerPrefs.DeleteKey(key);
            if (KVS.GetInt(key, int.MaxValue) == int.MaxValue)
                KVS.SetInt(key, ppVal);
        }

        return KVS.GetInt(key, defaultValue);
    }

    public static string GetString(string key, string defaultValue="")
    {
        var dummy = "__kvs-dummy__";
        var ppVal = PlayerPrefs.GetString(key, dummy);
        if (ppVal != dummy)
        {
            PlayerPrefs.DeleteKey(key);
            if (KVS.GetString(key, dummy) == dummy)
                KVS.SetString(key, ppVal);
        }

        return KVS.GetString(key, defaultValue);
    }
}
