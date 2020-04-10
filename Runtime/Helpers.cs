using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Helpers
{
    public static T[] Shuffle_<T>(this IReadOnlyList<T> list) =>
        list.OrderBy(_ => Random.value).ToArray();

    const string _clonedGameObjectSuffix = "(Clone)";
    /// <summary>
    /// Returns the game object's name minus the "(Clone)" suffix.
    /// </summary>
    public static string StandardName(this GameObject obj)
    {
        var name = obj.name;

        var choppedName = name.EndsWith(_clonedGameObjectSuffix)
            ? name.Remove(name.Length - _clonedGameObjectSuffix.Length)
            : name;

        return choppedName;
    }
}
