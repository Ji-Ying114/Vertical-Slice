using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : ScriptableObject
{
    public string unitName;
    public int movementPoint;

    public abstract void Move();
}
