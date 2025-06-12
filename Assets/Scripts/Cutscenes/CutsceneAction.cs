using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CutsceneAction{
    [SerializeField] string name;

    public string Name { get { return name; } set { name = value; }}

    public virtual IEnumerator Play(){ yield break; }
}
