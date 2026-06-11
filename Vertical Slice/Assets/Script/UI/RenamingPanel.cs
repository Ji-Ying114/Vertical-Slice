using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RenamingPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject panelRoot;

    private Town currentTown;

    // 新增：公开只读属性，供摄像头控制器等外部脚本查询面板状态
    public bool isShown { get; private set; }

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        // 初始隐藏，并同步状态
        panelRoot.SetActive(false);
        isShown = false;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show(Town town)
    {
        currentTown = town;
        if (currentTown == null) return;

        if (nameInputField != null)
            nameInputField.text = currentTown.GetName();

        panelRoot.SetActive(true);
        isShown = true;      // 更新状态

        if (nameInputField != null)
            nameInputField.Select();
    }

    public void Hide()
    {
        currentTown = null;
        panelRoot.SetActive(false);
        isShown = false;     // 更新状态
    }

    private void OnConfirm()
    {
        if (currentTown != null && nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            currentTown.SetName(nameInputField.text.Trim());
            if (TownPanel.Instance != null)
                TownPanel.Instance.RefreshTownName();
        }
        Hide();
    }

    private void OnCancel()
    {
        Hide();
    }
}