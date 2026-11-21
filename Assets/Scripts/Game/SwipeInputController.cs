using ConnectFour.Core;
using UnityEngine;

public sealed class SwipeInputController : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private float minimumSwipeDistance = 60f;

    private Vector2 pointerDownPosition;
    private bool trackingPointer;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
    }

    private void Update()
    {
        if (GameData.selectedRuleMode != GameData.RuleMode.SwipeGravity) return;
        if (gameController == null || !gameController.CanAcceptPlayerInput)
        {
            trackingPointer = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                pointerDownPosition = touch.position;
                trackingPointer = true;
            }
            else if (trackingPointer && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                SubmitSwipe(touch.position - pointerDownPosition);
                trackingPointer = false;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            pointerDownPosition = Input.mousePosition;
            trackingPointer = true;
        }
        else if (trackingPointer && Input.GetMouseButtonUp(0))
        {
            SubmitSwipe((Vector2)Input.mousePosition - pointerDownPosition);
            trackingPointer = false;
        }
    }

    private void SubmitSwipe(Vector2 delta)
    {
        if (gameController == null || !gameController.CanAcceptPlayerInput)
            return;
        if (delta.magnitude < minimumSwipeDistance) return;

        GravityDirection direction;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            direction = delta.x > 0f ? GravityDirection.Right : GravityDirection.Left;
        else
            direction = delta.y > 0f ? GravityDirection.Up : GravityDirection.Down;

        gameController.RotateGravity(direction);
    }
}
