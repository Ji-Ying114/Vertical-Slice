using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    private PlayerInput playerInput;
    [SerializeField] private GameObject console;

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
