using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class TownPanel : MonoBehaviour
{
    public static TownPanel Instance;

    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("UI References")]
    [SerializeField] private Button nameButton;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text popText;
    [SerializeField] private TMP_Text nextPopText;
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private TMP_Text materialText;
    [SerializeField] private TMP_Text buildingText;
    [SerializeField] private Button quitButton;
    [SerializeField] private RenamingPanel renamingPanel;

    private Town currentTown;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Start()
    {
        if (nameButton != null)
            nameButton.onClick.AddListener(OnNameClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(Hide);
    }

    void Update()
    {
        if (panelRoot.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }

        if (currentTown != null && panelRoot.activeSelf)
        {
            UpdateDynamicTexts();
        }
    }

    public void ShowTown(Town town)
    {
        currentTown = town;
        Canvas.ForceUpdateCanvases();
        if (panelRoot != null)
            panelRoot.SetActive(true);

        UpdateStaticTexts();
        UpdateDynamicTexts();

        // 如果有闲置人口，进入扩张模式并显示候选地块
        if (town.AvailableExpansions > 0 && TownManager.Instance != null)
        {
            TownManager.Instance.EnterExpansionMode(town.GetID());
            if (TownRenderer.Instance != null)
                TownRenderer.Instance.ShowExpansionCandidates(town);
        }
    }

    public void Hide()
    {
        currentTown = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (TownManager.Instance != null)
            TownManager.Instance.ExitExpansionMode();

        // 只清除候选地块，不再清除领土
        if (TownRenderer.Instance != null)
            TownRenderer.Instance.ClearExpansionCandidates();
    }

    public void RefreshTownName()
    {
        if (currentTown != null && nameText != null)
            nameText.text = currentTown.GetName();
    }

    private void OnNameClicked()
    {
        if (currentTown != null && renamingPanel != null)
        {
            renamingPanel.Show(currentTown);
        }
    }

    private void UpdateStaticTexts()
    {
        if (currentTown == null) return;
        if (nameText != null)
            nameText.text = currentTown.GetName();
    }

    private void UpdateDynamicTexts()
    {
        if (currentTown == null) return;

        if (popText != null)
            popText.text = $"Population: {currentTown.GetPopulation()}";

        ResourceProduction production = currentTown.GetResourceProduction();
        if (foodText != null)
            foodText.text = $"+{production.foodProduction:F0}";
        if (materialText != null)
            materialText.text = $"+{production.materialProduction:F0}";

        if (nextPopText != null)
        {
            float foodForGrowth = TownManager.Instance != null ? TownManager.Instance.foodForGrowth : 10f;
            float currentRemaining = currentTown.GetRemainingResource().foodProduction;
            float foodProduction = production.foodProduction;

            if (foodProduction <= 0f)
            {
                nextPopText.text = "Next Pop in: ∞";
            }
            else
            {
                float needed = foodForGrowth - (currentRemaining % foodForGrowth);
                int turns = Mathf.CeilToInt(needed / foodProduction);
                nextPopText.text = $"Next Pop in: {turns} Turn(s)";
            }
        }

        if (buildingText != null)
        {
            buildingText.text = GetConstructionText();
        }
    }

    private string GetConstructionText()
    {
        return "";
    }
}