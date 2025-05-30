using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;


public class BattleDialogBox : MonoBehaviour{
    [SerializeField] int letterPerSecond;
    [SerializeField] Color highlightedColor;

    [SerializeField] Text dialogText;
    [SerializeField] GameObject actionSelector;
    [SerializeField] GameObject moveSelector;
    [SerializeField] GameObject choiceBox;

    [SerializeField] Text yesText;
    [SerializeField] Text noText;

    [SerializeField] List<Text> actionTexts;
    [SerializeField] List<MoveBar> moveBars;
    [SerializeField] List<Sprite> typeBarSprites;

    [SerializeField] Sprite empty;

    public Sprite Empty => empty;

    Color originalColor;
    public void SetDialog(string dialog){
        dialogText.text = dialog;
    }

    public IEnumerator TypeDialog(string dialog){
        dialogText.text = "";
        foreach (var letter in dialog.ToCharArray()){
            dialogText.text += letter;
            yield return new WaitForSeconds( 1f / letterPerSecond);
        }
        yield return new WaitForSeconds(1f);
    }

    public void EnableDialogText(bool enabled){
        dialogText.enabled = enabled;
    }
    
    public void EnableActionSelector(bool enabled){
        actionSelector.SetActive(enabled);
    }
    
    public void EnableMoveSelector(bool enabled){
        moveSelector.SetActive(enabled);
    }

    public void EnableChoiceBox(bool enabled){
        choiceBox.SetActive(enabled);
    }

    public void UpdateActionSelection(int selectedAction){
        for(int i = 0; i < actionTexts.Count; i++){
            if(i == selectedAction){
                actionTexts[i].color = highlightedColor;
            } else {
                actionTexts[i].color = Color.white;
            }
        }
    }

    public void UpdateMoveSelection(int selectedMove){
        for(int i = 0; i < moveBars.Count; i++){
            if(i == selectedMove){
                moveBars[i].NameText.color = highlightedColor;
                moveBars[i].PpText.color = highlightedColor;
            } else {
                moveBars[i].NameText.color = Color.white;
                moveBars[i].PpText.color = originalColor;
            }
        }
    }

    public void UpdateChoiceSelection(bool selectedChoice){
        if(selectedChoice){
            yesText.color = highlightedColor;
            noText.color = Color.white;
        } else {
            noText.color = highlightedColor;
            yesText.color = Color.white;
        }
    }

    public void SetMoveBars(List<Move> moves){
        for(int i=0; i< moveBars.Count; ++i){
            if(i < moves.Count){
                moveBars[i].NameText.text = moves[i].Base.Name;
                moveBars[i].PpText.text = "PP: " + moves[i].PP.ToString() + "/" + moves[i].Base.PP.ToString();
                
                if(moves[i].PP <= 0){
                    originalColor = Color.red;
                } else {
                    originalColor = Color.white;
                }

                SetTypeBars(moves[i],moveBars[i]);
            } else {
                moveBars[i].NameText.text = "";
                moveBars[i].PpText.text = "";
                moveBars[i].TypeImage.sprite = empty;
            }
        }
    }

    public void SetTypeBars(Move move,MoveBar moveBar){
        string type = move.Base.Type.ToString();
        if(type == "Normal"){
            moveBar.TypeImage.sprite = typeBarSprites[0];
        } else if(type == "Fire"){
            moveBar.TypeImage.sprite = typeBarSprites[1];
        } else if(type == "Water"){
            moveBar.TypeImage.sprite = typeBarSprites[2];
        } else if(type == "Grass"){
            moveBar.TypeImage.sprite = typeBarSprites[3];
        } else if(type == "Electric"){
            moveBar.TypeImage.sprite = typeBarSprites[4];
        } else if(type == "Ice"){
            moveBar.TypeImage.sprite = typeBarSprites[5];
        } else if(type == "Fighting"){
            moveBar.TypeImage.sprite = typeBarSprites[6];
        } else if(type == "Poison"){
            moveBar.TypeImage.sprite = typeBarSprites[7];
        } else if(type == "Ground"){
            moveBar.TypeImage.sprite = typeBarSprites[8];
        } else if(type == "Flying"){
            moveBar.TypeImage.sprite = typeBarSprites[9];
        } else if(type == "Psychic"){
            moveBar.TypeImage.sprite = typeBarSprites[10];
        } else if(type == "Bug"){
            moveBar.TypeImage.sprite = typeBarSprites[11];
        } else if(type == "Rock"){
            moveBar.TypeImage.sprite = typeBarSprites[12];
        } else if(type == "Ghost"){
            moveBar.TypeImage.sprite = typeBarSprites[13];
        } else if(type == "Dragon"){
            moveBar.TypeImage.sprite = typeBarSprites[14];
        } else if(type == "Dark"){
            moveBar.TypeImage.sprite = typeBarSprites[15];
        } else if(type == "Steel"){
            moveBar.TypeImage.sprite = typeBarSprites[16];
        } else if(type == "Fairy"){
            moveBar.TypeImage.sprite = typeBarSprites[17];
        }
    }
}
