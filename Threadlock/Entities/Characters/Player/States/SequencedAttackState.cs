using Nez.Tweens;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.DebugTools;
using Threadlock.Components;
using Threadlock.Helpers;
using Nez.Sprites;

namespace Threadlock.Entities.Characters.Player.States
{
    public class SequencedAttackState : PlayerState
    {
        //consts
        const float _slowDuration = 1f;
        const float _speedUpDuration = .1f;
        const float _slowTimeScale = .25f;
        const float _normalTimeScale = 1f;

        ICoroutine _slowMoCoroutine;
        ICoroutine _normalSpeedCoroutine;

        List<PlayerAction2> _queuedActions = new List<PlayerAction2>();
        int _totalApCost { get => _queuedActions.Select(a => a.ApCost).Sum(); }

        SimPlayer _simPlayer;

        public override void Begin()
        {
            base.Begin();

            Game1.StartCoroutine(PrepSequence());
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);


        }

        IEnumerator PrepSequence()
        {
            yield return null;

            _slowMoCoroutine = Game1.StartCoroutine(SlowMoCoroutine());

            var actionManager = _context.GetComponent<ActionManager>();
            var apComponent = _context.GetComponent<ApComponent>();
            var cam = _context.Scene.Camera.GetComponent<CustomFollowCamera>();

            while (!Controls.Instance.Special.IsPressed)
            {
                //check if we're holding an action button
                if (actionManager.TryAction(false, out var actionSlot))
                {
                    //check if we'll be able to afford this action
                    if (actionSlot.Action.ApCost + _totalApCost <= apComponent.ActionPoints || DebugSettings.FreeActions)
                    {
                        var action = actionSlot.Action.Clone() as PlayerAction2;
                        Entity prepEntity = _simPlayer;
                        prepEntity ??= Player.Instance;
                        var prepActionCoroutine = Game1.StartCoroutine(action.Prepare(prepEntity));
                        while (actionSlot.Button.IsDown && !action.IsPrepared)
                            yield return null;

                        if (action.IsPrepared)
                        {
                            _queuedActions.Add(action);
                            var finalPos = action.GetFinalPosition();
                            if (finalPos != Player.Instance.Position && _simPlayer == null)
                            {
                                var animator = _context.GetComponent<SpriteAnimator>();
                                AnimatedSpriteHelper.PlayAnimation(ref animator, "Player_Idle");
                                _simPlayer = _context.Scene.AddEntity(new SimPlayer(SimPlayerType.Static, "Player_Idle", finalPos));
                                _simPlayer.SetPosition(finalPos);
                                cam.SetFollowTarget(_simPlayer);
                            }
                            else if (_simPlayer != null && finalPos != _simPlayer.Position)
                            {
                                _simPlayer.SetPosition(finalPos);
                            }
                        }
                        else
                        {
                            if (_simPlayer != null)
                            {
                                var animator = _simPlayer.GetComponent<SpriteAnimator>();
                                AnimatedSpriteHelper.PlayAnimation(ref animator, "Player_Idle");
                            }
                            else
                            {
                                var animator = _context.GetComponent<SpriteAnimator>();
                                AnimatedSpriteHelper.PlayAnimation(ref animator, "Player_Idle");
                            }
                            prepActionCoroutine?.Stop();
                            action.Reset();
                        }
                    }
                }

                yield return null;
            }

            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

            cam.SetFollowTarget(_context);

            _simPlayer?.Destroy();
            _simPlayer = null;

            foreach (var action in _queuedActions)
            {
                yield return action.Execute(Player.Instance);
            }

            _queuedActions.Clear();

            yield return null;

            _machine.ChangeState<Idle>();
        }

        IEnumerator SlowMoCoroutine()
        {
            _normalSpeedCoroutine?.Stop();
            _normalSpeedCoroutine = null;

            var initialTimeScale = Time.TimeScale;

            var time = 0f;
            while (time < _slowDuration)
            {
                time += Time.DeltaTime;

                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _slowTimeScale, time, _slowDuration);
                Time.TimeScale = currentTimeScale;

                yield return null;
            }

            _slowMoCoroutine = null;
        }

        IEnumerator NormalSpeedCoroutine()
        {
            _slowMoCoroutine?.Stop();
            _slowMoCoroutine = null;

            var initialTimeScale = Time.TimeScale;

            var time = 0f;
            while (time < _speedUpDuration)
            {
                time += Time.DeltaTime;

                var currentTimeScale = Lerps.Ease(EaseType.QuartOut, initialTimeScale, _normalTimeScale, time, _speedUpDuration);
                Time.TimeScale = currentTimeScale;

                yield return null;
            }

            _normalSpeedCoroutine = null;
        }
    }
}
