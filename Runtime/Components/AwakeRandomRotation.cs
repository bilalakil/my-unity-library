using UnityEngine;

public class AwakeRandomRotation : MonoBehaviour
{
#pragma warning disable CS0649
    [SerializeField] Vector2 _minMaxX;
    [SerializeField] Vector2 _minMaxY;
    [SerializeField] Vector2 _minMaxZ;
#pragma warning restore CS0649

    void Awake()
    {
        transform.rotation = Quaternion.Euler(
            Random.Range(_minMaxX.x, _minMaxX.y),
            Random.Range(_minMaxY.x, _minMaxY.y),
            Random.Range(_minMaxZ.x, _minMaxZ.y)
        );

        Destroy(this);
    }
}
