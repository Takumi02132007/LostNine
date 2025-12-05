using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleButtonSelector : MonoBehaviour
{
    [Header("ボタン (このセット専用)")]
    [SerializeField] private Button[] buttons;

    [Header("選択中の背景 (ボタンとindex対応)")]
    [SerializeField] private GameObject[] highlights;

    private int index = 0;

    void Start()
    {
        UpdateHighlight();
        SelectThis();  // 初期フォーカス
    }

    void Update()
    {
        // 現在 UI フォーカスがこのボタン群以外なら、入力無視
        if (!IsFocused()) return;

        // ↓ 移動（Down / S）
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            index = (index + 1) % buttons.Length;
            UpdateHighlight();
        }

        // ↑ 移動（Up / W）
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            index = (index - 1 + buttons.Length) % buttons.Length;
            UpdateHighlight();
        }

        // 決定（Enter / F）
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            buttons[index].onClick.Invoke();
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
}
