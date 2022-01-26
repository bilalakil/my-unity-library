using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
#pragma warning disable CS0649
    [SerializeField] GameObject _tooltip;
#pragma warning restore CS0649

    bool _isVisible;
    RectTransform _tooltipRect;
    RectTransform _anchorRect;
    RectTransform _canvasRect;

    void Awake()
    {
        _tooltipRect = (RectTransform)_tooltip.transform;
        _anchorRect = (RectTransform)transform;
        _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        TooltipPositioner.onTooltipWillShow += Hide;
        SetVisible(false);
    }

    void OnDisable()
    {
        TooltipPositioner.onTooltipWillShow -= Hide;
        SetVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipPositioner.onTooltipWillShow?.Invoke();
        SetVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData) =>
        SetVisible(false);

    void SetVisible(bool visible)
    {
        _tooltip.SetActive(visible);
        if (visible)
            TooltipPositioner.Position(_tooltipRect, _anchorRect, _canvasRect);
    }

    void Hide() => SetVisible(false);
}
