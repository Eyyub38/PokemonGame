using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new pokeball item")]
public class PokeballItem : ItemBase{
    [Header ("Animation Frames")]
    [Tooltip("Animation frames played while the ball is thrown.")]
    [SerializeField] List<Sprite> throwFrames;
    [Tooltip("Animation frames used while the ball is idle.")]
    [SerializeField] List<Sprite> idleFrames;
    [Tooltip("Animation frames used during shake checks.")]
    [SerializeField] List<Sprite> shakeFrames;
    [Tooltip("Animation frames played on successful capture.")]
    [SerializeField] List<Sprite> catchFrames;

    
    [Header ("Pokeball Details")]
    [Tooltip("Battle background sprite used for this ball animation.")]
    [SerializeField] Sprite background;
    [Tooltip("Multiplier applied to catch chance. 1 is normal Pokeball strength.")]
    [Min(0f)]
    [SerializeField] float catchRateModifier = 1;

    public List<Sprite> ThrowFrames => throwFrames;
    public List<Sprite> IdleFrames => idleFrames;
    public List<Sprite> ShakeFrames => shakeFrames;
    public List<Sprite> CatchFrames => catchFrames;
    public Sprite Background => background;
    public float CatchRateModifier => catchRateModifier;
    public override bool CanUseInOutsideBattle => false;

    public override bool Use(Pokemon pokemon){
        return true;
    }
}
