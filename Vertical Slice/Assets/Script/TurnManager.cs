using System;
using System.Collections.Generic;
using UnityEngine;

public enum PendingActionType
{
    None,
    CommandUnit,
    TownDevelopment
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<int> OnTurnChanged;
    private int currentTurn = 1;
    public int CurrentTurn => currentTurn;

    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MoveableTileDisplayer moveableTileDisplayer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (selectionManager == null) selectionManager = FindObjectOfType<SelectionManager>();
        if (moveableTileDisplayer == null) moveableTileDisplayer = FindObjectOfType<MoveableTileDisplayer>();
        OnTurnChanged?.Invoke(currentTurn);
    }

    public void NextTurn()
    {
        currentTurn++;
        ResetAllUnitsMovement();

        if (TownManager.Instance != null)
            TownManager.Instance.ProcessTurn();

        OnTurnChanged?.Invoke(currentTurn);

        if (moveableTileDisplayer != null)
            moveableTileDisplayer.RefreshDisplay();

        Debug.Log($"回合结束，进入第 {currentTurn} 回合。");
    }

    /// <summary>
    /// 检查当前玩家是否有待处理的单位或城镇扩张，如果有则跳转并返回 true；否则 false
    /// </summary>
    public bool TryProcessPendingActions()
    {
        // 1. 寻找满移动力且空闲的单位
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit.UnitData == null) continue;
            if (unit.currentMovementPoint >= unit.UnitData.movementPoint && unit.currentState == CurrentState.Idle)
            {
                SelectAndFocusOnUnit(unit);
                return true;
            }
        }

        // 2. 寻找有闲置人口的城镇
        Town town = TownManager.Instance?.GetTownWithAvailableExpansion();
        if (town != null)
        {
            FocusOnTown(town);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 返回当前待处理动作类型（不执行任何操作）
    /// </summary>
    public PendingActionType GetPendingActionType()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit.UnitData == null) continue;
            if (unit.currentMovementPoint >= unit.UnitData.movementPoint && unit.currentState == CurrentState.Idle)
                return PendingActionType.CommandUnit;
        }

        Town town = TownManager.Instance?.GetTownWithAvailableExpansion();
        if (town != null)
            return PendingActionType.TownDevelopment;

        return PendingActionType.None;
    }

    private void SelectAndFocusOnUnit(Unit unit)
    {
        if (selectionManager != null)
            selectionManager.Select(unit.gameObject);

        Vector3 worldPos = MapGenerator.Instance.worldPosition(unit.currentX, unit.currentY);
        Camera.main.transform.position = new Vector3(worldPos.x, worldPos.y, Camera.main.transform.position.z);
    }

    private void FocusOnTown(Town town)
    {
        TileID pos = town.GetPosition();
        Vector3 worldPos = MapGenerator.Instance.worldPosition(pos.x, pos.y);
        Camera.main.transform.position = new Vector3(worldPos.x, worldPos.y, Camera.main.transform.position.z);

        TownPanel panel = FindObjectOfType<TownPanel>();
        if (panel != null)
            panel.ShowTown(town);
    }

    private void ResetAllUnitsMovement()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
            unit.ResetMovementPoints();
    }
}