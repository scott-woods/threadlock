using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;
using Random = Nez.Random;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class GunEntity : Entity
    {
        const float _horizontalRadius = 10f;
        const float _verticalRadius = 8f;
        const float _shotgunSpread = 18f;
        const float _projectileOffset = 5f;
        const float _shotgunPosVariance = 3f;
        const float _shotgunLifetime = .2f;
        const float _bulletFadeTime = .04f;
        readonly Vector2 _staticOffset = new Vector2(0, 2);

        public event Action<Projectile> OnProjectileCreated;

        SpriteRenderer _renderer;
        OriginComponent _originComponent;

        Vector2 _direction = Vector2.One;
        Vector2 _offset = Vector2.Zero;
        bool _isReloading = false;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            if (_renderer == null)
            {
                var texture = Game1.Content.LoadTexture(Nez.Content.Textures.Characters.Player.Gun1);
                _renderer = AddComponent(new SpriteRenderer(texture));
                _renderer.SetRenderLayer(RenderLayers.YSort);
            }
            else
            {
                _renderer.SetEnabled(true);
            }

            if (Player.Instance.TryGetComponent<OriginComponent>(out var oc))
                _offset = oc.Origin - Player.Instance.Position;

            _originComponent ??= AddComponent(new OriginComponent(_offset));

            HandleDirection();
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            _renderer.SetEnabled(false);
        }

        public override void Update()
        {
            base.Update();

            HandleDirection();
        }

        public IEnumerator Fire()
        {
            var projectile = Scene.AddEntity(new Projectile(_direction, Projectiles.PlayerGunProjectile));
            projectile.SetPosition(Position + (_projectileOffset * _direction));
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_fire);
            OnProjectileCreated?.Invoke(projectile);

            yield break;
        }

        public IEnumerator ShotgunBlast(int bulletCount)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                var angleOffset = Random.Range(-_shotgunSpread, _shotgunSpread);
                var angleOffsetRadians = MathHelper.ToRadians(angleOffset);
                var sin = (float)Math.Sin(angleOffsetRadians);
                var cos = (float)Math.Cos(angleOffsetRadians);
                var bulletDir = new Vector2(cos * _direction.X - sin * _direction.Y, sin * _direction.X + cos * _direction.Y);
                var pos = Position + (_direction * _projectileOffset);
                pos.X += Random.Range(-_shotgunPosVariance, _shotgunPosVariance);
                pos.Y += Random.Range(-_shotgunPosVariance, _shotgunPosVariance);
                var projectile = Scene.AddEntity(new Projectile(bulletDir, Projectiles.PlayerShotgunProjectile));
                projectile.SetPosition(pos);
                OnProjectileCreated?.Invoke(projectile);

                Game1.Schedule(_shotgunLifetime, timer =>
                {
                    Game1.StartCoroutine(projectile.Fade(_bulletFadeTime));
                });
            }

            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._8d82b5_doom_shotgun_firing_sound_effect);
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.FartWithReverb);

            yield break;
        }

        public IEnumerator ReloadSpin()
        {
            var spinDuration = .15f;
            var timer = 0f;
            _isReloading = true;
            while (timer < spinDuration)
            {
                var progress = timer / spinDuration;
                var rotation = progress * 360;

                //get direction
                _direction = Player.Instance.GetFacingDirection();
                var radians = (float)Math.Atan2(_direction.Y, _direction.X);

                RotationDegrees = MathHelper.ToDegrees(radians) - rotation;
                timer += Time.DeltaTime;
                yield return null;
            }

            _isReloading = false;
        }

        void HandleDirection()
        {
            //get direction
            _direction = Player.Instance.GetFacingDirection();
            var radians = (float)Math.Atan2(_direction.Y, _direction.X);

            //rotate around player
            //_renderer.SetLocalOffset(_direction * _radius);
            var offsetX = _horizontalRadius * (float)Math.Cos(radians);
            var offsetY = _verticalRadius * (float)Math.Sin(radians);
            _renderer.SetLocalOffset(new Vector2(offsetX, offsetY) + _staticOffset);

            //rotate gun sprite
            if (!_isReloading)
                Rotation = radians;

            //flip sprite
            _renderer.FlipY = _direction.X < 0;

            //update origin so we y sort with the player
            if (DirectionHelper.GetDirectionStringByVector(_direction) == "Up")
                _originComponent.UpdateOffset(_offset + new Vector2(0, -1));
            else
                _originComponent.UpdateOffset(_offset + new Vector2(0, 1));
        }
    }
}
