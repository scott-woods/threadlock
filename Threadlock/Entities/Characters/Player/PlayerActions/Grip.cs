//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Nez;
//using Nez.Tweens;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.Components;
//using Threadlock.SaveData;
//using Threadlock.StaticData;

//namespace Threadlock.Entities.Characters.Player.PlayerActions
//{
//    [PlayerActionInfo("Grip", 3, "Grip an  anti-homie.", "229")]
//    public class Grip : PlayerAction
//    {
//        const float _radius = 10f;
//        const float _pullDuration = .15f;
//        readonly string _gripSound = Nez.Content.Audio.Sounds.RetroAlarmed06;
//        readonly List<string> _launchSounds = new List<string>() { Nez.Content.Audio.Sounds._22_Slash_04, Nez.Content.Audio.Sounds._23_Slash_05 };

//        SelectionComponent _currentSelection;
//        Vector2 _direction { get => Player.Instance.GetFacingDirection(); }

//        public override IEnumerator PreparationCoroutine()
//        {
//            while (_currentSelection == null || !Controls.Instance.Melee.IsPressed)
//            {
//                Player.Instance.IdleInFacingDirection();

//                var mousePos = Entity.Scene.Camera.MouseToWorldPoint();
//                var collider = Physics.OverlapCircle(mousePos, .5f, 1 << PhysicsLayers.Selectable);
//                if (collider != null)
//                {
//                    if (collider.Entity.TryGetComponent<SelectionComponent>(out var selectionComponent))
//                    {
//                        if (selectionComponent != _currentSelection)
//                        {
//                            //unhighlight previous selection if there was one
//                            _currentSelection?.Unhighlight();

//                            _currentSelection = selectionComponent;
//                            _currentSelection.Highlight(Color.Yellow);
//                        }
//                    }
//                }
//                else
//                {
//                    if (_currentSelection != null)
//                    {
//                        _currentSelection.Unhighlight();
//                        _currentSelection = null;
//                    }
//                }

//                yield return null;
//            }
//        }

//        public override IEnumerator ExecutionCoroutine()
//        {
//            //put gripped entity in stunned state
//            if (_currentSelection.Entity.TryGetComponent<StatusComponent>(out var statusComponent))
//                statusComponent.PushStatus(StatusPriority.Stunned);

//            yield return PullObject(_currentSelection.Entity);

//            //player is aiming
//            while (!Controls.Instance.Melee.IsPressed)
//            {
//                Player.Instance.IdleInFacingDirection();

//                var targetPos = Entity.Position + (_direction * _radius);
//                _currentSelection.Entity.Position = targetPos;

//                yield return null;
//            }

//            //fire
//            _currentSelection.Unhighlight();
//            Game1.AudioManager.PlaySound(_launchSounds.RandomItem());
//            var gripAttach = _currentSelection.AddComponent(new GripAttach());
//            gripAttach.Launch(_direction);

//            //wait one frame before returning to normal
//            yield return null;
//        }

//        public override void Reset()
//        {
//            base.Reset();

//            _currentSelection?.Unhighlight();
//            _currentSelection = null;
//        }

//        IEnumerator PullObject(Entity entity)
//        {
//            Game1.AudioManager.PlaySound(_gripSound);

//            var timer = 0f;
//            Vector2 startPos = entity.Position;

//            while (timer < _pullDuration)
//            {
//                timer += Time.DeltaTime;

//                var targetPos = Entity.Position + (_direction * _radius);

//                var progress = timer / _pullDuration;

//                entity.Position = Vector2.Lerp(startPos, targetPos, progress);

//                yield return null;
//            }
//        }
//    }
//}
