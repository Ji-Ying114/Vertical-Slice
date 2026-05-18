using UnityEngine;

[System.Serializable]
public enum SelectionType
{
    Unit,
    City,
}

public class Selectable : MonoBehaviour
{
    [SerializeField] private SelectionType selectionType;
}
