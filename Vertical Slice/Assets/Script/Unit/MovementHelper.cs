using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 存储寻路过程中每个节点的信息（值类型，减少GC）
/// </summary>
public struct PathNode
{
    public int remainingMP;           // 到达此单元格后剩余的移动力
    public Vector2Int parent;        // 到达此单元格的父节点坐标
    public bool isFirstStep;         // 是否是从起点直接跨越的首步（用于 permissive 规则）
    
    public PathNode(int remainingMP, Vector2Int parent, bool isFirstStep = false)
    {
        this.remainingMP = remainingMP;
        this.parent = parent;
        this.isFirstStep = isFirstStep;
    }
}

/// <summary>
/// 移动范围计算结果
/// </summary>
public struct MovementResult
{
    public List<Vector2Int> reachableCells;
    public Dictionary<Vector2Int, List<Vector2Int>> paths;
    public Dictionary<Vector2Int, int> remainingMP;   // 新增：每个格子到达后的剩余移动力

    public MovementResult(List<Vector2Int> reachableCells, 
                          Dictionary<Vector2Int, List<Vector2Int>> paths,
                          Dictionary<Vector2Int, int> remainingMP)
    {
        this.reachableCells = reachableCells;
        this.paths = paths;
        this.remainingMP = remainingMP;
    }
}

public class MovementHelper : MonoBehaviour
{
    public static MovementHelper Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 根据单位数据计算其可达范围和路径
    /// </summary>
    /// <param name="unit">当前选中的单位</param>
    /// <returns>包含可达单元格列表和路径字典的结果结构体</returns>
public MovementResult CalculateMovementRange(Unit unit)
{
    // 1. 参数有效性检查
    if (unit == null || unit.UnitData == null)
    {
        Debug.LogError($"CalculateMovementRange 失败：unit 或 UnitData 为 null。unit={unit}, UnitData={unit?.UnitData}");
        return new MovementResult(new List<Vector2Int>(), new Dictionary<Vector2Int, List<Vector2Int>>(), new Dictionary<Vector2Int, int>());
    }

    // 2. 确保 MapGenerator 可用
    if (MapGenerator.Instance == null)
    {
        Debug.LogError("MapGenerator.Instance is null，无法计算移动范围。");
        return new MovementResult(new List<Vector2Int>(), new Dictionary<Vector2Int, List<Vector2Int>>(), new Dictionary<Vector2Int, int>());
    }

    // ---------- 初始化 ----------
    int startX = unit.currentX;
    int startY = unit.currentY;
    int currentMP = unit.currentMovementPoint;
    int maxMP = unit.UnitData.movementPoint;
    UnitType unitType = unit.UnitData.unitType;

    Vector2Int startPos = new Vector2Int(startX, startY);

    // 存储每个单元格的节点信息
    Dictionary<Vector2Int, PathNode> nodeInfo = new Dictionary<Vector2Int, PathNode>();
    // 开放列表（按剩余移动力降序处理）
    List<Vector2Int> openList = new List<Vector2Int>();

    nodeInfo[startPos] = new PathNode(currentMP, startPos);
    openList.Add(startPos);

    int mapWidth = MapGenerator.Instance.GetMapWidth();
    int mapHeight = MapGenerator.Instance.GetMapHeight();

    // ---------- 主循环 ----------
    while (openList.Count > 0)
    {
        // 按剩余移动力降序排序（越大越优先）
        openList.Sort((a, b) => nodeInfo[b].remainingMP.CompareTo(nodeInfo[a].remainingMP));
        Vector2Int current = openList[0];
        openList.RemoveAt(0);

        PathNode currentNode = nodeInfo[current];

        // 遍历六个六边形方向
        for (int i = 0; i < 6; i++)
        {
            HexDirection dir = (HexDirection)i;

            // 边界检查
            if (DirectionHelper.Instance.OutOfBoundsCheck(current.x, current.y, dir))
                continue;

            Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(current.x, current.y, dir);

            // 获取格子数据（增加空引用防护）
            Tile tile = MapGenerator.Instance.GetTile(neighbor.x, neighbor.y);
            if (tile == null)
            {
                // 格子不存在时应跳过，避免空引用
                Debug.LogWarning($"CalculateMovementRange: 格子 ({neighbor.x},{neighbor.y}) 不存在，已跳过");
                continue;
            }

            int moveCost = GetMovementCostForUnit(tile, unitType);
            int parentRemaining = currentNode.remainingMP;

            // 判断是否可进入 (Permissive 规则)
            int newRemaining = 0;
            bool isFirstStep = false;
            bool canEnter = false;

            if (current == startPos && parentRemaining == maxMP)
            {
                // 首步：允许进入即使消耗大于剩余移动力
                canEnter = true;
                isFirstStep = true;
                newRemaining = Mathf.Max(0, parentRemaining - moveCost);
            }
            else
            {
                if (moveCost <= parentRemaining)
                {
                    canEnter = true;
                    newRemaining = parentRemaining - moveCost;
                }
            }

            if (!canEnter)
                continue;

            // 更新或添加邻居信息
            if (nodeInfo.ContainsKey(neighbor))
            {
                // 如果新路径剩余移动力更高，则更新
                if (newRemaining > nodeInfo[neighbor].remainingMP)
                {
                    nodeInfo[neighbor] = new PathNode(newRemaining, current, isFirstStep);
                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
            else
            {
                nodeInfo[neighbor] = new PathNode(newRemaining, current, isFirstStep);
                openList.Add(neighbor);
            }
        }
    }

    // ---------- 构建结果 ----------
    Dictionary<Vector2Int, List<Vector2Int>> paths = new Dictionary<Vector2Int, List<Vector2Int>>();
    List<Vector2Int> reachableCells = new List<Vector2Int>();
    Dictionary<Vector2Int, int> remainingMPs = new Dictionary<Vector2Int, int>();

    foreach (var kvp in nodeInfo)
    {
        Vector2Int cell = kvp.Key;
        if (cell == startPos)
            continue;

        reachableCells.Add(cell);

        // 回溯路径
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int backtrack = cell;
        while (backtrack != startPos)
        {
            path.Add(backtrack);
            if (!nodeInfo.ContainsKey(backtrack))
                break;
            backtrack = nodeInfo[backtrack].parent;
        }
        path.Add(startPos);
        path.Reverse();
        paths[cell] = path;

        // 记录剩余移动力
        remainingMPs[cell] = kvp.Value.remainingMP;
    }

    return new MovementResult(reachableCells, paths, remainingMPs);
}

    /// <summary>
    /// 根据单位类型获取在指定格子上的移动力消耗
    /// </summary>
    private int GetMovementCostForUnit(Tile tile, UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Light:
                return tile.GetLightUnitMovementCost();
            case UnitType.Medium:
                return tile.GetMediumUnitMovementCost();
            case UnitType.Heavy:
                return tile.GetHeavyUnitMovementCost();
            default:
                return tile.GetLightUnitMovementCost();
        }
    }
}