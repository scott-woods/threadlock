using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.StaticData
{
    /// <summary>
    /// Effect that is applied when a projectile entity hits something
    /// </summary>
    public abstract class HitEffect
    {
        public List<string> Layers = new List<string>();
        public bool RequiresDamage = false;

        public bool IsColliderValid(Collider collider)
        {
            if (RequiresDamage || Layers == null || Layers.Count == 0)
                return false;

            int mask = 0;
            foreach (var layer in Layers)
                Flags.SetFlag(ref mask, PhysicsLayers.GetLayerByName(layer));

            return Flags.IsFlagSet(mask, collider.PhysicsLayer);
        }

        public abstract void Apply(ProjectileEntity projectile, Collider hitCollider);
    }

    public class SoundHitEffect : HitEffect
    {
        public override void Apply(ProjectileEntity projectile, Collider hitCollider)
        {
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.FartWithReverb);
        }
    }

    public class DestroyHitEffect : HitEffect
    {
        public string NextProjectile;
        public List<string> Sounds = new List<string>();

        public override void Apply(ProjectileEntity projectile, Collider hitCollider)
        {
            if (Projectiles2.TryGetProjectile(NextProjectile, out var projectileConfig))
            {
                var nextProjectile = ProjectileEntity.CreateProjectileEntity(projectileConfig, Vector2.Zero);
                Game1.Scene.AddEntity(nextProjectile);
                nextProjectile.SetPosition(projectile.Position);
            }

            if (Sounds.Count > 0)
            {
                var sound = Sounds.RandomItem();
                Game1.AudioManager.PlaySound(sound);
            }

            projectile.End();
        }
    }

    public class ChainHitEffect : HitEffect
    {
        public float Radius;
        public int MaxChains;
        public float Delay;
        public int BaseDamage;
        public int DamageIncrement;
        public List<string> HitVfx;

        List<Entity> _hitEntities = new List<Entity>();

        public override void Apply(ProjectileEntity projectile, Collider hitCollider)
        {
            if (_hitEntities.Count > 0)
                return;

            _hitEntities.Add(hitCollider.Entity);

            Game1.StartCoroutine(ChainCoroutine());
        }

        IEnumerator ChainCoroutine()
        {
            yield return Coroutine.WaitForSeconds(Delay);

            var chain = 0;

            while (TryChain(chain))
            {
                chain++;

                if (Delay > 0)
                    yield return Coroutine.WaitForSeconds(Delay);
            }

            _hitEntities.Clear();
        }

        bool TryChain(int chain)
        {
            if (chain >= MaxChains)
                return false;

            var allEnemies = Game1.Scene.EntitiesOfType<Enemy>();
            var enemiesToHit = new List<Entity>();

            foreach (var enemy in allEnemies)
            {
                if (_hitEntities.Contains(enemy))
                    continue;

                if (_hitEntities.Any(hitEntity => EntityHelper.DistanceToEntity(enemy, hitEntity) <= Radius))
                    enemiesToHit.Add(enemy);
            }

            foreach (var enemy in enemiesToHit)
            {
                if (enemy.TryGetComponent<Hurtbox>(out var hurtbox))
                    hurtbox.ManualHit(BaseDamage + (chain * DamageIncrement));

                if (HitVfx.Count > 0)
                {
                    var hitVfx = HitVfx.RandomItem();
                    var hitVfxEntity = Game1.Scene.AddEntity(new HitVfx(hitVfx));
                    hitVfxEntity.SetPosition(enemy.Position);
                }

                _hitEntities.Add(enemy);
            }

            return enemiesToHit.Any();
        }
    }
}
