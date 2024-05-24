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
using Threadlock.Components.Hitboxes;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.BasicWeapons
{
    public class LuteNote : Entity
    {
        readonly List<string> _explosionSounds = new List<string>()
        {
            Nez.Content.Audio.Sounds.Luteexplosionsounds01,
            Nez.Content.Audio.Sounds.Luteexplosionsounds02,
            Nez.Content.Audio.Sounds.Luteexplosionsounds03,
            Nez.Content.Audio.Sounds.Luteexplosionsounds04,
            Nez.Content.Audio.Sounds.Luteexplosionsounds05,
            Nez.Content.Audio.Sounds.Luteexplosionsounds06,
            Nez.Content.Audio.Sounds.Luteexplosionsounds07,
            Nez.Content.Audio.Sounds.Luteexplosionsounds08,
            Nez.Content.Audio.Sounds.Luteexplosionsounds09
        };

        const string _explosionSound = Nez.Content.Audio.Sounds.Short_explosion;
        const int _pulseDamage = 1;
        const float _radius = 24;
        const float _pulseInterval = .75f;
        const float _lifespan = 3f;
        const float _initialLaunchTime = .4f;
        const float _initialLaunchSpeed = 450f;
        const float _finalSpeed = 50f;
        const float _explosionTime = .15f;

        public int ChainCount = 0;

        //components
        SpriteRenderer _renderer;
        ProjectileMover _mover;
        public CircleHitbox NoteHitbox { get; private set; }
        public CircleHitbox ExplosionHitbox { get; private set; }

        float _timer;
        int _currentPulseCount = 0;
        Vector2 _direction;
        bool _isExploding = false;
        ITimer _pulseTimer;

        public LuteNote(Vector2 direction)
        {
            _direction = direction;

            _renderer = AddComponent(new PrototypeSpriteRenderer(6, 6));

            _mover = AddComponent(new ProjectileMover());

            NoteHitbox = AddComponent(new CircleHitbox(_pulseDamage, _radius));
            NoteHitbox.PushForce = 0f;
            Flags.SetFlagExclusive(ref NoteHitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            NoteHitbox.CollidesWithLayers = 0;
            Flags.SetFlag(ref NoteHitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            Flags.SetFlag(ref NoteHitbox.CollidesWithLayers, PhysicsLayers.LuteNoteExplosion);
            NoteHitbox.IsTrigger = true;
            NoteHitbox.SetEnabled(false);

            ExplosionHitbox = AddComponent(new CircleHitbox(_pulseDamage, 0));
            ExplosionHitbox.PushForce = 1f;
            ExplosionHitbox.PhysicsLayer = 0;
            Flags.SetFlag(ref ExplosionHitbox.PhysicsLayer, PhysicsLayers.PlayerHitbox);
            Flags.SetFlag(ref ExplosionHitbox.PhysicsLayer, PhysicsLayers.LuteNoteExplosion);
            ExplosionHitbox.CollidesWithLayers = 0;
            Flags.SetFlag(ref ExplosionHitbox.CollidesWithLayers, PhysicsLayers.EnemyHurtbox);
            ExplosionHitbox.IsTrigger = true;
            ExplosionHitbox.SetEnabled(false);
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            NoteHitbox.SetEnabled(true);
            _pulseTimer = Game1.Schedule(.1f, timer => NoteHitbox.SetEnabled(false));
        }

        public override void Update()
        {
            base.Update();

            if (!_isExploding)
            {
                //increment timer
                _timer += Time.DeltaTime;

                //handle pulses
                if (_timer >= (_pulseInterval * (_currentPulseCount + 1)))
                {
                    _currentPulseCount++;
                    NoteHitbox.SetEnabled(true);
                    _pulseTimer = Game1.Schedule(.1f, timer => NoteHitbox.SetEnabled(false));
                }

                //move
                float speed = 0f;
                if (_timer <= _initialLaunchTime)
                    speed = Lerps.Ease(EaseType.CubicOut, _initialLaunchSpeed, _finalSpeed, _timer, _initialLaunchTime);
                else
                    speed = _finalSpeed;
                _mover.Move(_direction * speed * Time.DeltaTime);

                //check for collisions
                var colliders = Physics.BoxcastBroadphaseExcludingSelf(NoteHitbox, 1 << PhysicsLayers.LuteNoteExplosion);
                if (colliders.Count > 0)
                {
                    var collider = colliders.FirstOrDefault();
                    if (collider != null)
                    {
                        //check if it's another lute note hitting us, and use its chain count
                        if (collider.Entity is LuteNote luteNote)
                        {
                            ChainCount = luteNote.ChainCount + 1;
                            ExplosionHitbox.Damage += ChainCount;
                        }

                        //start explosion
                        Game1.StartCoroutine(Explode());
                        return;
                    }
                }

                //check lifespan
                if (_timer >= _lifespan)
                {
                    Burst();
                }
            }
        }

        void Burst()
        {
            Destroy();
        }

        IEnumerator Explode()
        {
            //update state
            _isExploding = true;

            //stop pulse timer
            _pulseTimer?.Stop();
            _pulseTimer = null;

            //disable normal hitbox
            NoteHitbox.SetEnabled(false);

            //wait just a momemt before starting explosion
            yield return Coroutine.WaitForSeconds(.15f);

            //update explosion hitbox
            ExplosionHitbox.SetEnabled(true);

            //play sounds
            Game1.AudioManager.PlaySound(_explosionSound);
            Game1.AudioManager.PlaySound(_explosionSounds.RandomItem());

            //increase size of hitbox radius over time
            var timer = 0f;
            while (timer <= _explosionTime)
            {
                var progress = Math.Clamp(timer / _explosionTime, 0, 1);
                var radius = Lerps.Lerp(0, _radius * 1.75f, progress);
                ExplosionHitbox.SetRadius(radius);
                timer += Time.DeltaTime;
                yield return null;
            }

            //destroy entity
            Destroy();
        }
    }
}
