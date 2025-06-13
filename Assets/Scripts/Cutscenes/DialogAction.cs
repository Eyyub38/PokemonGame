using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogAction : CutsceneAction{
    [SerializeField] Character character;
    [SerializeField] Dialog dialog;

    public override IEnumerator Play(){
        var player = PlayerController.i;
        character.LookTowards(player.Character.transform.position);
        yield return DialogManager.i.ShowDialog(dialog);
    }
}
