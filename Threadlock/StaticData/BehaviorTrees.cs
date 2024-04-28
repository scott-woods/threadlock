using Nez.AI.BehaviorTrees;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;

namespace Threadlock.StaticData
{
    public class BehaviorTrees
    {
        static readonly Dictionary<string, Func<Enemy, BehaviorTree<Enemy>>> _treeDictionary = new Dictionary<string, Func<Enemy, BehaviorTree<Enemy>>>()
        {
            { "Basic", GetBasicTree }
        };

        public static BehaviorTree<Enemy> CreateBehaviorTree(Enemy enemy, string treeType)
        {
            if (_treeDictionary.TryGetValue(treeType, out var func))
            {
                return func.Invoke(enemy);
            }
            else
                return null;
        }

        static BehaviorTree<Enemy> GetBasicTree(Enemy enemy)
        {
            var builder = BehaviorTreeBuilder<Enemy>.Begin(enemy);

            //selector
            builder.Selector(AbortTypes.Self);

            builder.ConditionalDecorator(x => x.IsPursued);
            builder.Sequence(AbortTypes.LowerPriority)
                .Action(x => x.MoveAway(x.TargetEntity, x.BaseSpeed))
                .EndComposite();

            builder.Sequence(AbortTypes.LowerPriority);
            builder.Conditional(x => x.TryQueueAction());
            builder.Action(x => x.ExecuteQueuedAction());
            //idle after action
            builder.ParallelSelector()
                .AlwaysFail()
                .Action(x => x.Idle(true))
                .WaitAction(enemy.BehaviorConfig.ActionCooldown)
                .EndComposite();
            builder.EndComposite();

            //start nothing selector
            builder.Selector();
            builder.Sequence(AbortTypes.LowerPriority)
                .Conditional(x => x.IsTooClose())
                .Action(x => x.MoveAway(x.TargetEntity, x.BaseSpeed))
                .EndComposite();
            builder.Sequence(AbortTypes.LowerPriority)
                .Conditional(x => x.IsTooFar() || (x.WantsLineOfSight && !EntityHelper.HasLineOfSight(x, x.TargetEntity)))
                .Action(x => x.MoveToTarget(x.TargetEntity, x.BaseSpeed))
                .EndComposite();
            builder.Sequence()
                .Action(x => x.Idle(true))
                .EndComposite();
            //end nothing selector
            builder.EndComposite();

            //end root selector
            builder.EndComposite();

            var subTree = builder.Build();
            subTree.UpdatePeriod = 0;

            var tree = GetDefaultTree(enemy, subTree);

            return tree;
        }

        static BehaviorTree<Enemy> GetDefaultTree(Enemy enemy, BehaviorTree<Enemy> subTree)
        {
            var tree = BehaviorTreeBuilder<Enemy>.Begin(enemy)
                .Selector()
                    .Sequence(AbortTypes.LowerPriority)
                        .Conditional(x => x.CheckStatus() != StatusPriority.Normal)
                    .EndComposite()
                    .Sequence(AbortTypes.LowerPriority)
                        .Conditional(c => c.ShouldRunSubTree())
                        .SubTree(subTree)
                    .EndComposite()
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(c => c.Idle())
                    .EndComposite()
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0;

            return tree;
        }
    }
}
