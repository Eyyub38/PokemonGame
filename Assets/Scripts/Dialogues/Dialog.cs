using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Dialog{
    [Tooltip("Dialog lines shown in order.")]
    [SerializeField] List<string> lines;

    public Dialog() {
        lines = new List<string>();
    }

    public Dialog(IEnumerable<string> lines) {
        this.lines = lines != null ? new List<string>(lines) : new List<string>();
    }

    public List<string> Lines{ get{return lines;} }
}
