using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class Gun : BasicWeapon
    {
        public override bool CanMove => true;

        GunEntity _gunEntity;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_draw);

            //create gun entity
            _gunEntity = Entity.Scene.AddEntity(new GunEntity());
            _gunEntity.SetParent(Entity);
            _gunEntity.OnProjectileHit += OnProjectileHit;
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _gunEntity?.Destroy();
            _gunEntity = null;
        }

        public override void OnUnequipped()
        {
            _gunEntity.SetEnabled(false);
        }

        protected override void StartAttack()
        {
            Game1.StartCoroutine(_gunEntity.Fire(CompletionCallback));
        }

        void OnProjectileHit(int damage)
        {
            Emitter.Emit(BasicWeaponEventTypes.Hit, damage);
        }
    }
}
