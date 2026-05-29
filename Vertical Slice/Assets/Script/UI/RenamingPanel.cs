using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RenamingPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject panelRoot;   // 可选，如果 RenamingPanel 本身有单独的根节点

    private Town currentTown;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        // 初始隐藏
        panelRoot.SetActive(false);

        // 绑定按钮事件
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
    }

    /// <summary>
    /// 显示重命名面板并填充当前城镇名
    /// </summary>
    public void Show(Town town)
    {
        currentTown = town;
        if (currentTown == null) return;

        if (nameInputField != null)
            nameInputField.text = currentTown.GetName();

        panelRoot.SetActive(true);
        if (nameInputField != null)
            nameInputField.Select();     // 自动聚焦输入框
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        currentTown = null;
        panelRoot.SetActive(false);
    }

    private void OnConfirm()
    {
        if (currentTown != null && nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            currentTown.SetName(nameInputField.text.Trim());
            // 通知 TownPanel 刷新名字显示
            if (TownPanel.Instance != null)
                TownPanel.Instance.RefreshTownName();   // 可选方法，见下文补充
        }
        Hide();
    }

    private void OnCancel()
    {
        Hide();
    }
}