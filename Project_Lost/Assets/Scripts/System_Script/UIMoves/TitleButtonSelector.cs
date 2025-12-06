using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleButtonSelector : MonoBehaviour
{
    [Header("ボタン (このセット専用)")]
    [SerializeField] private Button[] buttons;

    [Header("選択中の背景 (ボタンとindex対応)")]
    [SerializeField] private GameObject[] highlights;
    [Header("選択後のハイライト (ボタンとindex対応)")]
    [SerializeField] private GameObject[] selectedHighlights;//選択後のハイライト

    [Space]
    [Header("Sound Settings")]
    [SerializeField] private AudioClip moveSoundClip;
    [SerializeField] private AudioClip selectSoundClip;
    [SerializeField] private float soundVolume = 1f;

    private int index = 0;

    void Start()
    {
        UpdateHighlight();
        SelectThis();  // 初期フォーカス
        ClearSelectedHighlights();  // 選択ハイライトは初期状態では非表示
    }

    void Update()
    {
        // 現在 UI フォーカスがこのボタン群以外なら、入力無視
        if (!IsFocused()) return;

        // ↓ 移動（Down / S）
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            index = (index + 1) % buttons.Length;
            ClearSelectedHighlights();
            UpdateHighlight();
            PlaySound(moveSoundClip);
        }

        // ↑ 移動（Up / W）
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            index = (index - 1 + buttons.Length) % buttons.Length;
            ClearSelectedHighlights();
            UpdateHighlight();
            PlaySound(moveSoundClip);
        }

        // 決定（Enter / F）
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            PlaySound(selectSoundClip);
            buttons[index].onClick.Invoke();
            // 選択後のハイライト表示
            ShowSelectedHighlight(index);
        }
    }

    private bool IsFocused()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;

        // このオブジェクトが持つボタン群に含まれていればフォーカス中
        foreach (var b in buttons)
        {
            if (b.gameObject == selected) return true;
        }
        return false;
    }

    private void SelectThis()
    {
        EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(i == index);
        }
        SelectThis();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void ClearSelectedHighlights()
    {
        // すべての選択後ハイライトをオフにする
        for (int i = 0; i < selectedHighlights.Length; i++)
        {
            if (selectedHighlights[i] != null)
            {
                selectedHighlights[i].SetActive(false);
            }
        }
    }

    private void ShowSelectedHighlight(int buttonIndex)
    {
        // 指定ボタンの選択後ハイライトをオン
        if (selectedHighlights.Length > buttonIndex && selectedHighlights[buttonIndex] != null)
        {
            selectedHighlights[buttonIndex].SetActive(true);
        }
    }
}
