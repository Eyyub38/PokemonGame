using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class GameMenuState : State<GameController>{
    [SerializeField] MenuController menuController;

    GameController gameController;
    
    public static GameMenuState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        menuController.gameObject.SetActive(true);
        menuController.OnSelected += OnMenuItemSelected;
        menuController.OnBack += OnBack;
    }

    public override void Execute(){
        menuController.HandleUpdate();
    }
    
    public override void Exit(){
        menuController.gameObject.SetActive(false);
        menuController.OnSelected -= OnMenuItemSelected;
        menuController.OnBack -= OnBack;
    }

    void OnMenuItemSelected(int selection){
        if(selection == 0){
            gameController.StateMachine.Push(PartyState.i);
        } else if(selection == 1){
            gameController.StateMachine.Push(InventoryState.i);
        } else if(selection == 2){
            gameController.StateMachine.Push(StorageState.i);
        } else if(selection == 3){
            StartCoroutine(SaveSelected());
        } else if(selection == 4){
            StartCoroutine(LoadSelected());
        }
    }

    void OnBack(){
        gameController.StateMachine.Pop();
    }
    IEnumerator SaveSelected(){
        yield return Fader.i.FadeIn(0.5f);
        SavingSystem.i.Save("saveSlot1");
        yield return Fader.i.FadeOut(0.5f);
    }

    IEnumerator LoadSelected(){
        yield return Fader.i.FadeIn(0.5f);
        SavingSystem.i.Load("saveSlot1");
        yield return Fader.i.FadeOut(0.5f);
    }
}
