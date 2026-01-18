using UnityEngine;
using UnityEngine.UI;

namespace Tuning.Visuals
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class NoiseVisualizer : MaskableGraphic
    {
        [Header("Noise Settings")]
        [Tooltip("ノイズの数")]
        [SerializeField] private int noiseCount = 50;

        [Tooltip("ノイズの最大サイズ")]
        [SerializeField] private float maxNoiseSize = 50f;

        [Tooltip("ノイズ更新間隔（秒）")]
        [SerializeField] private float updateInterval = 0.05f;

        private float _timer;
        private bool _isActive;

        public bool IsEffectActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    SetVerticesDirty();
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isActive = false;
        }

        private void Update()
        {
            if (Application.isPlaying && _isActive && !canvasRenderer.cull)
            {
                _timer += Time.deltaTime;
                if (_timer >= updateInterval)
                {
                    _timer = 0f;
                    SetVerticesDirty();
                }
            }
            else if (!_isActive && rectTransform.rect.width > 0) // Ensure clear if disabled
            {
                // Logic handled in OnPopulateMesh check
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (!_isActive) return;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < noiseCount; i++)
            {
                float size = Random.Range(5f, maxNoiseSize);
                float x = Random.Range(-width / 2, width / 2);
                float y = Random.Range(-height / 2, height / 2);

                Vector2 center = new Vector2(x, y);
                Vector2 p1 = center + new Vector2(-size, -size) * 0.5f;
                Vector2 p2 = center + new Vector2(size, size) * 0.5f;

                AddQuad(vh, p1, p2, vertex);
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, UIVertex v)
        {
            int idx = vh.currentVertCount;

            v.position = new Vector3(min.x, min.y);
            vh.AddVert(v);

            v.position = new Vector3(min.x, max.y);
            vh.AddVert(v);

            v.position = new Vector3(max.x, max.y);
            vh.AddVert(v);

            v.position = new Vector3(max.x, min.y);
            vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx);
        }
    }
}
