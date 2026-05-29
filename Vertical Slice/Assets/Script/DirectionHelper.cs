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
    private static DirectionHelper instance;
    public static DirectionHelper Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DirectionHelper>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DirectionHelper");
                    instance = go.AddComponent<DirectionHelper>();
                }
            }
            return instance;
        }
    }

    // 奇数行偏移（原偶数行表，现用于奇数行，因为Grid默认Odd Row）
    private static readonly Vector2Int[] oddRowOffsets = new Vector2Int[6]
    {
        new Vector2Int( 1,  0),  // Right
        new Vector2Int( 1,  1),  // BottomRight
        new Vector2Int( 0,  1),  // BottomLeft
        new Vector2Int(-1,  0),  // Left
        new Vector2Int( 0, -1),  // TopLeft
        new Vector2Int( 1, -1),  // TopRight
    };

    // 偶数行偏移（原奇数行表）
    private static readonly Vector2Int[] evenRowOffsets = new Vector2Int[6]
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
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool OutOfBoundsCheck(int currentX, int currentY, HexDirection direction)
    {
        Vector2Int newPos = GetDirectionOffset(currentX, currentY, direction);
        int mapWidth = MapGenerator.Instance.GetMapWidth();
        int mapHeight = MapGenerator.Instance.GetMapHeight();

        if (newPos.x < 0 || newPos.x >= mapWidth) return true;
        if (newPos.y < 0 || newPos.y >= mapHeight) return true;

        return false;
    }

    public Vector2Int GetDirectionOffset(int currentX, int currentY, HexDirection direction)
    {
        // 修复：奇数行使用向右偏移的表，偶数行使用向左偏移的表（默认Odd Row布局）
        bool isOddRow = (currentY % 2 == 1);
        Vector2Int offset = isOddRow ? oddRowOffsets[(int)direction] : evenRowOffsets[(int)direction];
        return new Vector2Int(currentX + offset.x, currentY + offset.y);
    }

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