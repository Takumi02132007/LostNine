using UnityEngine;
using DG.Tweening;

public class LostNineLogoAnimation : MonoBehaviour
{
    [Header("🔸基本揺れ設定")]
    public float shakeRange = 4f;         // 揺れ幅（UIなら3～8 / Spriteなら0.03〜0.12）
    public float shakeDuration = 0.04f;   // 揺れ1回の長さ
    public float interval = 0.15f;        // 揺れの基本発生間隔

    [Header("🔸揺れタイミングのランダム性")]
    public float intervalRandom = 0.08f;  // +−ランダム追加（0で毎回一定）

    [Header("🔸回転ノイズ")]
    public bool enableRotation = true;
    public float rotationRange = 3f;      // 回転角度（Z軸）

    [Header("🔸バーストモード（連続グリッチ）")]
    public bool enableBurst = true;
    public int burstCountMin = 2;
    public int burstCountMax = 5;
    public float burstChance = 0.18f;     // 発生確率

    Vector3 originalPos;
    Quaternion originalRot;
    Sequence seq;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
        StartGlitchLoop();
    }

    void StartGlitchLoop()
    {
        seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            // ランダムに揺らす
            Vector3 offset = new Vector3(
                Random.Range(-shakeRange, shakeRange),
                Random.Range(-shakeRange, shakeRange),
                0
            );

            transform.DOLocalMove(originalPos + offset, shakeDuration).SetEase(Ease.OutQuad);

            if (enableRotation)
            {
                float rot = Random.Range(-rotationRange, rotationRange);
                transform.DOLocalRotate(new Vector3(0, 0, rot), shakeDuration);
            }
        });

        seq.AppendInterval(shakeDuration);

        seq.AppendCallback(() =>
        {
            // 戻す
            transform.DOLocalMove(originalPos, 0.02f);
            if (enableRotation) transform.DOLocalRotateQuaternion(originalRot, 0.02f);
        });

        seq.AppendInterval(GetInterval());

        seq.AppendCallback(() =>
        {
            // バーストの抽選
            if (enableBurst && Random.value < burstChance)
                StartBurst();
        });

        seq.AppendCallback(StartGlitchLoop);
    }

    void StartBurst()
    {
        int burstCount = Random.Range(burstCountMin, burstCountMax + 1);

        for (int i = 0; i < burstCount; i++)
        {
            seq.AppendCallback(() =>
            {
                Vector3 offset = new Vector3(
                    Random.Range(-shakeRange, shakeRange),
                    Random.Range(-shakeRange, shakeRange),
                    0
                );

                transform.localPosition = originalPos + offset;

                if (enableRotation)
                {
                    float rot = Random.Range(-rotationRange, rotationRange);
                    transform.localRotation = Quaternion.Euler(0, 0, rot);
                }
            });

            seq.AppendInterval(shakeDuration * Random.Range(0.6f, 1.2f));
        }

        seq.AppendCallback(() =>
        {
            transform.localPosition = originalPos;
            if (enableRotation) transform.localRotation = originalRot;
        });
    }

    float GetInterval()
    {
        return interval + Random.Range(-intervalRandom, intervalRandom);
    }

    void OnDisable()
    {
        if (seq != null) seq.Kill();
        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
    }
}
