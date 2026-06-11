using System.Collections.Generic;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 更新指定玩家的已知区域（knownToPlayer），基于所有单位和城镇的视野范围。
    /// </summary>
    public void UpdateKnownTiles(int playerIndex)
    {
        if (MapGenerator.Instance == null || MovementHelper.Instance == null) return;

        HashSet<TileID> tilesToReveal = new HashSet<TileID>();

        // 收集所有单位视野
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            if (unit.UnitData == null) continue;
            // 单玩家模式，所有单位都属于当前玩家
            if (playerIndex != GameController.currentPlayer) continue;

            List<TileID> visible = MovementHelper.Instance.GetTilesInRange(
                new TileID { x = unit.currentX, y = unit.currentY },
                unit.UnitData.visionRange);
            foreach (TileID tid in visible)
                tilesToReveal.Add(tid);
        }

        // 收集所有城镇视野（默认半径2）
        if (TownManager.Instance != null)
        {
            foreach (Town town in TownManager.Instance.GetTowns())
            {
                if (town.GetOwningPlayer() != playerIndex) continue;
                List<TileID> visible = MovementHelper.Instance.GetTilesInRange(town.GetPosition(), 2);
                foreach (TileID tid in visible)
                    tilesToReveal.Add(tid);
            }
        }

        // 标记已知
        foreach (TileID tid in tilesToReveal)
        {
            Tile tile = MapGenerator.Instance.GetTile(tid.x, tid.y);
            if (tile != null)
                tile.RevealToPlayer(playerIndex);
        }
    }
}