using UnityEngine;
using System.Collections.Generic;

namespace GDEUtills.StateMachine {
    public class State<T> : MonoBehaviour{
        public virtual void Enter(T owner){}
        public virtual void Execute(){}
        public virtual void Exit(){}
    }
}

