using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NextTurnButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    private void Update()
    {
        if (buttonText == null || TurnManager.Instance == null) return;

        switch (TurnManager.Instance.GetPendingActionType())
        {
            case PendingActionType.CommandUnit:
                buttonText.text = "Command Unit";
                break;
            case PendingActionType.TownDevelopment:
                buttonText.text = "Town Development";
                break;
            default:
                buttonText.text = "Next Turn";
                break;
        }
    }

    public void OnNextTurnClicked()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.TryEndTurnForCurrentPlayer();
        }
        else
        {
            Debug.LogError("GameController 实例不存在！");
        }
    }
}