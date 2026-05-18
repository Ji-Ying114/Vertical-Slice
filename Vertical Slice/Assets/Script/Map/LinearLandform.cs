using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SurroundingData
{
    Elevation,
    Humidity,
    Temperature,
}
[System.Serializable]
public enum SelectValue
{   
    None,

    seaLevel,
    hillLevel,

    frigidLevel,
    temperateLevel,

    desertLevel,
    grasslandLevel,
    forestLevel,
}
[System.Serializable]
public enum PathwayControl
{
    CompleteRandom,
    Lowest,
    SecondLowest,
    ThirdLowest,
    ThirdHighest,
    SecondHighest,
    Highest,
}
[System.Serializable]
public struct StartingLandform
{
    public LandformType LandformType;
    public float chance;
}
[System.Serializable]
public struct EndingLandform
{
    public LandformType LandformType;
    public float chance;
}
[System.Serializable]
public struct SurroundingCondition
{   
    public SelectValue Value1;
    public SelectValue Value2;
}
[System.Serializable]
public struct DirectionSettings
{   
    public SurroundingData dataToBeCompared;
    public PathwayControl pathwayControl;
    public float weight;
}

[CreateAssetMenu(fileName = "New Linear Landform", menuName = "Landform/Linear Landform")]
public class LinearLandform : LandformType
{
    [Header("Linear Landform Settings")]
    public List<StartingLandform> startingLandformSettings;
    public List<EndingLandform> endingLandformSettings;
    public SurroundingCondition surroundingSetting;
    public List<DirectionSettings> directionSettings;
    public float curvatureChanceWhenRandomGeneration;
}
