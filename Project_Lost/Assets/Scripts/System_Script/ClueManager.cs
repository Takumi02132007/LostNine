using System;
using System.Collections.Generic;
using UnityEngine;

public class ClueManager : MonoBehaviour
{
    public static ClueManager Instance { get; private set; }

    // キーワードの状態管理
    // discovered: セリフ中に「見つけた（1回目）」状態
    // clicked: 実際にクリックしてメモが追加された状態
    private HashSet<string> _discovered = new HashSet<string>();
    private HashSet<string> _clicked = new HashSet<string>();

    [Header("Clue Settings")]
    [Tooltip("キーワードが最初からクリック可能か。false の場合は最初に見つける（発見）必要がある。")]
    public bool clickableImmediately = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("[ClueManager] Start() called");
        // MessageWindowManager からの自動購読は行わず、UI 側が明示的に呼び出す形にする
        if (MessageWindowSystem.Core.MessageWindowManager.Instance != null)
        {
            Debug.Log("[ClueManager] MessageWindowManager.Instance found (no auto-subscribe)");
        }
        else
        {
            Debug.LogWarning("[ClueManager] MessageWindowManager.Instance is NULL! UI will call ClueManager methods directly when available.");
        }
    }

    // ステージ切り替えなどで状態をリセットする
    public void ResetForNewStage()
    {
        _discovered.Clear();
        _clicked.Clear();
    }

    // UI から直接呼び出す：発見（発見演出のみ）
    public void DiscoverKeyword(string linkID)
    {
        if (string.IsNullOrEmpty(linkID)) return;
        if (_discovered.Contains(linkID) || _clicked.Contains(linkID)) return;

        _discovered.Add(linkID);
        Debug.Log($"[ClueManager] 発見(DiscoverKeyword): {linkID}");
        MessageWindowSystem.Core.MessageWindowManager.Instance?.SetLinkColor(linkID, "#FFFF00");
        MessageWindowSystem.Core.MessageWindowManager.Instance?.ShakeLinkVisual(linkID);
    }

    // UI から直接呼び出す：クリック確定（メモ追加および会話起動を試みる）
    public void ProcessKeywordClick(string linkID)
    {
        if (string.IsNullOrEmpty(linkID)) return;

        Debug.Log($"[ClueManager] ProcessKeywordClick: {linkID}");

        bool isClickable = clickableImmediately || _discovered.Contains(linkID);

        if (!isClickable)
        {
            // 発見扱い（まだ確定はしない）
            DiscoverKeyword(linkID);
            return;
        }

        // クリック処理: メモ追加、会話開始、視覚変化
        if (!_clicked.Contains(linkID))
        {
            _clicked.Add(linkID);
            AddNoteAutomatically(linkID);
            MessageWindowSystem.Core.MessageWindowManager.Instance?.SetLinkColor(linkID, "#888888"); // 既読色
        }

        // キーワードに関する特別会話を開始
        MessageWindowSystem.Core.MessageWindowManager.Instance?.StartKeywordConversation(linkID);
    }

    private void AddNoteAutomatically(string id)
    {
        Debug.Log($"[ClueManager] 自動メモを追加しました: {id}");
        // TODO: NotebookManager があればそちらへ登録する
        // NotebookManager.Instance?.AddClue(id, "自動追加された記憶の欠片");
    }

    private void OnDestroy()
    {
        // no auto-subscription, nothing to unsubscribe
    }
}