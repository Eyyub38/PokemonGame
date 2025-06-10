using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ChoiceText : MonoBehaviour{
    Text text;

    public Text TextField => text;

    private void Awake(){
        text = GetComponent<Text>();
    }

    public void SetSelected(bool selected){
        text.color = (selected) ? GlobalSettings.i.HighlightedColor : Color.white;
    }
}
