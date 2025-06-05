using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum InventoryUIState{ ItemSelection, PartySelection, MoveToForget,Busy}

public class InventoryUI : MonoBehaviour{
    [Header("Item List")]
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [Header("Item Details")]
    [SerializeField] Text description;
    [SerializeField] Text itemTypeText;

    [Header("Categories")]
    [SerializeField] Text categoryText;
    [SerializeField] Image prevCategoryImage;
    [SerializeField] Image currCategoryImage;
    [SerializeField] Image nextCategoryImage;
    [SerializeField] List<Sprite> categoryImages;

    [Header ("Arrows")]
    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    [Header("Party Screen")]
    [SerializeField] PartyScreen partyScreen;
    [Header("Move Selection")]
    [SerializeField] MoveSelectionUI moveSelectionUI;

    Inventory inventory;
    InventoryUIState state;
    RectTransform itemListRect;
    MoveBase moveToLearn;

    List<ItemSlotUI> slotUIList;

    Action<ItemBase> onItemUsed;
    
    int selectedItem = 0;
    int selectedCategory = 0;

    const int itemsInViewport = 6;

    private void Awake(){
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    private void Start(){
        UpdateItemList();
        categoryText.text = Inventory.ItemCategories[selectedCategory];
        ChangeCategoryIcons(selectedCategory);
        inventory.OnUpdated += UpdateItemList;
    }

    void UpdateItemList(){
        foreach(Transform child in itemList.transform){
            Destroy(child.gameObject);
        }

        slotUIList = new List<ItemSlotUI>();
        foreach(var itemSlot in inventory.GetItemSlotsByCategory(selectedCategory)){
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetData(itemSlot);

            slotUIList.Add(slotUIObj);
        }

        UpdateItemSelection();
    }

    public void HandleUpdate(Action onBack, Action<ItemBase> onItemUsed = null){
        Debug.Log($"InventoryUI HandleUpdate: {GameController.i.State}");

        this.onItemUsed = onItemUsed;
        int prevCategory = selectedCategory;

        if(state == InventoryUIState.ItemSelection){
            int prevSelection = selectedItem;
            if(Input.GetKeyDown(KeyCode.DownArrow)){
                ++selectedItem;
            }
            else if(Input.GetKeyDown(KeyCode.UpArrow)){
                --selectedItem;
            }
            else if(Input.GetKeyDown(KeyCode.LeftArrow)){
                --selectedCategory;
            }
            else if(Input.GetKeyDown(KeyCode.RightArrow)){
                ++selectedCategory;
            }

            if(selectedCategory > Inventory.ItemCategories.Count - 1){
                selectedCategory = 0;
            } else if(selectedCategory < 0){
                selectedCategory = Inventory.ItemCategories.Count - 1;
            }

            selectedItem = Mathf.Clamp(selectedItem, 0, inventory.GetItemSlotsByCategory(selectedCategory).Count - 1);

            if(prevCategory != selectedCategory){
                ResetSelection();
                categoryText.text = Inventory.ItemCategories[selectedCategory];
                ChangeCategoryIcons(selectedCategory);
                UpdateItemList();

            } else if(prevSelection != selectedItem){
                UpdateItemSelection();
            }
            if(Input.GetKeyDown(KeyCode.Return)){
                StartCoroutine(ItemSelected());
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
        } else if(state == InventoryUIState.MoveToForget){
            var pokemon = partyScreen.SelectedMember;
            Action<int> onMoveSelected = (moveIndex) => {
                StartCoroutine(OnMoveToForgetSelector(moveIndex, pokemon));
            };
            moveSelectionUI.HandleMoveSelection(pokemon,onMoveSelected);
        }
    }

    void ChangeCategoryIcons(int selectedCategory){
        currCategoryImage.sprite = categoryImages[selectedCategory];
        if(selectedCategory == 0){
            nextCategoryImage.sprite = categoryImages[selectedCategory + 1];
            prevCategoryImage.sprite = categoryImages[Inventory.ItemCategories.Count - 1];
        } else if(selectedCategory == Inventory.ItemCategories.Count - 1){
            prevCategoryImage.sprite = categoryImages[selectedCategory - 1];
            nextCategoryImage.sprite = categoryImages[0];
        } else {
            prevCategoryImage.sprite = categoryImages[selectedCategory - 1];            
            nextCategoryImage.sprite = categoryImages[selectedCategory + 1];
        }
    }

    IEnumerator ItemSelected(){
        state = InventoryUIState.Busy;
        var item = inventory.GetItem(selectedItem, selectedCategory);
        Debug.Log($"InventoryUI ItemSelected: {GameController.i.State}");
        
        if(GameController.i.State == GameState.Battle){
            if(!item.CanUseInBattle){
                yield return DialogManager.i.ShowDialogText("This item can't be used in battle");
                state = InventoryUIState.ItemSelection;
                yield break;
            }
        } else {
            if(!item.CanUseInOffsideBattle){
                yield return DialogManager.i.ShowDialogText("This item can't be used in outside battle");
                state = InventoryUIState.ItemSelection;
                yield break;
            }
        }
        
        if(selectedCategory != (int)ItemCategory.Pokeball){
            OpenPartyScreen();
        } else {
            StartCoroutine(UseItem());
        }
    }

    void ResetSelection(){
        selectedItem = 0;
        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);

        description.text = "";
        itemTypeText.text = "";
    }

    IEnumerator UseItem(){
        state = InventoryUIState.Busy;

        yield return HandleTmItems();

        var usedItem = inventory.UseItem(selectedItem, partyScreen.SelectedMember,selectedCategory);
        if(usedItem != null){

            if(usedItem is RecoveryItem){
                yield return DialogManager.i.ShowDialogText($"You use {usedItem.Name}!");
            }
            onItemUsed?.Invoke(usedItem);
        } else {
            yield return DialogManager.i.ShowDialogText($"It won't have any effect.");
        }

        ClosePartyScreen();
    }

    void UpdateItemSelection(){
        var slots = inventory.GetItemSlotsByCategory(selectedCategory);

        selectedItem = Mathf.Clamp(selectedItem, 0, slots.Count - 1);

        for(int i = 0; i < slotUIList.Count; ++i){
            if(i == selectedItem){
                slotUIList[i].NameText.color = GlobalSettings.i.HighlightedColor;
                slotUIList[i].CountText.color = GlobalSettings.i.HighlightedColor;
            } else {
                slotUIList[i].NameText.color = Color.white;
                slotUIList[i].CountText.color = Color.white;
            }
        }
        if(slots.Count > 0){
            var item = slots[selectedItem].Item;
            description.text = item.Description;
            itemTypeText.text = item.ItemType.ToString();
        }
        HandleScrolling();
    }

    IEnumerator HandleTmItems(){
        var tmItem = inventory.GetItem(selectedItem, selectedCategory) as TmItems;
        if(tmItem == null){
            yield break;
        }

        var pokemon = partyScreen.SelectedMember;
        if(pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
            pokemon.LearnMove(tmItem.Move);
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} learned {tmItem.Move.Name}");
        } else {
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} trying to learn {tmItem.Move.Name}...");
            yield return DialogManager.i.ShowDialogText($"But its is already knew {PokemonBase.MaxNumberOfMoves} moves.");
            yield return ChooseMoveToForget(pokemon, tmItem.Move);
            yield return new WaitUntil(() => state != InventoryUIState.MoveToForget);
        }
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

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove){
        state = InventoryUIState.Busy;
        yield return DialogManager.i.ShowDialogText($"Choose a move you want {pokemon.Base.Name} to forget!", true);
        moveSelectionUI.gameObject.SetActive(true);

        moveSelectionUI.SetMoveSelectionBars(pokemon.Moves, newMove);
        moveSelectionUI.SetMoveDetails(pokemon.Moves[0].Base, newMove);
        moveToLearn = newMove;

        state = InventoryUIState.MoveToForget;
    }

    IEnumerator OnMoveToForgetSelector(int moveIndex, Pokemon pokemon){
        DialogManager.i.CloseDialog();
        if(moveIndex == PokemonBase.MaxNumberOfMoves){
            moveSelectionUI.gameObject.SetActive(false);
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} didn't learn{moveToLearn.Name}");
        } else {
            var selectedMove = pokemon.Moves[ moveIndex ].Base;
            moveSelectionUI.gameObject.SetActive(false);
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}");

            pokemon.Moves[ moveIndex ] = new Move(moveToLearn);
        }
        moveToLearn = null;
        state = InventoryUIState.ItemSelection;
    }
}
