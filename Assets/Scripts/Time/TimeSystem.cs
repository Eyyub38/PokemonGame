using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum DayPeriod { None ,Dawn, Morning, Noon, Afternoon, Sunset, Evening, Night}
public enum WeekDay { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }
public enum Month { January, February, March, April, May, June, July, August, September, October, November, December }
public enum Season { Spring, Summer, Autumn, Winter }

public class TimeSystem : MonoBehaviour{
    [SerializeField] public float timeDuration = 0.5f;
    [SerializeField] GameObject ClockUI;
    [SerializeField] Text clock;
    
    [Header("For Test")]
    [SerializeField] bool continueTime = true;
    [SerializeField] Vector2 clockTime = new Vector2( 0, 0);
    [SerializeField] DayPeriod currentPeriod = DayPeriod.None;
    [SerializeField] GeneralDayPeriod evolutionTime = GeneralDayPeriod.None;

    public bool ContinueTime {get; set;}
    public int Minute {get; set;} = 0;
    public int Hour {get; set;} = 0;
    public int Day { get; private set; } = 1;

    public DayPeriod CurrentPeriod => currentPeriod;
    public GeneralDayPeriod EvolutionTime => evolutionTime;

    float timer;

    public static TimeSystem i {get; private set;}

    public event System.Action OnDayChanged;
    public event System.Action OnTimeChanged;
    
    void Awake(){
        i = this;
    }

    void Update(){
        if(continueTime){
            timer += Time.deltaTime;
        } else {
            timer = 0;
            Hour = (int) clockTime.x;
            Minute = (int) clockTime.y;
        }

        if(timer >= timeDuration && GameController.i.StateMachine.CurrentState == FreeRoamState.i) {
            timer = 0;
            Minute++;
            
            if(Minute >= 60){
                Minute = 0;
                Hour++;
                
                if(Hour >= 24){
                    Hour = 0;
                    Day++;
                    OnDayChanged?.Invoke();
                }
            }
            OnTimeChanged?.Invoke();
        }

        bool showClock = GameController.i != null && GameController.i.StateMachine != null && GameController.i.StateMachine.CurrentState == GameMenuState.i;

        if(ClockUI != null) {
            if(ClockUI != gameObject) {
                if(ClockUI.activeSelf != showClock) ClockUI.SetActive(showClock);
            } else {
                if(clock != null) clock.enabled = showClock;
                if(TryGetComponent<Image>(out var image)) image.enabled = showClock;
                for(int i = 0; i < transform.childCount; i++) {
                    transform.GetChild(i).gameObject.SetActive(showClock);
                }
            }
        }

        if(showClock) UpdateClockDisplay();
        currentPeriod = GetCurrentPeriod();
    }

    void UpdateClockDisplay() {
        clock.text = $"{(Hour < 10 ? "0" : "")}{Hour}:{(Minute < 10 ? "0" : "")}{Minute}";
    }

    public DayPeriod GetCurrentPeriod(){
        if(Hour >= 5 && Hour < 7){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Dawn;
        } else if(Hour < 12){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Morning;
        } else if(Hour < 15){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Noon;
        } else if(Hour < 18){ 
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Afternoon;
        } else if(Hour < 19){
            if(UnityEngine.Random.Range(0, 250) < 25){
                evolutionTime = GeneralDayPeriod.Emerald;
            } else {
                evolutionTime = GeneralDayPeriod.Night;
            }
            return DayPeriod.Sunset;
        } else if(Hour < 21){
            evolutionTime = GeneralDayPeriod.Night;
            return DayPeriod.Evening;
        } else {
            evolutionTime = GeneralDayPeriod.Night;
            return DayPeriod.Night;
        }
    }
}
