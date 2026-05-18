using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapInteractionHelper : MonoBehaviour
{
    [SerializeField] private Grid grid;

    private Vector3 mouseMapPos;

    public Vector3Int GetMouseMapPosition()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        // 计算屏幕坐标对应的世界坐标，使结果 Z = 0
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; // 强制归零
        return Vector3Int.FloorToInt(grid.WorldToCell(mouseWorldPos));
    }
}
