using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CutsceneAction{
    [SerializeField] string name;
    [SerializeField] bool waitForCompletion = true;

    public string Name { get { return name; } set { name = value; }}
    public bool WaitForCompletion => waitForCompletion;

    public virtual IEnumerator Play(){ yield break; }
}
