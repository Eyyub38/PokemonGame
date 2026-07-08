using System;
using UnityEngine;

[Serializable]
public class RecipeGrant {
    [Tooltip("Recipe learned by this grant.")]
    public RecipeDefinition recipe;
    [Tooltip("Short source/reason stored in save/debug data, such as quest, shop, professor or activity.")]
    public string source;
    [Tooltip("If enabled, learning an already-known recipe refreshes its source/time metadata.")]
    public bool refreshExisting = true;

    public bool IsValid => recipe != null;
}
