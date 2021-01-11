using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class Tests_ScrollRectAnimatorHelper : UnityEditorBDD
{
    [UnityTest]
    public IEnumerator SelfDestructsWithNoAnimator()
    {
        var obj = GivenGameObject();

        var comp = WhenScrollRectAnimatorAdded(
            obj,
            CreationOptions.ExpectMissingAnimator
        );
        yield return WhenEndOfFrame();

        ThenIsNull(comp);
    }

    [UnityTest]
    public IEnumerator SelfDestructsWithNoScrollRect()
    {
        var obj = GivenTestGameObject(
            "ScrollRectAnimatorHelper/GameObjectWithAnimator.prefab"
        );

        var comp = WhenScrollRectAnimatorAdded(
            obj,
            CreationOptions.ExpectMissingScrollRect
        );
        yield return WhenEndOfFrame();

        ThenIsNull(comp);
    }

    [UnityTest]
    public IEnumerator SetsIsScrolledToTopOrBottomConsideringSizeAndPositionChange()
    {
        var (obj, anim, scrollRect) = GivenScrollRectAnimatorHelper();

        yield return WhenEndOfFrame(2);

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", true);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", true);

        WhenScrollRectContentHeightChanges(scrollRect, 6000);
        yield return WhenEndOfFrame();

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", true);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", false);

        WhenScrollRectPositionChanges(scrollRect, 0.5f);
        yield return WhenEndOfFrame();

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", false);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", false);

        WhenScrollRectPositionChanges(scrollRect, 0);
        yield return WhenEndOfFrame();

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", false);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", true);

        WhenScrollRectContentHeightChanges(scrollRect, 100);
        yield return WhenEndOfFrame();

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", true);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", true);
    }

    [UnityTest]
    public IEnumerator IsEffectiveOnEnable()
    {
        var (obj, anim) = GivenScrollRectWithoutAnimatorHelper();

        yield return WhenEndOfFrame(2);

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", false);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", false);

        WhenScrollRectAnimatorAdded(anim.gameObject);

        ThenAnimatorBoolIs(anim, "IsScrolledToTop", true);
        ThenAnimatorBoolIs(anim, "IsScrolledToBottom", true);
    }

    (GameObject, Animator, ScrollRect) GivenScrollRectAnimatorHelper()
    {
        var obj = GivenTestGameObject(
            "ScrollRectAnimatorHelper/ScrollRectAnimatorHelper.prefab"
        );
        var anim = obj.GetComponentInChildren<Animator>();
        var scrollRect = obj.GetComponentInChildren<ScrollRect>();

        return (obj, anim, scrollRect);
    }

    (GameObject, Animator) GivenScrollRectWithoutAnimatorHelper()
    {
        var obj = GivenTestGameObject(
            "ScrollRectAnimatorHelper/ScrollRectWithoutAnimatorHelper.prefab"
        );
        var anim = obj.GetComponentInChildren<Animator>();

        return (obj, anim);
    }

    enum CreationOptions
    {
        Default,
        ExpectMissingAnimator,
        ExpectMissingScrollRect
    }
    ScrollRectAnimatorHelper WhenScrollRectAnimatorAdded(
        GameObject obj,
        CreationOptions opts = CreationOptions.Default
    )
    {
        if (opts == CreationOptions.ExpectMissingAnimator)
            LogAssert.Expect(
                LogType.Error,
                "ScrollRectAnimatorHelper used without an Animator in game object"
            );
        else if (opts == CreationOptions.ExpectMissingScrollRect)
            LogAssert.Expect(
                LogType.Error,
                "ScrollRectAnimatorHelper used without a ScrollRect in game object or parents"
            );

        return obj.AddComponent<ScrollRectAnimatorHelper>();
    }

    void WhenScrollRectContentHeightChanges(ScrollRect scrollRect, float height) =>
        scrollRect.content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    
    void WhenScrollRectPositionChanges(ScrollRect scrollRect, float position) =>
        scrollRect.verticalNormalizedPosition = position;
}
