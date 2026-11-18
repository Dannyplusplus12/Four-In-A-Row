using ConnectFour.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GravityInputView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private GravitySensorController gravitySensor;
    [SerializeField] private Button[] verticalButtons = new Button[BoardModel.Columns];
    [SerializeField] private Button[] horizontalButtons = new Button[BoardModel.Rows];
    [SerializeField] private GameObject verticalInputRoot;
    [SerializeField] private GameObject horizontalInputRoot;
    [SerializeField] private RectTransform hoverHighlight;
    [SerializeField] private float dragOutMargin = 60f;
    [SerializeField] private bool matchButtonSize = true;

    private GravityDirection currentGravity = GravityDirection.Down;
    private bool isHolding;
    private int activePointerId = int.MinValue;
    private int selectedLine = -1;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (gravitySensor == null) gravitySensor = FindFirstObjectByType<GravitySensorController>();
        FindMissingButtonGroups();
        FindHoverHighlightIfMissing();
        BindButtons(verticalButtons);
        BindButtons(horizontalButtons);
        HideHighlight();
    }

    private void Start()
    {
        ApplyGravity(gameController == null ? currentGravity : gameController.CurrentGravity);
        if (gameController != null) gameController.GravityChanged += ApplyGravity;
    }

    private void OnDisable()
    {
        if (gameController != null) gameController.GravityChanged -= ApplyGravity;
        CancelHold();
    }

    public void BeginHold(int line, PointerEventData eventData)
    {
        if (eventData == null) return;
        if (gameController != null && !gameController.CanAcceptPlayerInput) return;

        isHolding = true;
        activePointerId = eventData.pointerId;
        selectedLine = line;
        SnapHighlightToLine(line);
    }

    public void UpdateHold(PointerEventData eventData)
    {
        if (!isHolding || eventData == null || eventData.pointerId != activePointerId) return;
        if (gameController != null && !gameController.CanAcceptPlayerInput)
        {
            CancelHold();
            return;
        }

        int resolvedLine = ResolveLineFromScreenPosition(eventData.position, eventData.pressEventCamera);
        selectedLine = resolvedLine;
        if (resolvedLine >= 0)
            SnapHighlightToLine(resolvedLine);
        else
            HideHighlight();
    }

    public void EndHold(PointerEventData eventData)
    {
        if (!isHolding || eventData == null || eventData.pointerId != activePointerId) return;

        UpdateHold(eventData);
        int lineToDrop = selectedLine;
        CancelHold();

        if (lineToDrop >= 0 && gameController != null && gameController.CanAcceptPlayerInput)
            gameController.DropPiece(lineToDrop);
    }

    public void CancelHoldIfLine(int line)
    {
        if (isHolding && selectedLine == line)
            CancelHold();
    }

    private void CancelHold()
    {
        isHolding = false;
        activePointerId = int.MinValue;
        selectedLine = -1;
        HideHighlight();
    }

    private void BindButtons(Button[] buttons)
    {
        for (int index = 0; index < buttons.Length; index++)
        {
            int line = index;
            Button button = buttons[index];
            if (button == null) continue;

            button.onClick.RemoveAllListeners();
            button.enabled = false;

            BoardLineInputButton inputButton = button.GetComponent<BoardLineInputButton>();
            if (inputButton == null)
                inputButton = button.gameObject.AddComponent<BoardLineInputButton>();
            inputButton.Initialize(this, line);
        }
    }

    private Button[] GetActiveButtons()
    {
        bool usesVerticalInput = currentGravity == GravityDirection.Down || currentGravity == GravityDirection.Up;
        return usesVerticalInput ? verticalButtons : horizontalButtons;
    }

    private int ResolveLineFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        Button[] buttons = GetActiveButtons();
        if (buttons == null || buttons.Length == 0) return -1;

        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPosition, eventCamera))
                return index;
        }

        int nearestLine = -1;
        float bestDistanceX = float.MaxValue;

        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect == null) continue;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(buttonRect, screenPosition, eventCamera, out Vector2 localPoint))
                continue;

            float halfHeight = buttonRect.rect.height * 0.5f;
            if (Mathf.Abs(localPoint.y) > halfHeight + dragOutMargin)
                continue;

            float absX = Mathf.Abs(localPoint.x);
            if (absX < bestDistanceX)
            {
                bestDistanceX = absX;
                nearestLine = index;
            }
        }

        if (nearestLine >= 0)
        {
            RectTransform nearestRect = buttons[nearestLine].transform as RectTransform;
            if (nearestRect != null && bestDistanceX <= nearestRect.rect.width * 0.5f + dragOutMargin)
                return nearestLine;
        }

        return -1;
    }

    private void SnapHighlightToLine(int line)
    {
        if (hoverHighlight == null) return;
        Button[] buttons = GetActiveButtons();
        if (buttons == null || line < 0 || line >= buttons.Length || buttons[line] == null)
        {
            HideHighlight();
            return;
        }

        RectTransform targetRect = buttons[line].transform as RectTransform;
        if (targetRect == null)
        {
            HideHighlight();
            return;
        }

        hoverHighlight.position = targetRect.position;
        hoverHighlight.rotation = targetRect.rotation;
        if (matchButtonSize)
            hoverHighlight.sizeDelta = targetRect.rect.size;
        hoverHighlight.gameObject.SetActive(true);
    }

    private void HideHighlight()
    {
        if (hoverHighlight != null)
            hoverHighlight.gameObject.SetActive(false);
    }

    private void FindHoverHighlightIfMissing()
    {
        if (hoverHighlight == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
                for (int index = 0; index < rects.Length; index++)
                {
                    if (rects[index].name == "HoverColumnHighlight")
                    {
                        hoverHighlight = rects[index];
                        break;
                    }
                }
            }
        }

        if (hoverHighlight != null)
        {
            Image highlightImage = hoverHighlight.GetComponent<Image>();
            if (highlightImage != null)
                highlightImage.raycastTarget = false;
        }
    }

    private void FindMissingButtonGroups()
    {
        if (verticalInputRoot == null)
        {
            GameObject rootObject = GameObject.Find("Vertical Buttons");
            Transform root = rootObject == null ? null : rootObject.transform;
            if (root != null) verticalInputRoot = root.gameObject;
        }
        if (horizontalInputRoot == null)
        {
            GameObject rootObject = GameObject.Find("Horizontal Buttons");
            Transform root = rootObject == null ? null : rootObject.transform;
            if (root != null) horizontalInputRoot = root.gameObject;
        }

        if (verticalInputRoot != null && HasMissing(verticalButtons))
            verticalButtons = verticalInputRoot.GetComponentsInChildren<Button>(true);
        if (horizontalInputRoot != null && HasMissing(horizontalButtons))
            horizontalButtons = horizontalInputRoot.GetComponentsInChildren<Button>(true);
    }

    private static bool HasMissing(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return true;
        for (int index = 0; index < buttons.Length; index++)
            if (buttons[index] == null) return true;
        return false;
    }

    private void ApplyGravity(GravityDirection direction)
    {
        currentGravity = direction;
        CancelHold();
        bool usesVerticalInput = direction == GravityDirection.Down || direction == GravityDirection.Up;
        SetInputGroupActive(verticalInputRoot, usesVerticalInput);
        SetInputGroupActive(horizontalInputRoot, !usesVerticalInput);

        if (verticalInputRoot == null)
            SetButtonsActive(verticalButtons, usesVerticalInput);
        if (horizontalInputRoot == null)
            SetButtonsActive(horizontalButtons, !usesVerticalInput);
    }

    private static void SetInputGroupActive(GameObject inputRoot, bool active)
    {
        if (inputRoot != null) inputRoot.SetActive(active);
    }

    private static void SetButtonsActive(Button[] buttons, bool active)
    {
        for (int index = 0; index < buttons.Length; index++)
            if (buttons[index] != null) buttons[index].gameObject.SetActive(active);
    }
}
