using UnityEngine;

namespace Tuning.Data
{
    /// <summary>
    /// 調律ミニゲームのステージ個別設定
    /// </summary>
    [CreateAssetMenu(fileName = "TuningStageSettings", menuName = "Tuning/Stage Settings")]
    public class TuningStageSettings : ScriptableObject
    {
        [Header("ターゲット設定")]
        [Tooltip("点がターゲットに入っているとみなす許容半径")]
        public float targetTolerance = 0.5f;

        [Tooltip("ターゲット領域が動くかどうか")]
        public bool isMovingTarget = false;

        [Tooltip("ターゲットが動く場合の速度")]
        public float targetMoveSpeed = 1f;

        [Tooltip("左ターゲットの初期位置")]
        public Vector2 leftTargetPosition = Vector2.zero;

        [Tooltip("右ターゲットの初期位置")]
        public Vector2 rightTargetPosition = Vector2.zero;

        [Header("操作設定")]
        [Tooltip("左側の操作反転フラグ")]
        public bool isInvertedLeft = false;

        [Tooltip("右側の操作反転フラグ")]
        public bool isInvertedRight = false;

        [Tooltip("左右の干渉強度 (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float interferenceStrength = 0f;

        [Tooltip("デフォルトの慣性（摩擦係数）")]
        public float baseInertia = 5f;

        [Header("ペナルティ設定")]
        [Tooltip("NGゾーン滞在時の慣性増加速度")]
        public float ngZonePenaltyRate = 2f;

        [Tooltip("NGゾーン外での慣性回復速度")]
        public float penaltyRecoverySpeed = 1f;

        [Tooltip("ゲームオーバーになるまでの限界時間")]
        public float overheatThreshold = 5f;

        [Header("安定度設定")]
        [Tooltip("安定度ゲージが上昇するために必要な同期率 (0.0-1.0)")]
        [Range(0f, 1f)]
        public float syncThresholdForStability = 0.8f;

        [Tooltip("同期率が低い時の安定度減少速度")]
        public float stabilityDecayRate = 0.2f;
    }
}
