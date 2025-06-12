using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(MapArea))]
public class MapAreaEditor : Editor{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();

        int totalCahnce = serializedObject.FindProperty("totalChance").intValue;

        var style = new GUIStyle();
        style.fontStyle = FontStyle.Bold;

        GUILayout.Label($"Total Chance:  {totalCahnce}", style);
        
        if(totalCahnce != 100){
            EditorGUILayout.HelpBox("The total chance is not 100", MessageType.Error);
        }
    }
}
