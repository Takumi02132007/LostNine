using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Main.UIMoves;

namespace Main.UIMoves
{
    /// <summary>
    /// マウスホバーで MoveWithEasing を使って移動・復帰するコンポーネント。
    /// - カーソルが乗ったら指定位置へ移動
    /// - カーソルが離れたら元の位置に戻る
    /// - ホバー時にサウンドを再生 (AudioSource仕様)
    /// </summary>
    public class HoverMoveButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover Move Settings")]
        [SerializeField] private Vector3 hoverWorldPosition = Vector3.zero;
        [SerializeField] private Vector2 hoverAnchoredPosition = Vector2.zero;
        [SerializeField] private bool useAnchoredPosition = true;

        [Space]
        [Header("Move Options")]
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private DG.Tweening.Ease ease = DG.Tweening.Ease.OutCubic;
        [SerializeField] private bool shakeOnComplete = false;
        [SerializeField] private float shakeStrength = 5f;
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float endAlpha = 1f;
        [SerializeField] private float fadeDuration = 0.2f;

        [Space]
        [Header("Sound Settings (AudioSource仕様)")]
        [SerializeField] private AudioSource hoverSoundSource;
        [SerializeField] private AudioSource exitSoundSource;
        [SerializeField] private float hoverSoundCooldown = 0.2f; // 連発防止

        private Vector3 _originalWorldPosition;
        private Vector2 _originalAnchoredPosition;
        private bool _isHovering = false;
        private DG.Tweening.Sequence _currentSequence;
        private float _lastHoverSoundTime = -999f;

        private void OnEnable()
        {
            if (useAnchoredPosition)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    _originalAnchoredPosition = rt.anchoredPosition;
                }
            }
            else
            {
                _originalWorldPosition = transform.position;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isHovering) return;
            _isHovering = true;

            KillCurrentSequence();
            PlaySound(hoverSoundSource);

            var opts = BuildMoveOptions();

            if (useAnchoredPosition)
            {
                _currentSequence = MoveWithEasing.MoveToAnchored(gameObject, hoverAnchoredPosition, opts);
            }
            else
            {
                _currentSequence = MoveWithEasing.MoveTo(gameObject, hoverWorldPosition, opts);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovering) return;
            _isHovering = false;

            KillCurrentSequence();
            PlaySound(exitSoundSource);

            var opts = BuildMoveOptions();

            if (useAnchoredPosition)
            {
                _currentSequence = MoveWithEasing.MoveToAnchored(gameObject, _originalAnchoredPosition, opts);
            }
            else
            {
                _currentSequence = MoveWithEasing.MoveTo(gameObject, _originalWorldPosition, opts);
            }
        }

        private MoveWithEasing.MoveOptions BuildMoveOptions()
        {
            return new MoveWithEasing.MoveOptions
            {
                duration = duration,
                ease = ease,
                shakeOnComplete = shakeOnComplete,
                shakeStrength = shakeStrength,
                shakeDuration = shakeDuration,
                endAlpha = endAlpha,
                fadeDuration = fadeDuration
            };
        }

        private void PlaySound(AudioSource source)
        {
            if (source == null) return;

            // クールタイム
            if (Time.time - _lastHoverSoundTime < hoverSoundCooldown) return;
            _lastHoverSoundTime = Time.time;

            source.Play();
        }

        private void KillCurrentSequence()
        {
            if (_currentSequence != null)
            {
                DOTween.Kill(_currentSequence);
            }
            _currentSequence = null;
        }

        private void OnDisable()
        {
            KillCurrentSequence();
        }
    }
}
