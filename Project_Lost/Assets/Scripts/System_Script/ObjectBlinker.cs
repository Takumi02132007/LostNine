using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class ObjectBlinker : MonoBehaviour
{
    [SerializeField] private float interval = 1f; // 1秒間隔

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        BlinkLoop().Forget();
    }

    private async UniTask BlinkLoop()
    {
        while (true)
        {
            // 表示→非表示を切り替え
            sr.DOFade(sr.color.a > 0 ? 0f : 1f, 0.1f);
            await UniTask.Delay((int)(interval * 1000));
        }
    }
}
