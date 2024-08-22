using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threadlock.Components;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.SceneComponents;

namespace Threadlock.Actions
{
    public class EnemyAction : BasicAction
    {
        //Requirements
        public bool RequiresLoS;
        public float MinDistance = 8;
        public float MaxDistance = float.MaxValue;
        public float MinAngle = 0f;
        public float MaxAngle = 90f;

        public int Priority;
        public float Cooldown;

        [JsonExclude]
        bool _isOnCooldown = false;
        public bool IsOnCooldown { get => _isOnCooldown; }

        /// <summary>
        /// clone this enemy action
        /// </summary>
        /// <returns></returns>
        public EnemyAction Clone()
        {
            return new EnemyAction()
            {
                RequiresLoS = RequiresLoS,
                MinDistance = MinDistance,
                MaxDistance = MaxDistance,
                MinAngle = MinAngle,
                MaxAngle = MaxAngle,
                Priority = Priority,
                Cooldown = Cooldown,
                Name = Name,
                PreActionPhase = PreActionPhase,
                ActionPhase = ActionPhase,
                PostActionPhase = PostActionPhase,
                AttackSounds = AttackSounds,
                Projectiles = Projectiles,
                IsCombo = IsCombo,
                ComboActions = ComboActions,
            };
        }

        /// <summary>
        /// determine if the enemy is able to execute based on the requirements
        /// </summary>
        /// <returns></returns>
        public bool CanExecute()
        {
            if (_isOnCooldown)
                return false;

            var currentPos = Context.Position;
            if (Context.TryGetComponent<OriginComponent>(out var oc))
                currentPos = oc.Origin;

            return CanExecuteAtPosition(currentPos);
        }

        bool CanExecuteAtPosition(Vector2 position)
        {
            var enemy = Context as Enemy;

            var targetPos = enemy.TargetEntity.Position;
            if (enemy.TargetEntity.TryGetComponent<OriginComponent>(out var targetOc))
                targetPos = targetOc.Origin;

            var dist = Vector2.Distance(targetPos, position);
            var angleToTarget = Math.Abs(MathHelper.ToDegrees(Mathf.AngleBetweenVectors(position, targetPos)));
            if (angleToTarget > 90)
                angleToTarget = 180 - angleToTarget;
            var hasLoS = EntityHelper.HasLineOfSight(position, enemy.TargetEntity);

            //check line of sight
            if (RequiresLoS && !hasLoS)
                return false;

            //check if too close or far away
            if (dist < MinDistance || dist > MaxDistance)
                return false;

            //check angle
            if (angleToTarget < MinAngle || angleToTarget > MaxAngle)
            {
                Debug.Log(angleToTarget);
                return false;
            }

            return true;
        }

        public Vector2 GetIdealPosition()
        {
            var enemy = Context as Enemy;

            var targetPos = enemy.TargetEntity.Position;
            if (enemy.TargetEntity.TryGetComponent<OriginComponent>(out var targetOc))
                targetPos = targetOc.Origin;

            var currentPos = Context.Position;
            if (Context.TryGetComponent<OriginComponent>(out var oc))
                currentPos = oc.Origin;

            //if we can already execute from current position, return that
            if (CanExecuteAtPosition(currentPos))
                return currentPos;

            var midAngle = (MaxAngle - MinAngle) / 2;
            var oppositeMidAngle = 180 - midAngle;

            List<float> angles = new List<float>()
            {
                midAngle,
                oppositeMidAngle,
                -midAngle,
                -oppositeMidAngle
            };

            var gridGraphManager = Core.Scene.GetOrCreateSceneComponent<GridGraphManager>();
            //var validPositions = angles.Select(a => targetPos + Mathf.AngleToVector(a, MinDistance)).Where(x => TiledHelper.ValidatePosition(Context.Scene, x));
            //var validPositions = angles.Select(a => targetPos + Mathf.AngleToVector(a, MinDistance));
            var validPositions = angles.Select(a => targetPos + Mathf.AngleToVector(a, MinDistance)).Where(x => gridGraphManager.IsPositionValid(x));
            if (validPositions.Any())
                return validPositions.MinBy(x => Vector2.Distance(currentPos, x));
            else
                return targetPos;
        }

        #region BASIC ACTION

        protected override void OnExecutionEnded()
        {
            base.OnExecutionEnded();

            _isOnCooldown = true;
            Core.Schedule(Cooldown, timer => _isOnCooldown = false);
        }

        protected override TargetingInfo GetTargetingInfo()
        {
            var enemy = Context as Enemy;

            return new TargetingInfo()
            {
                Direction = EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity),
                TargetEntity = enemy.TargetEntity,
                Position = enemy.TargetEntity.Position,
            };
        }

        #endregion
    }
}
