using UnityEngine;
using DG.Tweening;

public class FirstMove : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 originalPos;
    public Vector2 OriginalPos => originalPos;

    [SerializeField] float duration = 0.4f;
    [SerializeField] float offsetY = -300f; // 下からどれだけオフセットして出すか
    [SerializeField] Ease ease = Ease.OutBack; // 動きのタイプ

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        rect.anchoredPosition = originalPos + new Vector2(0, offsetY); // 画面下へずらす
        rect.DOAnchorPos(originalPos, duration).SetEase(ease);
    }
}
