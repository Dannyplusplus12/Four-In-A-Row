using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ConnectFourColumnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int column;

    private Button button;
    private ConnectFourGameController controller;
    private RectTransform rectTransform;

    public void Setup(ConnectFourGameController gameController, int columnIndex)
    {
        controller = gameController;
        column = columnIndex;
        button = GetComponent<Button>();
        rectTransform = transform as RectTransform;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClicked);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.ShowColumnHighlight(rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller != null) controller.HideColumnHighlight();
    }

    private void HandleClicked()
    {
        if (controller != null) controller.TryDropPiece(column);
    }
}