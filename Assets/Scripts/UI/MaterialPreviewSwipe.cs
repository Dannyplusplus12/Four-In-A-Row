using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public sealed class MaterialPreviewSwipe : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float swipeThreshold = 45f;

    [Header("Inspector Events")]
    public UnityEvent onSwipeLeft;
    public UnityEvent onSwipeRight;

    private Vector2 pointerDownPos;
    private bool tracking;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null) return;
        pointerDownPos = eventData.position;
        tracking = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Implemented so Unity delivers the full drag-and-release sequence to this handler.
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!tracking || eventData == null) return;
        tracking = false;

        Vector2 delta = eventData.position - pointerDownPos;
        if (Mathf.Abs(delta.x) < swipeThreshold || Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
            return;

        if (delta.x < 0f)
            onSwipeLeft?.Invoke();
        else
            onSwipeRight?.Invoke();
    }
}
