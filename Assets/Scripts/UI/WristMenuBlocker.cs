using UnityEngine;

public class WristMenuBlocker : MonoBehaviour
{
    public bool IsDrawing { get; private set; }
    public bool IsGrabbingObject => grabBlockCount > 0;

    public bool IsBlocked => IsDrawing || IsGrabbingObject;

    private int grabBlockCount = 0;

    public void SetDrawingBlocked(bool value)
    {
        IsDrawing = value;
    }

    public void SetGrabBlocked(bool value)
    {
        if (value)
            grabBlockCount++;
        else
            grabBlockCount = Mathf.Max(0, grabBlockCount - 1);
    }

    public void ClearAll()
    {
        IsDrawing = false;
        grabBlockCount = 0;
    }
}