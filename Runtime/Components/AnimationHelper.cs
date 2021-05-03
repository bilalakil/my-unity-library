using UnityEngine;

namespace MyLibrary
{
    public class AnimationHelper : MonoBehaviour
    {
        public void PlaySound(string sound) => SoundController.Play(sound);
        public void PlaySoundAtThisLocation(string sound) =>
            SoundController.PlayAtLocation(sound, transform.position);
        public void SelfDestruct() => Destroy(gameObject);
    }
}