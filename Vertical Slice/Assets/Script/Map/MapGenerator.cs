using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TerrainType
{
    Default,
    Plains,
    Hills,
    Mountains,
}

public enum BiomeType
{
    Default,
    Swamp,
    Forest,
    Grassland,
    Desert,
}

public enum TemperatureType
{   
    Default,
    Frigid,
    Temperate,
    Tropical,
}

[System.Serializable]
internal struct EnabledTerrainSettings
{   
    public TerrainType terrainType;
}

[System.Serializable]
internal struct EnabledMapResourceSettings
{   
    public MapResourceType mapResourceType;
    public float mapResourceChance;
}

[System.Serializable]
internal struct EnabledLandformSettings
{   
    public LandformType landformType;
    public float landformChance;
}

[System.Serializable]
public struct GenerationValues
{
    public float seaLevel;
    public float plainLevel;
    public float hillLevel;

    public float frigidLevel;
    public float temperateLevel;

    public float desertLevel;
    public float grasslandLevel;
    public float forestLevel;
}

public class MapGenerator : MonoBehaviour
{  
    public static MapGenerator Instance;

    private System.Random mapRandom;

    [SerializeField] private Grid grid;

    [SerializeField] public GenerationValues generationValues;

    [SerializeField] private float LandformFrequency;
    [SerializeField] private float LinearLandformLengthFactor;

    [SerializeField] private float mapResourceFrequency;
    [SerializeField] private EnabledMapResourceSettings[] enabledMapResourceSettings;
    [SerializeField] private EnabledLandformSettings[] enabledLandformSettings;
    
    [SerializeField] private bool fixedSeed;
    [SerializeField] private int setSeed;
    [SerializeField] private float noiseScale;
    
    [SerializeField] private int fbmOctaves = 4;
    [SerializeField] private float fbmPersistence = 0.5f;
    [SerializeField] private float fbmLacunarity = 2f;

    private int seed;
    private int mapWidth;
    private int mapHeight;
    public Tile[,] tiles;
    

