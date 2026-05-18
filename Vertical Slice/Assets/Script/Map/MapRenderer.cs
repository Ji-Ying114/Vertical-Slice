using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
internal struct MapTile
{
    public GameObject targetTilemap;
    public TileBase tileBase;
    public bool checkTerrainType;
    public TerrainType terrainType;
    public bool checkBiomeType;
    public BiomeType biomeType;
    public bool checkTemperatureType;
    public TemperatureType temperatureType;

    /// <summary>检查该规则是否符合TileData</summary>
    public bool CheckMatch(TileData tileData)
    {
        if (checkTerrainType && tileData.terrainType != terrainType)
            return false;
        if (checkBiomeType && tileData.biomeType != biomeType)
            return false;
        if (checkTemperatureType && tileData.temperatureType != temperatureType)
            return false;
        return true;
    }
}

public class MapRenderer : MonoBehaviour
{
    public static MapRenderer Instance;

    [SerializeField] private MapTile[] mapTiles;
    [SerializeField] private TileBase defaultTileBase;

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

    /// <summary>遍历地图所有Tile并渲染，寻找符合规则的瓦片进行匹配</summary>
    public void RenderMap()
    {
        if (MapGenerator.Instance == null || MapGenerator.Instance.tiles == null)
        {
            Debug.LogError("MapGenerator or tiles not set!");
            return;
        }

        int width = MapGenerator.Instance.tiles.GetLength(0);
        int height = MapGenerator.Instance.tiles.GetLength(1);

        // 遍历所有Tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = MapGenerator.Instance.tiles[x, y];
                TileData tileData = tile.GetTileData();
                TileID tileID = tile.GetTileID();

                // 从前往后查找符合规则的MapTile
                MapTile? matchedMapTile = null;
                for (int i = 0; i < mapTiles.Length; i++)
                {
                    if (mapTiles[i].CheckMatch(tileData))
                    {
                        matchedMapTile = mapTiles[i];
                        break;
                    }
                }

                // 渲染瓦片
                if (matchedMapTile.HasValue)
                {
                    Tilemap tilemap = matchedMapTile.Value.targetTilemap.GetComponent<Tilemap>();
                    if (tilemap != null)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), matchedMapTile.Value.tileBase);
                    }
                }
                else
                {
                    // 未找到匹配规则，使用默认瓦片并黄字提示
                    Debug.LogWarning($"<color=yellow>No matching MapTile for Tile({x},{y}), using default tile</color>");
                    
                    // 获取默认tilemap（假设所有mapTiles中的第一个targetTilemap作为默认）
                    if (mapTiles.Length > 0)
                    {
                        Tilemap tilemap = mapTiles[0].targetTilemap.GetComponent<Tilemap>();
                        if (tilemap != null)
                        {
                            tilemap.SetTile(new Vector3Int(x, y, 0), defaultTileBase);
                        }
                    }
                }

                // DebugMode开启时记录每个填充的瓦片
                if (Console.Instance.debugMode == DebugMode.On)
                {
                    Debug.Log($"Rendered Tile({x},{y}): Terrain={tileData.terrainType}, Biome={tileData.biomeType}, Temp={tileData.temperatureType}");
                }
            }
        }
    }
    public void RenderMap(int x, int y)
    {
        if (MapGenerator.Instance == null || MapGenerator.Instance.tiles == null)
        {
            Debug.LogError("MapGenerator or tiles not set!");
            return;
        }

        if (x < 0 || x >= MapGenerator.Instance.tiles.GetLength(0) || y < 0 || y >= MapGenerator.Instance.tiles.GetLength(1))
        {
            Debug.LogError($"Invalid tile coordinates: ({x}, {y})");
            return;
        }

        Tile tile = MapGenerator.Instance.tiles[x, y];
        TileData tileData = tile.GetTileData();
        TileID tileID = tile.GetTileID();

        // 从前往后查找符合规则的MapTile
        MapTile? matchedMapTile = null;
        for (int i = 0; i < mapTiles.Length; i++)
        {
            if (mapTiles[i].CheckMatch(tileData))
            {
                matchedMapTile = mapTiles[i];
                break;
            }
        }

        // 渲染瓦片
        if (matchedMapTile.HasValue)
        {
            Tilemap tilemap = matchedMapTile.Value.targetTilemap.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), matchedMapTile.Value.tileBase);
            }
        }
        else
        {
            // 未找到匹配规则，使用默认瓦片并黄字提示
            Debug.LogWarning($"<color=yellow>No matching MapTile for Tile({x},{y}), using default tile</color>");
            
            // 获取默认tilemap（假设所有mapTiles中的第一个targetTilemap作为默认）
            if (mapTiles.Length > 0)
            {
                Tilemap tilemap = mapTiles[0].targetTilemap.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), defaultTileBase);
                }
            }
        }

        // DebugMode开启时记录每个填充的瓦片
        if (Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log($"Rendered Tile({x},{y}): Terrain={tileData.terrainType}, Biome={tileData.biomeType}, Temp={tileData.temperatureType}");
        }
    }
}
