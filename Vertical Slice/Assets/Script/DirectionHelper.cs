using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection
{
    Right = 0,
    BottomRight = 1,
    BottomLeft = 2,
    Left = 3,
    TopLeft = 4,
    TopRight = 5,
}

public class DirectionHelper : MonoBehaviour
{
    public static DirectionHelper Instance;

    // 偶数行各方向的偏移量（使用值类型，避免堆分配）
    private static readonly Vector2Int[] evenRowOffsets = new Vector2Int[6]
    {
        new Vector2Int( 1,  0),  // Right
        new Vector2Int( 1,  1),  // BottomRight
        new Vector2Int( 0,  1),  // BottomLeft
        new Vector2Int(-1,  0),  // Left
        new Vector2Int( 0, -1),  // TopLeft
        new Vector2Int( 1, -1),  // TopRight
    };

    // 奇数行各方向的偏移量
    private static readonly Vector2Int[] oddRowOffsets = new Vector2Int[6]
    {
        new Vector2Int( 1,  0),  // Right
        new Vector2Int( 0,  1),  // BottomRight
        new Vector2Int(-1,  1),  // BottomLeft
        new Vector2Int(-1,  0),  // Left
        new Vector2Int(-1, -1),  // TopLeft
        new Vector2Int( 0, -1),  // TopRight
    };

    private void Awake()
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

    /// <summary>
    /// 检查指定方向上的邻居坐标是否超出地图边界
    /// </summary>
    public bool OutOfBoundsCheck(int currentX, int currentY, HexDirection direction)
    {
        Vector2Int newPos = GetDirectionOffset(currentX, currentY, direction);
        int mapWidth = MapGenerator.Instance.GetMapWidth();
        int mapHeight = MapGenerator.Instance.GetMapHeight();

        // 矩形地图中所有行的列数一致，因此X轴边界无需区分奇偶行
        if (newPos.x < 0 || newPos.x >= mapWidth) return true;
        if (newPos.y < 0 || newPos.y >= mapHeight) return true;

        return false;
    }

    /// <summary>
    /// 获取指定方向上的邻居坐标（值类型返回，不产生GC）
    /// </summary>
    public Vector2Int GetDirectionOffset(int currentX, int currentY, HexDirection direction)
    {
        bool isEvenRow = (currentY % 2 == 0);
        Vector2Int offset = isEvenRow ? evenRowOffsets[(int)direction] : oddRowOffsets[(int)direction];
        return new Vector2Int(currentX + offset.x, currentY + offset.y);
    }

    /// <summary>
    /// 获取当前坐标所有有效（边界内）的邻居坐标列表
    /// </summary>
    public List<Vector2Int> GetAllValidNeighbors(int currentX, int currentY)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        for (int i = 0; i < 6; i++)
        {
            HexDirection dir = (HexDirection)i;
            if (!OutOfBoundsCheck(currentX, currentY, dir))
            {
                neighbors.Add(GetDirectionOffset(currentX, currentY, dir));
            }
        }
        return neighbors;
    }
}