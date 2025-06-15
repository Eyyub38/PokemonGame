using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(Cutscene))]
public class CutsceneEditor : Editor{
    public override void OnInspectorGUI(){
        var cutscene = target as Cutscene;

        using (new GUILayout.HorizontalScope()){
            if(GUILayout.Button("Dialog")){
                cutscene.AddAction(new DialogAction());
            } else if(GUILayout.Button("Move Actor")){
                cutscene.AddAction(new MoveActorAction());
            } else if(GUILayout.Button("Turn Actor")){
                cutscene.AddAction(new TurnActorAction());
            }
        }
        
        using(new GUILayout.HorizontalScope()){   
            if(GUILayout.Button("Teleport Object")){
                cutscene.AddAction(new TeleportObjectAction());
            } else if(GUILayout.Button("Enbale Object")){
                cutscene.AddAction(new EnableObjectAction());
            } else if(GUILayout.Button("Disable Object")){
                cutscene.AddAction(new DisableObjectAction());
            }
        }
        
        using(new GUILayout.HorizontalScope()){
            if(GUILayout.Button("FadeIn")){
                cutscene.AddAction(new FadeInAction());
            } else if(GUILayout.Button("FadeOut")){
                cutscene.AddAction(new FadeOutAction());
            } else if(GUILayout.Button("NPC Interact")){
                cutscene.AddAction(new NPCInteractAction());
            }
        }
        
        base.OnInspectorGUI();
    }
}
