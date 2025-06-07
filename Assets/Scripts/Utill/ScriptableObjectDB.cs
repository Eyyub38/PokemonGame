using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScriptableObjectDB<T> : MonoBehaviour where T : ScriptableObject{
    static Dictionary<string, T> objects;

    public static void Init(){
        objects = new Dictionary<string, T>();

        var objectArray = Resources.LoadAll<T>("");
        foreach(var obj in objectArray){
            if(objects.ContainsKey(obj.name)){
                Debug.LogWarning($"Duplicate ScriptableObject found: {obj.name}. Only the first instance will be used.");
                continue;
            }
            objects[obj.name] = obj;
        }
    }

    public static T GetObjectByName(string name){
        if(!objects.ContainsKey(name)){
            Debug.LogWarning($"ScriptableObject with name '{name}' not found.");
            return null;
        }

        return objects[name];
    }
}
