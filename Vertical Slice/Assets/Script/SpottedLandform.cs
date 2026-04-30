using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Size
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
}

[System.Serializable]
public struct SizeSetting
{
    public Size size;
    public float chance;
}

[CreateAssetMenu(fileName = "New Spotted Landform", menuName = "Landform/Spotted Landform")]
public class SpottedLandform : LandformType
{
    [Header("Spotted Landform Settings")]
    public List<SizeSetting> sizeSettings;
}