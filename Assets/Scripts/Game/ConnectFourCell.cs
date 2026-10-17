using UnityEngine;
using UnityEngine.UI;

public class ConnectFourCell : MonoBehaviour
{
    [SerializeField] private Image pieceImage;
    [SerializeField] private Button button;

    private ConnectFourGameController controller;
    private int column;

    public void Setup(ConnectFourGameController gameController, int cellColumn)
    {
        controller = gameController;
        column = cellColumn;

        if (button == null) button = GetComponent<Button>();
        if (pieceImage == null) pieceImage = GetComponentInChildren<Image>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void SetPiece(Sprite sprite)
    {
        if (pieceImage == null) return;

        pieceImage.sprite = sprite;
        pieceImage.color = sprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private void HandleClicked()
    {
        if (controller != null) controller.TryDropPiece(column);
    }
}