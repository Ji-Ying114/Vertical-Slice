using UnityEngine;

/// <summary>
/// 生产队列中的单个项目，记录已投入的资源。
/// </summary>
[System.Serializable]
public class ProductionQueueItem
{
    public Building building;
    public UnitData unitData;
    public float investedFood;
    public float investedMaterial;

    public bool IsBuilding => building != null;
    public bool IsUnit => unitData != null;
    public string Name => IsBuilding ? building.townsName : (IsUnit ? unitData.unitName : "");
    public ResourceProduction Cost => IsBuilding ? building.cost : unitData.productionCost;
}