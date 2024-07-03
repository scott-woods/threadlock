using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Systems;
using Nez.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.DebugTools;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Player;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    /// <summary>
    /// Collider that detects when entity is hit by an enemy attack, and emits with info about the hit
    /// </summary>
    public class Hurtbox : Component, ITriggerListener, IUpdatable
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
            _collider.IsTrigger = true;
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

        public override void OnEnabled()
        {
            base.OnEnabled();

            _collider?.SetEnabled(true);
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            _collider?.SetEnabled(false);
        }

        public void Update()
        {
            var colliders = Physics.BoxcastBroadphaseExcludingSelf(_collider, _collider.CollidesWithLayers);
            foreach (var collider in colliders)
            {
                OnTriggerEnter(collider, _collider);
            }
        }

        #endregion

        public void ManualHit(int damage)
        {
            var hurtboxHit = new HurtboxHit(damage);

            HandleHit(hurtboxHit);
        }

        public void HandleHit(HurtboxHit hurtboxHit)
        {
            //play sound
            if (!String.IsNullOrWhiteSpace(_damageSound))
                Game1.AudioManager.PlaySound(_damageSound);

            //start recovery timer if necessary
            if (_recoveryTime > 0)
            {
                SetEnabled(false);
                _recoveryTimer = Game1.Schedule(_recoveryTime, timer =>
                {
                    SetEnabled(true);
                });
            }

            //emit hit signal
            Emitter.Emit(HurtboxEventTypes.Hit, hurtboxHit);
        }

        void OnDeathStarted(Entity entity)
        {
            SetEnabled(false);

            _recoveryTimer?.Stop();
            _recoveryTimer = null;
        }

        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (local != _collider)
                return;

            if (!other.Enabled || !local.Enabled)
                return;

            if (!Enabled)
                return;

            if (Entity.GetType() == typeof(Player) && !DebugSettings.PlayerHurtboxEnabled)
                return;

            //convert to hitbox
            var hitbox = other as IHitbox;
            if (hitbox == null)
                return;

            //make sure we haven't already been hit by this attack
            if (!_recentAttackIds.Contains(hitbox.AttackId))
            {
                //add this attack to recent attacks
                var id = hitbox.AttackId;
                _recentAttackIds.Add(id);
                Game1.Schedule(_attackLifespan, timer => _recentAttackIds.Remove(id));

                //get collision result
                if (other.CollidesWith(_collider, out CollisionResult collisionResult))
                {
                    //tell the projectile entity that it successully hit something
                    if (other.Entity is ProjectileEntity projectileEntity)
                        projectileEntity.OnHit(_collider, collisionResult);
                    else
                        hitbox.Hit(Entity, hitbox.Damage);
                }

                var hurtboxHit = new HurtboxHit(collisionResult, hitbox);

                HandleHit(hurtboxHit);
            }
        }

        public void OnTriggerExit(Collider other, Collider local)
        {

        }
    }

    public enum HurtboxEventTypes
    {
        Hit
    }
}
