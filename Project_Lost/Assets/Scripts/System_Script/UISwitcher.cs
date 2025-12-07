using UnityEngine;

public class UISwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] _panel;

    // 表示ボタンに割り当てる
    public void ShowPanel(int index)
    {
        if (_panel[index] != null)
            _panel[index].SetActive(true);
    }

    // 非表示ボタンに割り当てる
    public void HidePanel(int index)
    {
        if (_panel[index] != null)
            _panel[index].SetActive(false);
    }
}
