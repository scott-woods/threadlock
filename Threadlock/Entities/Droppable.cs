using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Player;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Entities
{
    public class Droppable : Entity
    {
        const float _minLandingOffset = -16f;
        const float _maxLandingOffset = 16f;
        const float _bobbingAmplitude = 2f;
        const float _bobbingFrequency = 2f;
        const int _bounceCount = 4;
        const float _magnetizeDistance = 128f;
        const float _initialMagnetizeSpeed = 50f;
        const float _magnetizeExpoFactor = 1.01f;

        LootConfig _config;

        //components
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
        int _bounceCounter = 0;
        float _landingOffset;
        bool _canLand = false;
        float _timeMoving = 0;
        float _currentMagnetizeSpeed;

        public Droppable(LootConfig config)
        {
            _config = config;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            Scale = _config.Scale;

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);
            var texture = Scene.Content.LoadTexture(_config.TexturePath);
            var sprites = Sprite.SpritesFromAtlas(texture, _config.CellWidth, _config.CellHeight);
            var firstIndex = Math.Clamp(_config.StartCell, 0, sprites.Count - 1);
            var lastIndex = Math.Clamp(_config.EndCell == 0 ? sprites.Count - 1 : _config.EndCell, _config.StartCell, sprites.Count - 1);
            _animator.AddAnimation("Idle", AnimatedSpriteHelper.GetSpriteArrayFromRange(sprites, firstIndex, lastIndex));
            _animator.Play("Idle");

            _collider = AddComponent(new CircleCollider(_config.Radius));
            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            if (_config.DelayBeforeEnabled > 0)
            {
                _collider.SetEnabled(false);
                Game1.Schedule(_config.DelayBeforeEnabled, timer => _collider.SetEnabled(true));
            }

            _mover = AddComponent(new ProjectileMover());

            Launch();
        }

        public override void Update()
        {
            base.Update();

            //check for collision
            if (_collider.Enabled && _collider.CollidesWithAny(out var result))
                HandleCollision();

            if (!_hasLanded)
            {
                //must have been above landing position at least once to be able to land
                if (!_canLand)
                {
                    if (Position.Y < _initialPosition.Y + _landingOffset)
                        _canLand = true;
                }
                else if (Position.Y > _initialPosition.Y + _landingOffset)
                {
                    //increment bounce counter
                    _bounceCounter++;

                    //if at max bounces, stop
                    if (_bounceCounter >= _bounceCount)
                    {
                        _isMoving = false;
                        _hasLanded = true;
                        _landingPosition = Position;
                    }
                    else //bounce again
                    {
                        Game1.AudioManager.PlaySound(_config.PickupSoundPath);
                        Position = new Vector2(Position.X, _initialPosition.Y + _landingOffset);
                        _velocity.Y *= -.4f;
                    }
                }

                //move
                if (_isMoving)
                {
                    _mover.Move(_velocity * Time.DeltaTime);
                    _velocity += Physics.Gravity * Time.DeltaTime;
                }
            }
            else
            {
                if (_config.Magnetized && EntityHelper.DistanceToEntity(this, Player.Instance) <= _magnetizeDistance)
                {
                    if (_currentMagnetizeSpeed == 0)
                        _currentMagnetizeSpeed = _initialMagnetizeSpeed;

                    var dir = EntityHelper.DirectionToEntity(this, Player.Instance);
                    _velocity = dir * _currentMagnetizeSpeed * Time.DeltaTime;
                    _mover.Move(_velocity);

                    _currentMagnetizeSpeed *= _magnetizeExpoFactor;
                }
                else
                {
                    _currentMagnetizeSpeed = _initialMagnetizeSpeed;

                    //bob up and down
                    float bobbingOffset = (float)Math.Sin(Time.TotalTime * _bobbingFrequency) * _bobbingAmplitude;
                    Position = new Vector2(Position.X, _landingPosition.Y - bobbingOffset);
                }
            }
        }

        public void Launch()
        {
            _landingOffset = Nez.Random.Range(_minLandingOffset, _maxLandingOffset);

            var velocityX = Nez.Random.Range(25f, 50f);
            if (Nez.Random.Chance(.5f))
                velocityX *= -1;

            var velocityY = Nez.Random.Range(-325f, -275f);

            _velocity = new Vector2(velocityX, velocityY);

            _isMoving = true;

            _initialPosition = Position;
        }

        void HandleCollision()
        {
            if (!string.IsNullOrWhiteSpace(_config.PickupSoundPath))
                Game1.AudioManager.PlaySound(_config.PickupSoundPath);

            _config.HandlePickup?.Invoke();

            Destroy();
        }
    }
}
