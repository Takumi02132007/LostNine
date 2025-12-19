using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class BlinkerUI : MonoBehaviour
{
    [SerializeField] private float interval = 1f; // 点滅間隔
    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    private void OnEnable()
    {
        BlinkLoop().Forget();
    }

    private async UniTask BlinkLoop()
    {
        while (true)
        {
            // フェード切り替え（見えてる→消える / 消えてる→見える）
            img.DOFade(img.color.a > 0f ? 0f : 1f, 0.1f);

            await UniTask.Delay((int)(interval * 1000));
        }
    }
}
