using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Tuning.Visuals
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class WaveformVisualizer : MaskableGraphic
    {
        [Header("Wave Settings")]
        [Tooltip("波の周波数")]
        [SerializeField] private float frequency = 10f;

        [Tooltip("波の振幅")]
        [SerializeField] private float amplitude = 50f;

        [Tooltip("波の移動速度")]
        [SerializeField] private float scrollSpeed = 5f;

        [Tooltip("線の太さ")]
        [SerializeField] private float thickness = 2f;

        [Tooltip("位相オフセット")]
        [SerializeField] private float phaseOffset = 0f;

        [SerializeField] private int resolution = 100;

        private float _offset;

        public float Frequency
        {
            get => frequency;
            set => frequency = value;
        }

        public float Amplitude
        {
            get => amplitude;
            set => amplitude = value;
        }

        public float Thickness
        {
            get => thickness;
            set => thickness = value;
        }

        public float ScrollSpeed
        {
            get => scrollSpeed;
            set => scrollSpeed = value;
        }

        public float PhaseOffset
        {
            get => phaseOffset;
            set => phaseOffset = value;
        }

        public void ResetWave()
        {
            _offset = 0f;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        private void Update()
        {
            if (Application.isPlaying && !canvasRenderer.cull)
            {
                _offset += Time.deltaTime * scrollSpeed;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float startX = -width / 2;

            int segments = Mathf.Max(2, resolution);
            float step = width / (segments - 1);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < segments - 1; i++)
            {
                float x1 = startX + i * step;
                float x2 = startX + (i + 1) * step;

                // Normalize x to 0-1 for frequency calculation, add offset
                float normalizedX1 = (float)i / (segments - 1);
                float normalizedX2 = (float)(i + 1) / (segments - 1);

                float y1 = Mathf.Sin((normalizedX1 * frequency) + _offset + phaseOffset) * amplitude;
                float y2 = Mathf.Sin((normalizedX2 * frequency) + _offset + phaseOffset) * amplitude;

                Vector2 p1 = new Vector2(x1, y1);
                Vector2 p2 = new Vector2(x2, y2);

                AddSegment(vh, p1, p2, thickness, vertex);
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 p1, Vector2 p2, float width, UIVertex v)
        {
            Vector2 dir = (p2 - p1).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * width * 0.5f;

            v.position = p1 - normal;
            int idx1 = vh.currentVertCount;
            vh.AddVert(v);

            v.position = p1 + normal;
            vh.AddVert(v);

            v.position = p2 + normal;
            vh.AddVert(v);

            v.position = p2 - normal;
            vh.AddVert(v);

            vh.AddTriangle(idx1, idx1 + 1, idx1 + 2);
            vh.AddTriangle(idx1 + 2, idx1 + 3, idx1);
        }
    }
}
