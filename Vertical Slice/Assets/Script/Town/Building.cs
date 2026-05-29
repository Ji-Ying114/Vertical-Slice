using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building")]
public class Building : ScriptableObject
{
    public string townsName;
    public ResourceProduction cost;
    public ResourceProduction production;
    public ResourceProduction maintenance;
    public Building[] prerequisite;
}
