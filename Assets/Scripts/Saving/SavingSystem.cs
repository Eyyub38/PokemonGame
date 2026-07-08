using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles saving and loading of all ISavable entities in the scene.
/// Uses JsonUtility instead of BinaryFormatter for safe, human-readable,
/// and refactoring-resilient save files.
/// </summary>
public class SavingSystem : MonoBehaviour{
    public static SavingSystem i { get; private set; }

    private void Awake(){
        i = this;
    }

    Dictionary<string, object> gameState = new Dictionary<string, object>();

    // ── Public API ────────────────────────────────────────────────────────────

    public void CaptureEntityStates(List<SavableEntity> savableEntities){
        foreach (SavableEntity savable in savableEntities){
            gameState[savable.UniqueId] = savable.CaptureState();
        }
    }

    public void RestoreEntityStates(List<SavableEntity> savableEntities){
        foreach (SavableEntity savable in savableEntities){
            string id = savable.UniqueId;
            if (gameState.ContainsKey(id)){
                savable.RestoreState(gameState[id]);
            }
        }
    }

    public bool CheckIfSaveExists(string saveFile){
        return File.Exists(GetPath(saveFile));
    }

    public void Save(string saveFile){
        CaptureState(gameState);
        SaveFile(saveFile, gameState);
    }

    public void Load(string saveFile){
        gameState = LoadFile(saveFile);
        RestoreState(gameState);
    }

    public void Delete(string saveFile){
        File.Delete(GetPath(saveFile));
    }

    public void RestoreEntity(SavableEntity entity){
        if(gameState.ContainsKey(entity.UniqueId)){
            entity.RestoreState(gameState[entity.UniqueId]);
        }
    }

    // ── Internal state capture / restore ─────────────────────────────────────

    private void CaptureState(Dictionary<string, object> state){
        foreach (SavableEntity savable in FindObjectsByType<SavableEntity>()){
            state[savable.UniqueId] = savable.CaptureState();
        }
    }

    private void RestoreState(Dictionary<string, object> state){
        foreach (SavableEntity savable in FindObjectsByType<SavableEntity>()){
            string id = savable.UniqueId;
            if (state.ContainsKey(id))
                savable.RestoreState(state[id]);
        }
    }

    // ── File I/O — JSON based ─────────────────────────────────────────────────

    /// <summary>
    /// Serializes the game state dictionary to a human-readable JSON file.
    /// Replaces the old BinaryFormatter approach.
    /// 
    /// Format: SaveFileData → List of EntitySaveData → List of ISavableStateEntry (typeName + json).
    /// Each ISavable component state is individually serialized via JsonUtility.ToJson
    /// so that type information is preserved for deserialization.
    /// </summary>
    void SaveFile(string saveFile, Dictionary<string, object> state){
        string path = GetPath(saveFile);
        Debug.Log($"[SavingSystem] Saving to {path}");

        var fileData = new SaveFileData();

        foreach (var entityKvp in state){
            var entityData = new EntitySaveData { entityId = entityKvp.Key };

            if (entityKvp.Value is Dictionary<string, object> componentStates){
                foreach (var compKvp in componentStates){
                    if (compKvp.Value == null) continue;

                    string json = JsonUtility.ToJson(compKvp.Value);
                    entityData.components.Add(new ISavableStateEntry {
                        typeName = compKvp.Key,
                        json = json
                    });
                }
            }

            fileData.entities.Add(entityData);
        }

        string fileJson = JsonUtility.ToJson(fileData, prettyPrint: true);
        File.WriteAllText(path, fileJson);
    }

    /// <summary>
    /// Loads and deserializes the JSON save file back into a Dictionary&lt;string, object&gt;
    /// that SavableEntity.RestoreState can consume.
    /// </summary>
    Dictionary<string, object> LoadFile(string saveFile){
        string path = GetPath(saveFile);
        if (!File.Exists(path)){
            return new Dictionary<string, object>();
        }

        string fileJson = File.ReadAllText(path);
        var fileData = JsonUtility.FromJson<SaveFileData>(fileJson);

        if (fileData == null || fileData.entities == null){
            Debug.LogWarning("[SavingSystem] Save file was empty or invalid.");
            return new Dictionary<string, object>();
        }

        var state = new Dictionary<string, object>();

        foreach (var entityData in fileData.entities){
            if (string.IsNullOrEmpty(entityData.entityId)) continue;

            var componentStates = new Dictionary<string, object>();

            foreach (var comp in entityData.components){
                if (string.IsNullOrEmpty(comp.typeName) || string.IsNullOrEmpty(comp.json)) continue;

                // Resolve the CLR type by name. Works as long as the class name
                // hasn't changed since the save was written.
                Type type = Type.GetType(comp.typeName);
                if (type == null){
                    Debug.LogWarning($"[SavingSystem] Could not resolve type '{comp.typeName}' — skipping component.");
                    continue;
                }

                object restored = JsonUtility.FromJson(comp.json, type);
                componentStates[comp.typeName] = restored;
            }

            state[entityData.entityId] = componentStates;
        }

        return state;
    }

    private string GetPath(string saveFile){
        return Path.Combine(Application.persistentDataPath, saveFile + ".json");
    }
}
