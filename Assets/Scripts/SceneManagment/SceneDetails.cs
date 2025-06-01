using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour{
    [SerializeField] List<SceneDetails> connectedScenes;

    public bool IsLoaded{get; private set;}
    
    private void OnTriggerEnter2D(Collider2D collision){
        Debug.Log("Entering Scene: " + gameObject.name);
        if(collision.tag == "Player"){
            LoadScene();
            GameController.Instance.SetCurrentScene(this);
            StartCoroutine(SetLocationUI(this.name.ToUpper()));

            foreach(var scene in connectedScenes){
                scene.LoadScene();
            }
            if(GameController.Instance.PrevScene != null){
                var prevLoadedScenes = GameController.Instance.PrevScene.connectedScenes;
                foreach(var scene in prevLoadedScenes){
                    if(!connectedScenes.Contains(scene) && scene != this){
                        scene.UnloadScene();
                    }
                }
            }
        }
    }

    public void LoadScene(){
        if(!IsLoaded){
            SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            IsLoaded = true;
        }
    }

    public void UnloadScene(){
        if(IsLoaded){
            SceneManager.UnloadSceneAsync(gameObject.name);
            IsLoaded = false;
        }
    }

    IEnumerator SetLocationUI(string location){
        GameController.Instance.LocationUI.gameObject.SetActive(true);
        GameController.Instance.LocationText.text = location;
        yield return new WaitForSeconds(2f);
        GameController.Instance.LocationUI.gameObject.SetActive(false);
    }
}
