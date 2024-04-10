using Microsoft.Xna.Framework;
using Nez;
using Nez.PhysicsShapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.Hitboxes;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public class GripAttach : Component, IUpdatable
    {
        //constants
        const float _speed = 500f;
        const int _damage = 3;

        //entities & components
        Entity _projectileEntity;
        ProjectileMover _mover;
        IHitbox _hitbox;

        bool _hasLaunched = false;
        Vector2 _direction;
        int _previousPhysicsLayer;
        int _previousCollidesWithLayers;

        public override void Initialize()
        {
            base.Initialize();

            _projectileEntity = new Entity();
            _mover = new ProjectileMover();
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            Entity.Scene.AddEntity(_projectileEntity);
            _projectileEntity.SetPosition(Entity.Position);
            Entity.Position = Vector2.Zero;
            Entity.SetParent(_projectileEntity);
            _projectileEntity.AddComponent(_mover);
        }

        public void Launch(Vector2 direction)
        {
            _direction = direction;

            var collider = Entity.GetComponents<Collider>().FirstOrDefault(c => Flags.IsFlagSet(c.CollidesWithLayers, 1 << PhysicsLayers.Environment));
            if (collider != null)
            {
                if (collider is BoxCollider boxCollider)
                    _hitbox = new BoxHitbox(_damage, new Rectangle(0, 0, (int)boxCollider.Bounds.Width, (int)boxCollider.Bounds.Height));
                if (collider is CircleCollider circleCollider)
                    _hitbox = new CircleHitbox(_damage, circleCollider.Radius);
                if (collider is PolygonCollider polyCollider)
                {
                    var polyHitbox = new PolygonHitbox(_damage);
                    polyHitbox.Shape = polyCollider.Shape.Clone();
                    _hitbox = polyHitbox;
                }

                if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                {
                    _previousPhysicsLayer = hurtbox.Collider.PhysicsLayer;
                    _previousCollidesWithLayers = hurtbox.Collider.CollidesWithLayers;
                    hurtbox.Collider.PhysicsLayer = 0;
                    hurtbox.Collider.CollidesWithLayers = 0;
                }

                //turn the entity into a player projectile
                var hitboxCollider = _hitbox as Collider;
                hitboxCollider.SetLocalOffset(hurtbox.Collider.LocalOffset);
                Flags.SetFlagExclusive(ref hitboxCollider.PhysicsLayer, PhysicsLayers.PlayerHitbox);
                hitboxCollider.CollidesWithLayers = 0;
                Flags.SetFlag(ref hitboxCollider.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
                Flags.SetFlag(ref hitboxCollider.CollidesWithLayers, PhysicsLayers.Environment);
                //Flags.SetFlag(ref hitboxCollider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);
                _projectileEntity.AddComponent(hitboxCollider);
            }

            //if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
            //{
            //    //copy entity's hurtbox to create a hitbox out of it
            //    if (hurtbox.Collider.GetType() == typeof(BoxCollider))
            //        _hitbox = new BoxHitbox(_damage, new Rectangle(0, 0, (int)hurtbox.Collider.Bounds.Width, (int)hurtbox.Collider.Bounds.Height));
            //    if (hurtbox.Collider.GetType() == typeof(CircleCollider))
            //        _hitbox = new CircleHitbox(_damage, hurtbox.Collider.Bounds.Width);
            //    if (hurtbox.Collider is PolygonCollider polyCollider)
            //    {
            //        var polyHitbox = new PolygonHitbox(_damage);
            //        polyHitbox.Shape = polyCollider.Shape.Clone();
            //        _hitbox = polyHitbox;
            //    }

            //    //unset flag here so entity doesn't hit itself
            //    _previousPhysicsLayer = hurtbox.Collider.PhysicsLayer;
            //    _previousCollidesWithLayers = hurtbox.Collider.CollidesWithLayers;
            //    hurtbox.Collider.PhysicsLayer = 0;
            //    hurtbox.Collider.CollidesWithLayers = 0;

            //    //turn the entity into a player projectile
            //    var collider = _hitbox as Collider;
            //    collider.SetLocalOffset(hurtbox.Collider.LocalOffset);
            //    Flags.SetFlagExclusive(ref collider.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            //    collider.CollidesWithLayers = 0;
            //    Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            //    Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.Environment);
            //    Flags.SetFlag(ref collider.CollidesWithLayers, PhysicsLayers.ProjectilePassableWall);
            //    _projectileEntity.AddComponent(collider);
            //}

            _hasLaunched = true;
        }

        public void Update()
        {
            if (_hasLaunched)
            {
                if (_mover.Move(_direction * _speed * Time.DeltaTime))
                {
                    Entity.Parent = null;
                    Entity.SetPosition(_projectileEntity.Position);

                    var hitboxCollider = _hitbox as Collider;

                    var killColliders = Physics.BoxcastBroadphaseExcludingSelf(hitboxCollider, 1 << PhysicsLayers.ProjectilePassableWall);
                    if (killColliders.Any())
                    {
                        if (Entity.TryGetComponent<HealthComponent>(out var hc))
                        {
                            hc.Health = 0;
                            SetEnabled(false);
                            return;
                        }
                    }

                    if (Entity.TryGetComponent<StatusComponent>(out var statusComponent))
                        statusComponent.PopStatus(StatusPriority.Stunned);
                    if (Entity.TryGetComponent<Hurtbox>(out var hurtbox))
                    {
                        hurtbox.Collider.PhysicsLayer = _previousPhysicsLayer;
                        hurtbox.Collider.CollidesWithLayers = _previousCollidesWithLayers;

                        hurtbox.HandleHit(_hitbox);
                    }
                    
                    hitboxCollider.SetEnabled(false);
                    Entity.RemoveComponent(this);
                }
            }
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            if (_hitbox != null)
            {
                var collider = _hitbox as Collider;
                collider?.SetEnabled(false);
                Entity.RemoveComponent(collider);
                _hitbox = null;
            }

            _projectileEntity?.Destroy();
            _projectileEntity = null;
        }
    }
}
