using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum SelectionMode
{
    NotSelected,
    Unit,
    City
}

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Visuals")]
    [SerializeField] private Material flashyMaterial;
    [SerializeField] private Material defaultSpriteMaterial;

    [Header("Selection Audio")]
    [SerializeField] private AudioClip selectionAudio;
    [SerializeField] private float audioVolume = 1f;

    private GameObject currentSelected;
    private AudioSource audioSource;

    public delegate void OnSelectionChanged(GameObject selected);
    public event OnSelectionChanged SelectionChanged;
    public delegate void OnDeselection();
    public event OnDeselection Deselection;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 自动清理已销毁的选中对象
        if (currentSelected != null && currentSelected == null)
        {
            Deselect();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Deselect();
                return;
            }

            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = -Camera.main.transform.position.z;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector2 mousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);

            if (hitCollider != null)
            {
                Selectable selectable = hitCollider.GetComponent<Selectable>();
                if (selectable != null)
                {
                    Select(selectable.gameObject);
                    return;
                }
            }

            Deselect();
        }
    }

    public void Select(GameObject newSelected)
    {
        if (currentSelected == newSelected) return;

        if (currentSelected != null)
        {
            DeselectVisual(currentSelected);
            Deselection?.Invoke();
        }

        currentSelected = newSelected;
        SelectVisual(currentSelected);
        SelectionChanged?.Invoke(currentSelected);

        // 播放选择音频
        PlaySelectionAudio();

        if (Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
        {
            Debug.Log($"选中了 {currentSelected.name}");
        }
    }

    void Deselect()
    {
        if (currentSelected != null)
        {
            DeselectVisual(currentSelected);
            Deselection?.Invoke();

            if (Console.Instance != null && Console.Instance.debugMode == DebugMode.On)
            {
                Debug.Log("取消选中");
            }
            currentSelected = null;
        }
    }

    void SelectVisual(GameObject obj)
    {
        if (flashyMaterial == null) return;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.material = flashyMaterial;
    }

    void DeselectVisual(GameObject obj)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (defaultSpriteMaterial != null)
                sr.material = defaultSpriteMaterial;
            else
                sr.material = new Material(Shader.Find("Sprites/Default")); // 回退
        }
    }

    void PlaySelectionAudio()
    {
        if (audioSource != null && selectionAudio != null)
        {
            audioSource.PlayOneShot(selectionAudio, audioVolume);
        }
    }
}