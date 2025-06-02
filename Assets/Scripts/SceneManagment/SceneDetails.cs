using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class SceneDetails : MonoBehaviour{
    [SerializeField] List<SceneDetails> connectedScenes;

    public bool IsLoaded{get; private set;}
    List<SavableEntity> savableEntities;
    
    private void OnTriggerEnter2D(Collider2D collision){
        Debug.Log("Entering Scene: " + gameObject.name);
        if(collision.tag == "Player"){
            LoadScene();
            GameController.Instance.SetCurrentScene(this);
            StartCoroutine(SetLocationUI(this.name.ToUpper()));

            foreach(var scene in connectedScenes){
                scene.LoadScene();
            }

            var prevScene = GameController.Instance.PrevScene;

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
                var savableEntities = GetSavableEntitiesInScene();
                SavingSystem.i.RestoreEntityStates(savableEntities);
            };
        }
    }

    public void UnloadScene(){
        if(IsLoaded){
            SavingSystem.i.CaptureEntityStates(savableEntities);

            savableEntities = GetSavableEntitiesInScene();
            SceneManager.UnloadSceneAsync(gameObject.name);
            IsLoaded = false;
        }
    }

    List<SavableEntity> GetSavableEntitiesInScene(){
        var scene = SceneManager.GetSceneByName(gameObject.name);
        var savableEntities = FindObjectsOfType<SavableEntity>().Where( x => x.gameObject.scene == scene).ToList();
        return savableEntities;
    }

    IEnumerator SetLocationUI(string location){
        GameController.Instance.LocationUI.gameObject.SetActive(true);
        GameController.Instance.LocationText.text = location;
        yield return new WaitForSeconds(2f);
        GameController.Instance.LocationUI.gameObject.SetActive(false);
    }
}
