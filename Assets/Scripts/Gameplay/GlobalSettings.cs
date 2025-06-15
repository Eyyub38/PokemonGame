using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GlobalSettings : MonoBehaviour{
    [Header("Pokemon")]
    [SerializeField] int maxEvs = 510;
    [SerializeField] int maxEvPerStat = 252;

    [Header("Moves")]
    [SerializeField] MoveBase backupMove;

    [Header("Color")]
    [SerializeField] Color highlightedColor;

    [Header("HP Bar")]
    [SerializeField] Gradient healthbarGradient;
    
    
    public Color HighlightedColor => highlightedColor;
    public MoveBase BackUpMove => backupMove;
    public Gradient HealthBarGradient => healthbarGradient;
    public int MaxEvs => maxEvs;
    public int MaxEvPerStat => maxEvPerStat;

    public static GlobalSettings i {get; private set;}

    private void Awake(){
        i = this;
    }
}