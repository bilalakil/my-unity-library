using UnityEngine;

namespace MyLibrary
{
    public class AnimationHelper : MonoBehaviour
    {
        Animator _animator;

        public void PlaySound(string sound) => SoundController.Play(sound);
        public void PlaySoundAtThisLocation(string sound) =>
            SoundController.PlayAtLocation(sound, transform.position);
        public void SelfDestruct() => Destroy(gameObject);
        public void DisableObject() => gameObject.SetActive(false);

        public void SetAnimatorBoolTrue(string boolName)
        {
            PrepareAnimator();
            _animator.SetBool(boolName, true);
        }
        public void SetAnimatorBoolFalse(string boolName)
        {
            PrepareAnimator();
            _animator.SetBool(boolName, false);
        }

        void PrepareAnimator()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }
    }
}