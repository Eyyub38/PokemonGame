using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wallet : MonoBehaviour, ISavable{
    [SerializeField] float money;

    public float Money => money;

    public event Action OnMoneyChanged;

    public static Wallet i {get; private set;}

    private void Awake(){
        i = this;
    }

    public void AddMoney(float amount){
        money += amount;
        OnMoneyChanged?.Invoke();
    }

    public void TakeMoney(float amount){
        money -= amount;
        OnMoneyChanged?.Invoke();
    }

    public bool HasMoney(float amount){
        return amount <= money;
    }

    public object CaptureState(){
        return money;
    }

    public void RestoreState(object state){
        money = (float)state;
    }
}
