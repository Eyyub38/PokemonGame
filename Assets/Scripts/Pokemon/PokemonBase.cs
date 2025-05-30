using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GrowthRate{ Fluctuating ,Slow, MediumSlow, MediumFast, Fast, Erratic}

public enum PokemonType{ None, Normal, Fire, Water, Grass, Electric, Ice, Fighting, Poison, Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy}

public enum Stat{ Attack, Defense , SpAttack, SpDefense, Speed, Accuracy, Evasion}

[CreateAssetMenu(fileName = "Pokemon", menuName = "Pokemon/Create new Pokemon")]
public class PokemonBase : ScriptableObject{
    //Name & Description
    [SerializeField] string _name;

    [TextArea]
    [SerializeField] string description;

    //Sprites
    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;
    [SerializeField] Sprite iconSprite;
    
    //Gender
    [SerializeField] Sprite femaleFrontSprite;
    [SerializeField] Sprite femaleBackSprite;
    [SerializeField] bool hasGenderDifferences;
    [SerializeField] float maleRatio = 0.5f;
    [SerializeField] bool isGenderless;

    //Types
    [SerializeField] PokemonType type1;
    [SerializeField] PokemonType type2;
    
    //Base States
    [SerializeField] int maxHp;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int spAttack;
    [SerializeField] int spDefense;
    [SerializeField] int speed;

    [SerializeField] int catchRate = 255;
    [SerializeField] int xpYield;
    [SerializeField] GrowthRate growthRate;

    //LearnableMoves
    [SerializeField] List<LearnableMoves> learnableMoves;

    //Properties
    public string Name{ get{return _name;}}
    public string Description{ get{return description;}}
    public Sprite FrontSprite{ get{return frontSprite;}}
    public Sprite BackSprite{ get{return backSprite;}}
    public Sprite IconSprite{ get{return iconSprite;}}
    public Sprite FemaleFrontSprite { get { return femaleFrontSprite; } }
    public Sprite FemaleBackSprite { get { return femaleBackSprite; } }
    public bool HasGenderDifferences { get { return hasGenderDifferences; } }
    public bool IsGenderless { get { return isGenderless; } }
    public float MaleRatio { get { return maleRatio; } }
    public PokemonType Type1{ get{return type1;}}
    public PokemonType Type2{ get{return type2;}}
    public GrowthRate GrowthRate => growthRate;
    public int MaxHp{ get{return maxHp;}}
    public int Attack{ get{return attack;}}
    public int Defense{ get{return defense;}}
    public int Speed{ get{return speed;}}
    public int SpAttack{ get{return spAttack;}}
    public int SpDefense{ get{return spDefense;}}
    public int CatchRate => catchRate;
    public int XpYield => xpYield;
    public List<LearnableMoves> LearnableMoves{ get{return learnableMoves;}}

    public int GetExpForLevel(int level){
        if(growthRate == GrowthRate.Erratic){
            if(level < 50){
                return Mathf.FloorToInt(( level * level * level ) * ( 100 - level ) / 50);
            } else if(level >= 50 && level < 68){
                return Mathf.FloorToInt(( level * level * level ) * ( 150 - level ) / 100);
            } else if(level >= 68 && level < 98){
                return Mathf.FloorToInt(( level * level * level ) * (( 1911 - level ) / 3) / 500);
            } else {
                return Mathf.FloorToInt(( level * level * level ) * ( 160 - level ) / 100);
            }

        } else if(growthRate == GrowthRate.Fast){
            return Mathf.FloorToInt(4 * (level * level * level) / 5);

        } else if(growthRate == GrowthRate.MediumFast){
            return (level * level * level);

        } else if(growthRate == GrowthRate.MediumSlow){
            return Mathf.FloorToInt((6 / 5) *( level * level * level ) - 15 * (level * level) + 100 * level - 140 );
        } else if(growthRate == GrowthRate.Slow){

            return Mathf.FloorToInt( 5 * (level * level * level) / 4 );

        } else if(growthRate == GrowthRate.Fluctuating){
            if(level < 15){
                return Mathf.FloorToInt((( level * level * level ) * (( level + 1 ) / 3) + 24)/ 50);
            } else if(level >= 15 && level < 36){
                return Mathf.FloorToInt(( level * level * level ) * ( level + 14 ) / 50);
            } else {
                return Mathf.FloorToInt(( level * level * level ) * (( level / 2 ) + 32) / 50);
            }
        }
        return -1;
    }
}

[System.Serializable]
public class LearnableMoves{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base{ get{return moveBase;}}
    public int Level{ get{return level;}}
}

