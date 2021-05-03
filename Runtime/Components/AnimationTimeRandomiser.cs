using System.Collections;
using UnityEngine;

namespace MyLibrary
{
    public class AnimationTimeRandomiser : MonoBehaviour
    {
        Animator _anim;

        void Awake() => _anim = GetComponent<Animator>();

        void OnEnable() => StartCoroutine(UpdateAnim());

        IEnumerator UpdateAnim()
        {
            yield return new WaitForEndOfFrame();

            var rng = Random.value;
            for (var layer = _anim.layerCount - 1; layer != -1; --layer)
                _anim.Play(
                    _anim.GetCurrentAnimatorStateInfo(layer).fullPathHash,
                    layer,
                    rng
                );
        }
    }
}