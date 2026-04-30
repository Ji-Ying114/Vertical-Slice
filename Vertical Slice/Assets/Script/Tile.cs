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

public class Tile
{
    private TileID tileID;
    private TileData tileData;

    public TileID GetTileID()
    {
        return tileID;
    }
    public TileData GetTileData()
    {
        return tileData;
    }

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
    }

    public void SetTileData(TileData newTileData)
    {
        tileData = newTileData;
        defaltCheck();
    }

    private void defaltCheck()
    {

    }

    public void AddTileFactor(TileFactor tileFactor, int duration, float multiplier)
    {
        TileTemporalFactor newTileTemporalFactor = new TileTemporalFactor
        {
            tileFactor = tileFactor,
            duration = duration,
            passedTurns = 0,
            multiplier = multiplier,
        };
        if (tileData.tileTemporalFactors == null)
        {
            tileData.tileTemporalFactors = new List<TileTemporalFactor> { newTileTemporalFactor };
        }
        else
        {
            tileData.tileTemporalFactors.Add(newTileTemporalFactor);
        }
    }
    // turn update for tile factors to be written in the future.
}
