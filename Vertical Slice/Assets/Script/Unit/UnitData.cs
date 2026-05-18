using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Light,
    Medium,
    Heavy
}

[CreateAssetMenu(fileName = "New Unit Data", menuName = "Unit Data")]
public class UnitData : ScriptableObject
{
    public UnitType unitType;
    public string unitName;
    public int movementPoint;
    public int hp;
    public int visionRange;
}
