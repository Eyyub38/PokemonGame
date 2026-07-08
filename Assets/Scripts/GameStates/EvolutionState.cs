using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class EvolutionState : State<GameController>{
    [SerializeField] GameObject evolutionUI;
    [SerializeField] Image evolutionImage;
    [SerializeField] AudioClip evolutionMusic;

    public static EvolutionState i { get; private set; }

    private void Awake(){
        i = this;
    }

    public override void Enter(GameController owner) {
        evolutionUI.SetActive(true);
        owner.InputMaps.EnableUI();
    }

    public override void Exit() {
        evolutionUI.SetActive(false);
        var prevState = GameController.i.StateMachine.GetPrevState();
        if(prevState is FreeRoamState) {
            GameController.i.InputMaps.EnablePlayer();
        } else {
            GameController.i.InputMaps.EnableUI();
        }
    }

    public IEnumerator Evolve(Pokemon pokemon, Evolution evolution){
        var gameController = GameController.i;
        gameController.StateMachine.Push(this);

        AudioManager.i.PlayMusic(evolutionMusic);

        evolutionImage.sprite = pokemon.Base.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is evolving!");

        var oldPokemon = pokemon.Base;
        
        // Simple animation
        float t = 0;
        while(t < 1) {
            t = Mathf.Clamp01(t + Time.deltaTime * 2);
            evolutionImage.transform.localScale = Vector3.one * (1 - t);
            yield return null;
        }

        pokemon.Evolve(evolution);
        evolutionImage.sprite = evolution.EvolvesInto.FrontSprite;
        
        t = 0;
        while(t < 1) {
            t = Mathf.Clamp01(t + Time.deltaTime * 2);
            evolutionImage.transform.localScale = Vector3.one * t;
            yield return null;
        }

        yield return DialogManager.i.ShowDialogText($"{oldPokemon.Name} evolved into {pokemon.Base.Name}!");

        // Learn moves after evolution
        var newMoves = pokemon.GetLearnableMoves();
        foreach (var move in newMoves) {
            if(pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
                pokemon.LearnMove(move);
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} learned {move.Name}!");
            } else {
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is trying to learn {move.Name}...");
                yield return DialogManager.i.ShowDialogText($"But it already knows {PokemonBase.MaxNumberOfMoves} moves.");
                yield return DialogManager.i.ShowDialogText($"Choose a move to forget.");

                MoveForgetState.i.CurrentMoves = pokemon.Moves;
                MoveForgetState.i.NewMove = move;
                yield return gameController.StateMachine.PushAndWait(MoveForgetState.i);

                var moveIndex = MoveForgetState.i.Selection;
                if(moveIndex == PokemonBase.MaxNumberOfMoves){
                    yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} didn't learn {move.Name}.");
                } else {
                    var selectedMove = pokemon.Moves[moveIndex].Base;
                    yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learned {move.Name}.");
                    pokemon.Moves[moveIndex] = new Move(move);
                }
            }
        }

        gameController.PartyScreen.SetPartyData();
        AudioManager.i.PlayMusic(gameController.CurrentScene.SceneMusic, fade: true);
        gameController.StateMachine.Pop();
    }

    public IEnumerator Evolve(Pokemon pokemon, PokemonEvolutionDefinition evolution, PokemonEvolutionTriggerKind trigger = PokemonEvolutionTriggerKind.Manual){
        if(pokemon == null || evolution == null || evolution.EvolvesInto == null){
            yield break;
        }

        var gameController = GameController.i;
        gameController.StateMachine.Push(this);

        AudioManager.i.PlayMusic(evolutionMusic);

        evolutionImage.sprite = pokemon.Base.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is evolving!");

        var oldPokemon = pokemon.Base;

        float t = 0;
        while(t < 1) {
            t = Mathf.Clamp01(t + Time.deltaTime * 2);
            evolutionImage.transform.localScale = Vector3.one * (1 - t);
            yield return null;
        }

        pokemon.Evolve(evolution, trigger, "evolution-state");
        evolutionImage.sprite = evolution.EvolvesInto.FrontSprite;

        t = 0;
        while(t < 1) {
            t = Mathf.Clamp01(t + Time.deltaTime * 2);
            evolutionImage.transform.localScale = Vector3.one * t;
            yield return null;
        }

        yield return DialogManager.i.ShowDialogText($"{oldPokemon.Name} evolved into {pokemon.Base.Name}!");

        var newMoves = pokemon.GetLearnableMoves();
        foreach (var move in newMoves) {
            if(pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
                pokemon.LearnMove(move);
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} learned {move.Name}!");
            } else {
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is trying to learn {move.Name}...");
                yield return DialogManager.i.ShowDialogText($"But it already knows {PokemonBase.MaxNumberOfMoves} moves.");
                yield return DialogManager.i.ShowDialogText($"Choose a move to forget.");

                MoveForgetState.i.CurrentMoves = pokemon.Moves;
                MoveForgetState.i.NewMove = move;
                yield return gameController.StateMachine.PushAndWait(MoveForgetState.i);

                var moveIndex = MoveForgetState.i.Selection;
                if(moveIndex == PokemonBase.MaxNumberOfMoves){
                    yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} didn't learn {move.Name}.");
                } else {
                    var selectedMove = pokemon.Moves[moveIndex].Base;
                    yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learned {move.Name}.");
                    pokemon.Moves[moveIndex] = new Move(move);
                }
            }
        }

        gameController.PartyScreen.SetPartyData();
        AudioManager.i.PlayMusic(gameController.CurrentScene.SceneMusic, fade: true);
        gameController.StateMachine.Pop();
    }
}
