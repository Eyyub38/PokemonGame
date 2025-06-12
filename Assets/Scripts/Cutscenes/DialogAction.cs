using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogAction : CutsceneAction{
    [SerializeField] Dialog dialog;

    public override IEnumerator Play(){
        yield return DialogManager.i.ShowDialog(dialog);
    }
}
