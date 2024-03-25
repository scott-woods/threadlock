using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tweens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;

namespace Threadlock.Components
{
    public class Pickup : Component, IUpdatable
    {
        const float _landingOffset = 16;
        const float _bobbingAmplitude = 2f;
        const float _bobbingFrequency = 2f;

        string _texturePath;
        string _soundPath;
        bool _magnetized;
        Action _collisionHandler;

        SpriteAnimator _animator;
        Collider _collider;
        ProjectileMover _mover;

        Vector2 _velocity;
        bool _isMoving = false;
        float _speed = 200f;
        ITimer _moveTimer;
        Vector2 _initialPosition;
        bool _hasLanded = false;
        Vector2 _landingPosition;

        public Pickup(string texturePath, string soundPath, bool magnetized, Action collisionHandler)
        {
            _texturePath = texturePath;
            _soundPath = soundPath;
            _magnetized = magnetized;
            _collisionHandler = collisionHandler;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            var texture = Entity.Scene.Content.LoadTexture(_texturePath);
            var sprites = Sprite.SpritesFromAtlas(texture, 16, 16);
            _animator.AddAnimation("Idle", sprites.ToArray());
            _animator.Play("Idle");

            _collider = Entity.AddComponent(new BoxCollider(8, 8));
            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);

            _mover = Entity.AddComponent(new ProjectileMover());

            Launch();
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _moveTimer?.Stop();
            _moveTimer = null;
        }

        public void Update()
        {
            if (_collider.CollidesWithAny(out var result))
                HandleCollision();

            if (!_hasLanded)
            {
                if (Entity.Position.Y > _initialPosition.Y + _landingOffset)
                {
                    _isMoving = false;
                    _hasLanded = true;
                    _landingPosition = Entity.Position;
                }

                if (_isMoving)
                {
                    _mover.Move(_velocity * Time.DeltaTime);
                    _velocity += Physics.Gravity * Time.DeltaTime;
                }
            }
            else
            {
                float bobbingOffset = (float)Math.Sin(Time.TotalTime * _bobbingFrequency) * _bobbingAmplitude;
                Entity.Position = new Vector2(Entity.Position.X, _landingPosition.Y - bobbingOffset);
            }
        }

        public void Launch()
        {
            var velocityX = Nez.Random.Range(25f, 50f);
            if (Nez.Random.Chance(.5f))
                velocityX *= -1;

            var velocityY = Nez.Random.Range(-325f, -275f);

            _velocity = new Vector2(velocityX, velocityY);

            _isMoving = true;

            _initialPosition = Entity.Position;
        }

        void HandleCollision()
        {
            Game1.AudioManager.PlaySound(_soundPath);
            _collisionHandler?.Invoke();
        }
    }
}
