using UnityEngine;
using UnityEngine.InputSystem;

namespace Tuning.Core
{
    /// <summary>
    /// 調律ミニゲームの入力処理
    /// 左: WASD, 右: IJKL
    /// </summary>
    public class TuningInput : MonoBehaviour
    {
        [Header("設定（TuningManagerから自動設定されます）")]
        [Tooltip("左側の操作を反転するか")]
        [SerializeField] private bool invertLeft;

        [Tooltip("右側の操作を反転するか")]
        [SerializeField] private bool invertRight;

        [Tooltip("左右の干渉強度 (0.0 - 1.0)")]
        [SerializeField] private float interferenceStrength;

        public Vector2 LeftInput { get; private set; }
        public Vector2 RightInput { get; private set; }
        public Vector2 LeftInterference { get; private set; }
        public Vector2 RightInterference { get; private set; }

        public void Configure(bool invertL, bool invertR, float interference)
        {
            invertLeft = invertL;
            invertRight = invertR;
            interferenceStrength = interference;
        }

        private void Update()
        {
            Vector2 rawLeft = GetLeftRawInput();
            Vector2 rawRight = GetRightRawInput();

            if (invertLeft) rawLeft = -rawLeft;
            if (invertRight) rawRight = -rawRight;

            LeftInput = rawLeft;
            RightInput = rawRight;

            LeftInterference = -rawRight * interferenceStrength;
            RightInterference = -rawLeft * interferenceStrength;
        }

        private Vector2 GetLeftRawInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed) y += 1f;

            return new Vector2(x, y).normalized;
        }

        private Vector2 GetRightRawInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (keyboard.jKey.isPressed) x -= 1f;
            if (keyboard.lKey.isPressed) x += 1f;
            if (keyboard.kKey.isPressed) y -= 1f;
            if (keyboard.iKey.isPressed) y += 1f;

            return new Vector2(x, y).normalized;
        }
    }
}
