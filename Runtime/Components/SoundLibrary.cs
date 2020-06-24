using System.Collections.Generic;
using UnityEngine;

/**
 * ## Notes
 *
 * #### Adding/removing sounds during play
 * Adding/removing child objects will trigger recomputation of the list of sounds.
 */

public class SoundLibrary : MonoBehaviour
{
    HashSet<AudioSource> _sounds;

    void OnEnable()
    {
        _sounds = new HashSet<AudioSource>();
        UpdateSounds();
    }

    void OnTransformChildrenChanged() => UpdateSounds();

    void OnDisable()
    {
        foreach (var sound in _sounds)
            SoundController.DeregisterSound(sound);
    }

    void UpdateSounds()
    {
        var soundsBefore = new HashSet<AudioSource>(_sounds);
        _sounds.Clear();

        foreach (var sound in GetComponentsInChildren<AudioSource>())
        {
            _sounds.Add(sound);

            if (soundsBefore.Contains(sound)) soundsBefore.Remove(sound);
            else SoundController.RegisterSound(sound);
        }

        foreach (var sound in soundsBefore)
            SoundController.DeregisterSound(sound);
    }
}
