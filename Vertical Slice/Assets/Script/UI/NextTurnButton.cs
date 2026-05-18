using UnityEngine;
using UnityEngine.UI;

public class NextTurnButton : MonoBehaviour
{
    public void OnNextTurnClicked()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.NextTurn();
            Debug.Log("下一回合按钮被点击，正在进入下一回合...");
        }
        else
        {
            Debug.LogError("GameController 实例不存在！");
        }
    }
}