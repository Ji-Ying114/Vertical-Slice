using System.Collections.Generic;
using UnityEngine;

public class TestInitializer : MonoBehaviour
{
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;
    [SerializeField] private GameObject scoutUnitPrefab;

    private void Start()
    {
        // 1. 生成地图
        if (MapGenerator.Instance == null)
        {
            Debug.LogError("TestInitializer: MapGenerator.Instance 不存在！");
            return;
        }
        MapGenerator.Instance.GenerateMap(width, height);

        // 2. 渲染地图
        if (MapRenderer.Instance != null)
        {
            MapRenderer.Instance.RenderMap();
        }
        else
        {
            Debug.LogWarning("TestInitializer: MapRenderer.Instance 不存在，地图未渲染");
        }

        // 3. 收集所有非水下单元格
        Tile[,] tiles = MapGenerator.Instance.tiles;
        int mapWidth = tiles.GetLength(0);
        int mapHeight = tiles.GetLength(1);
        List<Vector2Int> landTiles = new List<Vector2Int>();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (tiles[x, y].GetTileData().terrainType != TerrainType.Default)
                {
                    landTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (landTiles.Count == 0)
        {
            Debug.LogError("TestInitializer: 未找到任何陆地单元格，无法生成单位和城镇！");
            return;
        }

        // 随机选择出生点
        Vector2Int spawnPos = landTiles[Random.Range(0, landTiles.Count)];

        // 4. 创建起始城镇（必须在地图上先生成城镇，再生成单位，顺序可调）
        if (TownManager.Instance != null)
        {
            TownManager.Instance.CreateTown("Capital", new TileID { x = spawnPos.x, y = spawnPos.y }, 1);
            Debug.Log($"TestInitializer: 城镇 'Capital' 已创建在 ({spawnPos.x}, {spawnPos.y})");
        }
        else
        {
            Debug.LogError("TestInitializer: TownManager.Instance 不存在，无法创建城镇！");
        }

        // 5. 生成侦察单位
        if (scoutUnitPrefab == null)
        {
            Debug.LogError("TestInitializer: scoutUnitPrefab 未赋值！");
            return;
        }

        GameObject unitObj = Instantiate(scoutUnitPrefab);
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError("TestInitializer: 侦察单位预制体缺少 Unit 组件！");
            Destroy(unitObj);
            return;
        }

        // 确保单位可以被选中
        if (unitObj.GetComponent<Selectable>() == null)
        {
            unitObj.AddComponent<Selectable>();
        }

        unit.InitPosition(spawnPos.x, spawnPos.y);

        // 6. 移动摄像机到单位/城镇位置
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 worldPos = MapGenerator.Instance.worldPosition(spawnPos.x, spawnPos.y);
            mainCamera.transform.position = new Vector3(worldPos.x, worldPos.y, -10f);
        }
        else
        {
            Debug.LogError("TestInitializer: 未找到主摄像机！");
        }

        Debug.Log($"TestInitializer: 地图 ({width}x{height}) 已生成，城镇和侦察单位出生在 ({spawnPos.x}, {spawnPos.y})");
    }
}