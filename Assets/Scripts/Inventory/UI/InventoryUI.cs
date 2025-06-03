using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum InventoryUIState{ ItemSelection, PartySelection, Busy}

public class InventoryUI : MonoBehaviour{
    [Header("Item List")]
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [Header("Item Details")]
    [SerializeField] Text description;
    [SerializeField] Text itemTypeText;

    [Header ("Arrows")]
    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    [Header("Party Screen")]
    [SerializeField] PartyScreen partyScreen;

    Inventory inventory;
    InventoryUIState state;
    RectTransform itemListRect;
    List<ItemSlotUI> slotUIList;
    
    int selectedItem = 0;

    const int itemsInViewport = 6;

    private void Awake(){
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    private void Start(){
        UpdateItemList();

        inventory.OnUpdated += UpdateItemList;
    }

    void UpdateItemList(){
        foreach(Transform child in itemList.transform){
            Destroy(child.gameObject);
        }

        slotUIList = new List<ItemSlotUI>();
        foreach(var itemSlot in inventory.Slots){
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetData(itemSlot);

            slotUIList.Add(slotUIObj);
        }

        UpdateItemSelection();
    }

    public void HandleUpdate(Action onBack){
        if(state == InventoryUIState.ItemSelection){
            int prevSelection = selectedItem;
            if(Input.GetKeyDown(KeyCode.DownArrow)){
                ++selectedItem;
            }
            else if(Input.GetKeyDown(KeyCode.UpArrow)){
                --selectedItem;
            }

            selectedItem = Mathf.Clamp(selectedItem, 0, inventory.Slots.Count - 1);
            if(prevSelection != selectedItem){
                UpdateItemSelection();
            }
            if(Input.GetKeyDown(KeyCode.Return)){
                OpenPartyScreen();
            } else if(Input.GetKeyDown(KeyCode.Escape)){
                onBack?.Invoke();
            }
        } else if(state == InventoryUIState.PartySelection){
            Action onSelected = () => {
                StartCoroutine(UseItem());
            };
            Action onBackPartyScreen = () => {
                ClosePartyScreen();
            };
            partyScreen.HandleUpdate(onSelected,onBackPartyScreen);
        }
    }

    IEnumerator UseItem(){
        state = InventoryUIState.Busy;
        var usedItem = inventory.UseItem(selectedItem, partyScreen.SelectedMember);
        if(usedItem != null){
            yield return DialogManager.i.ShowDialogText(usedItem.Message);
        } else {
            yield return DialogManager.i.ShowDialogText($"{usedItem.Name} is not");
        }

        ClosePartyScreen();
    }

    void UpdateItemSelection(){
        for(int i = 0; i < slotUIList.Count; ++i){
            if(i == selectedItem){
                slotUIList[i].NameText.color = GlobalSettings.i.HighlightedColor;
                slotUIList[i].CountText.color = GlobalSettings.i.HighlightedColor;
            } else {
                slotUIList[i].NameText.color = Color.white;
                slotUIList[i].CountText.color = Color.white;
            }
        }

        var item = inventory.Slots[selectedItem].Item;
        description.text = item.Description;
        itemTypeText.text = item.ItemType.ToString();

        HandleScrolling();
    }

    void HandleScrolling(){
        if(slotUIList.Count <= itemsInViewport){
            return;
        }
        var scrollPos = Mathf.Clamp(selectedItem - itemsInViewport / 2, 0, selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem < slotUIList.Count - itemsInViewport / 2;
        downArrow.gameObject.SetActive(showDownArrow);
    }

    void OpenPartyScreen(){
        state = InventoryUIState.PartySelection;
        partyScreen.gameObject.SetActive(true);
    }
    
    void ClosePartyScreen(){
        state = InventoryUIState.ItemSelection;
        partyScreen.gameObject.SetActive(false);
    }
}
