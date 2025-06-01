using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EssentialObjectsSpawner : MonoBehaviour{
    [SerializeField] GameObject essentialObjectsPrefab;

    void Awake(){
        var existingObject = FindObjectsOfType<EssentialObjects>();
        if(existingObject.Length == 0){
            Instantiate(essentialObjectsPrefab, new Vector3(0,0,0), Quaternion.identity);
        }
    }
}
