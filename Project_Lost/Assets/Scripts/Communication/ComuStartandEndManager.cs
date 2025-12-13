using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;

public class ComuStartandEndManager : MonoBehaviour
{
    [SerializeField] private GameObject comuStartUI;
    [SerializeField] private GameObject comuEndUI;
    [SerializeField] private Image fadeFlame; // フェード用

    public void ComuStart()
    {
        StartComuFlow().Forget();
    }

    /// <summary>
    /// comuStartUI → 1秒後にもう一度Play
    /// </summary>
    private async UniTaskVoid StartComuFlow()
    {
        // ① 最初のPlay
        MoveOnClickandReturn fadeBlackFlame = fadeFlame.GetComponent<MoveOnClickandReturn>();
        fadeFlame.gameObject.SetActive(true);
        fadeBlackFlame.Play();

        await UniTask.Delay(1000);
        
        MoveOnClickandReturn callStartPanel = comuStartUI.GetComponent<MoveOnClickandReturn>();
        callStartPanel.Play();

        // ② 1秒待つ
        await UniTask.Delay(1500);

        // ③ もう一度Play
        callStartPanel.Play();

        await UniTask.Delay(500);

        fadeFlame.gameObject.SetActive(false);
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

        // ① End UI のアニメ再生
        callEndPanel.Play();

        await UniTask.Delay(1500);

        // ② フェードパネルを前面に表示
        callEndPanel.Play(); 
    }

}
