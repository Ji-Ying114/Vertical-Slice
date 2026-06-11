using System.Collections.Generic;
using UnityEngine;

public class Town
{
    private int ID;
    private int owningPlayer;
    private string name;
    private int population;
    private TileID position;
    private ResourceProduction resourceProduction;
    private ResourceProduction remainingResource;
    private List<Building> buildings;
    private List<TileID> ownedTiles;
    private List<ProductionQueueItem> productionQueue = new List<ProductionQueueItem>();

    public int GetID() => ID;
    public int GetOwningPlayer() => owningPlayer;
    public string GetName() => name;
    public int GetPopulation() => population;
    public TileID GetPosition() => position;
    public ResourceProduction GetResourceProduction() => resourceProduction;
    public ResourceProduction GetRemainingResource() => remainingResource;
    public List<Building> GetBuildings() => buildings;
    public IReadOnlyList<TileID> OwnedTiles => ownedTiles;
    public IReadOnlyList<ProductionQueueItem> ProductionQueue => productionQueue;

    public int AvailableExpansions => population - ownedTiles.Count;

    public Town(int id, string name, TileID position, int owningPlayer)
    {
        this.ID = id;
        this.name = name;
        this.position = position;
        this.owningPlayer = owningPlayer;
        this.population = 1;
        this.buildings = new List<Building>();
        this.remainingResource = new ResourceProduction();
        this.ownedTiles = new List<TileID> { position };
    }

    public void Recalculate()
    {
        if (MovementHelper.Instance == null || MapGenerator.Instance == null)
            return;

        List<TileID> tilesToCheck = MovementHelper.Instance.GetTilesInRange(position, 5);
        resourceProduction = new ResourceProduction();
        foreach (TileID tileID in tilesToCheck)
        {
            Tile tile = MapGenerator.Instance.GetTile(tileID.x, tileID.y);
            if (tile == null) continue;
            if (tile.GetOwningTown() != ID) continue;
            ResourceProduction tileProduction = tile.GetResourceProduction();
            resourceProduction.foodProduction += tileProduction.foodProduction;
            resourceProduction.materialProduction += tileProduction.materialProduction;
        }

        foreach (Building building in buildings)
        {
            resourceProduction.foodProduction += building.production.foodProduction;
            resourceProduction.materialProduction += building.production.materialProduction;
            resourceProduction.foodProduction -= building.maintenance.foodProduction;
            resourceProduction.materialProduction -= building.maintenance.materialProduction;
        }
    }

    public void IncreasePopulation(int amount) => population += amount;

    public void ModifyRemainingResource(float foodDelta, float materialDelta)
    {
        remainingResource.foodProduction += foodDelta;
        remainingResource.materialProduction += materialDelta;
    }

    public void SetRemainingResource(ResourceProduction rp) => remainingResource = rp;

    public void AddOwnedTile(TileID tileID) => ownedTiles.Add(tileID);

    public void resetOwner(int player) => this.owningPlayer = player;

    public void SetName(string newName) => this.name = newName;

    public void AddBuilding(Building building) => this.buildings.Add(building);

    public void RemoveBuilding(Building building)
    {
        if (this.buildings.Contains(building))
            this.buildings.RemoveAt(this.buildings.IndexOf(building));
        else
            Debug.LogWarning("Building not found in town's buildings list.");
    }

    // ========== 生产系统 ==========

    public void EnqueueProduction(Building building)
    {
        productionQueue.Add(new ProductionQueueItem { building = building });
    }

    public void EnqueueProduction(UnitData unitData)
    {
        productionQueue.Add(new ProductionQueueItem { unitData = unitData });
    }

    /// <summary>
    /// 取消当前正在生产的项目（保留已投入的进度）
    /// </summary>
    public void CancelCurrentProduction()
    {
        if (productionQueue.Count > 0)
            productionQueue.RemoveAt(0);
    }

    /// <summary>
    /// 每回合处理生产，消耗剩余资源投入队列中的第一个项目。
    /// 返回本回合完成的项目列表（建筑或单位）。
    /// </summary>
    public List<ProductionQueueItem> ProcessProduction()
    {
        List<ProductionQueueItem> completed = new List<ProductionQueueItem>();

        while (productionQueue.Count > 0)
        {
            ProductionQueueItem item = productionQueue[0];
            ResourceProduction cost = item.Cost;

            float needFood = cost.foodProduction - item.investedFood;
            float needMaterial = cost.materialProduction - item.investedMaterial;

            float availFood = remainingResource.foodProduction;
            float availMaterial = remainingResource.materialProduction;

            float investFood = Mathf.Min(needFood, availFood);
            float investMaterial = Mathf.Min(needMaterial, availMaterial);

            item.investedFood += investFood;
            item.investedMaterial += investMaterial;
            remainingResource.foodProduction -= investFood;
            remainingResource.materialProduction -= investMaterial;

            // 检查是否完成
            if (item.investedFood >= cost.foodProduction &&
                item.investedMaterial >= cost.materialProduction)
            {
                completed.Add(item);
                productionQueue.RemoveAt(0);
            }
            else
            {
                // 资源不足以继续推进本项，下回合再试
                break;
            }
        }

        return completed;
    }
    // 在 Town 类的末尾添加以下方法

/// <summary>
/// 将指定建筑设为当前生产项目（插入队列最前端，原第一项保留进度后排）
/// </summary>
public void SetCurrentProduction(Building building)
{
    // 如果队列第一个已经是同类型建筑，不做任何事
    if (productionQueue.Count > 0 && productionQueue[0].IsBuilding && productionQueue[0].building == building)
        return;

    // 将新项目插入到队列最前端
    productionQueue.Insert(0, new ProductionQueueItem { building = building });
}

/// <summary>
/// 将指定单位设为当前生产项目（插入队列最前端，原第一项保留进度后排）
/// </summary>
public void SetCurrentProduction(UnitData unitData)
{
    if (productionQueue.Count > 0 && productionQueue[0].IsUnit && productionQueue[0].unitData == unitData)
        return;

    productionQueue.Insert(0, new ProductionQueueItem { unitData = unitData });
}

/// <summary>
/// 获取当前正在生产的项目（队列第一项），若队列为空返回 null
/// </summary>
public ProductionQueueItem GetCurrentProduction()
{
    return productionQueue.Count > 0 ? productionQueue[0] : null;
}
}