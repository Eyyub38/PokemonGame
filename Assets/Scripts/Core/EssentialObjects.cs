using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EssentialObjects : MonoBehaviour {
    private void Awake(){
        DontDestroyOnLoad(gameObject);
    }
}