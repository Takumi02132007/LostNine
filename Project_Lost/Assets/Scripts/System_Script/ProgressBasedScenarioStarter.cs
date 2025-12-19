using UnityEngine;
using UnityEngine.UI;
using MessageWindowSystem.Core;
using MessageWindowSystem.Data;

/// <summary>
/// Starts a scenario based on current game progress when button is clicked.
/// Attach to a UI Button.
/// </summary>
[RequireComponent(typeof(Button))]
public class ProgressBasedScenarioStarter : MonoBehaviour
{
    [Header("Scenario Lookup")]
    [Tooltip("Database containing all scenarios.")]
    [SerializeField] private ScenarioDatabase scenarioDatabase;

    [Header("Optional Override")]
    [Tooltip("If set, uses this scenario instead of progress-based lookup.")]
    [SerializeField] private DialogueScenario overrideScenario;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _button?.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        DialogueScenario scenario = overrideScenario;

        // If no override, look up by progress
        if (scenario == null && scenarioDatabase != null && ProgressManager.Instance != null)
        {
            string key = ProgressManager.Instance.GetScenarioKey();
            scenario = scenarioDatabase.GetScenarioById(key);
        }

        if (scenario != null)
        {
            MessageWindowManager.Instance?.StartScenario(scenario);
        }
        else
        {
            Debug.LogWarning($"[ProgressBasedScenarioStarter] No scenario found for current progress.");
        }
    }
}
