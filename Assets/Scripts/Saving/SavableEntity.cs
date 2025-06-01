using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class SavableEntity : MonoBehaviour{
    [SerializeField] string uniqueId = "";
    
    static Dictionary<string, SavableEntity> globalLookup = new Dictionary<string, SavableEntity>();

    public string UniqueId => uniqueId;

    public object CaptureState(){
        Dictionary<string, object> state = new Dictionary<string, object>();
        foreach (ISavable savable in GetComponents<ISavable>()){
            state[savable.GetType().ToString()] = savable.CaptureState();
        }
        return state;
    }

    public void RestoreState(object state){
        Dictionary<string, object> stateDict = (Dictionary<string, object>)state;
        foreach (ISavable savable in GetComponents<ISavable>()){
            string id = savable.GetType().ToString();

            if (stateDict.ContainsKey(id)){
                savable.RestoreState(stateDict[id]);
            }
        }
    }

#if UNITY_EDITOR
    private void Update(){
        if (Application.IsPlaying(gameObject)) return;

        if (String.IsNullOrEmpty(gameObject.scene.path)) return;

        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty property = serializedObject.FindProperty("uniqueId");

        if (String.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue)){
            property.stringValue = Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties();
        }

        globalLookup[property.stringValue] = this;
    }
#endif

    private bool IsUnique(string candidate){
        if (!globalLookup.ContainsKey(candidate)) return true;

        if (globalLookup[candidate] == this) return true;

        if (globalLookup[candidate] == null){
            globalLookup.Remove(candidate);
            return true;
        }

        if (globalLookup[candidate].UniqueId != candidate){
            globalLookup.Remove(candidate);
            return true;
        }

        return false;
    }
}