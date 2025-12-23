using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Tuning.Core
{
    /// <summary>
    /// 調律ミニゲームの演出処理
    /// </summary>
    public class TuningFeedback : MonoBehaviour
    {
        [Header("オーディオ")]
        [Tooltip("BGMを再生するAudioSource")]
        [SerializeField] private AudioSource bgmSource;

        [Tooltip("同期率に連動するローパスフィルター")]
        [SerializeField] private AudioLowPassFilter lowPassFilter;

        [Tooltip("最低カットオフ周波数（同期率0%時）")]
        [SerializeField] private float minCutoff = 500f;

        [Tooltip("最高カットオフ周波数（同期率100%時）")]
        [SerializeField] private float maxCutoff = 22000f;

        [Header("ビジュアル")]
        [Tooltip("フラッシュ演出用オーバーレイ画像")]
        [SerializeField] private Image flashOverlay;

        [Tooltip("左側の点のビジュアル（シェイク演出用）")]
        [SerializeField] private RectTransform leftPointVisual;

        [Tooltip("右側の点のビジュアル（シェイク演出用）")]
        [SerializeField] private RectTransform rightPointVisual;

        [Tooltip("ノイズ演出用オーバーレイ（同期率が低いと表示）")]
        [SerializeField] private CanvasGroup noiseOverlay;

        [Header("成功演出")]
        [Tooltip("成功時に再生するSE")]
        [SerializeField] private AudioClip successSE;

        [Tooltip("SE再生用AudioSource")]
        [SerializeField] private AudioSource seSource;

        private float _targetCutoff;

        private void Awake()
        {
            if (flashOverlay != null)
            {
                var c = flashOverlay.color;
                c.a = 0f;
                flashOverlay.color = c;
            }
        }

        /// <summary>
        /// 同期率に基づいてオーディオフィルターを更新
        /// </summary>
        public void OnSyncUpdate(float totalSync)
        {
            if (lowPassFilter == null) return;

            _targetCutoff = Mathf.Lerp(minCutoff, maxCutoff, totalSync);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, _targetCutoff, Time.deltaTime * 5f);

            if (noiseOverlay != null)
                noiseOverlay.alpha = 1f - totalSync;

            ApplyShake(totalSync);
        }

        /// <summary>
        /// 片方の点がターゲットに入った時
        /// </summary>
        public void OnPointInTarget(int side)
        {
            if (flashOverlay != null)
            {
                flashOverlay.DOKill();
                flashOverlay.DOFade(0.3f, 0.05f).OnComplete(() => flashOverlay.DOFade(0f, 0.15f));
            }
        }

        /// <summary>
        /// 調律成功時
        /// </summary>
        public void OnSuccess()
        {
            if (flashOverlay != null)
            {
                flashOverlay.DOKill();
                flashOverlay.DOFade(1f, 0.1f).OnComplete(() => flashOverlay.DOFade(0f, 0.5f));
            }

            if (seSource != null && successSE != null)
                seSource.PlayOneShot(successSE);

            if (lowPassFilter != null)
                lowPassFilter.cutoffFrequency = maxCutoff;

            if (noiseOverlay != null)
                noiseOverlay.DOFade(0f, 0.5f);
        }

        /// <summary>
        /// ゲームオーバー時（オーバーヒート）
        /// </summary>
        public void OnGameOver()
        {
            if (flashOverlay != null)
            {
                flashOverlay.color = new Color(1f, 0f, 0f, 0f);
                flashOverlay.DOFade(0.8f, 0.2f);
            }
        }

        private void ApplyShake(float sync)
        {
            float shakeIntensity = (1f - sync) * 3f;

            if (leftPointVisual != null)
            {
                Vector2 offset = Random.insideUnitCircle * shakeIntensity;
                leftPointVisual.anchoredPosition += offset * Time.deltaTime * 60f;
            }

            if (rightPointVisual != null)
            {
                Vector2 offset = Random.insideUnitCircle * shakeIntensity;
                rightPointVisual.anchoredPosition += offset * Time.deltaTime * 60f;
            }
        }
    }
}
