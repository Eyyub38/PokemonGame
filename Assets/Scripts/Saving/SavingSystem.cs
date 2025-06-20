﻿using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;

public class SavingSystem : MonoBehaviour{
    public static SavingSystem i { get; private set; }
    private void Awake(){
        i = this;
    }

    Dictionary<string, object> gameState = new Dictionary<string, object>();

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

    private void CaptureState(Dictionary<string, object> state){
        foreach (SavableEntity savable in FindObjectsByType<SavableEntity>(FindObjectsSortMode.None)){
            state[savable.UniqueId] = savable.CaptureState();
        }
    }

    private void RestoreState(Dictionary<string, object> state){
        foreach (SavableEntity savable in FindObjectsByType<SavableEntity>(FindObjectsSortMode.None)){
            string id = savable.UniqueId;
            if (state.ContainsKey(id))
                savable.RestoreState(state[id]);
        }
    }

    public void RestoreEntity(SavableEntity entity){
        if(gameState.ContainsKey(entity.UniqueId)){
            entity.RestoreState(gameState[entity.UniqueId]);
        }
    }

    void SaveFile(string saveFile, Dictionary<string, object> state){
        string path = GetPath(saveFile);
        print($"saving to {path}");

        using (FileStream fs = File.Open(path, FileMode.Create)){
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fs, state);
        }
    }

    Dictionary<string, object> LoadFile(string saveFile){
        string path = GetPath(saveFile);
        if (!File.Exists(path)){
            return new Dictionary<string, object>();
        }

        using (FileStream fs = File.Open(path, FileMode.Open)){
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return (Dictionary<string, object>)binaryFormatter.Deserialize(fs);
        }
    }

    private string GetPath(string saveFile){
        return Path.Combine(Application.persistentDataPath, saveFile);
    }
}
