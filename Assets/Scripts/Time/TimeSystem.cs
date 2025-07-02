using System;
using UnityEngine;
using UnityEngine.UI;

public enum DayPeriod { None ,Dawn, Morning, Noon, Afternoon, Sunset, Evening, Night}

public class TimeSystem : MonoBehaviour{
    [SerializeField] float timeDuration = 0.5f;
    [SerializeField] GameObject ClockUI;
    [SerializeField] Text clock;
    
    [Header("For Test")]
    [SerializeField] bool ContinueTime = true;
    [SerializeField] Vector2 clockTime = new Vector2( 0, 0);
    [SerializeField] DayPeriod currentPeriod = DayPeriod.None;
    [SerializeField] GeneralDayPeriod evolutionTime = GeneralDayPeriod.None;

    public int Minute {get; private set;} = 0;
    public int Hour {get; private set;} = 0;
    public int Day {get; private set;} = 0;
    public DayPeriod CurrentPeriod => currentPeriod;
    public GeneralDayPeriod EvolutionTime => evolutionTime;

    float timer;

    public static TimeSystem i {get; private set;}
    
    void Awake(){
        i = this;
    }

    void Update(){
        if(ContinueTime){
            timer += Time.deltaTime;
        } else {
            timer = 0;

            Hour = (int) clockTime.x;
            Minute = (int) clockTime.y;
        }
        if(timer >= timeDuration){
            timer = 0;
            Minute++;
            if(Minute >= 60){
                Minute = 0;
                Hour++;
                if(Hour >= 24){
                    Hour = 0;
                    Day++;
                }
            }
        }

        if(GameController.i.StateMachine.CurrentState == FreeRoamState.i){
            ClockUI.gameObject.SetActive(true);
        } else {
            ClockUI.gameObject.SetActive(false);
        }
        
        clock.text =$"{(Hour < 10 ? "0" : "")}{Hour}:{(Minute < 10 ? "0" : "")}{Minute}";
        currentPeriod = GetCurrentPeriod();
    }

    public DayPeriod GetCurrentPeriod(){
        if (Hour >= 5 && Hour < 7){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Dawn;
        } else if (Hour < 12){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Morning;
        } else if (Hour < 15){
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Noon;
        } else if (Hour < 18){ 
            evolutionTime = GeneralDayPeriod.Day;
            return DayPeriod.Afternoon;
        } else if (Hour < 19){
            if(UnityEngine.Random.Range(0, 250) < 25){
                evolutionTime = GeneralDayPeriod.Emerald;
            } else {
                evolutionTime = GeneralDayPeriod.Night;
            }
            return DayPeriod.Sunset;
        } else if (Hour < 21){
            evolutionTime = GeneralDayPeriod.Night;
            return DayPeriod.Evening;
        } else {
            evolutionTime = GeneralDayPeriod.Night;
            return DayPeriod.Night;
        }
    }
}
