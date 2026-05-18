using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Unit unit;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float arrivalDistance = 0.05f;

    private bool isMoving = false;
    private Coroutine movementCoroutine;
    private List<Vector2Int> currentPath;
    private List<Vector3> worldPositions;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (unit == null) unit = GetComponent<Unit>();
    }

    private void Update()
    {
        // 非移动时处理空闲/攻击动画（与方向无关）
        if (!isMoving && animator != null)
        {
            int state = 0;
            if (unit.currentState == CurrentState.Attack)
                state = 2;
            animator.SetInteger("State", state);
        }
    }

    public void StartMovement(List<Vector2Int> path, System.Action onComplete = null)
    {
        if (path == null || path.Count < 2)
        {
            onComplete?.Invoke();
            return;
        }

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        currentPath = new List<Vector2Int>(path);
        worldPositions = new List<Vector3>();
        foreach (var gridPos in currentPath)
        {
            worldPositions.Add(MapGenerator.Instance.worldPosition(gridPos.x, gridPos.y));
        }

        unit.currentState = CurrentState.Move;
        isMoving = true;
        if (animator != null)
        {
            animator.SetInteger("State", 1);   // 播放移动动画层
            // 设置初始方向（从起点到第一个目标点）
            SetDirectionByPath(currentPath[0], currentPath[1]);
        }

        movementCoroutine = StartCoroutine(MoveAlongPath(onComplete));
    }

    private IEnumerator MoveAlongPath(System.Action onComplete)
    {
        transform.position = worldPositions[0];

        for (int i = 1; i < worldPositions.Count; i++)
        {
            Vector3 targetPos = worldPositions[i];
            Vector2Int currentGrid = currentPath[i - 1];
            Vector2Int nextGrid = currentPath[i];

            // 每步移动前设置正确的方向
            SetDirectionByPath(currentGrid, nextGrid);

            while (Vector3.Distance(transform.position, targetPos) > arrivalDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
        }

        isMoving = false;
        unit.currentState = CurrentState.Idle;
        if (animator != null)
            animator.SetInteger("State", 0);   // 回到 Idle

        movementCoroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 根据两个相邻格子坐标计算 HexDirection，并设置 Animator 的 Direction 参数（0~5）
    /// </summary>
    private void SetDirectionByPath(Vector2Int from, Vector2Int to)
    {
        HexDirection direction = GetDirectionBetweenHexes(from, to);
        if (animator != null)
        {
            animator.SetInteger("Direction", (int)direction);
        }
    }

    private HexDirection GetDirectionBetweenHexes(Vector2Int from, Vector2Int to)
    {
        for (int i = 0; i < 6; i++)
        {
            HexDirection dir = (HexDirection)i;
            Vector2Int neighbor = DirectionHelper.Instance.GetDirectionOffset(from.x, from.y, dir);
            if (neighbor.x == to.x && neighbor.y == to.y)
                return dir;
        }
        Debug.LogWarning($"无法找到从 {from} 到 {to} 的六边形方向");
        return HexDirection.Right;
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        isMoving = false;
        if (unit != null)
            unit.currentState = CurrentState.Idle;
        if (animator != null)
            animator.SetInteger("State", 0);
    }

    public bool IsMoving => isMoving;
}