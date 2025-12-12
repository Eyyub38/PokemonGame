using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace GDEUtills.StateMachine{
    public class StateMachine<T>{
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

        public IEnumerator PushAndWait(State <T> newState){
            var oldState = CurrentState;
            Push(newState);

            yield return new WaitUntil(() => CurrentState == oldState);
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
            if (StateStack.Count == 0){
                Debug.LogError("Cannot pop from an empty state stack.");
                return;
            }
            
            State<T> poppedState = StateStack.Pop();
            poppedState.Exit();
            
            if (StateStack.Count > 0){
                CurrentState = StateStack.Peek();
            } else {
                CurrentState = null;
            }
        }

        public State<T> GetPrevState(){
            return StateStack.ElementAtOrDefault(1);
        }
    }
}