using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EssentialObjectsSpawner : MonoBehaviour{
    [SerializeField] GameObject essentialObjectsPrefab;

    void Awake(){
        var existingObject = FindObjectsByType<EssentialObjects>(FindObjectsSortMode.None);
        if(existingObject.Length == 0){
            var spawnPos = new Vector3(0, 0, 0);
            var grid = FindFirstObjectByType<Grid>();

            if(grid != null){
                spawnPos = grid.transform.position;
            }
            Instantiate(essentialObjectsPrefab, spawnPos, Quaternion.identity);
        }
    }
}
