using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public struct BuildingButtonEntry
{
    public Button button;
    public Building building;
}

[System.Serializable]
public struct UnitButtonEntry
{
    public Button button;
    public UnitData unitData;
}

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

    [Header("Production Buttons")]
    [SerializeField] private List<BuildingButtonEntry> buildingButtons = new List<BuildingButtonEntry>();
    [SerializeField] private List<UnitButtonEntry> unitButtons = new List<UnitButtonEntry>();

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

        // 为所有生产按钮绑定事件
        foreach (var entry in buildingButtons)
        {
            if (entry.button != null)
                entry.button.onClick.AddListener(() => OnBuildingButtonClicked(entry.building));
        }
        foreach (var entry in unitButtons)
        {
            if (entry.button != null)
                entry.button.onClick.AddListener(() => OnUnitButtonClicked(entry.unitData));
        }
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
        if (panelRoot != null)
            panelRoot.SetActive(true);

        UpdateStaticTexts();
        UpdateDynamicTexts();
        UpdateProductionButtonsHighlight();

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

    private void OnBuildingButtonClicked(Building building)
    {
        if (currentTown == null) return;
        currentTown.SetCurrentProduction(building);
        UpdateProductionButtonsHighlight();
    }

    private void OnUnitButtonClicked(UnitData unitData)
    {
        if (currentTown == null) return;
        currentTown.SetCurrentProduction(unitData);
        UpdateProductionButtonsHighlight();
    }

    /// <summary>
    /// 更新按钮高亮：当前正在生产的项目对应的按钮设为不可交互（或改变颜色）
    /// </summary>
    private void UpdateProductionButtonsHighlight()
    {
        if (currentTown == null) return;

        ProductionQueueItem currentProd = currentTown.GetCurrentProduction();

        foreach (var entry in buildingButtons)
        {
            if (entry.button == null) continue;
            bool isCurrent = currentProd != null && currentProd.IsBuilding && currentProd.building == entry.building;
            entry.button.interactable = !isCurrent; // 或者保留可交互但改变颜色，这里用不可交互表示“正在建造”
        }
        foreach (var entry in unitButtons)
        {
            if (entry.button == null) continue;
            bool isCurrent = currentProd != null && currentProd.IsUnit && currentProd.unitData == entry.unitData;
            entry.button.interactable = !isCurrent;
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
        if (currentTown == null) return "";
        var prod = currentTown.GetCurrentProduction();
        if (prod == null) return "";
        return $"Building: {prod.Name} (F:{prod.investedFood}/{prod.Cost.foodProduction} M:{prod.investedMaterial}/{prod.Cost.materialProduction})";
    }
}