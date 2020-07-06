﻿using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRectPositioner2D : MonoBehaviour {
#pragma warning disable CS0649
    [SerializeField] Rect _worldRect;
    [SerializeField] RectTransform _uiRect;
#pragma warning restore CS0649

    Camera _cam;
    float _initialZ;
    float _initialSize;

    Vector3[] _uiRectCorners = new Vector3[4];

    void Awake() {
        _cam = GetComponent<Camera>();
        _initialZ = transform.position.z;
        _initialSize = _cam.orthographicSize;
    }

    void Update() {
        Vector2 containerCentre;
        if (_uiRect != null) {
            // Camera size/position influences screen -> world conversions,
            // so we need to keep them static for calculations.
            transform.position = new Vector3(0f, 0f, _initialZ);
            _cam.orthographicSize = _initialSize;

            _uiRect.GetWorldCorners(_uiRectCorners);
            var br = _uiRectCorners[0];
            var tl = _uiRectCorners[2];
            var worldBR = _cam.ScreenToWorldPoint(br);
            var worldTL = _cam.ScreenToWorldPoint(tl);

            var container = new Rect(worldBR.x, worldBR.y,
                worldTL.x - worldBR.x, worldTL.y - worldBR.y);
            containerCentre = container.center;

            var xFactor = container.width / _worldRect.width;
            var yFactor = container.height / _worldRect.height;

            _cam.orthographicSize = Mathf.Max(
                _initialSize / xFactor,
                _initialSize / yFactor);
        } else {
            var aspectRatio = (float)Screen.width / Screen.height;
            containerCentre = Vector2.zero;

            _cam.orthographicSize = Mathf.Max(
                _worldRect.width / aspectRatio,
                _worldRect.height) * 0.5f;
        }

        var offset = _worldRect.center - containerCentre;
        transform.position = new Vector3(offset.x, offset.y, _initialZ);
    }
}
