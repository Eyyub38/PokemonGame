using UnityEngine;
using UnityEngine.UI;

public class DayNightOverlay : MonoBehaviour
{
    [SerializeField] Image overlayImage;
    [SerializeField] Color dawnColor = new Color(1f, 0.7f, 0.3f, 0.15f);
    [SerializeField] Color morningColor = new Color(1f, 1f, 0.8f, 0.05f);
    [SerializeField] Color noonColor = new Color(0, 0, 0, 0);
    [SerializeField] Color afternoonColor = new Color(1f, 0.95f, 0.7f, 0.07f);
    [SerializeField] Color sunsetColor = new Color(1f, 0.5f, 0.2f, 0.18f);
    [SerializeField] Color eveningColor = new Color(0.2f, 0.2f, 0.4f, 0.25f);
    [SerializeField] Color nightColor = new Color(0.1f, 0.1f, 0.3f, 0.45f);

    TimeSystem timeSystem;

    void Start(){
        timeSystem = TimeSystem.i;
    }

    void Update(){
        if(GameController.i.StateMachine.CurrentState == FreeRoamState.i || GameController.i.StateMachine.CurrentState == GameMenuState.i){
            overlayImage.gameObject.SetActive(true);
        } else {
            overlayImage.gameObject.SetActive(false);
        }
        float t = GetDayProgress();
        overlayImage.color = GetSmoothOverlayColor(t);
    }

    float GetDayProgress(){
        float totalMinutes = timeSystem.Hour * 60 + timeSystem.Minute;
        return totalMinutes / (24f * 60f);
    }

    Color GetSmoothOverlayColor(float t){
        if(t < 0.208f)
            return Color.Lerp(nightColor, dawnColor, t / 0.208f);
        if(t < 0.292f)
            return Color.Lerp(dawnColor, morningColor, (t - 0.208f) / (0.292f - 0.208f));
        if(t < 0.417f)
            return Color.Lerp(morningColor, noonColor, (t - 0.292f) / (0.417f - 0.292f));
        if(t < 0.583f)
            return Color.Lerp(noonColor, afternoonColor, (t - 0.417f) / (0.583f - 0.417f));
        if(t < 0.708f)
            return Color.Lerp(afternoonColor, sunsetColor, (t - 0.583f) / (0.708f - 0.583f));
        if(t < 0.792f)
            return Color.Lerp(sunsetColor, eveningColor, (t - 0.708f) / (0.792f - 0.708f));
        if(t < 0.917f)
            return Color.Lerp(eveningColor, nightColor, (t - 0.792f) / (0.917f - 0.792f));
        return nightColor;
    }
} 
