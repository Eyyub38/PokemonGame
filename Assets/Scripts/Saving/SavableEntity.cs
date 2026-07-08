using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    private void OnValidate(){
        // OnValidate is only called on inspector change, not every frame.
        // This replaces the per-frame Update() approach, which was expensive
        // in scenes with many SavableEntity instances.
        if (Application.IsPlaying(gameObject)) return;
        if (String.IsNullOrEmpty(gameObject.scene.path)) return;

        // Defer to avoid "SendMessage cannot be called during Awake/OnEnable" issues.
        EditorApplication.delayCall += EnsureUniqueId;
    }

    void EnsureUniqueId(){
        // Guard: component may have been destroyed before the deferred call fires.
        if(this == null) return;
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
