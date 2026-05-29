using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TownLabel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text populationText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.3f, 0);

    private Town town;
    private RectTransform rectTransform;

    // 缓存上一次的位置，避免重复赋值
    private Vector3 lastWorldPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lastWorldPos = Vector3.negativeInfinity;   // 确保第一帧必定更新
    }

    public void SetTown(Town newTown)
    {
        town = newTown;
        if (town != null)
            UpdateDisplay();
    }

    // 将每帧位置更新移到 LateUpdate，确保在所有 Update / UI 事件完成后再执行
    void LateUpdate()
    {
        if (town == null) return;

        TileID pos = town.GetPosition();
        Vector3 worldPos = MapGenerator.Instance.worldPosition(pos.x, pos.y) + worldOffset;

        // 只有位置真正变化时才更新，避免不必要的重建
        if (worldPos != lastWorldPos)
        {
            rectTransform.position = worldPos;
            lastWorldPos = worldPos;
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (town == null) return;

        if (nameText != null)
            nameText.text = town.GetName();
        if (populationText != null)
            populationText.text = town.GetPopulation().ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (town != null && TownPanel.Instance != null)
        {
            TownPanel.Instance.ShowTown(town);
        }
    }
}