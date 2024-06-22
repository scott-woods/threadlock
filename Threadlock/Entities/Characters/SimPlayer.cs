using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
using Threadlock.Entities.Characters.Player.PlayerActions;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters
{
    public class SimPlayer : Entity
    {
        //components
        SpriteAnimator _animator;
        VelocityComponent _velocityComponent;
        Mover _mover;
        SpriteFlipper _flipper;
        OriginComponent _originComponent;
        Collider _collider;

        SimPlayerType _simType;
        string _animation;
        Vector2 _targetPosition;

        public SimPlayer(SimPlayerType type, string animation, Vector2 targetPosition)
        {
            _simType = type;
            _animation = animation;
            _targetPosition = targetPosition;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            //create animator, slightly transparent
            //_animator = AddComponent(new SpriteAnimator());

            //get animations from player animator
            if (Player.Player.Instance.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = AddComponent(animator.Clone() as SpriteAnimator);
            }

            _animator.SetColor(new Microsoft.Xna.Framework.Color(255, 255, 255, 128));
            _animator.SetRenderLayer(RenderLayers.YSort);

            _mover = AddComponent(new Mover());

            _velocityComponent = AddComponent(new VelocityComponent());

            _flipper = AddComponent(new SpriteFlipper());

            //collider
            _collider = AddComponent(new BoxCollider(-4, 4, 8, 5));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.None);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.None);

            _originComponent = AddComponent(new OriginComponent(_collider));
        }

        public override void Update()
        {
            base.Update();

            switch (_simType)
            {
                case SimPlayerType.AttachToCursor:
                    Position = _targetPosition;
                    AnimatedSpriteHelper.PlayAnimation(ref _animator, _animation);
                    break;
            }
            //var dir = DirectionHelper.GetDirectionStringByVector(_velocityComponent.Direction);
            //var animName = $"Idle{dir}";
            //if (!_animator.Animations.ContainsKey(animName))
            //    animName = "IdleDown";
            //if (!_animator.IsAnimationActive(animName))
            //    _animator.Play(animName);
        }

        public void UpdateTarget(Vector2 targetPosition)
        {
            _targetPosition = targetPosition;
        }
    }
}
