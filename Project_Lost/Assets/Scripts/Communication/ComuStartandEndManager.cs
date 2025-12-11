using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.DOTween;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ComuStartandEndManager : MonoBehaviour
{
    [SerializeField] private GameObject comuStartUI;
    [SerializeField] private GameObject comuEndUI;
    [SerializeField] private CanvasGroup fadeCanvas; // フェード用

    void Start()
    {
        StartComuFlow().Forget();
    }

    /// <summary>
    /// comuStartUI → 1秒後にもう一度Play
    /// </summary>
    private async UniTaskVoid StartComuFlow()
    {
        // ① 最初のPlay
        MoveOnClickandReturn callStartPanel = comuStartUI.GetComponent<MoveOnClickandReturn>();
        callStartPanel.Play();

        // ② 1秒待つ
        await UniTask.Delay(1000);

        // ③ もう一度Play
        callStartPanel.Play();
    }

    /// <summary>
    /// comuEndUI → Play() → フェード → シーン遷移(Main)
    /// </summary>
    public void ComuEnd()
    {
        EndComuFlow().Forget();
    }

    private async UniTaskVoid EndComuFlow()
    {
        MoveOnClickandReturn callEndPanel = comuEndUI.GetComponent<MoveOnClickandReturn>();

        // ① End UI のアニメを再生
        callEndPanel.Play();

        // **アニメ終了を待ちたい場合はここに wait を入れる（例: 1秒）**
        await UniTask.Delay(1000);

        // ② フェードアウト（DOTween）
        fadeCanvas.alpha = 0;
        fadeCanvas.gameObject.SetActive(true);

        await fadeCanvas
            .DOFade(1f, 1f)   // 1秒で真っ黒に
            .SetEase(Ease.Linear)
            .ToUniTask();

        // ③ シーン遷移
        SceneManager.LoadScene("Main");
    }
}
