using UnityEngine;
using System.Collections;

namespace MessageWindowSystem.Core
{
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
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
                    // Implement flash if needed
                    Debug.Log("Flash effect triggered");
                    break;
                default:
                    Debug.LogWarning($"Effect '{effectName}' not found.");
                    break;
            }
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
