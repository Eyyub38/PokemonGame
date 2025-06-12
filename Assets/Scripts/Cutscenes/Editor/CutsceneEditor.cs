using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(Cutscene))]
public class CutsceneEditor : Editor{
    public override void OnInspectorGUI(){
        var cutscene = target as Cutscene;

        if(GUILayout.Button("Add Dialog Action")){
            cutscene.AddAction(new DialogAction());
        } else if(GUILayout.Button("Add Move Action")){
            cutscene.AddAction(new MoveActorAction());
        }
        base.OnInspectorGUI();
    }
}
