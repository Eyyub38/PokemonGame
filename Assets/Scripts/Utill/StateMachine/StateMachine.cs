using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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
            if(newState == null){
                Debug.LogError("Attempted to push null state to state machine!");
                return;
            }
            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public IEnumerator PushAndWait(State <T> newState){
            if(newState == null){
                Debug.LogError("Attempted to push null state to state machine!");
                yield break;
            }
            var oldState = CurrentState;
            Push(newState);
            yield return new WaitUntil(() => CurrentState == oldState);
        }

        public void Execute(){
            if(CurrentState != null){
                CurrentState.Execute();
            }
        }

        public void ChangeState(State<T> newState){
            if(newState == null){
                Debug.LogError("Attempted to change to null state!");
                return;
            }
            
            if(CurrentState != null){
                StateStack.Pop();
                CurrentState.Exit();
            }
            
            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public void Pop(){
            if(StateStack.Count <= 1){
                Debug.LogWarning("Cannot pop state: Stack has only one or zero states remaining.");
                return;
            }
            
            StateStack.Pop();
            if(CurrentState != null){
            CurrentState.Exit();
            }
            CurrentState = StateStack.Peek();
        }

        public State<T> GetPrevState(){
            if(StateStack.Count < 2){
                return null;
            }
            return StateStack.ElementAt(1);
        }
    }
}