using System;
using UnityEngine;
using Tuning.Data;

namespace Tuning.Core
{
    /// <summary>
    /// Main controller for the Tuning (調律) minigame.
    /// Manages point movement, sync calculation, penalties, and stability.
    /// </summary>
    public class TuningManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("ステージ設定")]
        [Tooltip("章ごとのステージ設定（インデックス = 章番号 - 1）")]
        [SerializeField] private TuningStageSettings[] stageSettingsList;

        [Header("ポイント")]
        [Tooltip("プレイヤーが操作する左側の点（WASD操作）")]
        [SerializeField] private RectTransform leftPoint;

        [Tooltip("プレイヤーが操作する右側の点（IJKL操作）")]
        [SerializeField] private RectTransform rightPoint;

        [Tooltip("左側の点が目指すべきターゲット円")]
        [SerializeField] private RectTransform leftTarget;

        [Tooltip("右側の点が目指すべきターゲット円")]
        [SerializeField] private RectTransform rightTarget;

        [Header("移動設定")]
        [Tooltip("入力に対する移動力（大きいほど速く動く）")]
        [SerializeField] private float moveForce = 100f;

        [Tooltip("点の最大移動速度")]
        [SerializeField] private float maxSpeed = 300f;

        [Tooltip("左側の点が移動できる範囲（X, Y, 幅, 高さ）")]
        [SerializeField] private Rect leftMovementBounds = new(-200, -200, 400, 400);

        [Tooltip("右側の点が移動できる範囲（X, Y, 幅, 高さ）")]
        [SerializeField] private Rect rightMovementBounds = new(-200, -200, 400, 400);

        [Tooltip("左側のNGゾーン範囲（この外側に出るとペナルティ）")]
        [SerializeField] private Rect leftNgZoneBounds = new(-250, -250, 500, 500);

        [Tooltip("右側のNGゾーン範囲（この外側に出るとペナルティ）")]
        [SerializeField] private Rect rightNgZoneBounds = new(-250, -250, 500, 500);

        [Header("コンポーネント")]
        [Tooltip("入力処理を行うTuningInputコンポーネント")]
        [SerializeField] private TuningInput input;

        [Tooltip("演出処理を行うTuningFeedbackコンポーネント")]
        [SerializeField] private TuningFeedback feedback;

        #endregion

        #region Events

        public event Action OnTuningSuccess;
        public event Action OnTuningGameOver;

        #endregion

        #region Private State

        private TuningStageSettings _currentSettings;
        private Vector2 _leftVelocity;
        private Vector2 _rightVelocity;
        private float _currentInertia;
        private float _overheatTimer;
        private float _stabilityGauge;
        private float _totalSync;
        private bool _isActive;

        private bool _leftInTarget;
        private bool _rightInTarget;

        #endregion

        #region Properties

        public float TotalSync => _totalSync;
        public float StabilityGauge => _stabilityGauge;
        public float OverheatProgress => _currentSettings != null ? _overheatTimer / _currentSettings.overheatThreshold : 0f;
        public bool IsActive => _isActive;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isActive || _currentSettings == null) return;

            UpdatePointMovement();
            UpdateTargetMovement();
            UpdateSyncRate();
            UpdatePenalty();
            UpdateStability();

            feedback?.OnSyncUpdate(_totalSync);
        }

        #endregion

        #region Public API

        public void Initialize()
        {
            // ProgressManagerから現在の章を取得
            int chapter = ProgressManager.Instance != null ? ProgressManager.Instance.CurrentChapter : 1;
            int index = Mathf.Clamp(chapter - 1, 0, stageSettingsList.Length - 1);
            
            if (stageSettingsList == null || stageSettingsList.Length == 0)
            {
                Debug.LogError("[TuningManager] No stage settings assigned!");
                return;
            }

            _currentSettings = stageSettingsList[index];
            if (_currentSettings == null) return;

            // 状態をリセット
            _currentInertia = _currentSettings.baseInertia;
            _overheatTimer = 0f;
            _stabilityGauge = 0f;
            _leftVelocity = Vector2.zero;
            _rightVelocity = Vector2.zero;
            _isActive = true;

            // ターゲット位置を適用
            if (leftTarget != null)
                leftTarget.anchoredPosition = _currentSettings.leftTargetPosition;
            if (rightTarget != null)
                rightTarget.anchoredPosition = _currentSettings.rightTargetPosition;

            // 入力設定を適用
            input?.Configure(_currentSettings.isInvertedLeft, _currentSettings.isInvertedRight, _currentSettings.interferenceStrength);

            Debug.Log($"[TuningManager] Initialized for Chapter {chapter}");
        }

        public void SetSettings(TuningStageSettings newSettings)
        {
            _currentSettings = newSettings;
            Initialize();
        }

        public void SetActive(bool active) => _isActive = active;

        #endregion

        #region Movement

        private void UpdatePointMovement()
        {
            if (input == null) return;

            // Get combined input (direct + interference)
            Vector2 leftForce = (input.LeftInput + input.LeftInterference) * moveForce;
            Vector2 rightForce = (input.RightInput + input.RightInterference) * moveForce;

            // Apply force with inertia (friction)
            _leftVelocity += leftForce * Time.deltaTime;
            _rightVelocity += rightForce * Time.deltaTime;

            // Apply friction
            _leftVelocity = Vector2.Lerp(_leftVelocity, Vector2.zero, _currentInertia * Time.deltaTime);
            _rightVelocity = Vector2.Lerp(_rightVelocity, Vector2.zero, _currentInertia * Time.deltaTime);

            // Clamp speed
            _leftVelocity = Vector2.ClampMagnitude(_leftVelocity, maxSpeed);
            _rightVelocity = Vector2.ClampMagnitude(_rightVelocity, maxSpeed);

            // Apply movement
            if (leftPoint != null)
            {
                Vector2 newPos = leftPoint.anchoredPosition + _leftVelocity * Time.deltaTime;
                leftPoint.anchoredPosition = ClampToBounds(newPos, leftMovementBounds);
            }

            if (rightPoint != null)
            {
                Vector2 newPos = rightPoint.anchoredPosition + _rightVelocity * Time.deltaTime;
                rightPoint.anchoredPosition = ClampToBounds(newPos, rightMovementBounds);
            }
        }

        private void UpdateTargetMovement()
        {
            if (!_currentSettings.isMovingTarget) return;

            float time = Time.time * _currentSettings.targetMoveSpeed;

            if (leftTarget != null)
            {
                float x = Mathf.Sin(time) * 50f + _currentSettings.leftTargetPosition.x;
                float y = Mathf.Cos(time * 0.7f) * 50f + _currentSettings.leftTargetPosition.y;
                leftTarget.anchoredPosition = new Vector2(x, y);
            }

            if (rightTarget != null)
            {
                float x = Mathf.Cos(time * 0.8f) * 50f + _currentSettings.rightTargetPosition.x;
                float y = Mathf.Sin(time * 1.1f) * 50f + _currentSettings.rightTargetPosition.y;
                rightTarget.anchoredPosition = new Vector2(x, y);
            }
        }

        private Vector2 ClampToBounds(Vector2 pos, Rect bounds)
        {
            return new Vector2(
                Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax)
            );
        }

        #endregion

        #region Sync Calculation

        private void UpdateSyncRate()
        {
            float leftSync = CalculatePointSync(leftPoint, leftTarget);
            float rightSync = CalculatePointSync(rightPoint, rightTarget);

            // Multiplicative: both must be good for high sync
            _totalSync = leftSync * rightSync;

            // Check target entry for feedback
            bool leftNowInTarget = leftSync > 0.9f;
            bool rightNowInTarget = rightSync > 0.9f;

            if (leftNowInTarget && !_leftInTarget)
                feedback?.OnPointInTarget(0);
            if (rightNowInTarget && !_rightInTarget)
                feedback?.OnPointInTarget(1);

            _leftInTarget = leftNowInTarget;
            _rightInTarget = rightNowInTarget;
        }

        private float CalculatePointSync(RectTransform point, RectTransform target)
        {
            if (point == null || target == null) return 0f;

            float distance = Vector2.Distance(point.anchoredPosition, target.anchoredPosition);
            float sync = 1f - Mathf.Clamp01(distance / (_currentSettings.targetTolerance * 100f));
            return sync;
        }

        #endregion

        #region Penalty System

        private void UpdatePenalty()
        {
            bool isInNGZone = IsPointInNGZone(leftPoint, leftNgZoneBounds) || 
                              IsPointInNGZone(rightPoint, rightNgZoneBounds);

            if (isInNGZone)
            {
                _currentInertia += _currentSettings.ngZonePenaltyRate * Time.deltaTime;
                _overheatTimer += Time.deltaTime;
            }
            else
            {
                _currentInertia = Mathf.Lerp(_currentInertia, _currentSettings.baseInertia, _currentSettings.penaltyRecoverySpeed * Time.deltaTime);
                _overheatTimer = Mathf.Max(0f, _overheatTimer - Time.deltaTime * 0.5f);
            }

            if (_overheatTimer >= _currentSettings.overheatThreshold)
            {
                TriggerGameOver();
            }
        }

        private bool IsPointInNGZone(RectTransform point, Rect ngBounds)
        {
            if (point == null) return false;
            Vector2 pos = point.anchoredPosition;

            // NG zone is outside safe bounds
            return pos.x < ngBounds.xMin || pos.x > ngBounds.xMax ||
                   pos.y < ngBounds.yMin || pos.y > ngBounds.yMax;
        }

        #endregion

        #region Stability

        private void UpdateStability()
        {
            if (_totalSync >= _currentSettings.syncThresholdForStability)
            {
                _stabilityGauge += _totalSync * Time.deltaTime;
            }
            else
            {
                _stabilityGauge -= _currentSettings.stabilityDecayRate * Time.deltaTime;
            }

            _stabilityGauge = Mathf.Clamp01(_stabilityGauge);

            if (_stabilityGauge >= 1f)
            {
                TriggerSuccess();
            }
        }

        #endregion

        #region Game End

        private void TriggerSuccess()
        {
            _isActive = false;
            feedback?.OnSuccess();
            OnTuningSuccess?.Invoke();
            Debug.Log("[TuningManager] Tuning Success!");
        }

        private void TriggerGameOver()
        {
            _isActive = false;
            feedback?.OnGameOver();
            OnTuningGameOver?.Invoke();
            Debug.Log("[TuningManager] Game Over - Overheat!");
        }

        #endregion
    }
}
