using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum DebugMode
{
    Off,
    On,
}


// 游戏内控制台系统，允许玩家按~键呼出控制台并输入命令。
// 支持设置地图生成参数和重新生成地图。
public class Console : MonoBehaviour
{
    // 单例实例，确保游戏中只有一个控制台。
    public static Console Instance;
    public DebugMode debugMode = DebugMode.Off;

    // 控制台UI面板。
    [SerializeField] private GameObject consolePanel;

    // 输入字段，用于玩家输入命令。
    [SerializeField] private  TMP_InputField inputField;

    // 输出文本，用于显示命令结果和日志。
    [SerializeField] private TMP_Text outputText;

    // 控制台是否可见。
    private bool isVisible = false;

    // 初始化单例。
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

    // 初始化控制台，隐藏面板。
    void Start()
    {
        isVisible = false;
        if (consolePanel != null)
        {
            consolePanel.SetActive(false);
        }
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(_ => OnCommandEntered());
        }
    }

    // 显示控制台。
    private void ShowConsole()
    {
        isVisible = true;
        if (consolePanel != null)
        {
            consolePanel.SetActive(true);
        }
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    // 隐藏控制台。
    private void HideConsole()
    {
        isVisible = false;
        if (consolePanel != null)
        {
            consolePanel.SetActive(false);
        }
        if (inputField != null)
        {
            inputField.text = "";
            inputField.DeactivateInputField();
        }
    }

    // 切换控制台的可见性。
    public void ToggleConsole()
    {
        if (isVisible)
            HideConsole();
        else
            ShowConsole();
    }

    // 当玩家输入命令时调用。
    public void OnCommandEntered()
    {
        string command = inputField.text;
        inputField.text = "";
        ProcessCommand(command);
    }

    // 处理输入的命令。
    // 参数: command - 输入的命令字符串。
    private void ProcessCommand(string command)
    {
        string[] parts = command.Split(' ');// 将命令分割成部分，第一部分是命令，后续部分是参数。
        if (parts.Length == 0) return;// 如果没有输入命令，直接返回。

        string cmd = parts[0].ToLower();// 获取命令的主部分，并转换为小写以便比较。
        switch (cmd)
        {
            case "set":
                if (parts.Length >= 3)
                {
                    SetProperty(parts[1], parts[2]);
                }
                else
                {
                    Log("Usage: set <property> <value>");
                }
                break;
            case "regenerate":
                if (parts.Length == 1)
                {
                    RegenerateMap();
                }
                else if (parts.Length == 2)
                {
                    if (int.TryParse(parts[1], out int size))
                        RegenerateMap(size);
                    else
                        Log("Invalid parameter for regenerate");
                }
                else if (parts.Length == 3)
                {
                    if (int.TryParse(parts[1], out int width) && int.TryParse(parts[2], out int height))
                        RegenerateMap(width, height);
                    else
                        Log("Invalid parameters for regenerate");
                }
                else
                {
                    Log("Usage: regenerate [size] or regenerate [width] [height]");
                }
                break;
            case "mapdata":
                if (parts.Length == 1)
                {
                    PrintMapDataBasic();
                }
                else if (parts.Length == 2 && (parts[1].ToLower() == "stats" || parts[1].ToLower() == "all"))
                {
                    PrintMapDataStats();
                }
                else if (parts.Length == 3)
                {
                    if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                        PrintMapDataTile(x, y);
                    else
                        Log("Usage: mapdata [x] [y] or mapdata [stats|all]");
                }
                else
                {
                    Log("Usage: mapdata - show basic info");
                    Log("       mapdata stats - show statistics");
                    Log("       mapdata x y - show tile at position (x, y)");
                }
                break;
            case "seed":
                if (MapGenerator.Instance != null)
                {
                    Log($"Map Seed: {MapGenerator.Instance.GetMapSeed()}");
                }
                else
                {
                    Log("MapGenerator not found");
                }
                break;
            case "debug":
                if (parts.Length == 2)
                {
                    if (parts[1].ToLower() == "on")
                    {
                        debugMode = DebugMode.On;
                        Log("Debug mode enabled");
                    }
                    else if (parts[1].ToLower() == "off")
                    {
                        debugMode = DebugMode.Off;
                        Log("Debug mode disabled");
                    }
                    else
                    {
                        Log("Usage: debug [on|off]");
                    }
                }
                else
                {
                    Log("Usage: debug [on|off]");
                }
                break;
            case "render":
                if (parts.Length == 1)
                {
                    RenderMapCommand();
                }
                else if (parts.Length == 3)
                {
                    if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                        RenderMapCommand(x, y);
                    else
                        Log("Usage: render [x] [y]");
                }
                else
                {
                    Log("Usage: render - render all tiles");
                    Log("       render x y - render specific tile");
                }
                break;
            default:
                Log("Unknown command: " + cmd);
                break;
        }
    }

    // 设置地图生成器的属性。
    // 参数: property - 属性名, value - 属性值。
    private void SetProperty(string property, string value)
    {
        if (MapGenerator.Instance == null)
        {
            Log("MapGenerator not found");
            return;
        }

        float floatValue;
        if (!float.TryParse(value, out floatValue))
        {
            Log("Invalid value: " + value);
            return;
        }

        switch (property.ToLower())
        {
            case "sealevel":
                MapGenerator.Instance.generationValues.seaLevel = floatValue;
                Log("Sea level set to " + floatValue);
                break;
            case "hilllevel":
                MapGenerator.Instance.generationValues.hillLevel = floatValue;
                Log("Hill level set to " + floatValue);
                break;
            // 可以根据需要添加更多属性
            default:
                Log("Unknown property: " + property);
                break;
        }
    }

    // 重新生成地图。
    private void RegenerateMap(int width = -1, int height = -1)
    {
        if (MapGenerator.Instance != null)
        {
            if (width == -1 && height == -1)
            {
                MapGenerator.Instance.RegenerateMap();
                Log("Map regenerated with previous size");
            }
            else if (height == -1)
            {
                MapGenerator.Instance.RegenerateMap(width);
                Log($"Map regenerated with size {width}x{width}");
            }
            else
            {
                MapGenerator.Instance.RegenerateMap(width, height);
                Log($"Map regenerated with size {width}x{height}");
            }
        }
        else
        {
            Log("MapGenerator not found");
        }
    }

    // 打印地图基本信息
    private void PrintMapDataBasic()
    {
        if (MapGenerator.Instance == null)
        {
            Log("MapGenerator not found");
            return;
        }

        if (MapGenerator.Instance.tiles == null || MapGenerator.Instance.tiles.Length == 0)
        {
            Log("No tiles generated");
            return;
        }

        int width = MapGenerator.Instance.tiles.GetLength(0);
        int height = MapGenerator.Instance.tiles.GetLength(1);
        Log("=== Map Data ===");
        Log($"Size: {width}x{height}");
        Log($"Total Tiles: {width * height}");
        Log($"Seed: {MapGenerator.Instance.GetMapSeed()}");
        Log("Use 'mapdata stats' to see statistics");
        Log("Use 'mapdata x y' to see specific tile details");
    }

    // 打印地图统计信息
    private void PrintMapDataStats()
    {
        if (MapGenerator.Instance == null)
        {
            Log("MapGenerator not found");
            return;
        }

        if (MapGenerator.Instance.tiles == null || MapGenerator.Instance.tiles.Length == 0)
        {
            Log("No tiles generated");
            return;
        }

        int width = MapGenerator.Instance.tiles.GetLength(0);
        int height = MapGenerator.Instance.tiles.GetLength(1);

        // 统计各种类型的计数
        Dictionary<string, int> terrainCount = new Dictionary<string, int>();
        Dictionary<string, int> biomeCount = new Dictionary<string, int>();
        Dictionary<string, int> temperatureCount = new Dictionary<string, int>();
        int waterTiles = 0;
        int landTiles = 0;
        int tilesWithLandform = 0;
        int tilesWithResource = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = MapGenerator.Instance.tiles[x, y];
                if (tile == null) continue;

                TileData tileData = tile.GetTileData();

                // 统计地形
                string terrainName = tileData.terrainType.ToString();
                if (!terrainCount.ContainsKey(terrainName))
                    terrainCount[terrainName] = 0;
                terrainCount[terrainName]++;

                if (tileData.terrainType == TerrainType.Default)
                    waterTiles++;
                else
                    landTiles++;

                // 统计生物群系
                string biomeName = tileData.biomeType.ToString();
                if (!biomeCount.ContainsKey(biomeName))
                    biomeCount[biomeName] = 0;
                biomeCount[biomeName]++;

                // 统计温度
                string tempName = tileData.temperatureType.ToString();
                if (!temperatureCount.ContainsKey(tempName))
                    temperatureCount[tempName] = 0;
                temperatureCount[tempName]++;

                // 统计地形和资源
                if (tileData.landformType != null && tileData.landformType.Length > 0)
                    tilesWithLandform++;

                if (tileData.mapResourceType != null)
                    tilesWithResource++;
            }
        }

        Log("=== Map Statistics ===");
        Log($"Total Tiles: {width * height}");
        Log($"Water Tiles: {waterTiles} ({(float)waterTiles / (width * height) * 100:F1}%)");
        Log($"Land Tiles: {landTiles} ({(float)landTiles / (width * height) * 100:F1}%)");
        Log($"Tiles with Landform: {tilesWithLandform}");
        Log($"Tiles with Resource: {tilesWithResource}");

        Log("--- Terrain Types ---");
        foreach (var kvp in terrainCount)
        {
            Log($"  {kvp.Key}: {kvp.Value}");
        }

        Log("--- Biome Types ---");
        foreach (var kvp in biomeCount)
        {
            Log($"  {kvp.Key}: {kvp.Value}");
        }

        Log("--- Temperature Types ---");
        foreach (var kvp in temperatureCount)
        {
            Log($"  {kvp.Key}: {kvp.Value}");
        }
    }

    // 打印特定位置的 tile 详细信息
    private void PrintMapDataTile(int x, int y)
    {
        if (MapGenerator.Instance == null)
        {
            Log("MapGenerator not found");
            return;
        }

        if (MapGenerator.Instance.tiles == null || MapGenerator.Instance.tiles.Length == 0)
        {
            Log("No tiles generated");
            return;
        }

        int width = MapGenerator.Instance.tiles.GetLength(0);
        int height = MapGenerator.Instance.tiles.GetLength(1);

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Log($"Tile position ({x}, {y}) is out of bounds. Map size: {width}x{height}");
            return;
        }

        Tile tile = MapGenerator.Instance.tiles[x, y];
        if (tile == null)
        {
            Log($"Tile at ({x}, {y}) is null");
            return;
        }

        TileData tileData = tile.GetTileData();
        Log($"=== Tile Data at ({x}, {y}) ===");
        Log($"Terrain Type: {tileData.terrainType}");
        Log($"Biome Type: {tileData.biomeType}");
        Log($"Temperature Type: {tileData.temperatureType}");
        Log($"Map Resource: {(tileData.mapResourceType != null ? tileData.mapResourceType.ToString() : "None")}");

        if (tileData.landformType != null && tileData.landformType.Length > 0)
        {
            Log($"Landforms ({tileData.landformType.Length}):");
            foreach (var landform in tileData.landformType)
            {
                Log($"  - {landform.ToString()}");
            }
        }
        else
        {
            Log("Landforms: None");
        }

        if (tileData.tileTemporalFactors != null && tileData.tileTemporalFactors.Count > 0)
        {
            Log($"Temporal Factors ({tileData.tileTemporalFactors.Count}):");
            foreach (var factor in tileData.tileTemporalFactors)
            {
                Log($"  - {factor.tileFactor}: {factor.multiplier}x ({factor.passedTurns}/{factor.duration} turns)");
            }
        }
        else
        {
            Log("Temporal Factors: None");
        }
    }

    // 渲染地图
    private void RenderMapCommand()
    {
        if (MapRenderer.Instance != null)
        {
            MapRenderer.Instance.RenderMap();
            Log("Map rendered successfully");
        }
        else
        {
            Log("MapRenderer not found");
        }
    }

    // 渲染指定坐标的地块
    private void RenderMapCommand(int x, int y)
    {
        if (MapRenderer.Instance != null)
        {
            MapRenderer.Instance.RenderMap(x, y);
            Log($"Tile({x},{y}) rendered successfully");
        }
        else
        {
            Log("MapRenderer not found");
        }
    }

    // 记录消息到控制台输出。
    // 参数: message - 要记录的消息。
    private void Log(string message)
    {
        if (outputText != null)
        {
            outputText.text += message + "\n";
        }
        Debug.Log(message);
    }
}