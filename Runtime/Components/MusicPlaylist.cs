using System.Collections.Generic;
using UnityEngine;

namespace MyLibrary
{
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
        public IReadOnlyList<AudioSource> tracks => _tracks;

        public bool autoStart;
        public bool shuffle;

        AudioSource[] _tracks;

        void Awake() => UpdateTracks();

        void OnEnable() => MusicController.RegisterPlaylist(this);
        
        void OnTransformChildrenChanged() => UpdateTracks();

        void OnDisable() => MusicController.DeregisterPlaylist(this);

        void UpdateTracks() => _tracks = GetComponentsInChildren<AudioSource>();
    }
}