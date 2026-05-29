using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UnitController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MapInteractionHelper mapInteractionHelper;
    [SerializeField] private MoveableTileDisplayer moveableTileDisplayer;

    public static UnitController Instance;

    private bool isMoving = false;
    private Unit currentSelectedUnit;
    private MovementResult currentMovementRange;
    private bool hasValidMovementRange = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of UnitController detected. Destroying duplicate.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (selectionManager == null)
            selectionManager = FindObjectOfType<SelectionManager>();
        if (mapInteractionHelper == null)
            mapInteractionHelper = FindObjectOfType<MapInteractionHelper>();

        if (selectionManager != null)
        {
            selectionManager.SelectionChanged += OnUnitSelected;
            selectionManager.Deselection += OnUnitDeselected;
        }
        else
        {
            Debug.LogError("UnitController: SelectionManager not found!");
        }
    }

    private void OnDisable()
    {
        if (selectionManager != null)
        {
            selectionManager.SelectionChanged -= OnUnitSelected;
            selectionManager.Deselection -= OnUnitDeselected;
        }
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (currentSelectedUnit == null || !hasValidMovementRange || isMoving)
                return;

            Vector3Int mouseGridPos = mapInteractionHelper.GetMouseMapPosition();
            Vector2Int targetPos = new Vector2Int(mouseGridPos.x, mouseGridPos.y);

            if (currentMovementRange.reachableCells.Contains(targetPos))
            {
                TryMoveTo(targetPos);
            }
        }
    }

    private void OnUnitSelected(GameObject selectedObject)
    {
        Unit unit = selectedObject.GetComponent<Unit>();
        if (unit == null) return;

        currentSelectedUnit = unit;
        RefreshMovementRange();
    }

    private void OnUnitDeselected()
    {
        currentSelectedUnit = null;
        hasValidMovementRange = false;
        currentMovementRange = default;
    }

    private void RefreshMovementRange()
    {
        if (currentSelectedUnit == null)
        {
            hasValidMovementRange = false;
            return;
        }

        if (currentSelectedUnit.currentMovementPoint <= 0)
        {
            hasValidMovementRange = false;
            currentMovementRange = new MovementResult(
                new List<Vector2Int>(),
                new Dictionary<Vector2Int, List<Vector2Int>>(),
                new Dictionary<Vector2Int, int>()
            );
            return;
        }

        currentMovementRange = MovementHelper.Instance.CalculateMovementRange(currentSelectedUnit);
        hasValidMovementRange = currentMovementRange.reachableCells.Count > 0;
    }

    private void TryMoveTo(Vector2Int targetPos)
    {
        if (!hasValidMovementRange || currentSelectedUnit == null)
            return;

        if (isMoving)
            return;

        if (!currentMovementRange.remainingMP.TryGetValue(targetPos, out int remainingAfterMove))
        {
            Debug.LogWarning("目标格子剩余移动力未记录");
            return;
        }
        if (!currentMovementRange.paths.TryGetValue(targetPos, out List<Vector2Int> path))
        {
            Debug.LogWarning("目标格子路径未记录");
            return;
        }

        int startMP = currentSelectedUnit.currentMovementPoint;
        int consumedMP = startMP - remainingAfterMove;

        // 保存本地引用，防止动画期间选中状态改变导致回调空引用
        Unit movingUnit = currentSelectedUnit;

        // 立刻扣除移动力
        movingUnit.currentMovementPoint = remainingAfterMove;

        // 锁定移动，防止动画期间再次点击
        isMoving = true;

        // 暂时隐藏旧的移动范围
        if (moveableTileDisplayer != null)
            moveableTileDisplayer.ClearAllTiles();

        // 执行移动动画或瞬移
        UnitAnimation anim = movingUnit.GetComponent<UnitAnimation>();
        if (anim != null)
        {
            if (anim.IsMoving) return;

            anim.StartMovement(path, () =>
            {
                // 动画完成：更新单位逻辑坐标
                movingUnit.ChangePosition(targetPos.x, targetPos.y);

                // 解锁移动
                isMoving = false;

                // 刷新移动范围（如果仍然选中该单位）
                if (currentSelectedUnit == movingUnit)
                {
                    RefreshMovementRange();
                    if (moveableTileDisplayer != null)
                        moveableTileDisplayer.RefreshDisplay();
                }
            });
        }
        else
        {
            // 无动画组件，直接瞬移
            movingUnit.ChangePosition(targetPos.x, targetPos.y);

            isMoving = false;

            if (currentSelectedUnit == movingUnit)
            {
                RefreshMovementRange();
                if (moveableTileDisplayer != null)
                    moveableTileDisplayer.RefreshDisplay();
            }
        }

        if (Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log($"单位移动到 ({targetPos.x}, {targetPos.y})，消耗 {consumedMP} 移动力，剩余 {remainingAfterMove}");
        }
    }
}