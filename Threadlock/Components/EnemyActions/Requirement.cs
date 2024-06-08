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
    public abstract class Requirement
    {
        public string Type;

        public abstract bool IsMet(Enemy enemy);
    }

    public class OrRequirement : Requirement
    {
        public List<Requirement> Requirements;

        public override bool IsMet(Enemy enemy)
        {
            return Requirements.Any(r => r.IsMet(enemy));
        }
    }

    public class DistanceRequirement : Requirement
    {
        public float Min = float.MinValue;
        public float Max = float.MaxValue;
        public string Axis;

        public override bool IsMet(Enemy enemy)
        {
            float distance;
            if (Axis != null)
            {
                if (Axis.ToLower() == "x")
                    distance = Math.Abs(enemy.Position.X - enemy.TargetEntity.Position.X);
                else if (Axis.ToLower() == "y")
                    distance = Math.Abs(enemy.Position.Y - enemy.TargetEntity.Position.Y);
                else
                    distance = EntityHelper.DistanceToEntity(enemy, enemy.TargetEntity);
            }
            else
                distance = EntityHelper.DistanceToEntity(enemy, enemy.TargetEntity);

            return distance >= Min && distance <= Max;
        }
    }

    public class LineOfSight : Requirement
    {
        public override bool IsMet(Enemy enemy)
        {
            return EntityHelper.HasLineOfSight(enemy, enemy.TargetEntity);
        }
    }

    public class RequirementConverter : JsonObjectFactory
    {
        public override bool CanConvertType(Type objectType)
        {
            return objectType == typeof(Requirement);
        }

        public override object CreateObject(Type objectType, IDictionary objectData)
        {
            if (objectData.Contains("Type"))
            {
                var type = objectData["Type"].ToString();
                var requirementType = Assembly.GetExecutingAssembly().GetTypes()
                    .FirstOrDefault(t => t.Name == type && typeof(Requirement).IsAssignableFrom(t));

                if (requirementType == null)
                    return null;

                var requirementInstance = Activator.CreateInstance(requirementType);

                foreach (var field in requirementType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (objectData.Contains(field.Name))
                    {
                        field.SetValue(requirementInstance, Convert.ChangeType(objectData[field.Name], field.FieldType));
                    }
                }

                return requirementInstance;
            }

            return null;
        }
    }
}
