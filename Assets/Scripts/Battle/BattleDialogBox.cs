using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class BattleDialogBox : MonoBehaviour{
    [Header("Dialog")]
    [SerializeField] int letterPerSecond;
    [SerializeField] Text dialogText;

    [Header("Choice Selection")]
    [SerializeField] GameObject choiceBox;
    [SerializeField] Text yesText;
    [SerializeField] Text noText;

    [Header("Move Selection")]
    [SerializeField] List<MoveBar> moveBars;
    [SerializeField] List<Sprite> typeBarSprites;
    [SerializeField] Sprite empty;

    Color originalColor;
    Color  highlightedColor;

    public bool IsChoiceBoxEnabled => choiceBox.activeSelf;

    private void Start(){
        highlightedColor = GlobalSettings.i.HighlightedColor;
    }

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

    public void EnableChoiceBox(bool enabled){
        choiceBox.SetActive(enabled);
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
        
        if(spriteIndex >= 0 && spriteIndex < typeBarSprites.Count){
            moveBar.TypeImage.sprite = typeBarSprites[spriteIndex];
        } else {
            moveBar.TypeImage.sprite = empty;
        }
    }
}
