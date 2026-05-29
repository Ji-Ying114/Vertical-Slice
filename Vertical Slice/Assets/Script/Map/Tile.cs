using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public struct TileID
    {
        public int x;
        public int y;
    }
    public struct TileTemporalFactor
    {
        public TileFactor tileFactor;
        public int duration;
        public int passedTurns;
        public float multiplier;
    }
    public struct TileData
    {
        public TerrainType terrainType;
        public BiomeType biomeType;
        public TemperatureType temperatureType;
        public LandformType[] landformType;
        public MapResourceType mapResourceType;

        public List<TileTemporalFactor> tileTemporalFactors;
    }
    public struct MovementCost
    {
        public int forLightUnit;
        public int forMediumUnit;
        public int forHeavyUnit;
    }
    [System.Serializable]
    public struct ResourceProduction
    {
        public float foodProduction;
        public float materialProduction;
    }

public class Tile
{
    private TileID tileID;
    private TileData tileData;
    private MovementCost movementCost;
    private ResourceProduction resourceProduction;
    private int owningTown;

    public TileID GetTileID() => tileID;
    public TileData GetTileData() => tileData;
    public int GetLightUnitMovementCost() => movementCost.forLightUnit;
    public int GetMediumUnitMovementCost() => movementCost.forMediumUnit;
    public int GetHeavyUnitMovementCost() => movementCost.forHeavyUnit;
    public ResourceProduction GetResourceProduction() => resourceProduction;
    public int GetOwningTown() => owningTown;

    public Tile(TileID id)
    {
        tileID = id;
        // 先用默认值初始化 tileData，避免未赋值字段导致额外消耗计算错误
        tileData = new TileData
        {
            terrainType = TerrainType.Default,
            biomeType = BiomeType.Default,
            temperatureType = TemperatureType.Default,
            landformType = new LandformType[0],
            mapResourceType = null,
            tileTemporalFactors = new List<TileTemporalFactor>(),
        };
        // 基于默认 tileData 计算一次（后续会被 SetTileData 覆盖）
        movementCost = ComputeMovementCost();
        resourceProduction = ComputeResourceProduction();
        owningTown = -1;
    }

    public void SetTileData(TileData newTileData)
    {
        tileData = newTileData;
        // 根据新的真实数据重新计算移动消耗
        movementCost = ComputeMovementCost();
    }
    public void SetOwningTown(int town)
    {
        owningTown = town;
    }

    public void AddTileFactor(TileFactor tileFactor, int duration, float multiplier)
    {
        TileTemporalFactor factor = new TileTemporalFactor
        {
            tileFactor = tileFactor,
            duration = duration,
            passedTurns = 0,
            multiplier = multiplier,
        };
        if (tileData.tileTemporalFactors == null)
            tileData.tileTemporalFactors = new List<TileTemporalFactor> { factor };
        else
            tileData.tileTemporalFactors.Add(factor);
    }

    private MovementCost ComputeMovementCost()
    {
        int extra = 0;

        // 地形附加消耗
        switch (tileData.terrainType)
        {
            case TerrainType.Hills:    extra += 1; break;
            case TerrainType.Mountains: extra += 2; break;
        }

        // 生物群系附加消耗
        switch (tileData.biomeType)
        {
            case BiomeType.Forest: extra += 1; break;
            case BiomeType.Swamp:  extra += 2; break;
        }

        // 地貌附加消耗
        if (tileData.landformType != null)
        {
            foreach (LandformType landform in tileData.landformType)
            {
                extra += landform.additionalMovementCost;
            }
        }

        return new MovementCost
        {
            forLightUnit = 5 + extra,
            forMediumUnit = 5 + extra * 2,
            forHeavyUnit = 5 + extra * 3
        };
    }
    private ResourceProduction ComputeResourceProduction()
    {
        int food = 1;
        int material = 1;

        // further work to be done here
        return new ResourceProduction
        {
            foodProduction = food,
            materialProduction = material
        };
    }
}