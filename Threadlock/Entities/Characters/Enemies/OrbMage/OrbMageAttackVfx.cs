using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.StaticData;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Threadlock.Entities.Characters.Enemies.OrbMage
{
    public class OrbMageAttackVfx : Entity
    {
        //const
        const int _damage = 2;
        const int _hitboxActiveFrame = 0;
        const float _delay = .25f;

        //components
        SpriteAnimator _animator;
        BoxHitbox _hitbox;

        //misc
        bool _hasPlayedSound = false;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            _hitbox = AddComponent(new BoxHitbox(_damage, new Rectangle(-3, -4, 7, 18)));
            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, (int)PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, (int)PhysicsLayers.PlayerHurtbox);
            _hitbox.SetEnabled(false);

            _animator = AddComponent(new SpriteAnimator());
            _animator.SetLocalOffset(new Vector2(5, 0));
            var texture = Scene.Content.LoadTexture(Nez.Content.Textures.Characters.OrbMage.Attackvfx);
            var sprites = Sprite.SpritesFromAtlas(texture, 119, 34);
            _animator.AddAnimation("Telegraph", AnimatedSpriteHelper.GetSpriteArray(sprites, new List<int> { 5, 4, 3 }, true));
            _animator.AddAnimation("Attack", AnimatedSpriteHelper.GetSpriteArray(sprites, new List<int> { 0, 1, 2, 3, 4, 5 }, true));
            _animator.SetEnabled(false);

            var origin = AddComponent(new OriginComponent(_hitbox));
        }

        public IEnumerator Play()
        {
            //sound
            Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Orb_mage_telegraph);

            //enable animator
            _animator.SetEnabled(true);

            var animationWaiter = new AnimationWaiter(_animator);

            //telegraph
            yield return animationWaiter.WaitForAnimation("Telegraph");

            //delay
            yield return Coroutine.WaitForSeconds(_delay);

            //strike animation
            yield return animationWaiter.WaitForAnimation("Attack");

            //cleanup
            _animator.SetEnabled(false);
            _hitbox.SetEnabled(false);
            Destroy();
        }

        public override void Update()
        {
            base.Update();

            if (_animator.IsAnimationActive("Attack") && _animator.CurrentFrame == _hitboxActiveFrame)
            {
                _hitbox.SetEnabled(true);
                if (!_hasPlayedSound)
                {
                    Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds.Orb_mage_attack);
                    _hasPlayedSound = true;
                }
            }
            else
                _hitbox.SetEnabled(false);
        }
    }
}
