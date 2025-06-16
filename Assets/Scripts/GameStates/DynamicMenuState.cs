using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using System;

public class DynamicMenuState : State<GameController>{
    [SerializeField] DynamicMenuUI dynamicMenuUI;
    [SerializeField] TextSlot itemTextPrefab;

    GameController gameController;

    public BattleSystem BattleSystem { get; set; }
    public List<string> MenuItems { get; set; }
    public int? SelectedItem { get; private set; }

    public static DynamicMenuState i { get; private set; }

    public static DynamicMenuState EnsureInstance() {
        if (i == null) {
            var go = new GameObject("DynamicMenuState");
            i = go.AddComponent<DynamicMenuState>();
            DontDestroyOnLoad(go);
        }
        return i;
    }

    void Awake(){
        if (i == null) {
            i = this;
            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            } else {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        } else if (i != this) {
            Destroy(gameObject);
        }
    }

    public override void Enter(GameController owner){
        gameController = owner;

        if(BattleSystem != null){
            dynamicMenuUI = BattleSystem.DynamicMenuUI;
        }

        foreach(Transform child in dynamicMenuUI.transform){
            Destroy(child.gameObject);
        }

        var itemTextSlots = new List<TextSlot>();
        foreach(var menuItem in MenuItems){
            var itemTextSlot = Instantiate(itemTextPrefab, dynamicMenuUI.transform);
            itemTextSlot.SetText(menuItem);
            itemTextSlots.Add(itemTextSlot);
        }
        dynamicMenuUI.SetItems(itemTextSlots);

        dynamicMenuUI.gameObject.SetActive(true);
        dynamicMenuUI.OnSelected += OnItemSelected;
        dynamicMenuUI.OnBack += OnBack;
    }

    public override void Execute(){
        dynamicMenuUI.HandleUpdate();
    }

    public override void Exit(){
        dynamicMenuUI.ClearItems();
        BattleSystem = null;
        dynamicMenuUI.gameObject.SetActive(false);
        dynamicMenuUI.OnSelected -= OnItemSelected;
        dynamicMenuUI.OnBack -= OnBack;
    }
    
    private void OnItemSelected(int selectedItem){
        SelectedItem = selectedItem;
        gameController.StateMachine.Pop();
    }

    private void OnBack(){
        SelectedItem = null;
        gameController.StateMachine.Pop();
    }
}
