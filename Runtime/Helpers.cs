using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Helpers
{
    public static T PickRandom<T>(this IEnumerable<T> enumerable) =>
        enumerable.ToArray().PickRandom();
    public static T PickRandom<T>(this IReadOnlyList<T> list) =>
        list[Random.Range(0, list.Count)];

    public static IReadOnlyList<T> PickRandom<T>(this IEnumerable<T> enumerable, int n) =>
        enumerable.ToArray().PickRandom(n);
    public static IReadOnlyList<T> PickRandom<T>(this IReadOnlyList<T> list, int n) =>
        list.Shuffle_().Take(n).ToArray();

    public static IReadOnlyList<T> Shuffle_<T>(this IEnumerable<T> enumerable) =>
        enumerable.ToArray().Shuffle_();
    public static IReadOnlyList<T> Shuffle_<T>(this IReadOnlyList<T> list) =>
        (IReadOnlyList<T>)(new List<T>(list).ShuffleInPlace());

    public static IList<T> ShuffleInPlace<T>(this IList<T> list)
    {
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_modern_algorithm
        for (var i = list.Count - 1; i > 0; --i) {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

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

    public static void DestroyChildren(this Transform tfm)
    {
        for (var i = tfm.childCount - 1; i != -1; --i)
            GameObject.Destroy(tfm.GetChild(i).gameObject);
    }
}
