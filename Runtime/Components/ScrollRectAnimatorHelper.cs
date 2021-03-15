using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MyLibrary
{
    public class ScrollRectAnimatorHelper : MonoBehaviour
    {
        ScrollRect _scrollRect;
        Animator _animator;

        void OnEnable()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError(
                    "ScrollRectAnimatorHelper used without an Animator in game object"
                );
                Destroy(this);
                return;
            }

            _scrollRect = GetComponentInParent<ScrollRect>();
            if (_scrollRect == null)
            {
                Debug.LogError(
                    "ScrollRectAnimatorHelper used without a ScrollRect in game object or parents"
                );
                Destroy(this);
                return;
            }

            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            bool isTop, isBottom;
            if (_scrollRect.content.rect.height > _scrollRect.viewport.rect.height)
            {
                isTop = _scrollRect.verticalNormalizedPosition >= 1;
                isBottom = _scrollRect.verticalNormalizedPosition <= 0;
            }
            else
                isTop = isBottom = true;

            _animator.SetBool("IsScrolledToTop", isTop);
            _animator.SetBool("IsScrolledToBottom", isBottom);
        }
    }
}