using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class SummaryScreenUI : SelectionUI<TextSlot>{
    [Header("Pages")]
    [SerializeField] GameObject skillPage;
    [SerializeField] GameObject movePage;

    [Header("Pokemon Details")]
    [SerializeField] Image pokemonImage;
    [SerializeField] Text pokemonName;
    [SerializeField] Text pokemonLevel;
    [SerializeField] Image type1;
    [SerializeField] Image type2;
    
    [Header("Pokemon EXP")]
    [SerializeField] Text expPointText;
    [SerializeField] Text nextLevelText;
    [SerializeField] Transform expBar;

    [Header ("Type Images")]
    [SerializeField] List<Sprite> typeImages;

    [Header("Pokemon Skills Page")]
    [Header("Pokemon Stats")]
    [SerializeField] Text hpText;
    [SerializeField] Text attackText;
    [SerializeField] Text defenseText;
    [SerializeField] Text spAttackText;
    [SerializeField] Text spDefenseText;
    [SerializeField] Text speedText;

    [Header("Pokemon Moves Page")]
    [Header("Moves")]
    [SerializeField] List<Text> moveNames;
    [SerializeField] List<Text> moveTypes;
    [SerializeField] List<Text> movePPs;

    [Header("Selected Move Details")]
    [SerializeField] Text descriptionText;
    [SerializeField] Text categoryText;
    [SerializeField] Text powerText;
    [SerializeField] Text accuracyText;

    bool inMoveSelection;
    Pokemon pokemon;
    List<TextSlot> moveSlots;
    Dictionary<PokemonType, Sprite> typeSprites;

    public bool InMoveSelection {
        get => inMoveSelection;
        set{
            inMoveSelection = value;
            if(inMoveSelection){
                SetItems(moveSlots.Take(pokemon.Moves.Count).ToList());
            } else {
                descriptionText.text = "";
                categoryText.text = "";
                powerText.text = "";
                accuracyText.text = "";
                ClearItems();
            }
        }   
    }

    void Awake(){
        typeSprites = new Dictionary<PokemonType, Sprite>(){
            { PokemonType.Normal, typeImages[0]},
            { PokemonType.Fire, typeImages[1]},
            { PokemonType.Water, typeImages[2]},
            { PokemonType.Grass, typeImages[3]},
            { PokemonType.Electric, typeImages[4]},
            { PokemonType.Ice, typeImages[5]},
            { PokemonType.Fighting, typeImages[6]},
            { PokemonType.Poison, typeImages[7]},
            { PokemonType.Ground, typeImages[8]},
            { PokemonType.Flying, typeImages[9]},
            { PokemonType.Psychic, typeImages[10]},
            { PokemonType.Bug, typeImages[11]},
            { PokemonType.Rock, typeImages[12]},
            { PokemonType.Ghost, typeImages[13]},
            { PokemonType.Dragon, typeImages[14]},
            { PokemonType.Dark, typeImages[15]},
            { PokemonType.Steel, typeImages[16]},
            { PokemonType.Fairy, typeImages[17]}
        };
    }

    void Start(){
        moveSlots = moveNames.Select(n => n.GetComponent<TextSlot>()).ToList();
        descriptionText.text = "";
        categoryText.text = "";
        powerText.text = "";
        accuracyText.text = "";
    }

    public void ShowPage(int page){
        if(page == 0){
            movePage.SetActive(false);
            skillPage.SetActive(true);

            SetSkills();
        } else if(page == 1){
            skillPage.SetActive(false);
            movePage.SetActive(true);

            SetMoves();
        }
    }

    public void SetBasicDetails(Pokemon pokemon){
        this.pokemon = pokemon;
        pokemonImage.sprite = pokemon.Base.FrontSprite;
        pokemonName.text = pokemon.Base.Name.ToUpper();
        pokemonLevel.text = "Level: " + pokemon.Level;
    }

    public void SetSkills(){
        hpText.text = $"{pokemon.HP} / {pokemon.Base.MaxHp}";
        attackText.text = "" + pokemon.Attack;
        defenseText.text = "" + pokemon.Defense;
        spAttackText.text = "" + pokemon.SpAttack;
        spDefenseText.text = "" + pokemon.SpDefense;
        speedText.text = "" + pokemon.Speed;

        expPointText.text = "" + pokemon.Exp;
        nextLevelText.text = "" + (pokemon.Base.GetExpForLevel(pokemon.Level + 1) - pokemon.Exp);
        expBar.localScale = new Vector2(pokemon.GetNormalizedExp(), 1);
    }

    public void SetTypeImage(){
        type1.sprite = typeSprites[pokemon.Base.Type1];
        if(pokemon.Base.Type2 == PokemonType.None){
            type2.sprite = null;
        } else {
            type2.sprite = typeSprites[pokemon.Base.Type2];
        }
    }

    public void SetMoves(){
        for(int i = 0; i < moveNames.Count; ++i){
           if(i < pokemon.Moves.Count){
                var move = pokemon.Moves[i];
                moveNames[i].text = move.Base.Name.ToUpper();
                moveTypes[i].text = move.Base.Type.ToString().ToUpper();
                movePPs[i].text = "" + move.PP + "/" + move.Base.PP;
           } else {
                moveNames[i].text = "-";
                moveTypes[i].text = "-";
                movePPs[i].text = "-";
           }
        }
    }

    public override void HandleUpdate(){
        if(InMoveSelection){
            base.HandleUpdate();
        }
    }

    public override void UpdateSelectionInUI(){
        base.UpdateSelectionInUI();

        var move = pokemon.Moves[selectedItem];
        descriptionText.text = move.Base.Description;
        categoryText.text = move.Base.Category.ToString();
        powerText.text = "" + move.Base.Power;
        accuracyText.text = "" + move.Base.Accuracy;
    }
}
