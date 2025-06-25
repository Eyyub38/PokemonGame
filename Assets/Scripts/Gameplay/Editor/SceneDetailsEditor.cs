using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

[CanEditMultipleObjects]
[CustomEditor(typeof(SceneDetails))]
public class SceneDetailsEditor : Editor{
    public override void OnInspectorGUI(){
        using (new EditorGUILayout.HorizontalScope()){
            if(GUILayout.Button("Open Scene")){
                foreach(var t in targets){
                    var scene = t as SceneDetails;
                    if(scene != null){
                        scene.OpenSceneInEditor();
                    }
                }
            }
            if(GUILayout.Button("Close Scene")){
                foreach(var t in targets){
                    var scene = t as SceneDetails;
                    if(scene != null){
                        scene.CloseSceneInEditor();
                    }
                }
            }
        }

        base.OnInspectorGUI();
    }
}
