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
    private List<TileID> ownedTiles;          // 新增：所有已拥有的地块（含市中心）

    public int GetID() => ID;
    public int GetOwningPlayer() => owningPlayer;
    public string GetName() => name;
    public int GetPopulation() => population;
    public TileID GetPosition() => position;
    public ResourceProduction GetResourceProduction() => resourceProduction;
    public ResourceProduction GetRemainingResource() => remainingResource;
    public List<Building> GetBuildings() => buildings;
    public IReadOnlyList<TileID> OwnedTiles => ownedTiles;

    // 闲置人口（可用于扩张的数量）
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
        this.ownedTiles = new List<TileID> { position };   // 初始只有一个市中心地块
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
}