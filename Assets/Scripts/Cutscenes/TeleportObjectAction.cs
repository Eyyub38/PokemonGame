using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TeleportObjectAction : CutsceneAction{
    [SerializeField] GameObject gameObject;
    [SerializeField] Vector3 position;

    public override IEnumerator Play(){
        gameObject.transform.position = position;
        yield break;
    }
}
