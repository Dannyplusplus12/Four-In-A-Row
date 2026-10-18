using ConnectFour.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class GravityInputView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private GravitySensorController gravitySensor;
    [SerializeField] private Button[] verticalButtons = new Button[BoardModel.Columns];
    [SerializeField] private Button[] horizontalButtons = new Button[BoardModel.Rows];
    [SerializeField] private GameObject verticalInputRoot;
    [SerializeField] private GameObject horizontalInputRoot;

    private GravityDirection currentGravity = GravityDirection.Down;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (gravitySensor == null) gravitySensor = FindFirstObjectByType<GravitySensorController>();
        FindMissingButtonGroups();
        BindButtons(verticalButtons);
        BindButtons(horizontalButtons);
    }

    private void Start()
    {
        ApplyGravity(gameController == null ? currentGravity : gameController.CurrentGravity);
        if (gravitySensor != null) gravitySensor.GravityChanged += ApplyGravity;
    }

    private void OnDisable()
    {
        if (gravitySensor != null) gravitySensor.GravityChanged -= ApplyGravity;
    }

    private void BindButtons(Button[] buttons)
    {
        for (int index = 0; index < buttons.Length; index++)
        {
            int line = index;
            if (buttons[index] == null) continue;
            buttons[index].onClick.RemoveAllListeners();
            buttons[index].onClick.AddListener(() => HandleLinePressed(line));
        }
    }

    private void HandleLinePressed(int line)
    {
        if (gameController != null)
            gameController.DropPiece(line);
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
