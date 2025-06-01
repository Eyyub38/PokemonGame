using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EssentialObjectsSpawner : MonoBehaviour{
    [SerializeField] GameObject essentialObjectsPrefab;

    void Awake(){
        var existingObject = FindObjectsOfType<EssentialObjects>();
        if(existingObject.Length == 0){
            var spawnPos = new Vector3(0, 0, 0);
            var grid = FindObjectOfType<Grid>();

            if(grid != null){
                spawnPos = grid.transform.position;
            }
            Instantiate(essentialObjectsPrefab, spawnPos, Quaternion.identity);
        }
    }
}
