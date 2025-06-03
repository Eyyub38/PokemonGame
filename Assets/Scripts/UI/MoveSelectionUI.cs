using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MoveSelectionUI : MonoBehaviour{
    [SerializeField] List<MoveBar> moveBars;

    [SerializeField] Text currNameText;
    [SerializeField] Text currPowerText;
    [SerializeField] Text currAccurText;
    [SerializeField] Text currDescriptionText;
    [SerializeField] Image currCatagoryImage;
    [SerializeField] Text newNameText;
    [SerializeField] Text newPowerText;
    [SerializeField] Text newAccurText;
    [SerializeField] Text newDescriptionText;
    [SerializeField] Image newCatagoryImage;

    [SerializeField] GameObject currDetails;
    [SerializeField] List<Sprite> categories = new List<Sprite>();
    
    int currentSelection = 0;
    Color originalColor;
    MoveBase newMove;
    Sprite empty;
    Color highlightedColor;
    List<Sprite> typeBarSprites;
    
    private void Start(){
        empty = GlobalSettings.i.Empty;
        highlightedColor = GlobalSettings.i.HighlightedColor;
        typeBarSprites = GlobalSettings.i.TypeBarSprites;

    }

    public void SetMoveSelectionBars(List<Move> moves, MoveBase newMove) {
        this.newMove = newMove;

        for(int i = 0; i < moveBars.Count - 1; i++) {

            if(i < moves.Count) {
                moveBars[ i ].NameText.text = moves[ i ].Base.Name;

                moveBars[ i ].PpText.text = "PP: " + moves[ i ].Base.PP.ToString();

                if(moves[i].PP <= 0){
                    originalColor = Color.red;
                } else {
                    originalColor = Color.black;
                }

                SetMoveSelectionTypeBars(moves[ i ].Base, moveBars[ i ]);
            } else {
                moveBars[ i ].NameText.text = "";
                moveBars[ i ].PpText.text = "";
                moveBars[ i ].TypeImage.sprite = empty;
            }
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

    public void HandleMoveSelection(BattleUnit playerUnit, Action<int> onSelected){
        if(Input.GetKeyDown(KeyCode.DownArrow)){
            ++currentSelection;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            --currentSelection;
        }
        currentSelection = Mathf.Clamp(currentSelection, 0, PokemonBase.MaxNumberOfMoves);

        UpdateMoveSelection(currentSelection, playerUnit.Pokemon.Moves, newMove);

        if(Input.GetKeyDown(KeyCode.Return)){
            onSelected?.Invoke(currentSelection);
        }
    }

    public void UpdateMoveSelection(int selectedMove, List<Move> moves, MoveBase newMove){
        for(int i = 0; i < PokemonBase.MaxNumberOfMoves + 1; i++){
            bool isSelected = (i == selectedMove);

            if(i < moveBars.Count && moveBars[i] != null){
                moveBars[i].NameText.color = isSelected ? highlightedColor : Color.black;
                moveBars[i].PpText.color = isSelected ? highlightedColor : originalColor;
            }
        }

        MoveBase selectedMoveBase = null;
            
        if(selectedMove < moves.Count && moves[selectedMove] != null){
            selectedMoveBase = moves[selectedMove].Base;
        } else if(selectedMove == PokemonBase.MaxNumberOfMoves){
            selectedMoveBase = newMove;
        }

        if(selectedMoveBase == null || newMove == null){
            Debug.LogWarning("Selected move or new move is null. Details panel may not update.");
            return;
        }   

        SetMoveDetails(selectedMoveBase, newMove);
    }

    void SetMoveSelectionTypeBars(MoveBase move, MoveBar moveBar) {
        string type = move.Type.ToString();
        if(type == "Normal") {
            moveBar.TypeImage.sprite = typeBarSprites[ 0 ];
        } else if(type == "Fire") {
            moveBar.TypeImage.sprite = typeBarSprites[ 1 ];
        } else if(type == "Water") {
            moveBar.TypeImage.sprite = typeBarSprites[ 2 ];
        } else if(type == "Grass") {
            moveBar.TypeImage.sprite = typeBarSprites[ 3 ];
        } else if(type == "Electric") {
            moveBar.TypeImage.sprite = typeBarSprites[ 4 ];
        } else if(type == "Flying") {
            moveBar.TypeImage.sprite = typeBarSprites[ 5 ];
        } else if(type == "Bug") {
            moveBar.TypeImage.sprite = typeBarSprites[ 6 ];
        } else if(type == "Ice") {
            moveBar.TypeImage.sprite = typeBarSprites[ 7 ];
        } else if(type == "Dark") {
            moveBar.TypeImage.sprite = typeBarSprites[ 8 ];
        } else if(type == "Fairy") {
            moveBar.TypeImage.sprite = typeBarSprites[ 9 ];
        } else if(type == "Fighting") {
            moveBar.TypeImage.sprite = typeBarSprites[ 10 ];
        } else if(type == "Ground") {
            moveBar.TypeImage.sprite = typeBarSprites[ 11 ];
        } else if(type == "Steel") {
            moveBar.TypeImage.sprite = typeBarSprites[ 12 ];
        } else if(type == "Psychic") {
            moveBar.TypeImage.sprite = typeBarSprites[ 13 ];
        } else if(type == "Poison") {
            moveBar.TypeImage.sprite = typeBarSprites[ 14 ];
        } else if(type == "Rock") {
            moveBar.TypeImage.sprite = typeBarSprites[ 15 ];
        } else if(type == "Ghost") {
            moveBar.TypeImage.sprite = typeBarSprites[ 16 ];
        } else if(type == "Dragon") {
            moveBar.TypeImage.sprite = typeBarSprites[ 17 ];
        }
    }
}
