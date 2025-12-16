using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

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
        
        [Header("Center Image")]
        [SerializeField] private Image centerImage;

        [Header("Development Effect")]
        [SerializeField] private AudioClip chargeSE;
        [SerializeField] private AudioClip developCompleteSE;
        [SerializeField] private float developShakeStrength = 3f;
        [SerializeField] private float developShakeDuration = 0.5f;

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
                case MessageWindowSystem.Data.EffectType.ShowImage:
                    ShowImage(effectData.stringParam, effectData.spriteParam, effectData.colorParam);
                    break;
                case MessageWindowSystem.Data.EffectType.HideImage:
                    HideImage();
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

        private void ShowImage(string imageName, Sprite directSprite, Color colorParam)
        {
            if (centerImage == null) return;
            
            Sprite spriteToUse = directSprite;

            // If no direct sprite, try loading by name
            if (spriteToUse == null && !string.IsNullOrEmpty(imageName))
            {
                spriteToUse = Resources.Load<Sprite>($"Images/{imageName}");
                if (spriteToUse == null)
                {
                    Debug.LogWarning($"Image '{imageName}' not found in Resources/Images/");
                    return;
                }
            }

            if (spriteToUse != null)
            {
                centerImage.sprite = spriteToUse;
                centerImage.gameObject.SetActive(true);
                
                // If Color is provided (not clear/black), tint it. Defaults to white if alpha is 0.
                if (colorParam.a > 0) centerImage.color = colorParam;
                else centerImage.color = Color.white;
                
                centerImage.SetNativeSize(); // Adjust size to sprite
            }
        }

        private void HideImage()
        {
            if (centerImage != null)
            {
                centerImage.gameObject.SetActive(false);
            }
        }

        public void PlayChargeSE()
        {
            if (seAudioSource != null && chargeSE != null)
            {
                seAudioSource.clip = chargeSE;
                seAudioSource.loop = true;
                seAudioSource.Play();
            }
        }

        public void StopChargeSE()
        {
            if (seAudioSource != null && seAudioSource.clip == chargeSE)
            {
                seAudioSource.Stop();
                seAudioSource.loop = false;
                seAudioSource.clip = null;
            }
        }

        public void PlayDevelopmentEffect(System.Action onComplete = null)
        {
            // Sound
            if (seAudioSource != null && developCompleteSE != null)
            {
                seAudioSource.PlayOneShot(developCompleteSE);
            }

            // DOTween Sequence
            var seq = DG.Tweening.DOTween.Sequence();

            // 1. Shake Camera
            seq.AppendCallback(() => StartCoroutine(ShakeCamera(developShakeDuration, developShakeStrength)));

            // 2. Flash (Reuse FlashEffect or manual tween)
            // Using FlashEffect coroutine wrapped in callback or manual tween for sync
            if (flashOverlay != null)
            {
                seq.Join(flashOverlay.DOFade(1f, 0.1f));
                seq.Append(flashOverlay.DOFade(0f, 0.5f));
            }

            // 3. Desaturation/Monochrome (Simulated by simple overlay if no shader)
            // For now, assume Flash and Shake are the main "Physical" indications + Sound.
            // If user assigns a monochrome overlay to fadeOverlay, we can use that too.
            // But per spec, "Camera Shake" and "Sound" are key.

            seq.OnComplete(() => onComplete?.Invoke());
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