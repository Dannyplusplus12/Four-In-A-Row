using System;
using ConnectFour.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GravitySensorController : MonoBehaviour
{
    [SerializeField] private float accelerationThreshold = 0.35f;
    [SerializeField] private float repeatDelay = 0.35f;
    [SerializeField] private bool keyboardInEditor = true;
    [SerializeField] private bool simulateDeviceRotationInEditor = true;

    public event Action<GravityDirection> GravityChanged;
    private GravityDirection lastDirection = GravityDirection.Down;
    private float nextAllowedTime;
    private ScreenOrientation lastScreenOrientation;

    private void Awake()
    {
        lastScreenOrientation = Screen.orientation;
    }

    private void Update()
    {
        GravityDirection direction;
        if (keyboardInEditor && TryReadKeyboard(out direction))
        {
            Publish(direction);
            return;
        }

        if (simulateDeviceRotationInEditor && Application.isEditor && TryReadSimulatorRotation(out direction))
        {
            Publish(direction);
            return;
        }

        if (TryReadAcceleration(out direction))
            Publish(direction);
    }

    private bool TryReadKeyboard(out GravityDirection direction)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.sKey.wasPressedThisFrame) { direction = GravityDirection.Down; return true; }
            if (keyboard.aKey.wasPressedThisFrame) { direction = GravityDirection.Left; return true; }
            if (keyboard.wKey.wasPressedThisFrame) { direction = GravityDirection.Up; return true; }
            if (keyboard.dKey.wasPressedThisFrame) { direction = GravityDirection.Right; return true; }
        }
#endif
        if (Input.GetKeyDown(KeyCode.S)) { direction = GravityDirection.Down; return true; }
        if (Input.GetKeyDown(KeyCode.A)) { direction = GravityDirection.Left; return true; }
        if (Input.GetKeyDown(KeyCode.W)) { direction = GravityDirection.Up; return true; }
        if (Input.GetKeyDown(KeyCode.D)) { direction = GravityDirection.Right; return true; }
        direction = default(GravityDirection);
        return false;
    }

    private bool TryReadSimulatorRotation(out GravityDirection direction)
    {
        ScreenOrientation orientation = Screen.orientation;
        if (orientation == lastScreenOrientation)
        {
            direction = default(GravityDirection);
            return false;
        }

        lastScreenOrientation = orientation;
        switch (orientation)
        {
            case ScreenOrientation.LandscapeLeft:
                direction = GravityDirection.Left;
                return true;
            case ScreenOrientation.LandscapeRight:
                direction = GravityDirection.Right;
                return true;
            case ScreenOrientation.PortraitUpsideDown:
                direction = GravityDirection.Up;
                return true;
            default:
                direction = GravityDirection.Down;
                return true;
        }
    }

    private bool TryReadAcceleration(out GravityDirection direction)
    {
        Vector3 acceleration = Input.acceleration;
        if (Mathf.Abs(acceleration.x) < accelerationThreshold && Mathf.Abs(acceleration.y) < accelerationThreshold)
        {
            direction = default(GravityDirection);
            return false;
        }

        if (Mathf.Abs(acceleration.x) > Mathf.Abs(acceleration.y))
            direction = acceleration.x > 0f ? GravityDirection.Right : GravityDirection.Left;
        else
            direction = acceleration.y > 0f ? GravityDirection.Up : GravityDirection.Down;
        return true;
    }

    private void Publish(GravityDirection direction)
    {
        if (Time.unscaledTime < nextAllowedTime || direction == lastDirection) return;
        lastDirection = direction;
        nextAllowedTime = Time.unscaledTime + repeatDelay;
        GravityChanged?.Invoke(direction);
    }
}
