using Nez;
using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public abstract class PlayerAction : Component, IUpdatable
    {
        [JsonExclude]
        public PlayerActionState State = PlayerActionState.None;
        [JsonExclude]
        public Action PrepFinishedCallback;
        [JsonExclude]
        public Action ExecutionFinishedCallback;

        public virtual void Prepare(Action prepFinishedCallback)
        {
            PrepFinishedCallback = prepFinishedCallback;
            State = PlayerActionState.Preparing;
        }

        public virtual void Execute(Action executionFinishedCallback)
        {
            ExecutionFinishedCallback = executionFinishedCallback;
            State = PlayerActionState.Executing;
        }

        public virtual void Update()
        {

        }

        public virtual void HandlePrepFinished()
        {
            State = PlayerActionState.None;
            PrepFinishedCallback?.Invoke();
        }

        public virtual void HandleExecutionFinished()
        {
            State = PlayerActionState.None;
            ExecutionFinishedCallback?.Invoke();
        }

        public virtual void Abort()
        {
            State = PlayerActionState.None;
        }
    }

    public enum PlayerActionState
    {
        None,
        Preparing,
        Executing
    }
}
