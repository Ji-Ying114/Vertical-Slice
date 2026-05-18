using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveableTileDisplayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileBase moveableTileBase;
    [SerializeField] private GameObject targetTilemapObject;

    private Tilemap targetTilemap;
    private GameObject currentSelectedObject; // 缓存当前选中的对象

    private void Awake()
    {
        if (targetTilemapObject != null)
        {
            targetTilemap = targetTilemapObject.GetComponent<Tilemap>();
            if (targetTilemap == null)
                Debug.LogError("MoveableTileDisplayer: 目标对象没有 Tilemap 组件。");
        }
        else
        {
            Debug.LogError("MoveableTileDisplayer: 未指定 targetTilemapObject。");
        }

        // 不再需要查找 SelectionManager，仅通过事件接收选中对象
    }

    private void OnEnable()
    {
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.SelectionChanged += OnSelectionChanged;
            selectionManager.Deselection += OnDeselection;
        }
        else
        {
            Debug.LogError("MoveableTileDisplayer: 场景中找不到 SelectionManager。");
        }
    }

    private void OnDisable()
    {
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.SelectionChanged -= OnSelectionChanged;
            selectionManager.Deselection -= OnDeselection;
        }
    }

    private void OnSelectionChanged(GameObject selectedObject)
    {
        // 缓存当前选中的对象
        currentSelectedObject = selectedObject;
        // 刷新显示
        RefreshDisplay();
    }

    private void OnDeselection()
    {
        currentSelectedObject = null;
        ClearAllTiles();
    }

    /// <summary>
    /// 外部调用（如回合结束）可强制刷新移动范围显示
    /// </summary>
    public void RefreshDisplay()
    {
        // 先清除旧显示
        ClearAllTiles();

        if (currentSelectedObject == null) return;

        Unit unit = currentSelectedObject.GetComponent<Unit>();
        if (unit == null) return;

        if (unit.currentMovementPoint <= 0) return;

        MovementResult result = MovementHelper.Instance.CalculateMovementRange(unit);
        if (result.reachableCells == null || result.reachableCells.Count == 0) return;

        if (targetTilemap == null) return;

        foreach (Vector2Int cell in result.reachableCells)
        {
            Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);
            targetTilemap.SetTile(tilePos, moveableTileBase);
        }
    }

    public void ClearAllTiles()
    {
        if (targetTilemap != null)
        {
            targetTilemap.ClearAllTiles();
        }
    }
}