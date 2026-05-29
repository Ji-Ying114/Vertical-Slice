using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CurrentState
{
    Idle,
    Move,
    Attack,
}

public class Unit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject thisUnit;
    [SerializeField] private Transform unitTransform;
    [SerializeField] private UnitData unitData;
    [SerializeField] private GameObject lightPillar;

    [Header("Runtime Data")]
    [SerializeField] public int currentMovementPoint;
    [SerializeField] public int currentHp;
    [SerializeField] public int currentX;
    [SerializeField] public int currentY;

    public CurrentState currentState = CurrentState.Idle;

    public UnitData UnitData => unitData;

    private SelectionManager selectionManager;

    void OnEnable()
    {
        currentMovementPoint = unitData.movementPoint;
        currentHp = unitData.hp;
        transform.position = new Vector3(0, 0, 1);

        // 初始化lightPillar为隐藏状态
        if (lightPillar != null)
        {
            lightPillar.SetActive(false);
        }

        // 订阅选择事件
        if (selectionManager == null)
            selectionManager = FindObjectOfType<SelectionManager>();

        if (selectionManager != null)
        {
            selectionManager.SelectionChanged += OnUnitSelected;
            selectionManager.Deselection += OnUnitDeselected;
        }
    }

    void OnDisable()
    {
        // 取消订阅选择事件
        if (selectionManager != null)
        {
            selectionManager.SelectionChanged -= OnUnitSelected;
            selectionManager.Deselection -= OnUnitDeselected;
        }
    }

    public void InitPosition(int x, int y)
    {
        thisUnit.SetActive(true);
        currentX = x;
        currentY = y;
        transform.position = MapGenerator.Instance.worldPosition(x, y);
    }
    public void ReInit(int movementPoint)
    {
        currentMovementPoint = movementPoint;
    }
    public void ReInit(float hpPercentage)
    {
        currentHp = Mathf.RoundToInt(unitData.hp * hpPercentage);
    }
    public void ReInit(int movementPoint, float hpPercentage)
    {
        currentMovementPoint = movementPoint;
        currentHp = Mathf.RoundToInt(unitData.hp * hpPercentage);
    }

    public void ChangePosition(int x, int y)
    {
        currentX = x;
        currentY = y;
        transform.position = MapGenerator.Instance.worldPosition(x, y);
    }

    public void ResetMovementPoints()
    {
        currentMovementPoint = unitData.movementPoint;
    }
    public void CheckPosition()
    {
        if (transform.position != MapGenerator.Instance.worldPosition(currentX, currentY))
        {
            Debug.LogWarning($"Unit {name} position mismatch: expected ({currentX}, {currentY}), actual {transform.position}");
            transform.position = MapGenerator.Instance.worldPosition(currentX, currentY);
        }
    }
    public void CheckDestroy()
    {
        if (currentHp <= 0)
        {
            Destroy(thisUnit);
        }
    }

    private void OnUnitSelected(GameObject selectedObject)
    {
        // 如果选中的是这个单位，就显示lightPillar
        if (selectedObject == thisUnit && lightPillar != null)
        {
            lightPillar.SetActive(true);
        }
    }

    private void OnUnitDeselected()
    {
        // 取消选中时隐藏lightPillar
        if (lightPillar != null)
        {
            lightPillar.SetActive(false);
        }
    }
}
