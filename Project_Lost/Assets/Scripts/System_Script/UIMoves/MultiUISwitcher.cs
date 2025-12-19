using UnityEngine;

[System.Serializable]
public class PanelGroup
{
    public GameObject[] panels;
}

public class MultiUISwitcher : MonoBehaviour
{
    [Header("パネルのグループ (インスペクター編集可能)")]
    [SerializeField] private PanelGroup[] panelGroups;

    public void ShowPanels(int groupIndex)
    {
        if (!IsValidGroup(groupIndex)) return;

        foreach (var panel in panelGroups[groupIndex].panels)
        {
            if (panel != null)
                panel.SetActive(true);
        }
    }

    public void HidePanels(int groupIndex)
    {
        if (!IsValidGroup(groupIndex)) return;

        foreach (var panel in panelGroups[groupIndex].panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private bool IsValidGroup(int index)
    {
        return panelGroups != null && index >= 0 && index < panelGroups.Length;
    }
}
