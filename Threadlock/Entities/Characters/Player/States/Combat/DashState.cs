using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.States.Combat
{
    public class DashState : PlayerState
    {
        Dash _dash;
        ICoroutine _dashCoroutine, _localCoroutine;

        public override void OnInitialized()
        {
            base.OnInitialized();

            _dash = _context.GetComponent<Dash>();
        }

        public override void Begin()
        {
            base.Begin();

            _localCoroutine = Core.StartCoroutine(StartDashCoroutine());
        }

        public override void End()
        {
            base.End();

            _localCoroutine?.Stop();
            _localCoroutine = null;
            _dashCoroutine?.Stop();
            _dashCoroutine = null;

            _dash.Abort();
        }

        IEnumerator StartDashCoroutine()
        {
            _dashCoroutine = Core.StartCoroutine(_dash.StartDash());
            yield return _dashCoroutine;
            _dashCoroutine = null;
            _localCoroutine = null;

            if (TryMove())
                yield break;
            if (TryIdle())
                yield break;
        }
    }
}
