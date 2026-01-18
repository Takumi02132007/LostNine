using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace System_Script.UIMoves
{
    /// <summary>
    /// マウスホバー時に、対象のImageの背面に少し大きなコピーを生成してアウトラインとして表示するスクリプト。
    /// Unity標準のOutlineコンポーネント（頂点生成コストが高い）を使用しません。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GenerateOutlineOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Settings")]
        [Tooltip("アウトラインの色")]
        [SerializeField] private Color outlineColor = Color.white;

        [Tooltip("アウトラインの太さ（スケール倍率 1.1 = 10%増）")]
        [SerializeField] private float outlineScale = 1.1f;

        [Tooltip("ピクセル単位でサイズを大きくする場合（スケールより優先、0ならスケール使用）")]
        [SerializeField] private Vector2 pixelPadding = Vector2.zero;

        private GameObject _outlineObject;
        private Image _outlineImage;

        private void Start()
        {
            CreateOutlineObject();
        }

        private void CreateOutlineObject()
        {
            // 既存のアウトラインがあれば削除（再生成用）
            if (_outlineObject != null)
            {
                Destroy(_outlineObject);
            }

            var sourceImage = GetComponent<Image>();
            if (sourceImage == null) return;

            // アウトライン用オブジェクトを作成
            _outlineObject = new GameObject("GeneratedOutline");
            _outlineObject.transform.SetParent(transform, false);
            _outlineObject.transform.SetAsFirstSibling(); // 最背面に配置

            // Imageコンポーネントをコピー
            _outlineImage = _outlineObject.AddComponent<Image>();
            _outlineImage.sprite = sourceImage.sprite;
            _outlineImage.color = outlineColor;
            _outlineImage.raycastTarget = false; // レイキャストをブロックしない
            _outlineImage.type = sourceImage.type;
            _outlineImage.fillCenter = sourceImage.fillCenter; // etc... 必要に応じてコピー

            // UIレイアウトへの影響を無視する設定
            LayoutElement layoutElement = _outlineObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // サイズ調整
            RectTransform rt = _outlineObject.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (pixelPadding != Vector2.zero)
            {
                rt.sizeDelta = pixelPadding; // 親のサイズ + padding
                rt.localScale = Vector3.one;
            }
            else
            {
                rt.sizeDelta = Vector2.zero;
                rt.localScale = new Vector3(outlineScale, outlineScale, 1f);
            }

            // 初期状態は非表示
            _outlineObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_outlineObject != null)
            {
                _outlineObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_outlineObject != null)
            {
                _outlineObject.SetActive(false);
            }
        }
    }
}
