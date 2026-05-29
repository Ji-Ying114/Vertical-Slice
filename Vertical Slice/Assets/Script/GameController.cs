using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    public static GameController Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameController>();
            return instance;
        }
    }

    [Header("References")]
    [SerializeField] private MapInteractionHelper mapInteractionHelper;

    [Header("Game Settings")]
    public static int playerCount = 1;
    public static int currentPlayer = 1;

    public delegate void OnNextTurnEventHandler(int playerNumber);
    public static event OnNextTurnEventHandler OnNextTurn;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
        {
            Vector3 mouseMapPos = mapInteractionHelper.GetMouseMapPosition();
        }
    }

    /// <summary>
    /// 尝试为当前玩家结束回合。若有待处理单位/城镇则跳转，否则切换玩家或结束回合。
    /// </summary>
    public void TryEndTurnForCurrentPlayer()
    {
        // 先检查是否有未完成的行动
        if (TurnManager.Instance.TryProcessPendingActions())
            return; // 已跳转到单位/城镇，不结束回合

        // 所有事项已完成，进入下一个玩家或结束回合
        if (currentPlayer < playerCount)
        {
            currentPlayer++;
            OnNextTurn?.Invoke(currentPlayer);
        }
        else
        {
            currentPlayer = 1;
            TurnManager.Instance.NextTurn();
            OnNextTurn?.Invoke(currentPlayer);
        }
    }

    // 保留原有的 NextTurn 方法（若其他处有调用）
    public void NextTurn()
    {
        TryEndTurnForCurrentPlayer();
    }
}