using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;

namespace Threadlock.Components.EnemyActions
{
    public abstract class EnemyAction : Component
    {
        public abstract float CooldownTime { get; }
        public abstract int Priority { get; }

        public bool IsOnCooldown = false;
        public bool IsActive = false;

        protected Enemy Enemy { get => Entity as Enemy; }

        ICoroutine _executionCoroutine;

        public IEnumerator BeginExecution()
        {
            IsActive = true;

            _executionCoroutine = Game1.StartCoroutine(ExecutionCoroutine());
            yield return _executionCoroutine;

            _executionCoroutine = null;
            IsActive = false;

            if (CooldownTime > 0)
            {
                IsOnCooldown = true;
                Game1.Schedule(CooldownTime, timer => IsOnCooldown = false);
            }

            Reset();
        }

        public abstract bool CanExecute();

        /// <summary>
        /// called when knocked out of action early
        /// </summary>
        public virtual void Abort()
        {
            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            IsActive = false;

            Reset();
        }

        /// <summary>
        /// called after execution successfully finishes, do any cleanup here
        /// </summary>
        protected abstract void Reset();

        protected abstract IEnumerator ExecutionCoroutine();
    }
}
