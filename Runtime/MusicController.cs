using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

namespace MyLibrary
{
    /**
    * ## Notes
    *
    * #### Playlist naming convention
    * Registered playlists can be accessed by their game object's StandardName.
    * 
    * #### Auto-initialised playlist(s)
    * If a prefab named "DefaultName" exists in a Resources folder,
    * it will be automatically initialised and made `DontDestroyOnLoad`.
    *
    * #### Hot-reloading
    * Caveat: all playlists are deregistered and re-registered when this happens,
    * (meaning music will stop, and only start again if there is an autoPlay list registered).
    */

    [AddComponentMenu("")]
    [DefaultExecutionOrder(-1000)]
    public class MusicController : MonoBehaviour
    {
        static MusicController _i
        {
            get
            {
                if (!_haveInstantiated)
                {
                    var obj = new GameObject("MusicController");
                    DontDestroyOnLoad(obj);
                    obj.AddComponent<MusicController>();
                }
                return _iBacking;
            }
        }
        static MusicController _iBacking;
        static bool _haveInstantiated;

        public static MusicPlaylist ActivePlaylist => _i._activePlaylist;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _iBacking = null;
            _haveInstantiated = false;

            var defaultMusic = Resources.Load<GameObject>("DefaultMusic");
            if (defaultMusic == null)
                return;
            
            DontDestroyOnLoad(Instantiate(defaultMusic));
        }

        public static void Deinit()
        {
            if (_iBacking == null)
                return;

            Destroy(_iBacking.gameObject);
            _iBacking = null;
            _haveInstantiated = false;
        }

        /// <summary>
        /// Immediately takes over playing if `playlist.autoPlay == true`.
        /// </summary>
        public static void RegisterPlaylist(MusicPlaylist playlist) => _i.RegisterPlaylist_(playlist);
        /// <summary>
        /// Immediately stops playing if it was the active playlist.
        /// </summary>
        public static void DeregisterPlaylist(MusicPlaylist playlist) => _i?.DeregisterPlaylist_(playlist);
        /// <summary>See "Playlist naming convention" in this file's notes.</summary>
        public static void PlayPlaylist(string name) => PlayPlaylist(_i._playlists[name]);
        public static void PlayPlaylist(MusicPlaylist playlist) => _i.PlayPlaylist_(playlist);
        public static void Stop() => _i.Stop_();

        Dictionary<string, MusicPlaylist> _playlists;

        MusicPlaylist _activePlaylist;
        IReadOnlyList<AudioSource> _tracks;
        int _trackIndex;
        AudioSource _cur;

        void OnEnable()
        {
            if (_iBacking != null)
            {
                Debug.LogWarning("MusicController duplicated, self-destructing");
                Destroy(gameObject);
                return;
            }

            _iBacking = this;
            _haveInstantiated = true;
            _playlists = new Dictionary<string, MusicPlaylist>();
        }

        void Update()
        {
            if (_activePlaylist == null || _tracks.Count == 0)
            {
                if (_cur != null)
                    StopCur();
                return;
            }

            var shouldPlayNext = _cur == null || !_cur.isPlaying;
            if (!shouldPlayNext)
                return;

            _trackIndex = (_trackIndex + 1) % _tracks.Count;

            _cur = _tracks[_trackIndex];
            if (_cur.isActiveAndEnabled)
                _cur.Play();
            else _cur = null;
        }
        
        void OnDisable()
        {
            if (_iBacking == this)
                _iBacking = null;
        }
        
        void RegisterPlaylist_(MusicPlaylist playlist)
        {
            var name = playlist.gameObject.StandardName();

            Assert.IsFalse(_playlists.ContainsKey(name));
            _playlists[name] = playlist;

            if (playlist.autoStart)
                PlayPlaylist_(playlist);
        }

        void DeregisterPlaylist_(MusicPlaylist playlist)
        {
            if (this == null)
                return;

            var name = playlist.gameObject.StandardName();

            Assert.IsTrue(_playlists.ContainsKey(name));
            if (_activePlaylist == playlist)
                Stop_();

            _playlists.Remove(name);
        }

        void PlayPlaylist_(MusicPlaylist playlist)
        {
            Stop_();

            _activePlaylist = playlist;
            _trackIndex = -1;
            _tracks = _activePlaylist.shuffle
                ? _activePlaylist.tracks.Shuffle_()
                : _activePlaylist.tracks;
        }

        void Stop_()
        {
            StopCur();
            _activePlaylist = null;
        }

        void StopCur()
        {
            if (_cur == null)
                return;
            _cur.Stop();
            _cur = null;
        }
    }
}