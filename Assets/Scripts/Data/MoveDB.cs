using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveDB{
    static Dictionary<string, MoveBase> moves;

    public static void Init(){
        moves = new Dictionary<string, MoveBase>();

        var moveArray = Resources.LoadAll<MoveBase>("");
        foreach(var move in moveArray){
            if(moves.ContainsKey(move.Name)){
                continue;
            }
            moves[move.Name] = move;
        }
    }

    public static MoveBase GetMoveByName(string name){
        if(!moves.ContainsKey(name)){
            return null;
        }

        return moves[name];
    }
}