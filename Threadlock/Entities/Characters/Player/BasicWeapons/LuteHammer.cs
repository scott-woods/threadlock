using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class LuteHammer : BasicWeapon
    {
        readonly List<string> _luteSounds = new List<string>()
        {
            Nez.Content.Audio.Sounds.Lute_sound01,
            Nez.Content.Audio.Sounds.Lute_sound02,
            Nez.Content.Audio.Sounds.Lute_sound03,
            Nez.Content.Audio.Sounds.Lute_sound04,
            Nez.Content.Audio.Sounds.Lute_sound05,
            Nez.Content.Audio.Sounds.Lute_sound06,
            Nez.Content.Audio.Sounds.Lute_sound07,
            Nez.Content.Audio.Sounds.Lute_sound08,
            Nez.Content.Audio.Sounds.Lute_sound09
        };

        const string _guitarSmashSound = Nez.Content.Audio.Sounds.Guitar_smash;
        const string _cerealSound = Nez.Content.Audio.Sounds.Cereal_slot_2;
        const float _playNoteTime = .25f;
        const float _slamTime = 1f;
        const int _hitboxActiveFrame = 2;
        const float _animatorSpeedReduction = .5f;
        const int _slamDamage = 3;
        const float _hitboxRadius = 15;
        const float _hitboxOffset = 12;

        SpriteAnimator _animator;
        CircleHitbox _hitbox;

        float _defaultAnimatorSpeed;

        #region BASIC WEAPON

        public override bool CanMove => false;

        public override void OnUnequipped()
        {

        }

        public override void Reset()
        {
            base.Reset();

            if (_defaultAnimatorSpeed > 0)
                _animator.Speed = _defaultAnimatorSpeed;

            _hitbox.SetEnabled(false);
        }

        public override bool Poll()
        {
            if (Controls.Instance.Melee.IsPressed)
            {
                QueuedAction = LaunchNote;
                return true;
            }

            if (Controls.Instance.AltAttack.IsPressed)
            {
                QueuedAction = Slam;
                return true;
            }

            return false;
        }

        #endregion

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _animator = Entity.GetComponent<SpriteAnimator>();

            _hitbox = Entity.AddComponent(new CircleHitbox(_slamDamage, _hitboxRadius));
            WatchHitbox(_hitbox);
            _hitbox.PhysicsLayer = 0;
            Flags.SetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            Flags.SetFlag(ref _hitbox.PhysicsLayer, PhysicsLayers.LuteNoteExplosion);
            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            _hitbox.SetEnabled(false);
        }

        IEnumerator LaunchNote()
        {
            //play sound
            var randomSound = _luteSounds.RandomItem();
            Game1.AudioManager.PlaySound(randomSound);

            //launch note
            var note = Entity.Scene.AddEntity(new LuteNote(Player.Instance.GetFacingDirection()));
            WatchHitbox(note.NoteHitbox);
            WatchHitbox(note.ExplosionHitbox);
            note.SetPosition(Entity.Position);

            yield return Coroutine.WaitForSeconds(_playNoteTime);
        }

        IEnumerator Slam()
        {
            //play animation
            var animation = "Slash";
            var dir = Player.GetFacingDirection();
            animation += DirectionHelper.GetDirectionStringByVector(dir);
            _defaultAnimatorSpeed = _animator.Speed;
            _animator.Speed *= _animatorSpeedReduction;
            _animator.Play(animation, SpriteAnimator.LoopMode.Once);

            //rotate hitbox
            _hitbox.SetLocalOffset(dir * _hitboxOffset);

            bool hasPlayedSound = false;
            while (_animator.CurrentAnimationName == animation && _animator.AnimationState != SpriteAnimator.State.Completed)
            {
                if (_animator.CurrentFrame == _hitboxActiveFrame)
                {
                    _hitbox.SetEnabled(true);

                    if (!hasPlayedSound)
                    {
                        //play sound
                        Game1.AudioManager.PlaySound(_guitarSmashSound);
                        Game1.AudioManager.PlaySound(Nez.Content.Audio.Sounds._60_Special_move_02);
                        hasPlayedSound = true;
                    }
                }
                else
                    _hitbox.SetEnabled(false);

                yield return null;
            }
        }
    }
}
