using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new pokeball item")]
public class PokeballItem : ItemBase{
    [Header ("Animation Frames")]
    [SerializeField] List<Sprite> throwFrames;
    [SerializeField] List<Sprite> idleFrames;
    [SerializeField] List<Sprite> shakeFrames;
    [SerializeField] List<Sprite> catchFrames;

    
    [Header ("Pokeball Details")]
    [SerializeField] Sprite background;
    [SerializeField] float catchRateModifier = 1;

    public List<Sprite> ThrowFrames => throwFrames;
    public List<Sprite> IdleFrames => idleFrames;
    public List<Sprite> ShakeFrames => shakeFrames;
    public List<Sprite> CatchFrames => catchFrames;

    public Sprite Background => background;
    public float CatchRateModifier => catchRateModifier;
    public override bool CanUseInOffsideBattle => false;

    public override bool Use(Pokemon pokemon){
        return true;
    }
}