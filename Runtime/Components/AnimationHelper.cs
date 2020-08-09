using UnityEngine;

public class AnimationHelper : MonoBehaviour
{
    public void PlaySound(string sound) => SoundController.Play(sound);
}