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

    [Header("Advanced Time Settings")]
    [SerializeField] bool enableAdvancedTime = true;
    [SerializeField] int startDay = 1;
    [SerializeField] WeekDay startWeekDay = WeekDay.Monday;
    [SerializeField] Month startMonth = Month.January;
    [SerializeField] Season startSeason = Season.Spring;

    public bool ContinueTime {get; set;}
    public int Minute {get; set;} = 0;
    public int Hour {get; set;} = 0;
    public int Day {get; private set;} = 1;
    public int Week {get; private set;} = 1;
    public int Year {get; private set;} = 1;
    public WeekDay CurrentWeekDay {get; private set;} = WeekDay.Monday;
    public Month CurrentMonth {get; private set;} = Month.January;
    public Season CurrentSeason {get; private set;} = Season.Spring;
    
    public DayPeriod CurrentPeriod => currentPeriod;
    public GeneralDayPeriod EvolutionTime => evolutionTime;

    float timer;

    public static TimeSystem i {get; private set;}

    public event System.Action OnDayChanged;
    public event System.Action OnWeekChanged;
    public event System.Action OnMonthChanged;
    public event System.Action OnSeasonChanged;
    public event System.Action OnTimeChanged;
    
    void Awake(){
        i = this;
        InitializeTime();
    }

    void InitializeTime(){
        Day = startDay;
        CurrentWeekDay = startWeekDay;
        CurrentMonth = startMonth;
        CurrentSeason = startSeason;
        CalculateWeek();
    }

    void Update(){
        if(continueTime){
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
                    OnDayChanged?.Invoke();
                    
                    if(enableAdvancedTime){
                        UpdateAdvancedTime();
                    }
                }
            }
            OnTimeChanged?.Invoke();
        }

        if(GameController.i.StateMachine.CurrentState == FreeRoamState.i){
            ClockUI.gameObject.SetActive(true);
            
        } else {
            ClockUI.gameObject.SetActive(false);
        }
        
        UpdateClockDisplay();
        currentPeriod = GetCurrentPeriod();
    }

    void UpdateAdvancedTime(){
        UpdateWeekDay();
        UpdateMonth();
        UpdateSeason();
        CalculateWeek();
    }

    void UpdateWeekDay(){
        WeekDay previousWeekDay = CurrentWeekDay;
        CurrentWeekDay = (WeekDay)(((int)CurrentWeekDay + 1) % 7);
        
        if(CurrentWeekDay == WeekDay.Monday && previousWeekDay == WeekDay.Sunday){
            Week++;
            OnWeekChanged?.Invoke();
        }
    }

    void UpdateMonth(){
        Month previousMonth = CurrentMonth;
        int daysInMonth = GetDaysInMonth(CurrentMonth, Year);
        
        if(Day > daysInMonth){
            Day = 1;
            CurrentMonth = (Month)(((int)CurrentMonth + 1) % 12);
            
            if(CurrentMonth == Month.January && previousMonth == Month.December){
                Year++;
            }
            
            OnMonthChanged?.Invoke();
        }
    }

    void UpdateSeason(){
        Season previousSeason = CurrentSeason;
        CurrentSeason = GetSeasonForMonth(CurrentMonth);
        
        if(CurrentSeason != previousSeason){
            OnSeasonChanged?.Invoke();
        }
    }

    void CalculateWeek(){
        Week = ((Day - 1) / 7) + 1;
    }

    int GetDaysInMonth(Month month, int year){
        switch(month){
            case Month.January: case Month.March: case Month.May: case Month.July:
            case Month.August: case Month.October: case Month.December:
                return 31;
            case Month.April: case Month.June: case Month.September: case Month.November:
                return 30;
            case Month.February:
                return IsLeapYear(year) ? 29 : 28;
            default:
                return 30;
        }
    }

    bool IsLeapYear(int year){
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    Season GetSeasonForMonth(Month month){
        switch(month){
            case Month.March: case Month.April: case Month.May:
                return Season.Spring;
            case Month.June: case Month.July: case Month.August:
                return Season.Summer;
            case Month.September: case Month.October: case Month.November:
                return Season.Autumn;
            default:
                return Season.Winter;
        }
    }

    void UpdateClockDisplay(){
        if(enableAdvancedTime){
            clock.text = $"{CurrentWeekDay} {CurrentMonth} {Day}, Year {Year}\n{(Hour < 10 ? "0" : "")}{Hour}:{(Minute < 10 ? "0" : "")}{Minute}";
        } else {
            clock.text = $"{(Hour < 10 ? "0" : "")}{Hour}:{(Minute < 10 ? "0" : "")}{Minute}";
        }
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

    public bool IsWeekend(){
        return (CurrentWeekDay == WeekDay.Saturday || CurrentWeekDay == WeekDay.Sunday);
    }

    public bool IsWorkDay(){
        return !IsWeekend();
    }

    public bool IsNightTime(){
        return Hour >= 21 || Hour < 5;
    }

    public bool IsDayTime(){
        return Hour >= 5 && Hour < 21;
    }

    public float GetDayProgress(){
        return (Hour * 60f + Minute) / (24f * 60f);
    }

    public string GetFormattedDate(){
        return $"{CurrentWeekDay}, {CurrentMonth} {Day}, Year {Year}";
    }

    public string GetFormattedTime(){
        return $"{(Hour < 10 ? "0" : "")}{Hour}:{(Minute < 10 ? "0" : "")}{Minute}";
    }
}
