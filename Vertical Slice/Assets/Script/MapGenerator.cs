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

public enum HexDirection
{
    Right = 0,
    BottomRight = 1,
    BottomLeft = 2,
    Left = 3,
    TopLeft = 4,
    TopRight = 5,
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

    [SerializeField] public GenerationValues generationValues;

    [SerializeField] private float LandformFrequency;
    [SerializeField] private float LinearLandformLengthFactor;

    [SerializeField] private float mapResourceFrequency;
    [SerializeField] private EnabledMapResourceSettings[] enabledMapResourceSettings;
    [SerializeField] private EnabledLandformSettings[] enabledLandformSettings;
    
    [SerializeField] private bool fixedSeed;
    [SerializeField] private int setSeed;
    [SerializeField] private float noiseScale;
    
    [SerializeField] private int fbmOctaves = 4;              // FBM 层数
    [SerializeField] private float fbmPersistence = 0.5f;     // 每层的幅度衰减系数
    [SerializeField] private float fbmLacunarity = 2f;        // 每层的频率增加系数

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
    
    public void GenerateMap(int width, int height)
    {
        // 初始化种子
        seed = fixedSeed ? setSeed : System.DateTime.Now.Millisecond;
        mapRandom = new System.Random(seed);
        
        // 设置地图尺寸
        mapWidth = width;
        mapHeight = height;
        
        // 初始化Tile数组
        tiles = new Tile[mapWidth, mapHeight];
        
        // 生成基础地图
        cachedElevationMap = GenerateElevationMap();
        cachedTemperatureMap = GenerateTemperatureMap();
        cachedHumidityMap = GenerateHumidityMap(cachedElevationMap);
        
        // 生成地形（线性和斑点统一处理）
        Dictionary<(int, int), List<LandformType>> landformDict = GenerateLandforms(cachedElevationMap, cachedTemperatureMap, cachedHumidityMap);
        
        // 为每个Tile生成数据
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float elevation = cachedElevationMap[x, y];
                float temperature = cachedTemperatureMap[x, y];
                float humidity = cachedHumidityMap[x, y];
                
                // 创建新的Tile
                TileID tileID = new TileID { x = x, y = y };
                Tile newTile = new Tile(tileID);
                
                // 初始化TileData
                TileData tileData = new TileData
                {
                    terrainType = TerrainType.Default,
                    biomeType = BiomeType.Default,
                    temperatureType = TemperatureType.Default,
                    landformType = new LandformType[0],
                    tileTemporalFactors = new List<TileTemporalFactor>(),
                };
                
                // 确定地形类型
                tileData.terrainType = DetermineTerrainType(elevation);
                
                // 确定温度类型
                tileData.temperatureType = DetermineTemperatureType(temperature);
                
                // 所有地块都可以有生物群系（包括水域）
                tileData.biomeType = DetermineBiomeType(temperature, humidity);
                
                // 生成地图资源
                tileData.mapResourceType = GenerateMapResource(x, y, cachedElevationMap, cachedTemperatureMap, cachedHumidityMap, tileData);
                
                // 获取该格子的地形
                List<LandformType> tileLandforms = new List<LandformType>();
                var key = (x, y);
                if (landformDict.ContainsKey(key))
                {
                    tileLandforms.AddRange(landformDict[key]);
                }
                
                tileData.landformType = tileLandforms.ToArray();
                
                // 设置Tile数据
                newTile.SetTileData(tileData);
                tiles[x, y] = newTile;
                if (Console.Instance.debugMode == DebugMode.On)
                {
                    Debug.Log($"Tile ({x},{y}): Elevation={elevation:F2}, Temperature={temperature:F2}, Humidity={humidity:F2}, Terrain={tileData.terrainType}, Biome={tileData.biomeType}, TempType={tileData.temperatureType}, Landforms={string.Join(",", (object[])tileData.landformType)}, Resource={(tileData.mapResourceType != null ? tileData.mapResourceType.name : "None")}");
                }
            }
        }
    }
    // 重新生成地图 - 可通过控制台命令调用
    // 无参数：按照上一张的大小生成
    // 一个参数：生成正方形地图（size x size）
    // 两个参数：分别代表地图的宽和高
    public void RegenerateMap(int width = -1, int height = -1)
    {
        if (width == -1 && height == -1)
        {
            // 无参数，按照上一张的大小生成
            if (tiles == null || tiles.Length == 0)
            {
                Debug.LogWarning("No existing map to regenerate. Please use GenerateMap() with parameters.");
                return;
            }
            GenerateMap(mapWidth, mapHeight);
        }
        else if (height == -1)
        {
            // 一个参数，生成正方形地图
            GenerateMap(width, width);
        }
        else
        {
            // 两个参数，生成指定大小的地图
            GenerateMap(width, height);
        }
    }
    
    // 获取当前地图的种子
    public int GetMapSeed()
    {
        return seed;
    }

