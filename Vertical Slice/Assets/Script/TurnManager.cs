using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<int> OnTurnChanged; // 参数为新回合数

    private int currentTurn = 1;
    public int CurrentTurn => currentTurn;

    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MoveableTileDisplayer moveableTileDisplayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (selectionManager == null)
            selectionManager = FindObjectOfType<SelectionManager>();
        if (moveableTileDisplayer == null)
            moveableTileDisplayer = FindObjectOfType<MoveableTileDisplayer>();

        // 初始化时触发一次回合显示
        OnTurnChanged?.Invoke(currentTurn);
    }

    /// <summary>
    /// 结束当前回合，进入下一回合
    /// </summary>
    public void NextTurn()
    {
        // 1. 增加回合数
        currentTurn++;
        
        // 2. 重置所有单位的移动点
        ResetAllUnitsMovement();
        
        // 3. 触发回合改变事件（更新UI）
        OnTurnChanged?.Invoke(currentTurn);
        
        // 4. 刷新移动范围显示（如果当前有选中单位）
        if (moveableTileDisplayer != null)
            moveableTileDisplayer.RefreshDisplay();
        
        Debug.Log($"回合结束，进入第 {currentTurn} 回合。所有单位移动点已重置。");
    }

    private void ResetAllUnitsMovement()
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            unit.ResetMovementPoints();
        }
    }
}