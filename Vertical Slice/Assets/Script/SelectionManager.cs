using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum SelectionMode
{
    NotSelected,
    Unit,
    City
}

public class SelectionManager : MonoBehaviour
{

    // 当前选中的物体（可用来高亮、显示信息等）
    private GameObject currentSelected;

    // 选中改变事件，外部可以订阅以响应选中变化
    public delegate void OnSelectionChanged(GameObject selected);
    public event OnSelectionChanged SelectionChanged;
    public delegate void OnDeselection();
    public event OnDeselection Deselection;

void Update()
{
    // 自动清理已销毁的选中对象
    if (currentSelected != null && currentSelected == null)
    {
        Deselect();
        return;
    }

    // 鼠标左键点击处理
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
        // 如果鼠标在 UI 上，取消选中并忽略本次点击
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Deselect();
            return;
        }

        // 正确获取鼠标世界坐标
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 mousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);

        if (hitCollider != null)
        {
            Selectable selectable = hitCollider.GetComponent<Selectable>();
            if (selectable != null)
            {
                Select(selectable.gameObject);
                return;
            }
        }

        // 点击空地或非可选中物体，取消选中
        Deselect();
    }
}

    void Select(GameObject newSelected)
    {
        // 如果点的是同一个物体，可以选择忽略或保持
        if (currentSelected == newSelected) return;

        // 取消旧选中的高亮并广播取消选中事件
        if (currentSelected != null)
        {
            DeselectVisual(currentSelected);
            Deselection?.Invoke();
        }

        // 更新选中
        currentSelected = newSelected;
        SelectVisual(currentSelected);   // 新物体高亮
        SelectionChanged?.Invoke(currentSelected); // 广播选中事件

        if (Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log($"选中了 {currentSelected.name}");
        }
    }

    void Deselect()
    {
        if (currentSelected != null)
        {
            DeselectVisual(currentSelected);
            Deselection?.Invoke(); // 广播取消选中事件

            if (Console.Instance.debugMode == DebugMode.On)
            {
                Debug.Log("取消选中");
            }
            currentSelected = null;
        }
    }

    // 下面的方法是视觉反馈，您可以根据项目自己替换
    void SelectVisual(GameObject obj)
    {
        // 简单示例：把 sprite 颜色改亮
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.yellow;
    }

    void DeselectVisual(GameObject obj)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.white;
    }
}