using UnityEngine;

namespace MyLibrary
{
    public class ForceWorldRotation : MonoBehaviour
    {
        public Quaternion rotation = Quaternion.identity;

        void FixedUpdate() => Update();
        void Update()
        {
            if (transform.rotation == rotation) return;
            transform.rotation = rotation;
        }
        void LateUpdate() => Update();
    }
}