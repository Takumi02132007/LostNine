using System;
using UnityEngine;

/// <summary>
/// Singleton that manages game progress (chapter and phase).
/// </summary>
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    [Header("Current Progress")]
    [SerializeField] private int _currentChapter = 1;
    [SerializeField] private GamePhase _currentPhase = GamePhase.Prologue;

    public int CurrentChapter => _currentChapter;
    public GamePhase CurrentPhase => _currentPhase;

    public event Action OnProgressChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the current chapter and phase.
    /// </summary>
    public void SetProgress(int chapter, GamePhase phase)
    {
        _currentChapter = chapter;
        _currentPhase = phase;
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Advances to the next phase within the current chapter.
    /// </summary>
    public void AdvancePhase()
    {
        int phaseCount = Enum.GetValues(typeof(GamePhase)).Length;
        int nextPhase = ((int)_currentPhase + 1) % phaseCount;
        
        if (nextPhase == 0)
        {
            _currentChapter++;
        }
        
        _currentPhase = (GamePhase)nextPhase;
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Advances to the next chapter (starts at Prologue).
    /// </summary>
    public void AdvanceChapter()
    {
        _currentChapter++;
        _currentPhase = GamePhase.Prologue;
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Returns a string key for scenario lookup (e.g., "Ch1_Dialogue").
    /// </summary>
    public string GetScenarioKey() => $"Ch{_currentChapter}_{_currentPhase}";
}

/// <summary>
/// Game phases within each chapter.
/// </summary>
public enum GamePhase
{
    Prologue,     // プロローグ
    Dialogue,     // 対話
    Extraction,   // 抽出
    Tuning,       // 調律
    Fixation,     // 定着
    Presentation, // 提示
    Epilogue      // エピローグ
}
