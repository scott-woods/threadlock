using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.DebugTools;
using Threadlock.Entities.Characters.Player;
using Threadlock.Models;

namespace Threadlock.Components
{
    /// <summary>
    /// Collider that detects when entity is hit by an enemy attack, and emits with info about the hit
    /// </summary>
    public class Hurtbox : Component, IUpdatable
    {
        public Emitter<HurtboxEventTypes, HurtboxHit> Emitter = new Emitter<HurtboxEventTypes, HurtboxHit>();

        //consts
        const float _attackLifespan = 2f;

        Collider _collider;
        public Collider Collider { get => _collider; }

        float _recoveryTime;
        string _damageSound;
        List<string> _recentAttackIds = new List<string>();
        ITimer _recoveryTimer;

        public Hurtbox(Collider collider, float recoveryTime)
        {
            _collider = collider;
            _recoveryTime = recoveryTime;
        }

        public Hurtbox(Collider collider, float recoveryTime, string damageSound)
        {
            _collider = collider;
            _recoveryTime = recoveryTime;
            _damageSound = damageSound;
        }

        #region LIFECYCLE

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (Entity.TryGetComponent<DeathComponent>(out var dc))
                dc.Emitter.AddObserver(DeathEventTypes.Started, OnDeathStarted);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (Entity.TryGetComponent<DeathComponent>(out var dc))
                dc.Emitter.RemoveObserver(DeathEventTypes.Started, OnDeathStarted);
        }

        public void Update()
        {
            if (Entity.GetType() == typeof(Player) && !DebugSettings.PlayerHurtboxEnabled)
                return;

            var hitboxes = Physics.BoxcastBroadphaseExcludingSelf(_collider, _collider.CollidesWithLayers).ToList();
            for (int i = 0; i < hitboxes.Count; i++)
            {
                var hitbox = hitboxes[i] as IHitbox;
                if (!_recentAttackIds.Contains(hitbox.AttackId))
                {
                    var id = hitbox.AttackId;
                    _recentAttackIds.Add(id);
                    Game1.Schedule(_attackLifespan, timer => _recentAttackIds.Remove(id));
                    HandleHit(hitbox);
                }
            }
        }

        #endregion

        void HandleHit(IHitbox hitbox)
        {
            //play sound
            if (!String.IsNullOrWhiteSpace(_damageSound))
            {
                Game1.AudioManager.PlaySound(_damageSound);
            }

            //get collision result
            var collider = hitbox as Collider;
            if (collider.CollidesWith(_collider, out CollisionResult collisionResult))
            {
                //get angle from normal
                var angle = (float)Math.Atan2(collisionResult.Normal.Y, collisionResult.Normal.X);

                //choose hit effect
                //var effects = new List<HitEffect>() { HitEffects.Hit1, HitEffects.Hit2, HitEffects.Hit3 };
                //var effect = effects.RandomItem();

                //effect color
                //Color color = Color.White;
                //if (Entity == PlayerController.Instance.Entity)
                //    color = Color.Red;

                //hit effect
                //var effectEntity = Entity.Scene.CreateEntity("hit-effect", collisionResult.Point);
                //effectEntity.SetRotation(angle);
                //var effectComponent = effectEntity.AddComponent(new HitEffectComponent(effect, color));

                SetEnabled(false);
                _recoveryTimer = Game1.Schedule(_recoveryTime, timer =>
                {
                    SetEnabled(true);
                });

                //emit hit signal
                Emitter.Emit(HurtboxEventTypes.Hit, new HurtboxHit(collisionResult, hitbox));
            }
        }

        void OnDeathStarted(Entity entity)
        {
            SetEnabled(false);

            _recoveryTimer?.Stop();
            _recoveryTimer = null;
        }
    }

    public enum HurtboxEventTypes
    {
        Hit
    }
}
