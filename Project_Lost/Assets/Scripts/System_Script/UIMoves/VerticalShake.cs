using UnityEngine;
using DG.Tweening;

public class VerticalShake : MonoBehaviour
{
    [SerializeField] private float moveAmount = 10f; // 上下の移動量
    [SerializeField] private float duration = 0.6f;  // 片道にかかる時間
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.localPosition;

        transform
            .DOLocalMoveY(originalPos.y + moveAmount, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo); // 往復
    }
}
