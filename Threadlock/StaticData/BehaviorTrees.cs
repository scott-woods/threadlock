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
            var tree = BehaviorTreeBuilder<Enemy>.Begin(enemy)
                //root
                .Selector(AbortTypes.Self)

                    //handle stunned state
                    .Sequence(AbortTypes.LowerPriority)
                        .Conditional(x => x.CheckStatus() != StatusPriority.Normal)
                    .EndComposite()

                    //handle pursuit
                    .ConditionalDecorator(x => x.IsPursued)
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(x => x.MoveAway(x.TargetEntity, x.BaseSpeed))
                    .EndComposite()

                    //attack sequence
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(x => x.TryQueueAction())
                        .Action(x => x.ExecuteQueuedAction())
                        .ParallelSelector()
                            .WaitAction(1f)
                            .Action(x => x.Idle())
                        .EndComposite()
                    .EndComposite()

                    //no valid attack
                    .Selector(AbortTypes.LowerPriority)
                        .Action(x => x.GetInRange())
                    .EndComposite()
                .EndComposite()
                .Build();

            tree.UpdatePeriod = 0;
            return tree;
        }

        //static BehaviorTree<Enemy> GetBasicTree(Enemy enemy)
        //{
        //    var builder = BehaviorTreeBuilder<Enemy>.Begin(enemy);

        //    //selector
        //    builder.Selector();

        //    //if stunned, do nothing
        //    builder.Sequence(AbortTypes.LowerPriority)
        //        .Conditional(x => x.CheckStatus() != StatusPriority.Normal)
        //    .EndComposite();

        //    //if pursued, run away
        //    builder.Sequence(AbortTypes.LowerPriority)
        //        .Conditional(x => x.IsPursued)
        //        .Action(x => x.MoveAway(x.TargetEntity, x.BaseSpeed))
        //    .EndComposite();

        //    //attack sequence
        //    builder.Sequence(AbortTypes.LowerPriority)

        //        //try to pick an attack
        //        .Conditional(x => x.TryQueueAction())

        //        //handle attack sequence
        //        .Sequence()

        //            //try to get in range, fail if not in range within certain time
        //            .ParallelSelector()
        //                .Inverter()
        //                .WaitAction(5f)
        //                .Action(x => x.GetInRange())
        //            .EndComposite()

        //            //execute
        //            .Action(x => x.ExecuteQueuedAction())

        //            //idle after execution
        //            .ParallelSelector()
        //                .WaitAction(enemy.BehaviorConfig.ActionCooldown)
        //                .Action(x => x.Idle(true))
        //            .EndComposite()

        //        .EndComposite()
        //    .EndComposite();

        //    //end root selector
        //    builder.EndComposite();

        //    var tree = builder.Build();
        //    tree.UpdatePeriod = 0;

        //    return tree;
        //}

        //static BehaviorTree<Enemy> GetDefaultTree(Enemy enemy, BehaviorTree<Enemy> subTree)
        //{
        //    var tree = BehaviorTreeBuilder<Enemy>.Begin(enemy)
        //        .Selector()
        //            .Sequence(AbortTypes.LowerPriority)
        //                .Conditional(x => x.CheckStatus() != StatusPriority.Normal)
        //            .EndComposite()
        //            .Sequence(AbortTypes.LowerPriority)
        //                .Conditional(c => c.ShouldRunSubTree())
        //                .SubTree(subTree)
        //            .EndComposite()
        //            .Sequence(AbortTypes.LowerPriority)
        //                .Action(c => c.Idle())
        //            .EndComposite()
        //        .EndComposite()
        //        .Build();

        //    tree.UpdatePeriod = 0;

        //    return tree;
        //}
    }
}
