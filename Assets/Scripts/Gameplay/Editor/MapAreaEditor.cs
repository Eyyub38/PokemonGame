using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(MapArea))]
public class MapAreaEditor : Editor{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();

        int totalCahnceInGrass = serializedObject.FindProperty("totalChance").intValue;
        int totalCahnceInWater = serializedObject.FindProperty("totalChanceWater").intValue;

        if(totalCahnceInGrass != 100 && totalCahnceInWater != -1){
            EditorGUILayout.HelpBox($"The total chance in Grass is {totalCahnceInGrass} and not 100", MessageType.Error);
        }
                
        if(totalCahnceInWater != 100 && totalCahnceInWater != -1){
            EditorGUILayout.HelpBox($"The total chance in Water is {totalCahnceInWater} and not 100", MessageType.Error);
        }
    }
}
