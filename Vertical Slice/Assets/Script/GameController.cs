using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // 单例实例
    private static GameController instance;

    public static GameController Instance
    {
        get
        {
            return instance;
        }
    }

    [Header("References")]
    [SerializeField] private MapInteractionHelper mapInteractionHelper;

    [Header("Game Settings")]
    [SerializeField] public static int playerCount = 1;

    public static int currentPlayer = 1;

    // 定义下一回合事件的委托，传递当前玩家参数
    public delegate void OnNextTurnEventHandler(int playerNumber);
    // 下一回合事件
    public static event OnNextTurnEventHandler OnNextTurn;

    void Update()
    {
        if (Console.Instance.debugMode == DebugMode.On)
        {
            Vector3 mouseMapPos = mapInteractionHelper.GetMouseMapPosition();
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextTurn()
    {
        if (currentPlayer < playerCount)
        {
            currentPlayer++;
        }
        else
        {
            currentPlayer = 1;
            TurnManager.Instance.NextTurn(); // 进入下一回合
        }

        // 触发下一回合事件，传递当前玩家参数
        OnNextTurn?.Invoke(currentPlayer);
    }
}
