using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ImageSlot : MonoBehaviour, ISelectableItem{
    Image image;
    Color originalColor;

    void Awake(){
        image = GetComponent<Image>();
    }

    public void Clear(){
        image.color = originalColor;
    }

    public void Init(){
        originalColor = image.color;
    }

    public void OnSelectionChanged(bool selected){
        image.color = selected ? GlobalSettings.i.HighlightedImageColor : originalColor;
    }
}
