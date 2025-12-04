using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Main.UIMoves
{
    /// <summary>
    /// テスト用コンポーネント: Inspector から MoveWithEasing の各メソッドを呼べる。
    /// - ContextMenu または UI ボタンから呼び出して動作確認できます。
    /// </summary>
    public class SampleMoveTester : MonoBehaviour
    {
        [Header("Targets")]
        public GameObject targetObject;
        public RectTransform targetRectTransform; // 移動先を RectTransform 指定する場合

        [Header("Positions")]
        public Vector3 worldTarget = Vector3.zero;
        public Vector2 anchoredTarget = Vector2.zero;

        [Header("Options")]
        public float duration = 0.5f;
        public DG.Tweening.Ease ease = DG.Tweening.Ease.OutCubic;
        public bool shakeOnComplete = true;
        public float shakeStrength = 10f;
        public float shakeDuration = 0.35f;
        [Range(0f, 1f)]
        public float endAlpha = 1f;
        public float fadeDuration = 0.25f;

        MoveWithEasing.MoveOptions BuildOptions()
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

        [ContextMenu("Move World Target")]
        public void MoveWorld()
        {
            if (targetObject == null)
            {
                Debug.LogWarning("targetObject is null");
                return;
            }

            var opts = BuildOptions();
            MoveWithEasing.MoveTo(targetObject, worldTarget, opts, () => Debug.Log("MoveWorld complete"));
        }

        [ContextMenu("Move Anchored Target")]
        public void MoveAnchored()
        {
            if (targetObject == null)
            {
                Debug.LogWarning("targetObject is null");
                return;
            }

            var opts = BuildOptions();
            MoveWithEasing.MoveToAnchored(targetObject, anchoredTarget, opts, () => Debug.Log("MoveAnchored complete"));
        }

        [ContextMenu("Move To RectTransform")]
        public void MoveToRect()
        {
            if (targetObject == null || targetRectTransform == null)
            {
                Debug.LogWarning("targetObject or targetRectTransform is null");
                return;
            }

            var opts = BuildOptions();
            MoveWithEasing.MoveToRectTransform(targetObject, targetRectTransform, opts, () => Debug.Log("MoveToRectTransform complete"));
        }

        // エディタで初期値をわかりやすくする
        void OnValidate()
        {
            if (targetObject != null && targetRectTransform == null)
            {
                var rt = targetObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    anchoredTarget = rt.anchoredPosition;
                }
            }
        }
    }
}
