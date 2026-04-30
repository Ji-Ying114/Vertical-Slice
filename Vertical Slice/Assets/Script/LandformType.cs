using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LandOrMarine
{
    Land,
    Marine,
    Both,
}


public abstract class LandformType : ScriptableObject
{
    [Header("General Settings")]
    public string typeName;
    public LandOrMarine landOrMarine;
    public float elevationChanceFactor;
    public float humidityChanceFactor;
    public float temperatureChanceFactor;
}