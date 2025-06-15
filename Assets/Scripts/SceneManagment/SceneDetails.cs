using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class SceneDetails : MonoBehaviour{
    [SerializeField] List<SceneDetails> connectedScenes;

    [Header("Music")]
    [SerializeField] AudioClip sceneMusic;

    List<SavableEntity> savableEntities;

    public bool IsLoaded{get; private set;}
    public AudioClip SceneMusic => sceneMusic;

    private void OnTriggerEnter2D(Collider2D collision){
        if(collision.tag == "Player"){
            LoadScene();
            GameController.i.SetCurrentScene(this);

            if(sceneMusic != null){
                AudioManager.i.PlayMusic(sceneMusic, fade: true);
            }

            StartCoroutine(SetLocationUI(this.name.ToUpper()));

            foreach(var scene in connectedScenes){
                scene.LoadScene();
            }

            var prevScene = GameController.i.PrevScene;

            if(prevScene != null){
                var prevLoadedScenes = prevScene.connectedScenes;
                foreach(var scene in prevLoadedScenes){
                    if(!connectedScenes.Contains(scene) && scene != this){
                        scene.UnloadScene();
                    }

                    if(!connectedScenes.Contains(prevScene)){
                        prevScene.UnloadScene();
                    }
                }
            }
        }
    }

    public void LoadScene(){
        if(!IsLoaded){
            var operation = SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            IsLoaded = true;

            operation.completed += (AsyncOperation operation) => {
                if(SavingSystem.i != null){
                    var savableEntities = GetSavableEntitiesInScene();
                    SavingSystem.i.RestoreEntityStates(savableEntities);
                }
            };
        }
    }

    public void UnloadScene(){
        if(IsLoaded){
            savableEntities = GetSavableEntitiesInScene();
            if(SavingSystem.i != null){
                SavingSystem.i.CaptureEntityStates(savableEntities);
            }

            SceneManager.UnloadSceneAsync(gameObject.name);
            IsLoaded = false;
        }
    }

    List<SavableEntity> GetSavableEntitiesInScene(){
        var scene = SceneManager.GetSceneByName(gameObject.name);
        var savableEntities = FindObjectsByType<SavableEntity>(FindObjectsSortMode.None).Where( x => x.gameObject.scene == scene).ToList();
        return savableEntities;
    }

    IEnumerator SetLocationUI(string location){
        GameController.i.LocationUI.gameObject.SetActive(true);
        GameController.i.LocationText.text = location;
        yield return new WaitForSeconds(2f);
        GameController.i.LocationUI.gameObject.SetActive(false);
    }
}
