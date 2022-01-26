using System;
using UnityEngine;
using UnityEngine.UI;

public static class TooltipPositioner
{
    const bool DISABLE_TOP_TOOLTIP_POS = true;

    public static Action onTooltipWillShow;

    static Vector3[] _tooltipCnrs = new Vector3[4];
    static Vector3[] _anchorCnrs = new Vector3[4];
    static Vector3[] _canvasCnrs = new Vector3[4];

    /**
     * Assumes (0.5, 0) pivot on _rect.
     * Performs the following actions:
     *
     * 1. Centre on top (unless `DISABLE_TOP_TOOLTIP_POS`), or right if it can't fit on top
     * 2. If bottom overflowing, set bottom to bottom
     * 2. If top overflowing, set top to top
     * 3. Force no right overflow
     * 4. Force no left overflow
     */
    public static void Position(RectTransform tooltipRect, RectTransform anchorRect, RectTransform canvasRect)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        tooltipRect.GetWorldCorners(_tooltipCnrs);
        anchorRect.GetWorldCorners(_anchorCnrs);
        canvasRect.GetWorldCorners(_canvasCnrs);

        var anchorMidPoint = (_anchorCnrs[0] + _anchorCnrs[2]) * 0.5f;
        var tooltipHalfWidth = (_tooltipCnrs[2].x - _tooltipCnrs[0].x) * 0.5f;
        var tooltipHeight = _tooltipCnrs[2].y - _tooltipCnrs[0].y;

        var topOverflows = DISABLE_TOP_TOOLTIP_POS || _anchorCnrs[2].y + tooltipHeight > _canvasCnrs[2].y;
        var yPos = topOverflows ? anchorMidPoint.y - tooltipHeight * 0.5f : _anchorCnrs[2].y;

        if (yPos < _canvasCnrs[0].y)
            yPos = _canvasCnrs[0].y;
        if (yPos + tooltipHeight > _canvasCnrs[2].y)
            yPos = _canvasCnrs[2].y - tooltipHeight;
        
        var newPos = topOverflows
            ? new Vector3(
                anchorMidPoint.x < (_canvasCnrs[0].x + _canvasCnrs[2].x) * 0.5f
                    ? _anchorCnrs[2].x + tooltipHalfWidth
                    : _anchorCnrs[0].x - tooltipHalfWidth,
                yPos,
                0f
            )
            : new Vector3(anchorMidPoint.x, _anchorCnrs[2].y, 0f);
        
        if (newPos.x + tooltipHalfWidth > _canvasCnrs[2].x)
            newPos.x = _canvasCnrs[2].x - tooltipHalfWidth;
        if (newPos.x - tooltipHalfWidth < _canvasCnrs[0].x)
            newPos.x = _canvasCnrs[0].x + tooltipHalfWidth;
        
        tooltipRect.position = newPos;
    }
}
