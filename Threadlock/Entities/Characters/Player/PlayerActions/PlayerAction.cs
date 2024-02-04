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
        public PlayerActionState State = PlayerActionState.None;
        public event Action OnPreparationFinished;
        public event Action OnExecutionFinished;

        public virtual void Prepare()
        {
            State = PlayerActionState.Preparing;
        }

        public virtual void Execute()
        {
            State = PlayerActionState.Executing;
        }

        public virtual void Update()
        {

        }

        public virtual void Abort()
        {
            State = PlayerActionState.None;
        }

        public virtual void HandlePreparationFinished()
        {
            State = PlayerActionState.None;
            OnPreparationFinished?.Invoke();
        }

        public virtual void HandleExecutionFinished()
        {
            State = PlayerActionState.None;
            OnExecutionFinished?.Invoke();
        }
    }

    public enum PlayerActionState
    {
        None,
        Preparing,
        Executing
    }
}
