using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.SaveData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class Gun : BasicWeapon
    {
        const float _cooldown = .4f;
        const float _reloadTime = 1f;

        public override bool CanMove => true;

        int _maxAmmo = 10;
        public int MaxAmmo
        {
            get => _maxAmmo;
            set
            {
                var newAmmo = Math.Max(0, value);
                if (_maxAmmo != newAmmo)
                    OnMaxAmmoChanged?.Invoke(newAmmo);
                _maxAmmo = newAmmo;
            }
        }
        int _ammo = 10;
        public int Ammo
        {
            get => _ammo;
            private set
            {
                var newAmmo = Math.Clamp(value, 0, MaxAmmo);
                if (newAmmo != _ammo)
                    OnAmmoCountChanged?.Invoke(newAmmo);
                _ammo = newAmmo;
            }
        }

        public event Action<int> OnAmmoCountChanged;
        public event Action<int> OnMaxAmmoChanged;
        public event Action<float> OnReloadStarted;

        GunEntity _gunEntity;

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_draw);

            _ammo = MaxAmmo;

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

        void StartAttack()
        {
            Game1.StartCoroutine(Fire());
        }

        IEnumerator Fire()
        {
            if (_ammo <= 0)
            {
                yield return Reload();
                yield break;
            }

            //fire gun
            yield return _gunEntity.Fire();

            //handle ammo
            Ammo -= 1;

            //start cooldown timer, checking for input in the last half
            var timer = 0f;
            bool continueAttack = false;
            while (timer < _cooldown)
            {
                if (timer >= (_cooldown / 2) && Controls.Instance.Melee.IsPressed)
                    continueAttack = true;

                timer += Time.DeltaTime;
                yield return null;
            }

            //fire again if necessary
            if (continueAttack)
                yield return Fire();
            else
                CompletionEmitter.Emit(BasicWeaponEventTypes.Completed);
        }

        IEnumerator Reload()
        {
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_draw);
            OnReloadStarted?.Invoke(_reloadTime);

            yield return Coroutine.WaitForSeconds(_reloadTime);

            Ammo = _maxAmmo;

            CompletionEmitter.Emit(BasicWeaponEventTypes.Completed);
        }

        void OnProjectileHit(int damage)
        {
            Emitter.Emit(BasicWeaponEventTypes.Hit, damage);
        }

        public override bool Poll()
        {
            if (Controls.Instance.Melee.IsPressed)
            {
                StartAttack();
                return true;
            }
            if (Controls.Instance.Reload.IsPressed)
            {
                Game1.StartCoroutine(Reload());
                return true;
            }

            return false;
        }
    }
}
