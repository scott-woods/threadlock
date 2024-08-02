using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Helpers
{
    public class CoroutineHelper
    {
        public static IEnumerator CoroutineWrapper(IEnumerator enumerator, Action onCompleted)
        {
            yield return enumerator;
            onCompleted?.Invoke();
        }
    }
}
