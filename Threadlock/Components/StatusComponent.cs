using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class StatusComponent : Component
    {
        public Emitter<StatusEvents, StatusPriority> Emitter = new Emitter<StatusEvents, StatusPriority>();
        Stack<StatusPriority> _stateStack = new Stack<StatusPriority>();

        public StatusPriority CurrentStatusPriority => _stateStack.Count > 0 ? _stateStack.Peek() : StatusPriority.Normal;

        public StatusComponent(StatusPriority priority)
        {
            _stateStack.Push(priority);
        }

        /// <summary>
        /// try to push a new status. returns true if successful, or false if a more important status is already in place
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool PushStatus(StatusPriority priority)
        {
            if (_stateStack.Count == 0 || priority > CurrentStatusPriority)
            {
                _stateStack.Push(priority);
                Emitter.Emit(StatusEvents.Changed, CurrentStatusPriority);
                return true;
            }
            else if (priority == CurrentStatusPriority)
                return true;

            return false;
        }

        public void PopStatus(StatusPriority priority)
        {
            if (CurrentStatusPriority == priority)
            {
                _stateStack.Pop();
                Emitter.Emit(StatusEvents.Changed, CurrentStatusPriority);
            }
        }

        /// <summary>
        /// should only be called for situations like the player respawning
        /// </summary>
        public void Reset()
        {
            _stateStack.Clear();
            _stateStack.Push(StatusPriority.Normal);
            Emitter.Emit(StatusEvents.Changed, CurrentStatusPriority);
        }
    }

    public enum StatusPriority
    {
        Normal = 0,
        Stunned = 1,
        Death = 2
    }

    public enum StatusEvents
    {
        Changed
    }
}
