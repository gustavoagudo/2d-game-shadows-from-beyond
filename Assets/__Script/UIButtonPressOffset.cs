using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Moves the button's label down while it is pressed, matching the "pressed" sprite that sinks 1 art pixel (4 px on screen).
[RequireComponent(typeof(Button))]
public class UIButtonPressOffset : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] float offset = 4f;

    Button button;
    RectTransform label;
    Vector2 basePos;
    bool hasBase;

    void Awake()
    {
        button = GetComponent<Button>();
        if (transform.childCount > 0) label = transform.GetChild(0) as RectTransform;
        if (label != null) { basePos = label.anchoredPosition; hasBase = true; }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasBase && button.interactable) label.anchoredPosition = basePos + Vector2.down * offset;
    }

    public void OnPointerUp(PointerEventData eventData) { Restore(); }
    public void OnPointerExit(PointerEventData eventData) { Restore(); }
    void OnDisable() { Restore(); }

    void Restore()
    {
        if (hasBase) label.anchoredPosition = basePos;
    }
}