    private float[,] cachedElevationMap;
    private float[,] cachedTemperatureMap;
    private float[,] cachedHumidityMap;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetMapWidth()
    {
        return mapWidth;
    }
    public int GetMapHeight()
    {
        return mapHeight;
    }
    public int GetSeed()
    {
        return seed;
    }
    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            Debug.LogWarning($"GetTile: Coordinates ({x}, {y}) are out of bounds.");
            return null;
        }
        return tiles[x, y];
    }
    public Vector3 worldPosition(int x, int y)
    {
        return grid.GetCellCenterWorld(new Vector3Int(x, y, 0));
    }

    public void GenerateMap(int width, int height)
    {
        seed = fixedSeed ? setSeed : System.DateTime.Now.Millisecond;
        mapRandom = new System.Random(seed);
        
        mapWidth = width;
        mapHeight = height;
        
        tiles = new Tile[mapWidth, mapHeight];
        
        cachedElevationMap = GenerateElevationMap();
        cachedTemperatureMap = GenerateTemperatureMap();
        cachedHumidityMap = GenerateHumidityMap(cachedElevationMap);
        
        Dictionary<(int, int), List<LandformType>> landformDict = GenerateLandforms(cachedElevationMap, cachedTemperatureMap, cachedHumidityMap);
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float elevation = cachedElevationMap[x, y];
                float temperature = cachedTemperatureMap[x, y];
                float humidity = cachedHumidityMap[x, y];
                
                TileID tileID = new TileID { x = x, y = y };
                Tile newTile = new Tile(tileID);
                
                TileData tileData = new TileData
                {
                    terrainType = TerrainType.Default,
                    biomeType = BiomeType.Default,
                    temperatureType = TemperatureType.Default,
                    landformType = new LandformType[0],
                    tileTemporalFactors = new List<TileTemporalFactor>(),
                };
                
                tileData.terrainType = DetermineTerrainType(elevation);
                tileData.temperatureType = DetermineTemperatureType(temperature);
                tileData.biomeType = DetermineBiomeType(temperature, humidity);
                tileData.mapResourceType = GenerateMapResource(x, y, cachedElevationMap, cachedTemperatureMap, cachedHumidityMap, tileData);
                
                List<LandformType> tileLandforms = new List<LandformType>();
                var key = (x, y);
                if (landformDict.ContainsKey(key))
                {
                    tileLandforms.AddRange(landformDict[key]);
                }
                
                tileData.landformType = tileLandforms.ToArray();
                
                newTile.SetTileData(tileData);
                tiles[x, y] = newTile;
                if (Console.Instance.debugMode == DebugMode.On)
                {
                    Debug.Log($"Tile ({x},{y}): Elevation={elevation:F2}, Temperature={temperature:F2}, Humidity={humidity:F2}, Terrain={tileData.terrainType}, Biome={tileData.biomeType}, TempType={tileData.temperatureType}, Landforms={string.Join(",", (object[])tileData.landformType)}, Resource={(tileData.mapResourceType != null ? tileData.mapResourceType.name : "None")}");
                }
            }
        }
    }

    public void RegenerateMap(int width = -1, int height = -1)
    {
        if (width == -1 && height == -1)
        {
            if (tiles == null || tiles.Length == 0)
            {
                Debug.LogWarning("No existing map to regenerate. Please use GenerateMap() with parameters.");
                return;
            }
            GenerateMap(mapWidth, mapHeight);
        }
        else if (height == -1)
        {
            GenerateMap(width, width);
        }
        else
        {
            GenerateMap(width, height);
        }
    }
    
    public int GetMapSeed()
    {
        return seed;
    }

    private float GenerateFBMNoise(int x, int y, float baseScale, float offsetX, float offsetY)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;
        
        for (int i = 0; i < fbmOctaves; i++)
        {
            float sampleX = (x * baseScale * frequency) + offsetX;
            float sampleY = (y * baseScale * frequency) + offsetY;
            
            value += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
            maxValue += amplitude;
            
            amplitude *= fbmPersistence;
            frequency *= fbmLacunarity;
        }
        
        return Mathf.Clamp01(value / maxValue);
    }

    private TerrainType DetermineTerrainType(float elevation)
    {
        if (elevation < generationValues.seaLevel)
        {
            return TerrainType.Default;
        }
        else if (elevation < generationValues.plainLevel)
        {
            return TerrainType.Plains;
        }
        else if (elevation < generationValues.hillLevel)
        {
            return TerrainType.Hills;
        }
        else
        {
            return TerrainType.Mountains;
        }
    }

    private BiomeType DetermineBiomeType(float temperature, float humidity)
    {
        if (humidity < generationValues.desertLevel)
        {
            return BiomeType.Desert;
        }
        else if (humidity < generationValues.grasslandLevel)
        {
            return BiomeType.Grassland;
        }
        else if (humidity < generationValues.forestLevel)
        {
            return BiomeType.Forest;
        }
        else
        {
            return BiomeType.Swamp;
        }
    }

    private TemperatureType DetermineTemperatureType(float temperature)
    {
        if (temperature < generationValues.frigidLevel)
            return TemperatureType.Frigid;
        else if (temperature < generationValues.temperateLevel)
            return TemperatureType.Temperate;
        else
            return TemperatureType.Tropical;
    }

    private MapResourceType GenerateMapResource(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, TileData tileData)
    {
        float resourceChance = (float)mapRandom.NextDouble();
        
        if (resourceChance >= mapResourceFrequency)
        {
            return null;
        }
        
        float currentElevation = elevationMap[x, y];
        bool isWater = currentElevation < generationValues.seaLevel;
        
        List<MapResourceType> candidates = new List<MapResourceType>();
        List<float> weights = new List<float>();
        
        foreach (var resourceSetting in enabledMapResourceSettings)
        {
            var resourceType = resourceSetting.mapResourceType;
            
            if (isWater && resourceType.landOrMarine == LandOrMarine.Land)
                continue;
            if (!isWater && resourceType.landOrMarine == LandOrMarine.Marine)
                continue;
            
            float weight = resourceSetting.mapResourceChance * resourceType.basicGenerationChance;
            
            float elevationModifier = EvaluateChanceCoefficient(currentElevation, resourceType.elevationChanceFactor);
            float humidityModifier = EvaluateChanceCoefficient(humidityMap[x, y], resourceType.humidityChanceFactor);
            float temperatureModifier = EvaluateChanceCoefficient(temperatureMap[x, y], resourceType.temperatureChanceFactor);
            
            weight *= elevationModifier * humidityModifier * temperatureModifier;
            
            if (weight <= 0f)
                continue;
            
            candidates.Add(resourceType);
            weights.Add(weight);
        }
        
        if (candidates.Count == 0)
        {
            return null;
        }
        
        float totalWeight = 0f;
        foreach (float w in weights)
        {
            totalWeight += w;
        }
        
        if (totalWeight <= 0f)
            return null;
        
        float randomValue = (float)mapRandom.NextDouble() * totalWeight;
        float cumulativeWeight = 0f;
        
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                return candidates[i];
            }
        }
        
        return candidates[candidates.Count - 1];
    }

    private float EvaluateChanceCoefficient(float value, float factor)
    {
        float normalizedValue = Mathf.Clamp01(value);
        float coefficient = factor * (normalizedValue - 0.5f) + 1f;
        return Mathf.Max(0f, coefficient);
    }

    private float[,] GenerateElevationMap()
    {
        float[,] elevationMap = new float[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                elevationMap[x, y] = GenerateFBMNoise(x, y, noiseScale, seed * 0.1f, seed * 0.1f);
            }
        }
        if (Console.Instance.debugMode == DebugMode.On)
        {
            string mapOutput = "Elevation Map:\n";
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < mapWidth; x++)
                {
                    row += elevationMap[x, y].ToString("F2") + " ";
                }
                mapOutput += row + "\n";
            }
            Debug.Log(mapOutput);
        }
        return elevationMap;
    }

    private float[,] GenerateTemperatureMap()
    {
        float[,] temperatureMap = new float[mapWidth, mapHeight];
        float equatorY = mapHeight / 2f;
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float noiseValue = GenerateFBMNoise(x, y, noiseScale, seed * 0.1f + 100f, seed * 0.1f + 100f);
                float latitudeFactor = 1f - Mathf.Abs(y - equatorY) / equatorY;
                temperatureMap[x, y] = Mathf.Clamp01(noiseValue * latitudeFactor);
            }
        }
        if (Console.Instance.debugMode == DebugMode.On){
            string mapOutput = "Temperature Map:\n";
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < mapWidth; x++)
                {
                    row += temperatureMap[x, y].ToString("F2") + " ";
                }
                mapOutput += row + "\n";
            }
            Debug.Log(mapOutput);
        }
        return temperatureMap;
    }

    private float[,] GenerateHumidityMap(float[,] elevationMap)
    {
        float[,] humidityMap = new float[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float noiseValue = GenerateFBMNoise(x, y, noiseScale, seed * 0.1f + 200f, seed * 0.1f + 200f);
                float elevationModifier = 1f - elevationMap[x, y];
                humidityMap[x, y] = Mathf.Clamp01(noiseValue * (1f + elevationModifier));
            }
        }
        if (Console.Instance.debugMode == DebugMode.On){
            string mapOutput = "Humidity Map:\n";
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < mapWidth; x++)
                {
                    row += humidityMap[x, y].ToString("F2") + " ";
                }
                mapOutput += row + "\n";
            }
            Debug.Log(mapOutput);
        }
        return humidityMap;
    }

    private Dictionary<(int, int), List<LandformType>> GenerateLandforms(float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap)
    {
        Dictionary<(int, int), List<LandformType>> landformDict = new Dictionary<(int, int), List<LandformType>>();
        bool[,] processedCells = new bool[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (processedCells[x, y])
                    continue;
                
                float elevation = elevationMap[x, y];
                float temperature = temperatureMap[x, y];
                float humidity = humidityMap[x, y];
                
                LandformType landform = SelectLandform(elevation, temperature, humidity);
                
                if (landform == null)
                    continue;

                if (landform is LinearLandform linearLandform)
                {
                    List<(int, int, LandformType)> pathCells = GenerateLinearPath(x, y, elevationMap, temperatureMap, humidityMap, processedCells, linearLandform);
                    foreach (var (px, py, l) in pathCells)
                    {
                        var key = (px, py);
                        if (!landformDict.ContainsKey(key))
                            landformDict[key] = new List<LandformType>();
                        landformDict[key].Add(l);
                    }
                }
                else if (landform is SpottedLandform spottedLandform)
                {
                    List<(int, int, LandformType)> patchCells = GenerateSpottedPatch(x, y, elevationMap, temperatureMap, humidityMap, processedCells, spottedLandform);
                    foreach (var (px, py, l) in patchCells)
                    {
                        var key = (px, py);
                        if (!landformDict.ContainsKey(key))
                            landformDict[key] = new List<LandformType>();
                        landformDict[key].Add(l);
                    }
                }
            }
        }
        if (Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log("Landform Distribution:");
            for (int y = mapHeight - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < mapWidth; x++)
                {
                    var key = (x, y);
                    if (landformDict.ContainsKey(key))
                    {
                        row += landformDict[key].Count + " ";
                    }
                    else
                    {
                        row += "0 ";
                    }
                }
                Debug.Log(row);
            }
        }
        return landformDict;
    }

    private LandformType SelectLandform(float elevation, float temperature, float humidity)
    {
        if (enabledLandformSettings == null || enabledLandformSettings.Length == 0)
            return null;

        List<LandformType> candidates = new List<LandformType>();
        List<float> weights = new List<float>();

        foreach (var setting in enabledLandformSettings)
        {
            if (setting.landformType == null)
                continue;

            LandformType landform = setting.landformType;
            
            float baseChance = LandformFrequency * setting.landformChance;
            
            float elevationModifier = EvaluateChanceCoefficient(elevation, landform.elevationChanceFactor);
            float humidityModifier = EvaluateChanceCoefficient(humidity, landform.humidityChanceFactor);
            float temperatureModifier = EvaluateChanceCoefficient(temperature, landform.temperatureChanceFactor);
            
            float weight = baseChance * elevationModifier * humidityModifier * temperatureModifier;
            
            if (weight <= 0f)
                continue;

            candidates.Add(landform);
            weights.Add(weight);
        }

        if (candidates.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (float w in weights)
            totalWeight += w;

        float randomValue = (float)mapRandom.NextDouble() * totalWeight;
        float cumulativeWeight = 0f;

        LandformType selectedLandform = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                selectedLandform = candidates[i];
                break;
            }
        }

        return selectedLandform ?? candidates[candidates.Count - 1];
    }

    private List<(int x, int y, LandformType landform)> GenerateLinearPath(int startX, int startY, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, bool[,] processedCells, LinearLandform landform)
    {
        List<(int x, int y, LandformType)> pathCells = new List<(int, int, LandformType)>();
        
        if (landform == null)
            return pathCells;

        int currentX = startX;
        int currentY = startY;
        int maxSteps = Mathf.RoundToInt((5f + (float)mapRandom.NextDouble() * 15f) * LinearLandformLengthFactor);
        
        for (int step = 0; step < maxSteps; step++)
        {
            if (currentX < 0 || currentX >= mapWidth || currentY < 0 || currentY >= mapHeight)
                break;
            
            processedCells[currentX, currentY] = true;
            pathCells.Add((currentX, currentY, landform));
            
            // 使用 DirectionHelper 获取六边形邻居
            List<Vector2Int> vecNeighbors = DirectionHelper.Instance.GetAllValidNeighbors(currentX, currentY);
            List<(int x, int y)> neighbors = new List<(int, int)>();
            foreach (var v in vecNeighbors) neighbors.Add((v.x, v.y));
            
            if (!CheckSurroundingConditions(neighbors, elevationMap, temperatureMap, humidityMap, landform))
                break;
            
            (int nextX, int nextY) = SelectNextDirection(neighbors, elevationMap, temperatureMap, humidityMap, landform);
            
            if (nextX == -1 || nextY == -1)
                break;
            
            currentX = nextX;
            currentY = nextY;
        }
        
        return pathCells;
    }

    private List<(int x, int y, LandformType landform)> GenerateSpottedPatch(int startX, int startY, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, bool[,] processedCells, SpottedLandform landform)
    {
        List<(int x, int y, LandformType)> patchCells = new List<(int, int, LandformType)>();
        
        if (landform == null || landform.sizeSettings == null || landform.sizeSettings.Count == 0)
        {
            patchCells.Add((startX, startY, landform));
            processedCells[startX, startY] = true;
            return patchCells;
        }
        
        float totalChance = 0f;
        foreach (var setting in landform.sizeSettings)
        {
            totalChance += setting.chance;
        }
        
        float randomValue = (float)mapRandom.NextDouble() * totalChance;
        float cumulativeChance = 0f;
        int selectedSize = 1;
        
        foreach (var setting in landform.sizeSettings)
        {
            cumulativeChance += setting.chance;
            if (randomValue <= cumulativeChance)
            {
                selectedSize = (int)setting.size + 1;
                break;
            }
        }
        
        patchCells.Add((startX, startY, landform));
        processedCells[startX, startY] = true;
        
        if (selectedSize == 1)
            return patchCells;
        
        // 使用 DirectionHelper 获取六边形邻居
        List<Vector2Int> vecNeighbors = DirectionHelper.Instance.GetAllValidNeighbors(startX, startY);
        List<(int x, int y)> neighbors = new List<(int, int)>();
        foreach (var v in vecNeighbors) neighbors.Add((v.x, v.y));
        
        int cellsToAdd = Mathf.Min(selectedSize - 1, neighbors.Count);
        
        for (int i = 0; i < cellsToAdd; i++)
        {
            int randomIdx = mapRandom.Next(neighbors.Count);
            var (x, y) = neighbors[randomIdx];
            
            if (!processedCells[x, y])
            {
                patchCells.Add((x, y, landform));
                processedCells[x, y] = true;
            }
            
            neighbors.RemoveAt(randomIdx);
        }
        
        return patchCells;
    }

    private bool CheckSurroundingConditions(List<(int x, int y)> neighbors, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, LinearLandform landform)
    {
        foreach (var (x, y) in neighbors)
        {
            if (CheckValueInRange(x, y, elevationMap, temperatureMap, humidityMap, landform.surroundingSetting.Value1, landform.surroundingSetting.Value2))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool CheckValueInRange(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, SelectValue value1, SelectValue value2)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            return false;
        
        float val1 = GetValueFromSelectValue(value1);
        float val2 = GetValueFromSelectValue(value2);
        
        if (value1 == SelectValue.None && value2 == SelectValue.None)
            return true;
        
        float cellValue = GetCellValueBySelectValue(x, y, elevationMap, temperatureMap, humidityMap, value1);
        
        if (value1 == SelectValue.None)
            return cellValue <= val2;
        if (value2 == SelectValue.None)
            return cellValue >= val1;
        
        float minVal = Mathf.Min(val1, val2);
        float maxVal = Mathf.Max(val1, val2);
        return cellValue >= minVal && cellValue <= maxVal;
    }

    private float GetCellValueBySelectValue(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, SelectValue valueType)
    {
        return valueType switch
        {
            SelectValue.seaLevel or SelectValue.plainLevel or SelectValue.hillLevel => elevationMap[x, y],
            SelectValue.frigidLevel or SelectValue.temperateLevel => temperatureMap[x, y],
            SelectValue.desertLevel or SelectValue.grasslandLevel or SelectValue.forestLevel => humidityMap[x, y],
            _ => 0f,
        };
    }

    private float GetValueFromSelectValue(SelectValue value)
    {
        return value switch
        {
            SelectValue.None => 0f,
            SelectValue.seaLevel => generationValues.seaLevel,
            SelectValue.plainLevel => generationValues.plainLevel,
            SelectValue.hillLevel => generationValues.hillLevel,
            SelectValue.frigidLevel => generationValues.frigidLevel,
            SelectValue.temperateLevel => generationValues.temperateLevel,
            SelectValue.desertLevel => generationValues.desertLevel,
            SelectValue.grasslandLevel => generationValues.grasslandLevel,
            SelectValue.forestLevel => generationValues.forestLevel,
            _ => 0f,
        };
    }

    private (int x, int y) SelectNextDirection(List<(int x, int y)> neighbors, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, LinearLandform landform)
    {
        if (landform.directionSettings == null || landform.directionSettings.Count == 0)
            return (-1, -1);
        
        List<(int x, int y)> candidates = new List<(int, int)>();
        List<float> weights = new List<float>();
        
        foreach (var directionSetting in landform.directionSettings)
        {
            List<(int x, int y, float value)> directionCandidates = new List<(int, int, float)>();
            
            foreach (var (x, y) in neighbors)
            {
                if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                    continue;
                
                float value = GetSurroundingDataValue(x, y, elevationMap, temperatureMap, humidityMap, directionSetting.dataToBeCompared);
                directionCandidates.Add((x, y, value));
            }
            
            List<(int x, int y)> selectedCandidates = SelectByPathwayControl(directionCandidates, directionSetting.pathwayControl);
            
            foreach (var (x, y) in selectedCandidates)
            {
                candidates.Add((x, y));
                weights.Add(directionSetting.weight);
            }
        }
        
        if (candidates.Count == 0)
            return (-1, -1);
        
        return WeightedRandomSelect(candidates, weights);
    }

    private float GetSurroundingDataValue(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, SurroundingData dataType)
    {
        return dataType switch
        {
            SurroundingData.Elevation => elevationMap[x, y],
            SurroundingData.Humidity => humidityMap[x, y],
            SurroundingData.Temperature => temperatureMap[x, y],
            _ => 0f,
        };
    }

    private List<(int x, int y)> SelectByPathwayControl(List<(int x, int y, float value)> candidates, PathwayControl control)
    {
        if (candidates.Count == 0)
            return new List<(int, int)>();
        
        candidates.Sort((a, b) => a.value.CompareTo(b.value));
        
        List<(int x, int y)> result = new List<(int, int)>();
        
        switch (control)
        {
            case PathwayControl.CompleteRandom:
                foreach (var (x, y, _) in candidates)
                    result.Add((x, y));
                break;
            
            case PathwayControl.Lowest:
                result.Add((candidates[0].x, candidates[0].y));
                break;
            
            case PathwayControl.SecondLowest:
                if (candidates.Count >= 2)
                    result.Add((candidates[1].x, candidates[1].y));
                break;
            
            case PathwayControl.ThirdLowest:
                if (candidates.Count >= 3)
                    result.Add((candidates[2].x, candidates[2].y));
                break;
            
            case PathwayControl.ThirdHighest:
                if (candidates.Count >= 3)
                    result.Add((candidates[candidates.Count - 3].x, candidates[candidates.Count - 3].y));
                break;
            
            case PathwayControl.SecondHighest:
                if (candidates.Count >= 2)
                    result.Add((candidates[candidates.Count - 2].x, candidates[candidates.Count - 2].y));
                break;
            
            case PathwayControl.Highest:
                result.Add((candidates[candidates.Count - 1].x, candidates[candidates.Count - 1].y));
                break;
        }
        
        return result;
    }

    private (int x, int y) WeightedRandomSelect(List<(int x, int y)> candidates, List<float> weights)
    {
        if (candidates.Count == 0)
            return (-1, -1);
        
        float totalWeight = 0f;
        foreach (float weight in weights)
            totalWeight += weight;
        
        if (totalWeight <= 0f)
            return candidates[mapRandom.Next(candidates.Count)];
        
        float randomValue = (float)mapRandom.NextDouble() * totalWeight;
        float cumulativeWeight = 0f;
        
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
                return candidates[i];
        }
        
        return candidates[candidates.Count - 1];
    }
}