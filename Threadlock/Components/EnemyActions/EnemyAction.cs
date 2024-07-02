using Nez;
using System;
using System.Collections;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.Components.EnemyActions
{
    public class EnemyAction : BasicAction, ICloneable
    {
        //Requirements
        public bool RequiresLoS;
        public float MinDistance;
        public float MaxDistance = float.MaxValue;
        public float MinDistanceX;
        public float MaxDistanceX = float.MaxValue;
        public float MinDistanceY;
        public float MaxDistanceY = float.MaxValue;

        public string Name;
        public int Priority;
        public float Cooldown;

        bool _isActive;
        public bool IsActive { get => _isActive; }

        /// <summary>
        /// determine if the enemy is able to execute based on the requirements
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public bool CanExecute(Enemy enemy)
        {
            if (RequiresLoS && !EntityHelper.HasLineOfSight(enemy, enemy.TargetEntity))
                return false;

            var dist = EntityHelper.DistanceToEntity(enemy, enemy.TargetEntity);
            if (dist < MinDistance || dist > MaxDistance)
                return false;

            var distX = Math.Abs(enemy.TargetEntity.Position.X - enemy.Position.X);
            if (distX < MinDistanceX || distX > MaxDistanceX)
                return false;

            var distY = Math.Abs(enemy.TargetEntity.Position.Y - enemy.Position.Y);
            if (distY < MinDistanceY || distY > MaxDistanceY)
                return false;

            return true;
        }

        public void Abort(Enemy enemy)
        {

        }

        #region BASIC ACTION

        public override IEnumerator Execute(Entity entity)
        {
            _isActive = true;

            yield return base.Execute(entity);

            _isActive = false;
        }

        public override TargetingInfo GetTargetingInfo(Entity entity)
        {
            var enemy = entity as Enemy;

            return new TargetingInfo()
            {
                Direction = EntityHelper.DirectionToEntity(enemy, enemy.TargetEntity),
                TargetEntity = enemy.TargetEntity,
                Position = enemy.TargetEntity.Position,
            };
        }

        #endregion

        #region ICLONEABLE

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
