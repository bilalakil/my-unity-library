using UnityEngine;

/**
 * ## Notes
 *
 * #### Adding/removing tracks during play
 * Adding/removing child objects will trigger recomputation of the list of tracks.
 * However, if a track that's currently being played is removed, it will continue
 * to be played until interruption/completion.
 */

public class MusicPlaylist : MonoBehaviour
{
    public bool autoStart;
    public bool shuffle;

    [HideInInspector] public AudioSource[] tracks;

    void Awake() => UpdateTracks();

    void OnEnable() => MusicController.RegisterPlaylist(this);
    
    void OnTransformChildrenChanged() => UpdateTracks();

    void OnDisable() => MusicController.DeregisterPlaylist(this);

    void UpdateTracks() => tracks = GetComponentsInChildren<AudioSource>();
}
