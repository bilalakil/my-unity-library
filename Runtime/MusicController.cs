using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/**
 * ## Notes
 *
 * #### Playlist naming convention
 * Registered playlists can be accessed via the name of their game object,
 * ignoring "(Clone)" at the end.
 * 
 * #### Auto-initialised playlist
 * If prefab with the name "DefaultName" exists in a Resources folder,
 * it will be automatically initialised and made `DontDestroyOnLoad`.
 * You can create multiple playlists within this object if needed.
 *
 * #### Hot-reloading
 * Caveat: all playlists are deregistered and re-registered when this happens,
 * (meaning music will stop and only start again if there is an autoPlay list).
 */

[DefaultExecutionOrder(-1000)]
public class MusicController : MonoBehaviour
{
    static MusicController _i;

    public static MusicPlaylist ActivePlaylist => _i._activePlaylist;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var obj = new GameObject();
        obj.name = "MusicController";
        DontDestroyOnLoad(obj);
        obj.AddComponent<MusicController>();

        var defaultMusic = Resources.Load<GameObject>("DefaultMusic");
        if (defaultMusic != null) DontDestroyOnLoad(Instantiate(defaultMusic));
    }

    /// <summary>
    /// Immediately takes over playing if `playlist.autoPlay == true`.
    /// </summary>
    public static void RegisterPlaylist(MusicPlaylist playlist) => _i.RegisterPlaylist_(playlist);
    /// <summary>
    /// Immediately stops playing if it was the active playlist.
    /// </summary>
    public static void DeregisterPlaylist(MusicPlaylist playlist) => _i.DeregisterPlaylist_(playlist);
    /// <summary>See "Playlist naming convention" in this file's notes.</summary>
    public static void PlayPlaylist(string name) => PlayPlaylist(_i._playlists[name]);
    public static void PlayPlaylist(MusicPlaylist playlist) => _i.PlayPlaylist_(playlist);
    public static void Stop() => _i.Stop_();

    Dictionary<string, MusicPlaylist> _playlists;

    MusicPlaylist _activePlaylist;
    IReadOnlyList<AudioSource> _tracks;
    int _trackIndex;
    AudioSource _cur;
    Coroutine _playCoroutine;

    void OnEnable()
    {
        _i = this;
        _playlists = new Dictionary<string, MusicPlaylist>();
    }
    
    void RegisterPlaylist_(MusicPlaylist playlist)
    {
        var name = playlist.gameObject.StandardName();

        Assert.IsFalse(_playlists.ContainsKey(name));
        _playlists[name] = playlist;

        if (playlist.autoStart) PlayPlaylist_(playlist);
    }

    void DeregisterPlaylist_(MusicPlaylist playlist)
    {
        if (this == null) return;

        var name = playlist.gameObject.StandardName();

        Assert.IsTrue(_playlists.ContainsKey(name));
        if (_activePlaylist == playlist) Stop_();

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

        PlayNextSong();
    }

    void Stop_()
    {
        if (_playCoroutine != null) StopCoroutine(_playCoroutine);

        if (_cur != null) _cur.Stop();

        _activePlaylist = null;
    }

    void PlayNextSong() => _playCoroutine = StartCoroutine(PlayNextSongCo());

    IEnumerator PlayNextSongCo()
    {
        if (_cur != null) _cur.Stop();

        _trackIndex = (_trackIndex + 1) % _tracks.Count;

        _cur = _tracks[_trackIndex];
        _cur.Play();

        while (_cur.isPlaying) yield return null;

        PlayNextSong();
    }
}