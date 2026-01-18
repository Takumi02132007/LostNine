using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current objective text based on ProgressManager state.
/// Monitors progress changes and updates automatically.
/// </summary>
public class ObjectiveDisplay : MonoBehaviour
{
    public static ObjectiveDisplay Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private TMP_Text objectiveText;

    [Header("Objective Settings")]
    [Tooltip("List of objectives for each chapter and phase combination.")]
    [SerializeField] private List<ObjectiveEntry> objectives = new();

    [Header("Fallback")]
    [Tooltip("Text to display when no matching objective is found.")]
    [SerializeField] private string fallbackText = "---";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Subscribe to progress changes
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnProgressChanged += UpdateObjectiveText;
            // Initial update
            UpdateObjectiveText();
        }
        else
        {
            Debug.LogWarning("[ObjectiveDisplay] ProgressManager.Instance is null.");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnProgressChanged -= UpdateObjectiveText;
        }
    }

    /// <summary>
    /// Updates the objective text based on current progress.
    /// </summary>
    public void UpdateObjectiveText()
    {
        if (objectiveText == null || ProgressManager.Instance == null) return;

        int chapter = ProgressManager.Instance.CurrentChapter;
        GamePhase phase = ProgressManager.Instance.CurrentPhase;

        string text = GetObjectiveText(chapter, phase);
        objectiveText.text = text;
    }

    private string GetObjectiveText(int chapter, GamePhase phase)
    {
        foreach (var entry in objectives)
        {
            if (entry.chapter == chapter && entry.phase == phase)
            {
                return entry.objectiveText;
            }
        }
        return fallbackText;
    }

    /// <summary>
    /// Represents a single objective entry for a specific chapter and phase.
    /// </summary>
    [Serializable]
    public class ObjectiveEntry
    {
        [Tooltip("Target chapter number.")]
        public int chapter = 1;

        [Tooltip("Target game phase.")]
        public GamePhase phase = GamePhase.Prologue;

        [TextArea(2, 5)]
        [Tooltip("Objective text to display.")]
        public string objectiveText = "";
    }
}
