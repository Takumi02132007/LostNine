using UnityEngine;

namespace System_Script.UIMoves
{
    /// <summary>
    /// オブジェクトが画面外に出た場合に、初期位置にリセットするスクリプト。
    /// Rigidbody2Dがついている場合は速度もリセットします。
    /// </summary>
    public class ResetPositionOutOfBounds : MonoBehaviour
    {
        [Tooltip("画面外判定のマージン（ピクセル単位）。この値分だけ画面外に出たらリセットする。")]
        [SerializeField] private float margin = 100f;

        private RectTransform _rectTransform;
        private Vector2 _initialPosition;
        private Rigidbody2D _rb;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rb = GetComponent<Rigidbody2D>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            if (_rectTransform != null)
            {
                _initialPosition = _rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (_canvas == null || _rectTransform == null) return;

            // ワールド座標をスクリーン座標（のようなもの）として扱い判定
            // CanvasがScreen Space - Overlayの場合はtransform.positionがスクリーン座標と一致
            // Cameraの場合は変換が必要だが、ここでは簡易的にワールド座標で判定するか、ビューポート変換を行う

            // より汎用的な判定: RectTransformのワールド座標をスクリーン・ビューポート座標に変換
            Vector3 worldPos = _rectTransform.position;
            Vector3 screenPos = worldPos;

            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.worldCamera != null)
            {
                screenPos = _canvas.worldCamera.WorldToScreenPoint(worldPos);
            }
            
            // 画面範囲チェック
            bool isOffScreen = 
                screenPos.x < -margin || 
                screenPos.x > Screen.width + margin ||
                screenPos.y < -margin || 
                screenPos.y > Screen.height + margin;

            if (isOffScreen)
            {
                ResetToInitialPosition();
            }
        }

        public void ResetToInitialPosition()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _initialPosition;
                _rectTransform.rotation = Quaternion.identity; // 回転もリセット
            }

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero; // Unity 6 / New Physics
                _rb.angularVelocity = 0f;
            }
        }
    }
}
