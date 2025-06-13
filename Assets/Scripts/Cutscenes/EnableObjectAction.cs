using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnableObjectAction : CutsceneAction{
    [SerializeField] GameObject gameObject;

    public override IEnumerator Play(){
        gameObject.SetActive(true);
        yield break;
    }
}
