using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardLineInputButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private GravityInputView owner;
    private int line;

    public void Initialize(GravityInputView inputView, int lineIndex)
    {
        owner = inputView;
        line = lineIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null)
            owner.BeginHold(line, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner != null)
            owner.UpdateHold(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (owner != null)
            owner.EndHold(eventData);
    }

    private void OnDisable()
    {
        if (owner != null)
            owner.CancelHoldIfLine(line);
    }
}
