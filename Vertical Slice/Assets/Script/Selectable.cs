using UnityEngine;

[System.Serializable]
public enum SelectionType
{
    Unit,
    Town,
}

public class Selectable : MonoBehaviour
{
    [SerializeField] private SelectionType selectionType;
}
