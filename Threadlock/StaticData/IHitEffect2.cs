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
    public interface IHitEffect2
    {
        public void Apply(ProjectileEntity projectile, Collider hitCollider);
    }

    public class ChainHitEffect : IHitEffect2
    {
        public float Radius;
        public int MaxChains;
        public float Delay;
        public int BaseDamage;
        public int DamageIncrement;
        public List<string> HitVfx;

        List<Entity> _hitEntities = new List<Entity>();

        public void Apply(ProjectileEntity projectile, Collider hitCollider)
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
