using System;
using System.Collections.Generic;

/// <summary>
/// Stores the serialized JSON of a single ISavable component's state,
/// along with the full type name needed to deserialize it back.
/// </summary>
[Serializable]
public class ISavableStateEntry {
    /// <summary>Full type name (e.g. "Inventory") used as key and for deserialization.</summary>
    public string typeName;
    /// <summary>JsonUtility.ToJson output of the ISavable state object.</summary>
    public string json;
}

/// <summary>
/// Stores the serialized states of all ISavable components on a single SavableEntity.
/// </summary>
[Serializable]
public class EntitySaveData {
    /// <summary>Unique ID of the SavableEntity (matches SavableEntity.UniqueId).</summary>
    public string entityId;
    public List<ISavableStateEntry> components = new List<ISavableStateEntry>();
}

/// <summary>
/// Root save file object — a flat list of all entity states in the scene.
/// Serialized to / deserialized from JSON by JsonUtility.
/// </summary>
[Serializable]
public class SaveFileData {
    public List<EntitySaveData> entities = new List<EntitySaveData>();
}
