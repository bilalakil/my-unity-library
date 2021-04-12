using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace MyLibrary
{
    public class BDD
    {
        protected GameObject GivenGameObject() => new GameObject();
        
        protected IEnumerator WhenEndOfFrame(int numFrames = 1)
        {
            while (numFrames > 0)
            {
                yield return new WaitForEndOfFrame();
                --numFrames;
            }
        }

        protected IEnumerator WhenPostUpdate() =>
            WhenEndOfFrame(2);
        
        protected void ThenAnimatorBoolIs(
            Animator anim,
            string paramName,
            bool expected
        )
        {
            var actual = anim.GetBool(paramName);
            Assert.AreEqual(expected, actual);
        }

        
        protected void ThenIsNull(UnityEngine.Object obj)
        {
            // Uses `==` to check instead of `Assert.IsNull`
            // so that Unity's overloaded equality operator is used for null checks.
            var isNull = obj == null;
            Assert.IsTrue(isNull);
        }
    }
}
