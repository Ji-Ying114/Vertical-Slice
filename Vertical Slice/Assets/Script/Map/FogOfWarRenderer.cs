using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogOfWarRenderer : MonoBehaviour
{
    public static FogOfWarRenderer Instance;

    [SerializeField] private Tilemap fogTilemap;
    [SerializeField] private TileBase unknownTile;
    [SerializeField] private float vanishDuration = 1.0f;

    private HashSet<Vector3Int> vanishingCells = new HashSet<Vector3Int>();
    private int currentPlayer = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 强制设置材质，确保支持顶点颜色
        if (fogTilemap != null)
        {
            var renderer = fogTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null && renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                Debug.Log("[FogOfWarRenderer] 已自动设置 Sprites/Default 材质");
            }
        }
    }

    public void InitializeFogMap()
    {
        if (fogTilemap == null || unknownTile == null || MapGenerator.Instance == null) return;

        int width = MapGenerator.Instance.GetMapWidth();
        int height = MapGenerator.Instance.GetMapHeight();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                fogTilemap.SetTile(pos, unknownTile);
                fogTilemap.SetColor(pos, Color.white);
            }
        }
    }

    public void UpdateFogDisplay()
    {
        if (fogTilemap == null || MapGenerator.Instance == null) return;

        int width = MapGenerator.Instance.GetMapWidth();
        int height = MapGenerator.Instance.GetMapHeight();
        currentPlayer = GameController.currentPlayer;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Tile tile = MapGenerator.Instance.GetTile(x, y);
                if (tile == null) continue;

                bool isKnown = tile.IsKnownByPlayer(currentPlayer);
                if (isKnown)
                {
                    // 已知但迷雾还在且未开始消失动画 → 启动淡出
                    if (fogTilemap.GetTile(pos) != null && !vanishingCells.Contains(pos))
                    {
                        StartCoroutine(TestRevealTile(pos));
                        vanishingCells.Add(pos);
                    }
                }
                else
                {
                    // 未知区域：确保有迷雾瓦片，且颜色为白色（不透明）
                    if (fogTilemap.GetTile(pos) == null)
                    {
                        fogTilemap.SetTile(pos, unknownTile);
                        fogTilemap.SetColor(pos, Color.white);
                    }
                }
            }
        }
    }

    private IEnumerator TestRevealTile(Vector3Int position)
    {
        float elapsed = 0f;
        Color startColor = Color.white;
        Color endColor = new Color(1, 1, 1, 0);

        // 确保瓦片存在
        if (fogTilemap.GetTile(position) == null)
        {
            Debug.LogWarning($"[FogOfWar] TestRevealTile: 位置 {position} 无瓦片，跳过渐变");
            vanishingCells.Remove(position);
            yield break;
        }

        while (elapsed < vanishDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vanishDuration;
            Color currentColor = Color.Lerp(startColor, endColor, t);
            fogTilemap.SetColor(position, currentColor);

            // 每5帧输出一次当前颜色，用于调试
            if (Time.frameCount % 5 == 0 && Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
            {
                Debug.Log($"Fog vanish at {position}: alpha = {currentColor.a:F2}");
            }

            yield return null;
        }

        // 渐变结束后移除瓦片
        fogTilemap.SetTile(position, null);
        vanishingCells.Remove(position);
    }

    /// <summary>
    /// 空的 RevealTile 方法，留作以后实现 shader 效果。
    /// </summary>
    private void RevealTile(Vector3Int position)
    {
        // TODO: 实现基于 shader 的揭示效果
    }
}