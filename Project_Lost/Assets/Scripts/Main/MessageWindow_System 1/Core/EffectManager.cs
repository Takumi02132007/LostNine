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

        [Header("Fade Overlay")]
        [SerializeField] private Image fadeOverlay;

        [Header("Audio")]
        [SerializeField] private AudioSource seAudioSource;
        [SerializeField] private AudioSource bgmAudioSource;

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

            if (fadeOverlay != null)
            {
                var col = fadeOverlay.color;
                col.a = 0f;
                fadeOverlay.color = col;
            }
        }

        public void PlayEffect(MessageWindowSystem.Data.ScreenEffectData effectData)
        {
            switch (effectData.effectType)
            {
                case MessageWindowSystem.Data.EffectType.Shake:
                    StartCoroutine(ShakeCamera(effectData.floatParam > 0 ? effectData.floatParam : 0.5f, 0.2f));
                    break;
                case MessageWindowSystem.Data.EffectType.Flash:
                    StartCoroutine(FlashEffect(effectData.colorParam));
                    break;
                case MessageWindowSystem.Data.EffectType.FadeIn: // From black/color to transparent (Show Screen)
                    StartCoroutine(FadeEffect(effectData.floatParam, effectData.colorParam, true));
                    break;
                case MessageWindowSystem.Data.EffectType.FadeOut: // From transparent to color (Hide Screen)
                    StartCoroutine(FadeEffect(effectData.floatParam, effectData.colorParam, false));
                    break;
                case MessageWindowSystem.Data.EffectType.PlaySE:
                    PlaySE(effectData.stringParam);
                    break;
                case MessageWindowSystem.Data.EffectType.PlayBGM:
                    PlayBGM(effectData.stringParam, effectData.floatParam);
                    break;
                case MessageWindowSystem.Data.EffectType.StopBGM:
                    StopBGM(effectData.floatParam);
                    break;
            }
        }

        private void PlaySE(string clipName)
        {
            if (seAudioSource == null || string.IsNullOrEmpty(clipName)) return;
            var clip = Resources.Load<AudioClip>($"Audio/SE/{clipName}"); // Assumption: Resources folder
            if (clip != null) seAudioSource.PlayOneShot(clip);
            else Debug.LogWarning($"SE '{clipName}' not found in Resources/Audio/SE/");
        }

        private void PlayBGM(string clipName, float fadeDuration)
        {
            if (bgmAudioSource == null || string.IsNullOrEmpty(clipName)) return;
            var clip = Resources.Load<AudioClip>($"Audio/BGM/{clipName}"); // Assumption: Resources folder
            if (clip != null)
            {
                if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying) return; // Already playing
                bgmAudioSource.clip = clip;
                bgmAudioSource.loop = true;
                bgmAudioSource.Play();
                // TODO: Implement fade in if needed using fadeDuration
            }
            else Debug.LogWarning($"BGM '{clipName}' not found in Resources/Audio/BGM/");
        }

        private void StopBGM(float fadeDuration)
        {
            if (bgmAudioSource == null) return;
            bgmAudioSource.Stop();
             // TODO: Implement fade out if needed
        }

        private IEnumerator FlashEffect(Color color)
        {
            if (flashOverlay == null)
            {
                Debug.LogWarning("Flash overlay image not assigned.");
                yield break;
            }

            Color c = color == Color.clear ? flashColor : color;
            float t = 0f;
            
            // In
            while (t < flashInDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, 1f, t / flashInDuration);
                flashOverlay.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }

            // Out
            t = 0f;
            while (t < flashOutDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / flashOutDuration);
                flashOverlay.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            flashOverlay.color = new Color(c.r, c.g, c.b, 0f);
        }

        private IEnumerator FadeEffect(float duration, Color color, bool fadeIn)
        {
            if (fadeOverlay == null) yield break;
            
            float targetAlpha = fadeIn ? 0f : 1f;
            float startAlpha = fadeOverlay.color.a; // or fadeIn ? 1f : 0f;
            Color targetColor = color == Color.clear ? Color.black : color;
            
            // If fading out (hiding screen), ensure color is set before alpha increases
            // If fading in (showing screen), ensure color matches what it should fade from
            if (!fadeIn) fadeOverlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, startAlpha);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                fadeOverlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, a);
                yield return null;
            }
            fadeOverlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetAlpha);
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