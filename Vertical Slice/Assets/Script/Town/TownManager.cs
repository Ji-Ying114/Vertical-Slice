using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TownManager : MonoBehaviour
{
    public static TownManager Instance;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject townLabelPrefab;
    [SerializeField] private Canvas worldCanvas;

    [Header("Town Settings")]
    [SerializeField] public int foodForGrowth = 10;

    [Header("Unit Spawning")]
    [SerializeField] private List<UnitPrefabEntry> unitPrefabs;

    private List<Town> towns = new List<Town>();
    private List<GameObject> townLabelInstances = new List<GameObject>();
    private int expandingTownId = -1;

    public event System.Action OnTownsUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void EnterExpansionMode(int townId)
    {
        if (GetTown(townId) != null && GetTown(townId).AvailableExpansions > 0)
            expandingTownId = townId;
    }

    public void ExitExpansionMode()
    {
        expandingTownId = -1;
    }

    public bool IsInExpansionMode => expandingTownId != -1;

    public Town GetExpandingTown()
    {
        if (expandingTownId == -1) return null;
        return GetTown(expandingTownId);
    }

    private void Update()
    {
        if (IsInExpansionMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            MapInteractionHelper mapHelper = FindObjectOfType<MapInteractionHelper>();
            if (mapHelper == null) return;

            Vector3Int gridPos = mapHelper.GetMouseMapPosition();
            if (gridPos.x < 0 || gridPos.y < 0) return;

            TileID clickedTile = new TileID { x = gridPos.x, y = gridPos.y };
            TryExpandTown(expandingTownId, clickedTile);
        }
    }

    public Town CreateTown(string name, TileID position, int owningPlayer)
    {
        int newId = towns.Count;
        Town town = new Town(newId, name, position, owningPlayer);

        Tile centerTile = MapGenerator.Instance.GetTile(position.x, position.y);
        if (centerTile != null)
            centerTile.SetOwningTown(newId);

        town.Recalculate();
        towns.Add(town);

        if (townLabelPrefab != null && worldCanvas != null)
        {
            GameObject labelObj = Instantiate(townLabelPrefab, worldCanvas.transform);
            TownLabel label = labelObj.GetComponent<TownLabel>();
            if (label != null) label.SetTown(town);
            townLabelInstances.Add(labelObj);
        }

        OnTownsUpdated?.Invoke();
        return town;
    }

    public Town GetTown(int id) => towns.Find(t => t.GetID() == id);
    public List<Town> GetTowns() => towns;
    public Town GetTownWithAvailableExpansion()
    {
        foreach (var town in towns)
            if (town.AvailableExpansions > 0) return town;
        return null;
    }

    public bool TryExpandTown(int townId, TileID targetTile)
    {
        Town town = GetTown(townId);
        if (town == null || town.AvailableExpansions <= 0) return false;

        Tile tile = MapGenerator.Instance.GetTile(targetTile.x, targetTile.y);
        if (tile == null || tile.GetOwningTown() != -1) return false;
        if (tile.GetTileData().terrainType == TerrainType.Default) return false;

        bool isAdjacent = false;
        foreach (var owned in town.OwnedTiles)
        {
            if (AreTilesAdjacent(owned, targetTile))
            {
                isAdjacent = true;
                break;
            }
        }
        if (!isAdjacent) return false;

        tile.SetOwningTown(townId);
        town.AddOwnedTile(targetTile);
        town.Recalculate();
        ExitExpansionMode();

        if (TownRenderer.Instance != null)
            TownRenderer.Instance.ClearExpansionCandidates();

        OnTownsUpdated?.Invoke();
        return true;
    }

    private bool AreTilesAdjacent(TileID a, TileID b)
    {
        for (int i = 0; i < 6; i++)
        {
            HexDirection dir = (HexDirection)i;
            Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(a.x, a.y, dir);
            if (neighbor.x == b.x && neighbor.y == b.y)
                return true;
        }
        return false;
    }

    public bool BuildBuilding(int townId, Building building)
    {
        Town town = GetTown(townId);
        if (town == null || building == null) return false;

        ResourceProduction cost = building.cost;
        ResourceProduction remaining = town.GetRemainingResource();

        if (remaining.foodProduction < cost.foodProduction ||
            remaining.materialProduction < cost.materialProduction)
            return false;

        town.ModifyRemainingResource(-cost.foodProduction, -cost.materialProduction);
        town.AddBuilding(building);
        town.Recalculate();
        OnTownsUpdated?.Invoke();
        return true;
    }

    public void ProcessTurn()
    {
        foreach (Town town in towns)
        {
            town.Recalculate();
            ResourceProduction production = town.GetResourceProduction();
            town.ModifyRemainingResource(production.foodProduction, production.materialProduction);

            // 生产建筑/单位
            List<ProductionQueueItem> completedItems = town.ProcessProduction();
            foreach (var item in completedItems)
            {
                if (item.IsBuilding)
                {
                    town.AddBuilding(item.building);
                }
                else if (item.IsUnit)
                {
                    SpawnUnit(item.unitData, town.GetPosition());
                }
            }

            // 人口增长（基于剩余食物）
            ResourceProduction remaining = town.GetRemainingResource();
            while (remaining.foodProduction >= foodForGrowth)
            {
                remaining.foodProduction -= foodForGrowth;
                town.IncreasePopulation(1);
            }
            town.SetRemainingResource(remaining);
        }
        OnTownsUpdated?.Invoke();
    }

    private void SpawnUnit(UnitData unitData, TileID position)
    {
        if (unitData == null) return;

        // 优先按 UnitData 名称匹配预制件（避免资源实例不同导致匹配失败）
        GameObject prefab = GetUnitPrefabByName(unitData.unitName);
        if (prefab == null)
        {
            Debug.LogError($"TownManager: 找不到 UnitData '{unitData.unitName}' 对应的预制件！请在 Inspector 中为 TownManager 配置 unitPrefabs 列表。");
            return;
        }

        GameObject unitObj = Instantiate(prefab);
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"TownManager: 预制件 '{prefab.name}' 缺少 Unit 组件！");
            Destroy(unitObj);
            return;
        }

        if (unitObj.GetComponent<Selectable>() == null)
            unitObj.AddComponent<Selectable>();

        unit.InitPosition(position.x, position.y);

        if (Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log($"城镇生产了单位 {unitData.unitName} 在 ({position.x}, {position.y})");
        }
    }

    /// <summary>
    /// 通过单位名称（UnitData.unitName）查找对应的预制件
    /// </summary>
    private GameObject GetUnitPrefabByName(string unitName)
    {
        foreach (var entry in unitPrefabs)
        {
            if (entry.unitData != null && entry.unitData.unitName == unitName)
                return entry.prefab;
        }
        return null;
    }

    [System.Serializable]
    public struct UnitPrefabEntry
    {
        public UnitData unitData;
        public GameObject prefab;
    }
}