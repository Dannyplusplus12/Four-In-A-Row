using UnityEngine;

public sealed class PortraitOrientationController : MonoBehaviour
{
    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
            LockPortraitOrientation();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            LockPortraitOrientation();
    }

    private static void LockPortraitOrientation()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }
}