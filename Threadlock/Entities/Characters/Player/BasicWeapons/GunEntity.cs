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
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class GunEntity : Entity
    {
        const float _horizontalRadius = 10f;
        const float _verticalRadius = 8f;
        readonly Vector2 _staticOffset = new Vector2(0, 2);

        public event Action<int> OnProjectileHit;

        SpriteRenderer _renderer;
        OriginComponent _originComponent;

        Vector2 _direction = Vector2.One;
        Vector2 _offset = Vector2.Zero;

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
            projectile.SetPosition(Position);
            projectile.OnHit += OnHit;
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Quickshot_fire);

            yield break;
        }

        void OnHit(Collider collider, int damage)
        {
            OnProjectileHit?.Invoke(damage);
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
