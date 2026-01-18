using UnityEngine;
using UnityEngine.EventSystems;

namespace System_Script.UIMoves
{
    /// <summary>
    /// UIオブジェクトをドラッグ＆ドロップで動かせるスクリプト。
    /// 左右に動かすと、慣性で揺れる（回転する）演出が入ります。
    /// </summary>
    public class DragAndShake : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Settings")]
        [Tooltip("揺れの強さ")]
        [SerializeField] private float shakePower = 3f;

        [Tooltip("揺れが戻る速さ")]
        [SerializeField] private float restoreSpeed = 5f;

        [Tooltip("最大回転角度")]
        [SerializeField] private float maxAngle = 45f;

        private RectTransform _rectTransform;
        private Rigidbody2D _rb;
        private Canvas _canvas;
        private float _currentAngle;
        private Vector2 _lastPosition;

        private bool _isDragging = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _rb = GetComponent<Rigidbody2D>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _lastPosition = eventData.position;
            if (_rb != null)
            {
                _rb.simulated = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            if (_canvas == null) return;

            // 画面外チェック (画面外に出たらドラッグ解除)
            if (!IsMouseInScreen())
            {
                ForceEndDrag(eventData);
                return;
            }

            // マウスの移動量に応じて位置を更新
            Vector2 delta = eventData.delta / _canvas.scaleFactor;
            _rectTransform.anchoredPosition += delta;

            // 横方向の速度を計算（揺れ用）
            float moveX = eventData.delta.x;
            
            float targetAngle = -moveX * shakePower; 
            
            // シンプルに「速度＝傾き」とする
            _currentAngle = Mathf.Lerp(_currentAngle, targetAngle, Time.deltaTime * 10f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDragProcess();
        }

        private void ForceEndDrag(PointerEventData eventData)
        {
            EndDragProcess();
            eventData.pointerDrag = null;
            eventData.dragging = false;
        }

        private void EndDragProcess()
        {
            _isDragging = false;
            if (_rb != null)
            {
                _rb.simulated = true;
                // 必要なら速度をリセット
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        private bool IsMouseInScreen()
        {
#if UNITY_EDITOR
            // エディタ上ではGameViewの範囲外に出るとmousePositionが範囲外の値を返す
            Vector3 mousePos = Input.mousePosition;
            return mousePos.x >= 0 && mousePos.x <= Screen.width &&
                   mousePos.y >= 0 && mousePos.y <= Screen.height;
#else
            // ビルド後やプラットフォームによっては挙動が異なる場合があるが、基本は同じ
            Vector3 mousePos = Input.mousePosition;
            return mousePos.x >= 0 && mousePos.x <= Screen.width &&
                   mousePos.y >= 0 && mousePos.y <= Screen.height;
#endif
        }

        private void Update()
        {
            // ドラッグしていない時も慣性で揺れを戻す
            if (!_isDragging) 
            {
                _currentAngle = Mathf.Lerp(_currentAngle, 0f, Time.deltaTime * restoreSpeed);
            }
            else
            {
                // ドラッグ中も徐々に戻ろうとする力は働くが、OnDragで上書きされる成分が強い
                 _currentAngle = Mathf.Lerp(_currentAngle, 0f, Time.deltaTime * 2f);
            }

            // 角度制限
            _currentAngle = Mathf.Clamp(_currentAngle, -maxAngle, maxAngle);

            if (_rectTransform != null)
            {
                _rectTransform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
            }
        }
    }
}
