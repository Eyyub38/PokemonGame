using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new pokeball item")]
public class PokeballItem : ItemBase{
    [SerializeField] List<Sprite> throwFrames;
    [SerializeField] List<Sprite> idleFrames;
    [SerializeField] List<Sprite> shakeFrames;
    [SerializeField] List<Sprite> catchFrames;

    public List<Sprite> ThrowFrames => throwFrames;
    public List<Sprite> IdleFrames => idleFrames;
    public List<Sprite> ShakeFrames => shakeFrames;
    public List<Sprite> CatchFrames => catchFrames;

    public override bool Use(Pokemon pokemon){
        return true;
    }
}