//------------------------------------------------------------------------
    
    // 分形布朗运动 (Fractal Brownian Motion)
    // 通过多层 Perlin 噪声叠加生成平滑且细节丰富的噪声
    // baseScale: 基础缩放（对应原来的 noiseScale）
    // offsetX/offsetY: 噪声偏移（用于生成不同的噪声）
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

//------------------------------------------------------------------------


    // 根据海拔高度确定地形类型
    private TerrainType DetermineTerrainType(float elevation)
    {
        // 海平面以下为水域
        if (elevation < generationValues.seaLevel)
        {
            return TerrainType.Default;
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

    // 根据温度和湿度确定生物群落类型
    private BiomeType DetermineBiomeType(float temperature, float humidity)
    {
        // 根据湿度确定生物群落
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

    // 根据温度值确定温度类型
    private TemperatureType DetermineTemperatureType(float temperature)
    {
        if (temperature < generationValues.frigidLevel)
            return TemperatureType.Frigid;
        else if (temperature < generationValues.temperateLevel)
            return TemperatureType.Temperate;
        else
            return TemperatureType.Tropical;
    }

    // 为地块生成地图资源
    private MapResourceType GenerateMapResource(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, TileData tileData)
    {
        float resourceChance = (float)mapRandom.NextDouble();
        
        // 根据全局资源生成概率进行判定
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
            
            // 检查海陆设定
            if (isWater && resourceType.landOrMarine == LandOrMarine.Land)
                continue;
            if (!isWater && resourceType.landOrMarine == LandOrMarine.Marine)
                continue;
            
            // 计算基础权重
            float weight = resourceSetting.mapResourceChance * resourceType.basicGenerationChance;
            
            // 应用条件生成概率修正因子，MapResourceType 的 factor 采用 p = k * (x - 0.5) + 1
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
        
        // 基于权重随机选择资源
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

    // 计算连续概率系数 p = k * (x - 0.5) + 1
    // x 为输入值（通常来自 Perlin 噪声或归一化后的地图值），k 为 resourceType 的 factor
    // 保证输入值在 [0,1] 内，并返回非负系数
    private float EvaluateChanceCoefficient(float value, float factor)
    {
        float normalizedValue = Mathf.Clamp01(value);
        float coefficient = factor * (normalizedValue - 0.5f) + 1f;
        return Mathf.Max(0f, coefficient);
    }

    
    // 生成海拔高度地图 - 使用 FBM 确保平滑过渡
    private float[,] GenerateElevationMap()
    {
        float[,] elevationMap = new float[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // 使用 FBM 生成平滑且细节丰富的地形
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

    // 生成温度地图 - 基于纬度和 FBM 噪声
    private float[,] GenerateTemperatureMap()
    {
        float[,] temperatureMap = new float[mapWidth, mapHeight];
        float equatorY = mapHeight / 2f;
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // 使用 FBM 生成平滑的温度变化
                float noiseValue = GenerateFBMNoise(x, y, noiseScale, seed * 0.1f + 100f, seed * 0.1f + 100f);
                
                // 考虑纬度因素：赤道处温度最高，两极最低
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

    // 生成湿度地图 - 基于 FBM 噪声
    private float[,] GenerateHumidityMap(float[,] elevationMap)
    {
        float[,] humidityMap = new float[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // 使用 FBM 生成平滑的湿度变化
                float noiseValue = GenerateFBMNoise(x, y, noiseScale, seed * 0.1f + 200f, seed * 0.1f + 200f);
                
                // 水域处湿度较高，高地处湿度较低
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

    // 统一生成所有地形（线性和斑点）
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
                
                // 获取该格子的所有环境因素
                float elevation = elevationMap[x, y];
                float temperature = temperatureMap[x, y];
                float humidity = humidityMap[x, y];
                
                // 选择地形类型
                LandformType landform = SelectLandform(elevation, temperature, humidity);
                
                if (landform == null)
                    continue;

                // 根据地形类型进行不同的生成处理
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

    // 选择地形
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
            
            // 计算基础概率 b = LandformFrequency * landformChance
            float baseChance = LandformFrequency * setting.landformChance;
            
            // 应用环境修正因子，采用 p = k(x - 0.5) + b 的公式
            float elevationModifier = EvaluateChanceCoefficient(elevation, landform.elevationChanceFactor);
            float humidityModifier = EvaluateChanceCoefficient(humidity, landform.humidityChanceFactor);
            float temperatureModifier = EvaluateChanceCoefficient(temperature, landform.temperatureChanceFactor);
            
            // 计算最终权重：p = k(x - 0.5) + b，这里 b 是基础概率
            float weight = baseChance * elevationModifier * humidityModifier * temperatureModifier;
            
            if (weight <= 0f)
                continue;

            candidates.Add(landform);
            weights.Add(weight);
        }

        if (candidates.Count == 0)
            return null;

        // 基于权重随机选择地形
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

    // 生成线性地形路径，返回生成的所有格子及其地形
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
            // 越界检查
            if (currentX < 0 || currentX >= mapWidth || currentY < 0 || currentY >= mapHeight)
                break;
            
            // 标记为已处理
            processedCells[currentX, currentY] = true;
            pathCells.Add((currentX, currentY, landform));
            
            // 获取相邻格子
            List<(int x, int y)> neighbors = GetHexagonalNeighbors(currentX, currentY);
            
            // 检查surroundingSettings - 周围的地块必须满足所有条件，路径才会继续延伸
            if (!CheckSurroundingConditions(neighbors, elevationMap, temperatureMap, humidityMap, landform))
                break;
            
            // 基于directionSettings选择下一步方向
            (int nextX, int nextY) = SelectNextDirection(neighbors, elevationMap, temperatureMap, humidityMap, landform);
            
            // 如果没有找到合适的下一步位置，路径停止
            if (nextX == -1 || nextY == -1)
                break;
            
            currentX = nextX;
            currentY = nextY;
        }
        
        return pathCells;
    }

    // 生成斑点地形，返回生成的所有格子及其地形
    private List<(int x, int y, LandformType landform)> GenerateSpottedPatch(int startX, int startY, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, bool[,] processedCells, SpottedLandform landform)
    {
        List<(int x, int y, LandformType)> patchCells = new List<(int, int, LandformType)>();
        
        if (landform == null || landform.sizeSettings == null || landform.sizeSettings.Count == 0)
        {
            // 如果没有尺寸设置，默认只放置中心格子
            patchCells.Add((startX, startY, landform));
            processedCells[startX, startY] = true;
            return patchCells;
        }
        
        // 根据手动设置的尺寸概率选择大小
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
        
        // 中心格子
        patchCells.Add((startX, startY, landform));
        processedCells[startX, startY] = true;
        
        if (selectedSize == 1)
            return patchCells;
        
        // 获取相邻格子
        List<(int x, int y)> neighbors = GetHexagonalNeighbors(startX, startY);
        
        // 最多从相邻格子中选择(selectedSize - 1)个
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

    // 检查surrounding条件
    private bool CheckSurroundingConditions(List<(int x, int y)> neighbors, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, LinearLandform landform)
    {
        // 检查是否至少有一个相邻格子满足surroundingSetting条件
        foreach (var (x, y) in neighbors)
        {
            if (CheckValueInRange(x, y, elevationMap, temperatureMap, humidityMap, landform.surroundingSetting.Value1, landform.surroundingSetting.Value2))
            {
                return true;
            }
        }
        
        return false;
    }

    // 检查指定格子的值是否在范围内
    private bool CheckValueInRange(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, SelectValue value1, SelectValue value2)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            return false;
        
        float val1 = GetValueFromSelectValue(value1);
        float val2 = GetValueFromSelectValue(value2);
        
        // 如果两个值都是None，不做限制
        if (value1 == SelectValue.None && value2 == SelectValue.None)
            return true;
        
        // 根据SelectValue类型从对应的map中获取格子的值
        float cellValue = GetCellValueBySelectValue(x, y, elevationMap, temperatureMap, humidityMap, value1);
        
        // 处理单个值的情况
        if (value1 == SelectValue.None)
            return cellValue <= val2;
        if (value2 == SelectValue.None)
            return cellValue >= val1;
        
        // 处理范围
        float minVal = Mathf.Min(val1, val2);
        float maxVal = Mathf.Max(val1, val2);
        return cellValue >= minVal && cellValue <= maxVal;
    }

    // 根据SelectValue类型从对应的map中获取格子的值
    private float GetCellValueBySelectValue(int x, int y, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, SelectValue valueType)
    {
        return valueType switch
        {
            SelectValue.seaLevel or SelectValue.hillLevel => elevationMap[x, y],
            SelectValue.frigidLevel or SelectValue.temperateLevel => temperatureMap[x, y],
            SelectValue.desertLevel or SelectValue.grasslandLevel or SelectValue.forestLevel => humidityMap[x, y],
            _ => 0f,
        };
    }

    // 获取SelectValue对应的实际值
    private float GetValueFromSelectValue(SelectValue value)
    {
        return value switch
        {
            SelectValue.None => 0f,
            SelectValue.seaLevel => generationValues.seaLevel,
            SelectValue.hillLevel => generationValues.hillLevel,
            SelectValue.frigidLevel => generationValues.frigidLevel,
            SelectValue.temperateLevel => generationValues.temperateLevel,
            SelectValue.desertLevel => generationValues.desertLevel,
            SelectValue.grasslandLevel => generationValues.grasslandLevel,
            SelectValue.forestLevel => generationValues.forestLevel,
            _ => 0f,
        };
    }

    // 基于directionSettings选择下一个方向
    private (int x, int y) SelectNextDirection(List<(int x, int y)> neighbors, float[,] elevationMap, float[,] temperatureMap, float[,] humidityMap, LinearLandform landform)
    {
        if (landform.directionSettings == null || landform.directionSettings.Count == 0)
            return (-1, -1);
        
        List<(int x, int y)> candidates = new List<(int, int)>();
        List<float> weights = new List<float>();
        
        // 对每个DirectionSettings，选择符合要求的方向
        foreach (var directionSetting in landform.directionSettings)
        {
            List<(int x, int y, float value)> directionCandidates = new List<(int, int, float)>();
            
            // 收集所有相邻格子的值
            foreach (var (x, y) in neighbors)
            {
                if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                    continue;
                
                float value = GetSurroundingDataValue(x, y, elevationMap, temperatureMap, humidityMap, directionSetting.dataToBeCompared);
                directionCandidates.Add((x, y, value));
            }
            
            // 根据PathwayControl选择候选
            List<(int x, int y)> selectedCandidates = SelectByPathwayControl(directionCandidates, directionSetting.pathwayControl);
            
            foreach (var (x, y) in selectedCandidates)
            {
                candidates.Add((x, y));
                weights.Add(directionSetting.weight);
            }
        }
        
        if (candidates.Count == 0)
            return (-1, -1);
        
        // 基于权重进行随机选择
        return WeightedRandomSelect(candidates, weights);
    }

    // 获取surroundingData对应的值
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

    // 根据PathwayControl规则选择候选格子
    private List<(int x, int y)> SelectByPathwayControl(List<(int x, int y, float value)> candidates, PathwayControl control)
    {
        if (candidates.Count == 0)
            return new List<(int, int)>();
        
        // 排序候选值
        candidates.Sort((a, b) => a.value.CompareTo(b.value));
        
        List<(int x, int y)> result = new List<(int, int)>();
        
        switch (control)
        {
            case PathwayControl.CompleteRandom:
                // 返回所有候选
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

    // 加权随机选择
    private (int x, int y) WeightedRandomSelect(List<(int x, int y)> candidates, List<float> weights)
    {
        if (candidates.Count == 0)
            return (-1, -1);
        
        // 计算总权重
        float totalWeight = 0f;
        foreach (float weight in weights)
            totalWeight += weight;
        
        if (totalWeight <= 0f)
            return candidates[mapRandom.Next(candidates.Count)];
        
        // 进行加权随机选择
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

    // 获取六边形相邻单元坐标
    private List<(int x, int y)> GetHexagonalNeighbors(int x, int y)
    {
        List<(int, int)> neighbors = new List<(int, int)>();
        
        // 六个方向：右、右下、左下、左、左上、右上
        int[] offsetX = { 1, 1, 0, -1, -1, 0 };
        int[] offsetY = { 0, -1, -1, 0, 1, 1 };
        
        for (int i = 0; i < 6; i++)
        {
            int newX = x + offsetX[i];
            int newY = y + offsetY[i];
            
            // 边界检查
            if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight)
            {
                neighbors.Add((newX, newY));
            }
        }
        
        return neighbors;
    }
}