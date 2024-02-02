using Nez;
using Nez.AI.BehaviorTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using TaskStatus = Nez.AI.BehaviorTrees.TaskStatus;

namespace Threadlock.Components
{
    public abstract class EnemyAction<T> : Component where T : Enemy<T>
    {
        protected T _enemy;

        ICoroutine _startExecutionCoroutine;
        ICoroutine _executionCoroutine;

        public EnemyAction(T enemy)
        {
            _enemy = enemy;
        }

        public TaskStatus Execute()
        {
            //if execution hasn't started yet, start it here
            if (_startExecutionCoroutine == null)
            {
                _startExecutionCoroutine = Game1.StartCoroutine(StartExecution());
            }

            //if we've started executing and the execution coroutine is null, that means we've finished. return success
            if (_startExecutionCoroutine != null && _executionCoroutine == null)
            {
                _startExecutionCoroutine = null;
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        IEnumerator StartExecution()
        {
            _executionCoroutine = Game1.StartCoroutine(ExecutionCoroutine());
            yield return _executionCoroutine;

            _executionCoroutine = null;
        }

        public virtual void Abort()
        {
            _executionCoroutine?.Stop();
            _executionCoroutine = null;
            _startExecutionCoroutine?.Stop();
            _startExecutionCoroutine = null;
        }

        protected abstract IEnumerator ExecutionCoroutine();
    }
}
