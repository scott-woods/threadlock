using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMageSweepAttackVfx : Entity
    {
        //constants
        const int _damage = 2;
        const int _hitboxActiveFrame = 0;
        const int _offset = 44;

        //components
        SpriteAnimator _animator;
        BoxHitbox _hitbox;

        //misc
        Vector2 _directionToPlayer;

        AnimationWaiter _animationWaiter;

        public OrbMageSweepAttackVfx(Vector2 directionToPlayer)
        {
            _directionToPlayer = directionToPlayer;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _animator = AddComponent(new SpriteAnimator());
            var texture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.VFXforSweep);
            var sprites = Sprite.SpritesFromAtlas(texture, 87, 34);
            _animator.AddAnimation("Attack", sprites.ToArray());
            _animator.SetEnabled(false);

            _hitbox = AddComponent(new BoxHitbox(_damage, new Rectangle(-22, 6, 62, 11)));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);
            _hitbox.SetEnabled(false);

            _animationWaiter = new AnimationWaiter(_animator);
        }

        public IEnumerator Play()
        {
            _animator.SetEnabled(true);

            if (_directionToPlayer.X < 0)
                _animator.FlipY = true;

            Position += (_offset * _directionToPlayer);
            Rotation = (float)Math.Atan2(_directionToPlayer.Y, _directionToPlayer.X);

            Game1.StartCoroutine(_animationWaiter.WaitForAnimation("Attack"));

            while (_animator.CurrentFrame < _hitboxActiveFrame)
                yield return null;

            _hitbox.SetEnabled(true);
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Orb_mage_attack);

            while (_animator.CurrentFrame == _hitboxActiveFrame)
                yield return null;

            _hitbox.SetEnabled(false);

            while (_animator.IsAnimationActive("Attack") && _animator.AnimationState == SpriteAnimator.State.Running)
                yield return null;

            _hitbox.SetEnabled(false);
            _animator.SetEnabled(false);
            Destroy();
        }
    }
}
