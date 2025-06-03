using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GlobalSettings : MonoBehaviour{
    [Header("Color")]
    [SerializeField] Color highlightedColor;
    
    public Color HighlightedColor => highlightedColor;

    public static GlobalSettings i {get; private set;}

    private void Awake(){
        i = this;
    }
}