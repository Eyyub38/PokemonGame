using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System;

public class MoveSelectionUI : MonoBehaviour{
    [SerializeField] List<MoveBar> moveBars;
    [SerializeField] List<Sprite> typeBarSprites;
    [SerializeField] Sprite empty;

    [Header("Input")]
    [SerializeField] InputActionAsset actions;

    [SerializeField] string actionMapName = "UI";
    [SerializeField] string navigateName = "Navigate";
    [SerializeField] string selectName = "Select";
    [SerializeField] string backName = "Back";
    
    public event Action<int> OnSelected;
    public event Action OnBack;

    int selectedItem = 0;
    List<Move> currentMoves;
    Pokemon currentPokemon;
    PokemonVitalProfileDefinition currentVitalProfile;
    bool isActive = false;

    float navTimer = 0f;
    const float navSpeed = 8f;
    
    InputAction navigate;
    InputAction select;
    InputAction back;
    
    List<Sprite> TypeBarSprites => typeBarSprites;
    Sprite Empty => empty;

    void Awake() {
        if(actions == null) {
            Debug.LogError("MoveSelectionUI: actions not set");
            enabled = false;
            return;
        }

        var map = actions.FindActionMap(actionMapName);
        navigate = map.FindAction(navigateName);
        select = map.FindAction(selectName);
        back = map.FindAction(backName);
    }

    void OnEnable() {
        navigate?.Enable();
        select?.Enable();
        back?.Enable();
    }

    void OnDisable() {
        navigate?.Disable();
        select?.Disable();
        back?.Disable();
    }
    
    void Update(){
        if(!isActive || currentMoves == null || currentMoves.Count == 0) return;
        
        navTimer = Mathf.Clamp(navTimer - Time.deltaTime, 0f, navTimer);
        
        Vector2 navVector = navigate.ReadValue<Vector2>();
        navVector.x = Mathf.RoundToInt(navVector.x);
        navVector.y = Mathf.RoundToInt(navVector.y);

        if(navTimer == 0 && (Mathf.Abs(navVector.x) > 0.2f) || (Mathf.Abs(navVector.y) > 0.2f)) {
            int prevSelection = selectedItem;

            if((Mathf.Abs(navVector.y) >= (Mathf.Abs(navVector.x)))) {
                selectedItem += -(int)Mathf.Sign(navVector.y) * 2;
            } else {
                selectedItem += -(int)Mathf.Sign(navVector.x);
            }
            
            selectedItem = Mathf.Clamp(selectedItem, 0, moveBars.Count - 1);
            
            if(prevSelection != selectedItem) {
                UpdateSelection();
            }
            
            navTimer = 1f/navSpeed;
        }

        if(select.WasPressedThisFrame()) {
            if(selectedItem < currentMoves.Count && IsMoveSelectable(currentMoves[selectedItem])) {
                OnSelected?.Invoke(selectedItem);
            }
        } else if(back.WasPressedThisFrame()) {
            OnBack?.Invoke();
        }
    }

    void UpdateSelection(){
        for(int i = 0; i < moveBars.Count; i++){
            if(i < currentMoves.Count){
                Color textColor = (i == selectedItem) ? GlobalSettings.i.HighlightedTextColor : 
                                !IsMoveSelectable(currentMoves[i]) ? Color.red : Color.white;
                moveBars[i].NameText.color = textColor;
                moveBars[i].PpText.color = textColor;
            }
        }
    }

    public void SetMoves(List<Move> moves, Pokemon pokemon = null, PokemonVitalProfileDefinition vitalProfile = null){
        currentMoves = moves;
        currentPokemon = pokemon;
        currentVitalProfile = vitalProfile;
        selectedItem = 0;
        isActive = true;
        for(int i = 0; i < moves.Count; i++){
            if(IsMoveSelectable(moves[i])){
                selectedItem = i;
                break;
            }
        }
        
        for(int i=0; i< moveBars.Count; ++i){
            if(i < moves.Count){
                moveBars[i].NameText.text = moves[i].Base.Name;
                moveBars[i].PpText.text = "PP: " + moves[i].PP.ToString() + "/" + moves[i].Base.PP.ToString();
                
                Color textColor = !IsMoveSelectable(moves[i]) ? Color.red : Color.white;
                moveBars[i].NameText.color = textColor;
                moveBars[i].PpText.color = textColor;

                SetTypeBars(moves[i],moveBars[i]);
            } else {
                moveBars[i].NameText.text = "";
                moveBars[i].PpText.text = "";
                moveBars[i].TypeImage.sprite = Empty;
            }
        }
        
        UpdateSelection();
    }

    public void ClearItems(){
        isActive = false;
        currentMoves = null;
        currentPokemon = null;
        currentVitalProfile = null;
        selectedItem = 0;
    }

    bool IsMoveSelectable(Move move){
        if(currentPokemon != null){
            return currentPokemon.CanUseMove(move, currentVitalProfile);
        }

        return move != null && move.PP > 0;
    }

    public void SetTypeBars(Move move,MoveBar moveBar){
        string type = move.Base.Type.ToString();
        
        int spriteIndex = -1;
        
        if(type == "Normal"){
            spriteIndex = 0;
        } else if(type == "Fire"){
            spriteIndex = 1;
        } else if(type == "Water"){
            spriteIndex = 2;
        } else if(type == "Grass"){
            spriteIndex = 3;
        } else if(type == "Electric"){
            spriteIndex = 4;
        } else if(type == "Ice"){
            spriteIndex = 5;
        } else if(type == "Fighting"){
            spriteIndex = 6;
        } else if(type == "Poison"){
            spriteIndex = 7;
        } else if(type == "Ground"){
            spriteIndex = 8;
        } else if(type == "Flying"){
            spriteIndex = 9;
        } else if(type == "Psychic"){
            spriteIndex = 10;
        } else if(type == "Bug"){
            spriteIndex = 11;
        } else if(type == "Rock"){
            spriteIndex = 12;
        } else if(type == "Ghost"){
            spriteIndex = 13;
        } else if(type == "Dragon"){
            spriteIndex = 14;
        } else if(type == "Dark"){
            spriteIndex = 15;
        } else if(type == "Steel"){
            spriteIndex = 16;
        } else if(type == "Fairy"){
            spriteIndex = 17;
        }
        
        if(spriteIndex >= 0 && spriteIndex < TypeBarSprites.Count){
            moveBar.TypeImage.sprite = TypeBarSprites[spriteIndex];
        } else {
            moveBar.TypeImage.sprite = Empty;
        }
    }
}
