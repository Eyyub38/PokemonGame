using UnityEngine;
using UnityEngine.UI;

public class MapViewportMarkerView : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Image used as the colored marker body. If empty, the first Image on this object is used.")]
    [SerializeField] Image markerImage;
    [Tooltip("Optional text shown near or inside the marker.")]
    [SerializeField] Text labelText;
    [Tooltip("Optional object enabled when this marker is the active navigation target.")]
    [SerializeField] GameObject navigationTargetIndicator;
    [Tooltip("Optional object enabled when this marker is favorited by the player.")]
    [SerializeField] GameObject favoriteIndicator;

    [Header("Display")]
    [Tooltip("Color used when the marker record has no usable color.")]
    [SerializeField] Color fallbackColor = new Color32(67, 123, 133, 255);
    [Tooltip("Color used for the active navigation target.")]
    [SerializeField] Color navigationTargetColor = new Color32(239, 101, 96, 255);
    [Tooltip("Color used for hidden markers when they are still included by the UI.")]
    [SerializeField] Color hiddenMarkerColor = new Color32(120, 126, 132, 255);

    public RectTransform RectTransform => transform as RectTransform;

    void Awake() {
        ResolveReferences();
    }

    public void Bind(Image image, Text label, GameObject targetIndicator = null, GameObject favorite = null) {
        markerImage = image;
        labelText = label;
        navigationTargetIndicator = targetIndicator;
        favoriteIndicator = favorite;
        ResolveReferences();
    }

    public void Apply(MapMarkerRecord record, bool showLabel, bool isNavigationTarget) {
        ResolveReferences();

        if(markerImage != null) {
            markerImage.sprite = record != null ? record.icon : null;
            markerImage.color = ResolveColor(record, isNavigationTarget);
            markerImage.preserveAspect = true;
        }

        if(labelText != null) {
            labelText.gameObject.SetActive(showLabel);
            labelText.text = record != null ? record.displayName : string.Empty;
        }

        if(navigationTargetIndicator != null) {
            navigationTargetIndicator.SetActive(isNavigationTarget);
        }

        if(favoriteIndicator != null) {
            favoriteIndicator.SetActive(record != null && record.favorite);
        }
    }

    void ResolveReferences() {
        if(markerImage == null) {
            markerImage = GetComponent<Image>();
        }
    }

    Color ResolveColor(MapMarkerRecord record, bool isNavigationTarget) {
        if(isNavigationTarget) {
            return navigationTargetColor;
        }

        if(record != null && record.hidden) {
            return hiddenMarkerColor;
        }

        if(record != null && record.color.a > 0f) {
            return record.color;
        }

        return fallbackColor;
    }
}
