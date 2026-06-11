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

    // 战争迷雾
    private List<int> knownToPlayer;
    private List<int> shownToPlayer;

    public TileID GetTileID() => tileID;
    public TileData GetTileData() => tileData;
    public int GetLightUnitMovementCost() => movementCost.forLightUnit;
    public int GetMediumUnitMovementCost() => movementCost.forMediumUnit;
    public int GetHeavyUnitMovementCost() => movementCost.forHeavyUnit;
    public ResourceProduction GetResourceProduction() => resourceProduction;
    public int GetOwningTown() => owningTown;

    public bool IsKnownByPlayer(int playerIndex) => knownToPlayer.Contains(playerIndex);
    public bool IsShownByPlayer(int playerIndex) => shownToPlayer.Contains(playerIndex);

    public Tile(TileID id)
    {
        tileID = id;
        tileData = new TileData
        {
            terrainType = TerrainType.Default,
            biomeType = BiomeType.Default,
            temperatureType = TemperatureType.Default,
            landformType = new LandformType[0],
            mapResourceType = null,
            tileTemporalFactors = new List<TileTemporalFactor>(),
        };
        movementCost = ComputeMovementCost();
        resourceProduction = ComputeResourceProduction();
        owningTown = -1;

        knownToPlayer = new List<int>();
        shownToPlayer = new List<int>();
    }

    public void SetTileData(TileData newTileData)
    {
        tileData = newTileData;
        movementCost = ComputeMovementCost();
    }

    public void SetOwningTown(int town) => owningTown = town;

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

    // 迷雾操作
    public void RevealToPlayer(int playerIndex)
    {
        if (!knownToPlayer.Contains(playerIndex))
            knownToPlayer.Add(playerIndex);
    }

    public void ShowToPlayer(int playerIndex)
    {
        if (!shownToPlayer.Contains(playerIndex))
            shownToPlayer.Add(playerIndex);
    }

    public void HideFromPlayer(int playerIndex)
    {
        shownToPlayer.Remove(playerIndex);
    }

    private MovementCost ComputeMovementCost()
    {
        int extra = 0;
        switch (tileData.terrainType)
        {
            case TerrainType.Hills: extra += 1; break;
            case TerrainType.Mountains: extra += 2; break;
        }
        switch (tileData.biomeType)
        {
            case BiomeType.Forest: extra += 1; break;
            case BiomeType.Swamp: extra += 2; break;
        }
        if (tileData.landformType != null)
        {
            foreach (LandformType landform in tileData.landformType)
                extra += landform.additionalMovementCost;
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
        return new ResourceProduction { foodProduction = 1, materialProduction = 1 };
    }
}