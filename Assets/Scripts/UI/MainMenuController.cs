using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;
using UnityEngine.SceneManagement;

public class MainMenuController : SelectionUI<TextSlot>{

    void Start(){
        var textSlots = GetComponentsInChildren<TextSlot>();

        if(SavingSystem.i.CheckIfSaveExists("saveSlot1")){
            SetItems(GetComponentsInChildren<TextSlot>().ToList());
        } else {
            SetItems(textSlots.TakeLast(4).ToList());
            textSlots.First().GetComponent<Text>().color = Color.gray;
        }

        OnSelected += OnItemSelected;
    }

    void Update(){
        HandleUpdate();
    }

    void OnItemSelected(int selectedIndex){
        if(!SavingSystem.i.CheckIfSaveExists("saveSlot1")){
            ++selectedIndex;
        }

        if(selectedIndex == 0){
            DontDestroyOnLoad(gameObject);
            
            GameController.i.StateMachine.ChangeState(FreeRoamState.i);
            SceneManager.LoadScene(1);
            SavingSystem.i.Load("saveSlot1");
            
            Destroy(gameObject);

        } else if(selectedIndex == 1){
            GameController.i.StateMachine.ChangeState(FreeRoamState.i);
            SavingSystem.i.Delete("saveSlot1");
            SceneManager.LoadScene(1);

        } else if(selectedIndex == 2){
            //Options
        } else if(selectedIndex == 3){
            //Credits
        } else if(selectedIndex == 4){
            //Exit
        }
    }
}
