using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MessageWindowSystem.Core
{
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Flash Overlay")]
        [SerializeField] private Image flashOverlay; // ← ここにUI Imageを割り当て

        [Header("Flash Settings")]
        [SerializeField] private float flashInDuration = 0.08f;
        [SerializeField] private float flashOutDuration = 0.25f;
        [SerializeField] private Color flashColor = Color.white;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (flashOverlay != null)
            {
                var col = flashOverlay.color;
                col.a = 0f;
                flashOverlay.color = col;
            }
        }

        public void PlayEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName)) return;

            switch (effectName.ToLower())
            {
                case "shake":
                    StartCoroutine(ShakeCamera(0.5f, 0.2f));
                    break;
                case "flash":
                    StartCoroutine(FlashEffect());
                    break;
                default:
                    Debug.LogWarning($"Effect '{effectName}' not found.");
                    break;
            }
        }

        private IEnumerator FlashEffect()
        {
            if (flashOverlay == null)
            {
                Debug.LogWarning("Flash overlay image not assigned.");
                yield break;
            }

            // フェードイン
            float t = 0f;
            while (t < flashInDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, 1f, t / flashInDuration);
                flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
                yield return null;
            }

            // フェードアウト
            t = 0f;
            while (t < flashOutDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / flashOutDuration);
                flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
                yield return null;
            }

            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }

        private IEnumerator ShakeCamera(float duration, float magnitude)
        {
            Vector3 originalPos = Camera.main.transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                Camera.main.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.transform.localPosition = originalPos;
        }
    }
}