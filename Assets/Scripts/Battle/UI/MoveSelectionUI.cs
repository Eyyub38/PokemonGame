using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MoveSelectionUI : MonoBehaviour{
    [SerializeField] List<MoveBar> moveBars;
    [SerializeField] List<Sprite> typeBarSprites;
    [SerializeField] Sprite empty;

    public event Action<int> OnSelected;
    public event Action OnBack;

    int selectedItem = 0;
    List<Move> currentMoves;
    bool isActive = false;

    List<Sprite> TypeBarSprites => typeBarSprites;
    Sprite Empty => empty;

    void Update(){
        if(!isActive) return;

        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)){
            selectedItem = Mathf.Max(0, selectedItem - 2);
            UpdateSelection();
        } else if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)){
            selectedItem = Mathf.Min(currentMoves.Count - 1, selectedItem + 2);
            UpdateSelection();
        } else if(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)){
            selectedItem = Mathf.Max(0, selectedItem - 1);
            UpdateSelection();
        } else if(Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)){
            selectedItem = Mathf.Min(currentMoves.Count - 1, selectedItem + 1);
            UpdateSelection();
        } else if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)){
            if(selectedItem < currentMoves.Count && currentMoves[selectedItem].PP > 0){
                OnSelected?.Invoke(selectedItem);
            }
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            OnBack?.Invoke();
        }
    }

    void UpdateSelection(){
        for(int i = 0; i < moveBars.Count; i++){
            if(i < currentMoves.Count){
                Color textColor = (i == selectedItem) ? GlobalSettings.i.HighlightedColor : 
                                (currentMoves[i].PP <= 0) ? Color.red : Color.white;
                moveBars[i].NameText.color = textColor;
                moveBars[i].PpText.color = textColor;
            }
        }
    }

    public void SetMoves(List<Move> moves){
        currentMoves = moves;
        selectedItem = 0;
        isActive = true;

        for(int i=0; i< moveBars.Count; ++i){
            if(i < moves.Count){
                moveBars[i].NameText.text = moves[i].Base.Name;
                moveBars[i].PpText.text = "PP: " + moves[i].PP.ToString() + "/" + moves[i].Base.PP.ToString();
                
                Color textColor = (moves[i].PP <= 0) ? Color.red : Color.white;
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
        selectedItem = 0;
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
        
        // Check if we have a valid sprite index and if the sprite exists in the array
        if(spriteIndex >= 0 && spriteIndex < TypeBarSprites.Count){
            moveBar.TypeImage.sprite = TypeBarSprites[spriteIndex];
        } else {
            // Fallback to empty sprite if type is not found or sprite doesn't exist
            moveBar.TypeImage.sprite = Empty;
        }
    }
}
