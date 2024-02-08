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

        bool _executionStarted = false;
        bool _executionFinished = false;

        public EnemyAction(T enemy)
        {
            _enemy = enemy;
        }

        public TaskStatus Execute()
        {
            //if execution hasn't started yet, start it here
            if (!_executionStarted)
            {
                _startExecutionCoroutine = Game1.StartCoroutine(StartExecution());
                return TaskStatus.Running;
            }
            else if (!_executionFinished)
            {
                return TaskStatus.Running;
            }
            else
            {
                _executionStarted = false;
                _executionFinished = false;
                return TaskStatus.Success;
            }

            //if we've started executing and the execution coroutine is null, that means we've finished. return success
            //if (_startExecutionCoroutine != null && _executionCoroutine == null)
            //{
            //    _startExecutionCoroutine = null;
            //    return TaskStatus.Success;
            //}

            //return TaskStatus.Running;
        }

        IEnumerator StartExecution()
        {
            //update states
            _executionStarted = true;
            _executionFinished = false;

            //wait for execution
            _executionCoroutine = Game1.StartCoroutine(ExecutionCoroutine());
            yield return _executionCoroutine;
            _executionFinished = true;

            _startExecutionCoroutine = null;
            _executionCoroutine = null;

            Reset();
        }

        /// <summary>
        /// called when knocked out of action early
        /// </summary>
        public virtual void Abort()
        {
            _executionStarted = false;
            _executionFinished = false;

            _executionCoroutine?.Stop();
            _executionCoroutine = null;
            _startExecutionCoroutine?.Stop();
            _startExecutionCoroutine = null;

            Reset();
        }

        /// <summary>
        /// called after execution successfully finishes, do any cleanup here
        /// </summary>
        protected abstract void Reset();

        protected abstract IEnumerator ExecutionCoroutine();
    }
}
