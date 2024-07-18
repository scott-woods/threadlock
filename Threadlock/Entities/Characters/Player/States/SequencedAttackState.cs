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
using Threadlock.StaticData;

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
                        //get a unique instance of the action
                        var action = AllPlayerActions.CreatePlayerAction(actionSlot.Action.Name, _context);

                        //determine the entity we'll base from. use sim player if it exists, real player otherwise
                        Entity prepEntity = _simPlayer;
                        prepEntity ??= Player.Instance;

                        //start preparing action
                        var request = action.Prepare(prepEntity);
                        var prepActionCoroutine = Game1.StartCoroutine(request);

                        //yield while button is held and we haven't finished preparing
                        while (actionSlot.Button.IsDown && !action.IsPrepared)
                            yield return null;

                        //if successfully prepared
                        if (action.IsPrepared)
                        {
                            //add to queued actions
                            _queuedActions.Add(action);

                            //get final position player would be after performing this action
                            var finalPos = action.GetFinalPosition();

                            //if final pos is different from current and sim player doesn't exist yet, create one
                            if (finalPos != Player.Instance.Position && _simPlayer == null)
                            {
                                //have the player idle while preparing the rest of the actions
                                var animator = _context.GetComponent<SpriteAnimator>();
                                AnimatedSpriteHelper.PlayAnimation(ref animator, "Player_Idle");

                                //create sim player entity at new final pos
                                _simPlayer = _context.Scene.AddEntity(new SimPlayer(SimPlayerType.Static, "Player_Idle", finalPos));
                                _simPlayer.SetPosition(finalPos);

                                //update camera to follow the sim player
                                cam.SetFollowTarget(_simPlayer);
                            }
                            else if (_simPlayer != null && finalPos != _simPlayer.Position)
                            {
                                //update sim player position if final pos has changed
                                _simPlayer.SetPosition(finalPos);
                            }
                        }
                        else //did not successfully prepare
                        {
                            //return either the sim player or normal player to idle
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

                            //stop the action prep coroutine and null out the cloned action
                            prepActionCoroutine?.Stop();
                            action.Abort();
                            action = null;
                        }
                    }
                }

                yield return null;
            }

            //start returning to normal speed
            _normalSpeedCoroutine = Game1.StartCoroutine(NormalSpeedCoroutine());

            //return cam to follow player
            cam.SetFollowTarget(_context);

            //destroy the sim player
            _simPlayer?.Destroy();
            _simPlayer = null;

            //execute each prepared action
            foreach (var action in _queuedActions)
            {
                yield return action.Execute();
            }

            //clear queued actions list
            _queuedActions.Clear();

            //wait one frame
            yield return null;

            //return to idle state
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
