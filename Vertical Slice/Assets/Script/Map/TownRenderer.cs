using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TownRenderer : MonoBehaviour
{
    public static TownRenderer Instance;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap territoryTilemap;
    [SerializeField] private Tilemap improvementTilemap;
    [SerializeField] private Tilemap candidateTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase territoryBase;
    [SerializeField] private TileBase cityCenterBase;
    [SerializeField] private TileBase candidateBase;

    [Header("Settings")]
    [SerializeField] private int territorySortingOrder = 2;
    [SerializeField] private int improvementSortingOrder = 3;
    [SerializeField] private int candidateSortingOrder = 4;

    private Town currentExpansionTown;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (territoryTilemap != null)
            territoryTilemap.GetComponent<TilemapRenderer>().sortingOrder = territorySortingOrder;
        if (improvementTilemap != null)
            improvementTilemap.GetComponent<TilemapRenderer>().sortingOrder = improvementSortingOrder;
        if (candidateTilemap != null)
            candidateTilemap.GetComponent<TilemapRenderer>().sortingOrder = candidateSortingOrder;
    }

    void Start()
    {
        if (TownManager.Instance != null)
            TownManager.Instance.OnTownsUpdated += RenderAllTerritories;

        RenderAllTerritories();
    }

    void OnDestroy()
    {
        if (TownManager.Instance != null)
            TownManager.Instance.OnTownsUpdated -= RenderAllTerritories;
    }

    /// <summary>
    /// 重绘所有城镇的领土和市中心（常驻显示）
    /// </summary>
    public void RenderAllTerritories()
    {
        if (territoryTilemap != null) territoryTilemap.ClearAllTiles();
        if (improvementTilemap != null) improvementTilemap.ClearAllTiles();

        if (TownManager.Instance == null) return;

        foreach (Town town in TownManager.Instance.GetTowns())
        {
            if (territoryTilemap != null && territoryBase != null)
            {
                foreach (TileID tileID in town.OwnedTiles)
                {
                    Vector3Int pos = new Vector3Int(tileID.x, tileID.y, 0);
                    territoryTilemap.SetTile(pos, territoryBase);
                }
            }

            if (improvementTilemap != null && cityCenterBase != null)
            {
                TileID center = town.GetPosition();
                Vector3Int centerPos = new Vector3Int(center.x, center.y, 0);
                improvementTilemap.SetTile(centerPos, cityCenterBase);
            }
        }
    }

    /// <summary>
    /// 显示指定城镇的可扩张候选地块
    /// </summary>
    public void ShowExpansionCandidates(Town town)
    {
        ClearExpansionCandidates();
        if (town == null || candidateTilemap == null || candidateBase == null) return;

        currentExpansionTown = town;

        List<TileID> candidates = GetCandidateTiles(town);
        foreach (TileID cand in candidates)
        {
            Vector3Int pos = new Vector3Int(cand.x, cand.y, 0);
            candidateTilemap.SetTile(pos, candidateBase);
        }
    }

    /// <summary>
    /// 清除候选地块高亮（不影响常驻领土）
    /// </summary>
    public void ClearExpansionCandidates()
    {
        if (candidateTilemap != null) candidateTilemap.ClearAllTiles();
        currentExpansionTown = null;
    }

    /// <summary>
    /// 清除所有临时高亮（当前仅候选地块）
    /// </summary>
    public void ClearAllHighlights()
    {
        ClearExpansionCandidates();
    }

    private List<TileID> GetCandidateTiles(Town town)
    {
        List<TileID> candidates = new List<TileID>();
        HashSet<(int, int)> ownedSet = new HashSet<(int, int)>();
        foreach (var t in town.OwnedTiles)
            ownedSet.Add((t.x, t.y));

        foreach (var owned in town.OwnedTiles)
        {
            for (int i = 0; i < 6; i++)
            {
                HexDirection dir = (HexDirection)i;
                Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(owned.x, owned.y, dir);
                if (neighbor.x < 0 || neighbor.x >= MapGenerator.Instance.GetMapWidth() ||
                    neighbor.y < 0 || neighbor.y >= MapGenerator.Instance.GetMapHeight())
                    continue;

                if (ownedSet.Contains((neighbor.x, neighbor.y))) continue;

                Tile tile = MapGenerator.Instance.GetTile(neighbor.x, neighbor.y);
                if (tile == null || tile.GetOwningTown() != -1) continue;
                if (tile.GetTileData().terrainType == TerrainType.Default) continue;

                candidates.Add(new TileID { x = neighbor.x, y = neighbor.y });
            }
        }
        return candidates;
    }
}