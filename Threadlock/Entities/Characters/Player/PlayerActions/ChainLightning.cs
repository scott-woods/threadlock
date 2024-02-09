using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    [PlayerActionInfo("Chain Lightning", 3, "Strike in any direction, chaining to nearby enemies.", "063")]
    public class ChainLightning : PlayerAction
    {
        //consts
        const int _damage = 3;
        const int _hitboxRadius = 12;
        const int _hitboxDistFromPlayer = 12;
        const int _hitboxActiveFrame = 0;

        //components
        SpriteAnimator _animator;
        CircleHitbox _hitbox;

        AnimationWaiter _animationWaiter;

        //misc
        Vector2 _direction;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            _hitbox = Entity.AddComponent(new CircleHitbox(_damage, _hitboxRadius));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, (int)PhysicsLayers.PlayerHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, (int)PhysicsLayers.EnemyHurtbox);
            _hitbox.SetEnabled(false);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                _animator = animator;
                _animationWaiter = new AnimationWaiter(_animator);
            }
        }

        #endregion

        #region PLAYER ACTION

        public override IEnumerator PreparationCoroutine()
        {
            Aim();

            while (!Controls.Instance.Confirm.IsPressed)
            {
                Aim();
                yield return null;
            }
        }

        public override IEnumerator ExecutionCoroutine()
        {
            //get animation by angle
            var angle = Math.Atan2(_direction.Y, _direction.X) * Mathf.Rad2Deg;
            //var angle = MathHelper.ToDegrees(Mathf.AngleBetweenVectors(Entity.Position, Entity.Position + _target.LocalOffset));
            angle = (angle + 360) % 360;
            var animation = "Slash";
            if (angle >= 45 && angle < 135) animation = "SlashDown";
            else if (angle >= 225 && angle < 315) animation = "SlashUp";

            //play animation
            Game1.StartCoroutine(_animationWaiter.WaitForAnimation(animation));

            //while animation is running
            List<Entity> hitEntities = new List<Entity>();
            bool hasPlayedSound = false;
            while (_animator.IsAnimationActive(animation) && _animator.AnimationState == SpriteAnimator.State.Running)
            {
                if (_animator.CurrentFrame == _hitboxActiveFrame)
                {
                    //play sound when first getting to hitbox active frame
                    if (!hasPlayedSound)
                    {
                        Game1.AudioManager.PlaySound(Content.Audio.Sounds.Big_lightning);
                        Game1.AudioManager.PlaySound(Content.Audio.Sounds._31_swoosh_sword_1);
                        hasPlayedSound = true;
                    }

                    //enable hitbox
                    _hitbox.SetEnabled(true);

                    //track which enemies are hit with this attack
                    var colliders = Physics.BoxcastBroadphaseExcludingSelf(_hitbox, _hitbox.CollidesWithLayers);
                    foreach (var collider in colliders)
                    {
                        if (!hitEntities.Contains(collider.Entity))
                            hitEntities.Add(collider.Entity);
                    }
                }
                else
                    _hitbox.SetEnabled(false);

                yield return null;
            }

            //start chain
            foreach (var ent in hitEntities)
            {
                if (ent.TryGetComponent<StatusComponent>(out var sc))
                {
                    if (sc.CurrentStatusPriority == StatusPriority.Death)
                        continue;
                }

                var attach = Entity.Scene.AddEntity(new ChainLightningAttach(0, ent, ref hitEntities));
            }
        }

        public override void Reset()
        {
            base.Reset();

            _hitbox.SetEnabled(false);

            _animationWaiter?.Cancel();
        }

        #endregion

        void Aim()
        {
            _direction = Player.Instance.GetFacingDirection();

            _hitbox.SetLocalOffset(_direction * _hitboxDistFromPlayer);
        }
    }
}
