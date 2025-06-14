using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class MoveSelectionUI : SelectionUI<TextSlot>{
    [SerializeField] List<MoveBar> moveBars;
    [SerializeField] List<Sprite> typeBarSprites;
    [SerializeField] Sprite empty;

    Color originalColor;

    List<Sprite> TypeBarSprites => typeBarSprites;
    Sprite Empty => empty;

    void Start(){
        SetSelectionSettings(SelectionType.Grid, 2);
    }

    public void SetMoves(List<Move> moves){
        SetItems(moveBars.Take(moves.Count).Select(b => b.GetComponent<TextSlot>()).ToList());

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
                moveBars[i].TypeImage.sprite = Empty;
            }
        }
    }

    public void SetTypeBars(Move move,MoveBar moveBar){
        string type = move.Base.Type.ToString();
        if(type == "Normal"){
            moveBar.TypeImage.sprite = TypeBarSprites[0];
        } else if(type == "Fire"){
            moveBar.TypeImage.sprite = TypeBarSprites[1];
        } else if(type == "Water"){
            moveBar.TypeImage.sprite = TypeBarSprites[2];
        } else if(type == "Grass"){
            moveBar.TypeImage.sprite = TypeBarSprites[3];
        } else if(type == "Electric"){
            moveBar.TypeImage.sprite = TypeBarSprites[4];
        } else if(type == "Flying"){
            moveBar.TypeImage.sprite = TypeBarSprites[5];
        } else if(type == "Bug"){
            moveBar.TypeImage.sprite = TypeBarSprites[6];
        } else if(type == "Ice"){
            moveBar.TypeImage.sprite = TypeBarSprites[7];
        } else if(type == "Dark"){
            moveBar.TypeImage.sprite = TypeBarSprites[8];
        } else if(type == "Fairy"){
            moveBar.TypeImage.sprite = TypeBarSprites[9];
        } else if(type == "Fighting"){
            moveBar.TypeImage.sprite = TypeBarSprites[10];
        } else if(type == "Ground"){
            moveBar.TypeImage.sprite = TypeBarSprites[11];
        } else if(type == "Steel"){
            moveBar.TypeImage.sprite = TypeBarSprites[12];
        } else if(type == "Psychic"){
            moveBar.TypeImage.sprite = TypeBarSprites[13];
        } else if(type == "Poison"){
            moveBar.TypeImage.sprite = TypeBarSprites[14];
        } else if(type == "Rock"){
            moveBar.TypeImage.sprite = TypeBarSprites[15];
        } else if(type == "Ghost"){
            moveBar.TypeImage.sprite = TypeBarSprites[16];
        } else if(type == "Dragon"){
            moveBar.TypeImage.sprite = TypeBarSprites[17];
        }
    }
}
