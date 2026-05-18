using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an abstract scriptable object for map resource types

public struct NameOnSpecificTileSettings
{   
    public string name;
    public TerrainType terrainType;
    public BiomeType biomeType;
    public TemperatureType temperatureType;
    public LandformType landformType;
}

public abstract class MapResourceType : ScriptableObject
{
    [Header("Name Settings")]
    public string defaultName;
    public NameOnSpecificTileSettings[] nameOnSpecificTileSettings;
    public LandOrMarine landOrMarine;
    public float basicGenerationChance;
    public float elevationChanceFactor;
    public float humidityChanceFactor;
    public float temperatureChanceFactor;
}
