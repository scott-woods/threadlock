//using Microsoft.Xna.Framework;
//using Nez;
//using Nez.Sprites;
//using Nez.Textures;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.Components;
//using Threadlock.Components.Hitboxes;
//using Threadlock.Entities.Characters.Enemies;
//using Threadlock.StaticData;

//namespace Threadlock.Entities.Characters.Player.PlayerActions
//{
//    public class ChainLightningAttach : HitEffect
//    {
//        //consts
//        const int _baseDamage = 2;
//        const int _damageAddedPerChain = 1;
//        const float _delay = .1f;
//        const int _chainRadius = 75;

//        //components
//        CircleHitbox _hitbox;

//        //misc
//        int _chainCount = 0;
//        List<Entity> _hitEntities;
//        Entity _entityToHit;

//        public ChainLightningAttach(int chainCount, Entity entityToHit, ref List<Entity> hitEntities) : base(HitEffects.Lightning1)
//        {
//            _chainCount = chainCount;
//            _entityToHit = entityToHit;
//            _hitEntities = hitEntities;

//            //set position
//            if (entityToHit.TryGetComponent<OriginComponent>(out var origin))
//                Position = origin.Origin;
//            else if (entityToHit.TryGetComponent<Hurtbox>(out var hurtbox))
//                Position = hurtbox.Collider.AbsolutePosition;
//            else
//                Position = entityToHit.Position;
//        }

//        #region LIFECYCLE

//        public override void OnAddedToScene()
//        {
//            _hitbox = AddComponent(new CircleHitbox(_baseDamage + _chainCount * _damageAddedPerChain, 1));
//            Flags.SetFlagExclusive(ref _hitbox.PhysicsLayer, (int)PhysicsLayers.PlayerHitbox);
//            Flags.SetFlagExclusive(ref _hitbox.CollidesWithLayers, (int)PhysicsLayers.EnemyHurtbox);
//            _hitbox.SetEnabled(false);

//            base.OnAddedToScene();
//        }

//        #endregion

//        public override void PlayEffect()
//        {
//            base.PlayEffect();

//            Game1.StartCoroutine(Play());
//        }

//        IEnumerator Play()
//        {
//            if (_chainCount > 0)
//            {
//                //play animation
//                _animator.SetEnabled(true);
//                _animator.Play("Hit", SpriteAnimator.LoopMode.Once);

//                //enable hitbox
//                _hitbox.SetEnabled(true);
//            }

//            //wait for delay
//            yield return Coroutine.WaitForSeconds(_delay);

//            //chain
//            var allEnemies = Scene.EntitiesOfType<Enemy>();
//            if (allEnemies.Count > 0)
//            {
//                var unhitEnemies = allEnemies.Where(e => !_hitEntities.Contains(e)).ToList();

//                List<Entity> entitiesToHit = new List<Entity>();
//                foreach (var unhitEnemy in unhitEnemies)
//                {
//                    if (Vector2.Distance(unhitEnemy.Position, Position) <= _chainRadius)
//                    {
//                        _hitEntities.Add(unhitEnemy);
//                        var attach = Scene.AddEntity(new ChainLightningAttach(_chainCount + 1, unhitEnemy, ref _hitEntities));
//                    }
//                }
//            }

//            _hitbox.SetEnabled(false);

//            Destroy();
//        }

//        void OnAnimationCompleted(string animationName)
//        {
//            _animator.OnAnimationCompletedEvent -= OnAnimationCompleted;
//            _animator.SetEnabled(false);
//        }
//    }
//}
