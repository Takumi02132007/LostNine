using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using MessageWindowSystem.Testing;

/// <summary>
/// Manages communication start/end UI transitions.
/// </summary>
public class ComuStartandEndManager : MonoBehaviour
{
    [SerializeField] private GameObject comuStartUI;
    [SerializeField] private GameObject comuEndUI;
    [SerializeField] private GameObject desk;
    [SerializeField] private GameObject messageWindow;
    [SerializeField] private GameObject messageWindowBackGround;
    [SerializeField] private GameObject NamePlate;
    [SerializeField] private GameObject NamePlateBackGround;
    [SerializeField] private Image fadeFrame;

    [SerializeField] private GameObject Portrait;
    
    [SerializeField] private MessageWindowIndexStarter messageWindowIndexStarter;

    [Header("Scenario IDs")]
    [Tooltip("If true, uses ProgressManager to generate scenario IDs automatically.")]
    [SerializeField] private bool useProgressBasedId = true;
    [Tooltip("Manual start ID (used if useProgressBasedId is false).")]
    [SerializeField] private string startScenarioId;
    [Tooltip("Manual end ID (used if useProgressBasedId is false).")]
    [SerializeField] private string endScenarioId;

    private bool _isInCommunication = false;
    private bool _isAnimating = false;

    public void ComuStart(string scenarioId) => StartComuFlow(scenarioId).Forget();
    public void ComuEnd(string scenarioId) => EndComuFlow(scenarioId).Forget();

    /// <summary>
    /// Toggles between Start and End communication flows.
    /// Uses Progress-based or Inspector-configured IDs. Can be called from Button.onClick.
    /// </summary>
    public void ToggleComu()
    {
        if (_isAnimating) return;

        if (_isInCommunication)
        {
            Portrait.GetComponent<Button>().onClick.Invoke();
            _isInCommunication = false;
        }
        else
        {
            Portrait.GetComponent<Button>().onClick.Invoke();
            _isInCommunication = true;
        }
    }

    public void ToggleComuforPortrait()
    {
        if (_isAnimating) return;

        string startId = GetStartScenarioId();
        string endId = GetEndScenarioId();

        if (_isInCommunication)
        {
            ComuEnd(endId);
            _isInCommunication = false;
        }
        else
        {
            ComuStart(startId);
            _isInCommunication = true;
        }
    }

    private string GetStartScenarioId()
    {
        if (useProgressBasedId && ProgressManager.Instance != null)
            return ProgressManager.Instance.GetScenarioKey();
        return startScenarioId;
    }

    private string GetEndScenarioId()
    {
        if (useProgressBasedId && ProgressManager.Instance != null)
            return $"Ch{ProgressManager.Instance.CurrentChapter}_loop";
        return endScenarioId;
    }

    /// <summary>
    /// Overload with explicit IDs (for script calls).
    /// </summary>
    public void ToggleComu(string startId, string endId)
    {
        if (_isAnimating) return;

        if (_isInCommunication)
        {
            ComuEnd(endId);
            _isInCommunication = false;
        }
        else
        {
            ComuStart(startId);
            _isInCommunication = true;
        }
    }

    private async UniTaskVoid StartComuFlow(string Startid)
    {
        _isAnimating = true;
        SetPortraitInteractable(false);

        var fadeAnim = fadeFrame.GetComponent<MoveOnClickandReturn>();
        fadeFrame.gameObject.SetActive(true);
        NamePlate.SetActive(false);
        messageWindow.SetActive(false);
        messageWindowBackGround.SetActive(false);
        NamePlateBackGround.SetActive(false);

        PlayDeskAnimation();
        fadeAnim.Play();

        await UniTask.Delay(1000);

        var startPanel = comuStartUI.GetComponent<MoveOnClickandReturn>();
        startPanel.Play();

        await UniTask.Delay(1500);

        startPanel.Play();
        PlayDeskAnimation();

        await UniTask.Delay(500);

        NamePlate.SetActive(true);
        messageWindow.SetActive(true);
        messageWindowBackGround.SetActive(true);
        NamePlateBackGround.SetActive(true);

        fadeFrame.gameObject.SetActive(false);

        _isAnimating = false;
        SetPortraitInteractable(true);

        messageWindowIndexStarter.StartScenarioById(Startid);
    }

    private async UniTaskVoid EndComuFlow(string Endid)
    {
        _isAnimating = true;
        SetPortraitInteractable(false);

        var endPanel = comuEndUI.GetComponent<MoveOnClickandReturn>();
        var fadeAnim = fadeFrame.GetComponent<MoveOnClickandReturn>();
        endPanel.Play();
        NamePlate.SetActive(false);
        messageWindow.SetActive(false);
        messageWindowBackGround.SetActive(false);
        NamePlateBackGround.SetActive(false);
        fadeFrame.gameObject.SetActive(true);
        fadeAnim.Play();

        await UniTask.Delay(1500);

        endPanel.Play();
        NamePlate.SetActive(true);
        messageWindow.SetActive(true);
        messageWindowBackGround.SetActive(true);
        NamePlateBackGround.SetActive(true);
        fadeFrame.gameObject.SetActive(false);

        _isAnimating = false;
        SetPortraitInteractable(true);

        messageWindowIndexStarter.StartScenarioById(Endid);
    }

    private void PlayDeskAnimation()
    {
        if (desk.TryGetComponent<MoveOnClickandReturn>(out var move)) move.Play();
        else if (desk.TryGetComponent<FirstMove>(out var first)) first.Play();
    }

    private void SetPortraitInteractable(bool interactable)
    {
        if (Portrait != null && Portrait.TryGetComponent<Button>(out var btn))
            btn.interactable = interactable;
    }
}
