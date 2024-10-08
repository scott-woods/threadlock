﻿using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Actions;
using Threadlock.Components;
using Threadlock.Entities.Characters.Player;
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
        OriginComponent _originComponent;
        Collider _collider;
        DirectionComponent _directionComponent;

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
            _animator = AddComponent(new SpriteAnimator());
            _animator.SetColor(new Microsoft.Xna.Framework.Color(255, 255, 255, 128));
            _animator.SetRenderLayer(RenderLayers.YSort);

            //get animations from player animator
            if (Scene.FindEntity("Player") is Player.Player player && player.TryGetComponent<SpriteAnimator>(out var animator))
            {
                foreach (var anim in animator.Animations)
                    _animator.AddAnimation(anim.Key, anim.Value);

                _animator.SetLocalOffset(animator.LocalOffset);
            }

            _mover = AddComponent(new Mover());

            _velocityComponent = AddComponent(new VelocityComponent());

            //collider
            _collider = AddComponent(new BoxCollider(-4, 4, 8, 5));
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.None);
            _collider.CollidesWithLayers = 0;
            Flags.SetFlag(ref _collider.CollidesWithLayers, PhysicsLayers.None);

            _originComponent = AddComponent(new OriginComponent(_collider));

            _directionComponent = AddComponent(new DirectionComponent());

            AddComponent(new AnimationComponent());

            //AnimatedSpriteHelper.PlayAnimation(_animator, _animation);
        }

        public override void Update()
        {
            base.Update();

            switch (_simType)
            {
                case SimPlayerType.AttachToCursor:
                    Position = _targetPosition;
                    AnimatedSpriteHelper.PlayAnimation(_animator, _animation);
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
