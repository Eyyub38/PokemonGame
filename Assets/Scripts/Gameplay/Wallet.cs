using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wallet : MonoBehaviour{
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
}
