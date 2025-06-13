using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GDEUtills.StateMachine {
    public class StateMachine<T> {
        T owner;

        public State<T> CurrentState { get; private set;}
        public Stack<State<T>> StateStack { get; private set;}

        public StateMachine( T owner){
            this.owner = owner;
            StateStack = new Stack<State<T>>();
        }

        public void Push(State <T> newState){
            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public void Execute(){
            CurrentState?.Execute();
        }

        public void ChangeState(State<T> newState){
            if(CurrentState != null){
                StateStack.Pop();
                CurrentState.Exit();
            }
            
            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public void Pop(){
            StateStack.Pop();
            CurrentState.Exit();
            CurrentState = StateStack.Peek();
        }

        public State<T> GetPrevState(){
            return StateStack.ElementAt(1);
        }
    }
}