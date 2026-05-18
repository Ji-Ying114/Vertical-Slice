using TMPro;  // 需要引用 TextMeshPro
using UnityEngine;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    private PlayerInput playerInput;
    [SerializeField] private GameObject console;
    [SerializeField] private TMP_Text turnText;   // 新增：回合数显示文本

    private void Start()
    {
        if (turnText == null)
            turnText = GetComponentInChildren<TMP_Text>();
        
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnChanged += UpdateTurnDisplay;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnChanged -= UpdateTurnDisplay;
    }

    private void UpdateTurnDisplay(int newTurn)
    {
        if (turnText != null)
            turnText.text = $"Turn {newTurn}";
    }

    public void ToggleConsole(InputAction.CallbackContext context)
    {
        if (Console.Instance != null)
        {
            Console.Instance.ToggleConsole();
        }
        else
        {
            Debug.LogWarning("Console instance not found.");
        }
    }
}