public class TypeChart{
    static float[][] chart ={
                                 //Normal  Fire  Water Grass Electric Ice Fighting Poison Ground Flying Psychic Bug Rock Ghost Dragon Dark Steel Fairy
        /*Normal*/  new float []{   1f,     1f,   1f,    1f,     1f,  1f,    1f,     1f,    1f,    1f,    1f,   1f, 0.5f,  0f,   1f,   1f, 0.5f,  1f },
        /*Fire*/    new float []{   1f,    0.5f, 0.5f,   2f,     1f,  2f,    1f,     1f,    1f,    1f,    1f,   2f, 0.5f,  1f,  0.5f,  1f,  2f,   1f },
        /*Water*/   new float []{   1f,     2f,  0.5f,  0.5f,    1f,  1f,    1f,     1f,    2f,    1f,    1f,   1f,  2f,   1f,  0.5f,  1f,  1f,   1f },
        /*Grass*/   new float []{   1f,    0.5f,  2f,   0.5f,    1f,  1f,    1f,    0.5f,   2f,   0.5f,   1f,  0.5f, 2f,   1f,  0.5f,  1f, 0.5f,  1f },
        /*Electric*/new float []{   1f,     1f,   2f,   0.5f,   0.5f, 1f,    1f,     1f,    0f,    2f,    1f,   1f,  1f,   1f,  0.5f,  1f,  1f,   1f },
        /*Ice*/     new float []{   1f,    0.5f, 0.5f,   2f,     1f, 0.5f,   1f,     1f,    2f,    2f,    1f,   1f,  1f,   1f,   2f,   1f, 0.5f,  1f },
        /*Fighting*/new float []{   2f,     1f,   1f,    1f,     1f,  2f,    1f,    0.5f,   1f,   0.5f,  0.5f, 0.5f, 2f,   0f,   1f,   2f,  2f,  0.5f},
        /*Poison*/  new float []{   1f,     1f,   1f,    2f,     1f,  1f,    1f,    0.5f,  0.5f,   1f,    1f,   1f, 0.5f, 0.5f,  1f,   1f,  0f,   2f },
        /*Ground*/  new float []{   1f,     2f,   1f,   0.5f,    2f,  1f,    1f,     2f,    1f,    0f,    1f,  0.5f, 2f,   1f,   1f,   1f,  2f,   1f },
        /*Flying*/  new float []{   1f,     1f,   1f,    2f,    0.5f, 1f,    2f,     1f,    1f,    1f,    1f,   2f, 0.5f,  1f,   1f,   1f, 0.5f,  1f },
        /*Psychic*/ new float []{   1f,     1f,   1f,    1f,     1f,  1f,    2f,     2f,    1f,    1f,   0.5f,  1f,  1f,   1f,   1f,   0f, 0.5f,  1f },
        /*Bug*/     new float []{   1f,    0.5f,  1f,    2f,     1f,  1f,   0.5f,   0.5f,   1f,   0.5f,   2f,   1f,  1f,  0.5f,  1f,   2f, 0.5f, 0.5f},
        /*Rock*/    new float []{   1f,     2f,   1f,    1f,     2f,  2f,   0.5f,    1f,   0.5f,   2f,    1f,   2f,  1f,   1f,   1f,   1f, 0.5f,  1f },
        /*Ghost*/   new float []{   0f,     1f,   1f,    1f,     1f,  1f,    1f,     1f,    1f,    1f,    2f,   1f,  1f,   2f,   1f,  0.5f, 1f,   1f },
        /*Dragon*/  new float []{   1f,     1f,   1f,    1f,     1f,  1f,    1f,     1f,    1f,    1f,    1f,   1f,  1f,   1f,   2f,   1f, 0.5f,  0f },
        /*Dark*/    new float []{   1f,     1f,   1f,    1f,     1f,  1f,   0.5f,    1f,    1f,    1f,    2f,   1f,  1f,   2f,   1f,  0.5f, 1f,  0.5f},
        /*Steel*/   new float []{   1f,    0.5f, 0.5f,   1f,    0.5f, 2f,    1f,     1f,    1f,    1f,    1f,   1f,  2f,   1f,   1f,   1f, 0.5f,  2f },
        /*Fairy*/   new float []{   1f,    0.5f,  1f,    1f,     1f,  1f,    2f,    0.5f,   1f,    1f,    1f,   1f,  1f,   1f,   2f,   2f, 0.5f,  1f }
    };

    public static float GetEffectiveness(PokemonType attackType, PokemonType defenseType){
        if(attackType == PokemonType.None || defenseType == PokemonType.None) 
            return 1f;

        int row = (int)attackType - 1;
        int column = (int)defenseType - 1;
        
        return chart[row][column];
    }
}
