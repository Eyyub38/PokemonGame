using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;
using System.Linq;

public class MoveForgetSelectionUI : SelectionUI<TextSlot>{
    [Header("Move Bars")]
    [SerializeField] List<MoveBar> moveBars;

    [Header("Current Move Details")]
    [SerializeField] GameObject currDetails;
    [SerializeField] Text currNameText;
    [SerializeField] Text currPowerText;
    [SerializeField] Text currAccurText;
    [SerializeField] Text currDescriptionText;
    [SerializeField] Image currCatagoryImage;
    
    [Header("New Move Details")]
    [SerializeField] Text newNameText;
    [SerializeField] Text newPowerText;
    [SerializeField] Text newAccurText;
    [SerializeField] Text newDescriptionText;
    [SerializeField] Image newCatagoryImage;

    [Header("Sprite Lists")]
    [SerializeField] List<Sprite> categories = new List<Sprite>();
    [SerializeField] List<Sprite> typeBarSprites = new List<Sprite>();
    [SerializeField] Sprite empty;

    Color originalColor;
    MoveBase newMove;
    List<Move> currentMoves;

    public void SetMoveSelectionBars(List<Move> moves, MoveBase newMove) {
        this.newMove = newMove;
        this.currentMoves = moves;

        for(int i = 0; i < moveBars.Count - 1; i++) {
            moveBars[ i ].gameObject.SetActive(true);
        }
        var moveBar = moveBars[ PokemonBase.MaxNumberOfMoves ];
        if(newMove != null) {
            moveBar.NameText.text = newMove.Name;
            moveBar.PpText.text = "PP: " + newMove.PP.ToString();
            if(newMove.PP <= 0) {
                moveBar.PpText.color = Color.red;
            } else {
                moveBar.PpText.color = Color.black;
            }
            SetMoveSelectionTypeBars(newMove, moveBars[ PokemonBase.MaxNumberOfMoves ]);
        } else {
            moveBars[ 4 ].NameText.text = "";
            moveBars[ 4 ].PpText.text = "";
            moveBars[ 4 ].TypeImage.sprite = empty;
        }

        SetItems(moveBars.Select(b => b.GetComponent<TextSlot>()).ToList());
    }

    public void SetMoveDetails(MoveBase currMove, MoveBase newMove) {
        if(currMove == null || newMove == null) {
            return;
        }
        if( ReferenceEquals(currMove, newMove) ){
            SetCategories(newMove, newCatagoryImage);
            newPowerText.text = newMove.Power.ToString();
            newNameText.text = newMove.Name;
            newAccurText.text = newMove.Accuracy.ToString();
            newDescriptionText.text = newMove.Description;

            currDetails.gameObject.SetActive(false);
            return;
        }

        currDetails.gameObject.SetActive(true);
        SetCategories(currMove, currCatagoryImage);
        currPowerText.text = currMove.Power.ToString();
        currNameText.text = currMove.Name;
        currAccurText.text = currMove.Accuracy.ToString();
        currDescriptionText.text = currMove.Description;

        SetCategories(newMove, newCatagoryImage);
        newPowerText.text = newMove.Power.ToString();
        newNameText.text = newMove.Name;
        newAccurText.text = newMove.Accuracy.ToString();
        newDescriptionText.text = newMove.Description;
    }

    public override void UpdateSelectionInUI(){
        base.UpdateSelectionInUI();
        
        if(currentMoves != null && newMove != null && selectedItem < currentMoves.Count) {
            MoveBase selectedMoveBase = currentMoves[selectedItem].Base;
            
            if(ReferenceEquals(selectedMoveBase, newMove)){
                SetCategories(newMove, newCatagoryImage);
                newPowerText.text = newMove.Power.ToString();
                newNameText.text = newMove.Name;
                newAccurText.text = newMove.Accuracy.ToString();
                newDescriptionText.text = newMove.Description;

                currDetails.gameObject.SetActive(false);
                return;
            }

            currDetails.gameObject.SetActive(true);
            SetCategories(selectedMoveBase, currCatagoryImage);
            currPowerText.text = selectedMoveBase.Power.ToString();
            currNameText.text = selectedMoveBase.Name;
            currAccurText.text = selectedMoveBase.Accuracy.ToString();
            currDescriptionText.text = selectedMoveBase.Description;

            SetCategories(newMove, newCatagoryImage);
            newPowerText.text = newMove.Power.ToString();
            newNameText.text = newMove.Name;
            newAccurText.text = newMove.Accuracy.ToString();
            newDescriptionText.text = newMove.Description;
        }
    }

    void SetCategories(MoveBase moveBase, Image image) {
        var category = moveBase.Category;
        if(category == MoveCategory.Physical) {
            image.sprite = categories[ 0 ];
        } else if(category == MoveCategory.Special) {
            image.sprite = categories[ 1 ];
        } else if(category == MoveCategory.Status) {
            image.sprite = categories[ 2 ];
        } else {
            return;
        }
    }

    void SetMoveSelectionTypeBars(MoveBase move, MoveBar moveBar) {
        string type = move.Type.ToString();
        
        int spriteIndex = -1;
        
        if(type == "Normal") {
            spriteIndex = 0;
        } else if(type == "Fire") {
            spriteIndex = 1;
        } else if(type == "Water") {
            spriteIndex = 2;
        } else if(type == "Grass") {
            spriteIndex = 3;
        } else if(type == "Electric") {
            spriteIndex = 4;
        } else if(type == "Ice") {
            spriteIndex = 5;
        } else if(type == "Fighting") {
            spriteIndex = 6;
        } else if(type == "Poison") {
            spriteIndex = 7;
        } else if(type == "Ground") {
            spriteIndex = 8;
        } else if(type == "Flying") {
            spriteIndex = 9;
        } else if(type == "Psychic") {
            spriteIndex = 10;
        } else if(type == "Bug") {
            spriteIndex = 11;
        } else if(type == "Rock") {
            spriteIndex = 12;
        } else if(type == "Ghost") {
            spriteIndex = 13;
        } else if(type == "Dragon") {
            spriteIndex = 14;
        } else if(type == "Dark") {
            spriteIndex = 15;
        } else if(type == "Steel") {
            spriteIndex = 16;
        } else if(type == "Fairy") {
            spriteIndex = 17;
        }
        
        // Check if we have a valid sprite index and if the sprite exists in the array
        if(spriteIndex >= 0 && spriteIndex < typeBarSprites.Count){
            moveBar.TypeImage.sprite = typeBarSprites[spriteIndex];
        } else {
            // Fallback to empty sprite if type is not found or sprite doesn't exist
            moveBar.TypeImage.sprite = empty;
        }
    }
}
