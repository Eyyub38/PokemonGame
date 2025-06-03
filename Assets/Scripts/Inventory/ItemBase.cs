using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemBase : ScriptableObject{
    [SerializeField] string _name;
    [SerializeField] string description;
    [SerializeField] Sprite icon;

    public string Name => _name;
    public string Description => description;
    public Sprite Icon => icon;
}