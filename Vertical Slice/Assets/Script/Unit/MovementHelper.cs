using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct PathNode
{
    public int remainingMP;
    public Vector2Int parent;
    
    public PathNode(int remainingMP, Vector2Int parent, bool isFirstStep = false)
    {
        this.remainingMP = remainingMP;
        this.parent = parent;
    }
}

public struct MovementResult
{
    public List<Vector2Int> reachableCells;
    public Dictionary<Vector2Int, List<Vector2Int>> paths;
    public Dictionary<Vector2Int, int> remainingMP;

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
    private static MovementHelper instance;
    public static MovementHelper Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MovementHelper>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MovementHelper");
                    instance = go.AddComponent<MovementHelper>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    public MovementResult CalculateMovementRange(Unit unit)
    {
        if (unit == null || unit.UnitData == null)
        {
            Debug.LogError($"CalculateMovementRange 失败：unit 或 UnitData 为 null。");
            return new MovementResult(new List<Vector2Int>(), new Dictionary<Vector2Int, List<Vector2Int>>(), new Dictionary<Vector2Int, int>());
        }

        if (MapGenerator.Instance == null)
        {
            Debug.LogError("MapGenerator.Instance is null，无法计算移动范围。");
            return new MovementResult(new List<Vector2Int>(), new Dictionary<Vector2Int, List<Vector2Int>>(), new Dictionary<Vector2Int, int>());
        }

        int startX = unit.currentX;
        int startY = unit.currentY;
        int currentMP = unit.currentMovementPoint;
        int maxMP = unit.UnitData.movementPoint;
        UnitType unitType = unit.UnitData.unitType;
        Vector2Int startPos = new Vector2Int(startX, startY);

        Dictionary<Vector2Int, PathNode> nodeInfo = new Dictionary<Vector2Int, PathNode>();
        List<Vector2Int> openList = new List<Vector2Int>();

        nodeInfo[startPos] = new PathNode(currentMP, startPos);
        openList.Add(startPos);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => nodeInfo[b].remainingMP.CompareTo(nodeInfo[a].remainingMP));
            Vector2Int current = openList[0];
            openList.RemoveAt(0);

            PathNode currentNode = nodeInfo[current];

            for (int i = 0; i < 6; i++)
            {
                HexDirection dir = (HexDirection)i;

                if (DirectionHelper.Instance.OutOfBoundsCheck(current.x, current.y, dir))
                    continue;

                Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(current.x, current.y, dir);

                Tile tile = MapGenerator.Instance.GetTile(neighbor.x, neighbor.y);
                if (tile == null)
                {
                    Debug.LogWarning($"CalculateMovementRange: 格子 ({neighbor.x},{neighbor.y}) 不存在，已跳过");
                    continue;
                }

                int moveCost = GetMovementCostForUnit(tile, unitType);
                int parentRemaining = currentNode.remainingMP;
                int newRemaining = 0;
                bool canEnter = false;

                if (current == startPos && parentRemaining == maxMP)
                {
                    canEnter = true;
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

                if (nodeInfo.ContainsKey(neighbor))
                {
                    if (newRemaining > nodeInfo[neighbor].remainingMP)
                    {
                        nodeInfo[neighbor] = new PathNode(newRemaining, current, false);
                        if (!openList.Contains(neighbor))
                            openList.Add(neighbor);
                    }
                }
                else
                {
                    nodeInfo[neighbor] = new PathNode(newRemaining, current, false);
                    openList.Add(neighbor);
                }
            }
        }

        Dictionary<Vector2Int, List<Vector2Int>> paths = new Dictionary<Vector2Int, List<Vector2Int>>();
        List<Vector2Int> reachableCells = new List<Vector2Int>();
        Dictionary<Vector2Int, int> remainingMPs = new Dictionary<Vector2Int, int>();

        foreach (var kvp in nodeInfo)
        {
            Vector2Int cell = kvp.Key;
            if (cell == startPos)
                continue;

            reachableCells.Add(cell);

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int backtrack = cell;
            bool validPath = true;

            while (backtrack != startPos)
            {
                path.Add(backtrack);
                if (!nodeInfo.ContainsKey(backtrack))
                {
                    validPath = false;
                    break;
                }

                Vector2Int parent = nodeInfo[backtrack].parent;
                if (!AreNeighbors(backtrack, parent))
                {
                    validPath = false;
                    break;
                }

                backtrack = parent;
            }

            if (validPath)
            {
                path.Add(startPos);
                path.Reverse();
                paths[cell] = path;
            }
            else
            {
                paths[cell] = new List<Vector2Int> { startPos, cell };
            }

            remainingMPs[cell] = kvp.Value.remainingMP;
        }

        return new MovementResult(reachableCells, paths, remainingMPs);
    }

    private bool AreNeighbors(Vector2Int a, Vector2Int b)
    {
        for (int i = 0; i < 6; i++)
        {
            HexDirection dir = (HexDirection)i;
            if (DirectionHelper.Instance.OutOfBoundsCheck(a.x, a.y, dir))
                continue;
            Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(a.x, a.y, dir);
            if (neighbor == b)
                return true;
        }
        return false;
    }

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

    public List<TileID> GetTilesInRange(TileID center, int range)
    {
        List<TileID> result = new List<TileID>();
        if (range < 0 || MapGenerator.Instance == null)
            return result;

        Vector2Int centerPos = new Vector2Int(center.x, center.y);
        Queue<(Vector2Int pos, int dist)> queue = new Queue<(Vector2Int, int)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue((centerPos, 0));
        visited.Add(centerPos);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            result.Add(new TileID { x = current.x, y = current.y });

            if (dist == range)
                continue;

            for (int i = 0; i < 6; i++)
            {
                HexDirection dir = (HexDirection)i;
                if (DirectionHelper.Instance.OutOfBoundsCheck(current.x, current.y, dir))
                    continue;

                Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(current.x, current.y, dir);
                if (visited.Contains(neighbor))
                    continue;

                Tile tile = MapGenerator.Instance.GetTile(neighbor.x, neighbor.y);
                if (tile == null)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, dist + 1));
            }
        }

        return result;
    }
}