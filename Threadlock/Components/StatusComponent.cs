using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Components
{
    public class StatusComponent : Component
    {
        Stack<StatusPriority> _stateStack = new Stack<StatusPriority>();

        public StatusPriority CurrentStatusPriority => _stateStack.Count > 0 ? _stateStack.Peek() : StatusPriority.Normal;

        public StatusComponent(StatusPriority priority)
        {
            _stateStack.Push(priority);
        }

        public bool PushStatus(StatusPriority priority)
        {
            if (_stateStack.Count == 0 || priority > CurrentStatusPriority)
            {
                _stateStack.Push(priority);
                return true;
            }

            return false;
        }

        public void PopStatus(StatusPriority priority)
        {
            if (CurrentStatusPriority == priority)
            {
                _stateStack.Pop();
            }
        }
    }

    public enum StatusPriority
    {
        Normal = 0,
        Stunned = 1
    }
}
