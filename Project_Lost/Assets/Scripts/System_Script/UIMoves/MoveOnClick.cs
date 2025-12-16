using UnityEngine;
using Main.UIMoves;
using DG.Tweening;

namespace Main.UIMoves
{
    public class MoveOnClick : MonoBehaviour
    {
        [Header("移動先")]
        [SerializeField] private Vector2 targetAnchoredPosition;

        private RectTransform _rect;

        [Header("アニメオプション")]
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private DG.Tweening.Ease ease = DG.Tweening.Ease.OutBack;

        [SerializeField] private bool shakeOnComplete = false;
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private float shakeDuration = 0.25f;

        [SerializeField] private float endAlpha = 1f;
        [SerializeField] private float fadeDuration = 0.2f;

        [Space]
        [Header("Scale Settings")]
        [SerializeField] private bool enableScale = false;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private float scaleDuration = 0.6f;
        [SerializeField] private DG.Tweening.Ease scaleEase = DG.Tweening.Ease.OutBack;

        [Space]
        [Header("Size Settings")]
        [SerializeField] private bool enableSize = false;
        [SerializeField] private Vector2 targetSizeDelta = Vector2.zero;
        [SerializeField] private float sizeDuration = 0.6f;
        [SerializeField] private DG.Tweening.Ease sizeEase = DG.Tweening.Ease.OutBack;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Play()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_rect == null) return;

            MoveWithEasing.MoveToAnchored(
                gameObject,
                targetAnchoredPosition,
                new MoveWithEasing.MoveOptions
                {
                    duration = duration,
                    ease = ease,
                    shakeOnComplete = shakeOnComplete,
                    shakeStrength = shakeStrength,
                    shakeDuration = shakeDuration,
                    endAlpha = endAlpha,
                    fadeDuration = fadeDuration
                }
            );

            if (enableScale)
            {
                transform.DOScale(targetScale, scaleDuration).SetEase(scaleEase);
            }

            if (enableSize)
            {
                _rect.DOSizeDelta(targetSizeDelta, sizeDuration).SetEase(sizeEase);
            }
        }
    }
}
