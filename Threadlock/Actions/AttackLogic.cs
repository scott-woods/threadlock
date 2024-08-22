using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Actions
{
    public abstract class AttackLogic
    {
        public abstract IEnumerator Execute(AttackTargetingInfo targetingInfo);
    }

    public class AttackTargetingInfo
    {

    }
}
