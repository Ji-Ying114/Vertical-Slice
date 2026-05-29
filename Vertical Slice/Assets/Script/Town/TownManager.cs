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

        // 首次渲染已由 OnTownsUpdated 事件触发（订阅在 TownRenderer 中）
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

        // 刷新候选地块（如果仍处于扩张模式）
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
            ResourceProduction remaining = town.GetRemainingResource();
            remaining.foodProduction += production.foodProduction;
            remaining.materialProduction += production.materialProduction;

            while (remaining.foodProduction >= foodForGrowth)
            {
                remaining.foodProduction -= foodForGrowth;
                town.IncreasePopulation(1);
            }
            town.SetRemainingResource(remaining);
        }
        OnTownsUpdated?.Invoke();
    }
}