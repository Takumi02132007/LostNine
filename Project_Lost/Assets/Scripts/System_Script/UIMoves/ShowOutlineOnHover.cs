using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace System_Script.UIMoves
{
    /// <summary>
    /// マウスホバー時に指定したOutline用Imageを表示するスクリプト
    /// </summary>
    public class ShowOutlineOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("ホバー時に表示するアウトライン画像")]
        [SerializeField] private Image outlineImage;

        [Tooltip("開始時に画像を非表示にするか")]
        [SerializeField] private bool hideOnStart = true;

        private void Start()
        {
            if (outlineImage != null && hideOnStart)
            {
                outlineImage.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (outlineImage != null)
            {
                outlineImage.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (outlineImage != null)
            {
                outlineImage.gameObject.SetActive(false);
            }
        }
    }
}
