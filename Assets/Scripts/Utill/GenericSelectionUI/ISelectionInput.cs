using UnityEngine;

public interface ISelectionInput{
        Vector2 Navigate {  get;  }
        bool SelectPressedThisFrame { get; }
        bool BackPressedThisFrame { get; }
        bool NextPressedThisFrame { get; }
        bool PreviousPressedThisFrame { get; }
        bool UpPressedThisFrame { get; }
        bool DownPressedThisFrame { get; }
        bool LeftPressedThisFrame { get; }
        bool RightPressedThisFrame { get; }
}
