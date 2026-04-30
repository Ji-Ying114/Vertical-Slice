using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Factor", menuName = "Tile/Tile Factor")]
public abstract class TileFactor : ScriptableObject
{
    [Header("Tile Factor General Settings")]
    public string factorName;
    public int time;
    private int timeCounter;
}