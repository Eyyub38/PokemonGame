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

    public static EvolutionState i { get; set; }

    private void Awake(){
        i = this;
    }

    public IEnumerator Evolve(Pokemon pokemon, Evolution evolution){
        var gameController = GameController.i;
        gameController.StateMachine.Push(this);
        evolutionUI.SetActive(true);

        AudioManager.i.PlayMusic(evolutionMusic);

        evolutionImage.sprite = pokemon.Base.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is evolving!");

        var oldPokemon = pokemon.Base;
        pokemon.Evolve(evolution);
        evolutionImage.sprite = evolution.EvolvesInto.FrontSprite;
        yield return DialogManager.i.ShowDialogText($"{oldPokemon.Name} evolved into {pokemon.Base.Name}!");

        evolutionUI.SetActive(false);

        gameController.PartyScreen.SetPartyData();
        AudioManager.i.PlayMusic(gameController.CurrentScene.SceneMusic, fade: true);
        gameController.StateMachine.Pop();
    }
}
