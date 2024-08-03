//using Microsoft.Xna.Framework;
//using Nez;
//using Nez.Sprites;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.SaveData;

//namespace Threadlock.Entities.Characters.Player.BasicWeapons
//{
//    public class Gun : BasicWeapon
//    {
//        const float _cooldown = .33f;
//        const float _altAttackCooldown = .66f;
//        const float _reloadTime = 1.3f;

//        public override bool CanMove => true;

//        int _maxAmmo = 10;
//        public int MaxAmmo
//        {
//            get => _maxAmmo;
//            set
//            {
//                var newAmmo = Math.Max(0, value);
//                if (_maxAmmo != newAmmo)
//                    OnMaxAmmoChanged?.Invoke(newAmmo);
//                _maxAmmo = newAmmo;
//            }
//        }
//        int _ammo = 10;
//        public int Ammo
//        {
//            get => _ammo;
//            private set
//            {
//                var newAmmo = Math.Clamp(value, 0, MaxAmmo);
//                if (newAmmo != _ammo)
//                    OnAmmoCountChanged?.Invoke(newAmmo);
//                _ammo = newAmmo;
//            }
//        }

//        public event Action<int> OnAmmoCountChanged;
//        public event Action<int> OnMaxAmmoChanged;
//        public event Action<float> OnReloadStarted;

//        GunEntity _gunEntity;

//        #region LIFECYCLE

//        public override void OnAddedToEntity()
//        {
//            base.OnAddedToEntity();

//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_reload_1);

//            _ammo = MaxAmmo;

//            //create gun entity
//            _gunEntity = Entity.Scene.AddEntity(new GunEntity());
//            _gunEntity.SetParent(Entity);
//            _gunEntity.OnProjectileCreated += OnProjectileCreated;
//        }

//        public override void OnRemovedFromEntity()
//        {
//            base.OnRemovedFromEntity();

//            _gunEntity?.Destroy();
//            _gunEntity = null;
//        }

//        #endregion

//        #region BASIC WEAPON

//        public override bool Poll()
//        {
//            if (Controls.Instance.Melee.IsPressed)
//            {
//                QueuedAction = Fire;
//                return true;
//            }
//            else if (Ammo < MaxAmmo && Controls.Instance.Reload.IsPressed)
//            {
//                QueuedAction = Reload;
//                return true;
//            }
//            else if (Controls.Instance.AltAttack.IsPressed)
//            {
//                QueuedAction = SecondaryAttack;
//                return true;
//            }

//            return false;
//        }

//        public override void OnUnequipped()
//        {
//            _gunEntity.SetEnabled(false);
//        }

//        #endregion

//        IEnumerator SecondaryAttack()
//        {
//            if (_ammo <= 0)
//            {
//                yield return Reload();
//                yield break;
//            }

//            //fire shotgun shot
//            yield return _gunEntity.ShotgunBlast(Math.Min(_ammo, 5));

//            //handle ammo
//            Ammo -= Math.Min(_ammo, 5);

//            //start cooldown timer
//            var timer = 0f;
//            while (timer < _altAttackCooldown)
//            {
//                timer += Time.DeltaTime;
//                yield return null;
//            }
//        }

//        IEnumerator Fire()
//        {
//            if (_ammo <= 0)
//            {
//                yield return Reload();
//                yield break;
//            }

//            //fire gun
//            yield return _gunEntity.Fire();

//            //handle ammo
//            Ammo -= 1;

//            //start cooldown timer, checking for input in the last half
//            var timer = 0f;
//            bool continueAttack = false;
//            while (timer < _cooldown)
//            {
//                if (timer >= (_cooldown / 2) && Controls.Instance.Melee.IsPressed)
//                    continueAttack = true;

//                timer += Time.DeltaTime;
//                yield return null;
//            }

//            //fire again if necessary
//            if (continueAttack)
//                yield return Fire();
//            else
//                yield break;
//        }

//        IEnumerator Reload()
//        {
//            _gunEntity.SetEnabled(false);
//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_draw);
//            OnReloadStarted?.Invoke(_reloadTime);

//            yield return Coroutine.WaitForSeconds(_reloadTime);

//            _gunEntity.SetEnabled(true);

//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Ap_full);
//            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_reload_1);

//            yield return _gunEntity.ReloadSpin();

//            Ammo = _maxAmmo;
//        }

//        void OnProjectileCreated(Projectile projectile)
//        {
//            WatchHitbox(projectile.Hitbox);
//        }
//    }
//}
