using UnityEngine;

public interface ISelectionInput{
        Vector2 Navigate {  get;  }
        bool SubmitPressedThisFrame { get; }
        bool BackPressedThisFrame { get; }
        float Horizontal { get; }
        bool NextPressedThisFrame { get; }
        bool PreviousPressedThisFrame { get; }
}
