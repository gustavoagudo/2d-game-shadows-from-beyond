using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hovering a button with the mouse selects it, so mouse and keyboard/gamepad share one "focused" button
// (and the menu's selector arrow follows it).
public class UIPointerSelect : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Selectable s = GetComponent<Selectable>();
        if (s != null && s.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
