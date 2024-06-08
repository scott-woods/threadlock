using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.Components.EnemyActions
{
    public abstract class EnemyAction2
    {
        //properties
        public string Name;
        public string Type;
        public int Priority;

        //Requirements
        public bool RequiresLoS;
        public float MinDistance;
        public float MaxDistance = float.MaxValue;
        public float MinDistanceX;
        public float MaxDistanceX = float.MaxValue;
        public float MinDistanceY;
        public float MaxDistanceY = float.MaxValue;

        bool _isActive;
        public bool IsActive { get => _isActive; }

        ICoroutine _executionCoroutine;

        public IEnumerator BeginExecution(Enemy enemy)
        {
            _isActive = true;

            _executionCoroutine = Game1.StartCoroutine(Execute(enemy));
            yield return _executionCoroutine;

            _executionCoroutine = null;
            _isActive = false;
        }

        protected abstract IEnumerator Execute(Enemy enemy);

        public abstract void Abort(Enemy enemy);

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
    }

    public class EnemyActionFactory : JsonObjectFactory
    {
        public override bool CanConvertType(Type objectType)
        {
            return objectType == typeof(EnemyAction2);
        }

        public override object CreateObject(Type objectType, IDictionary objectData)
        {
            if (objectData.Contains("Type"))
            {
                var type = objectData["Type"].ToString();
                var enemyActionType = Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == type && typeof(EnemyAction2).IsAssignableFrom(t));

                if (enemyActionType == null)
                    return null;

                var enemyActionInstance = Activator.CreateInstance(enemyActionType);

                foreach (var field in enemyActionType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (objectData.Contains(field.Name))
                    {
                        field.SetValue(enemyActionInstance, Convert.ChangeType(objectData[field.Name], field.FieldType));
                    }
                }

                return enemyActionInstance;
            }

            return null;
        }
    }
}
