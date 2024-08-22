using Nez;
using Nez.AI.BehaviorTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Threadlock.Components;

namespace Threadlock.Scenes
{
    public class BehaviorTestScene : Scene
    {
        BehaviorTree<TestBehaviorGuy> _tree;

        public override void Begin()
        {
            base.Begin();

            _tree = CreateTree();
        }

        public override void Update()
        {
            base.Update();

            _tree?.Tick();
        }

        BehaviorTree<TestBehaviorGuy> CreateTree()
        {
            var tree = BehaviorTreeBuilder<TestBehaviorGuy>.Begin(new TestBehaviorGuy())
                //root
                .Selector(AbortTypes.Self)

                    //handle stunned state
                    //.Sequence(AbortTypes.LowerPriority)
                    //    .Conditional(x => x.CheckStatus() != StatusPriority.Normal)
                    //.EndComposite()

                    //handle pursuit
                    .ConditionalDecorator(x => x.IsPursued)
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(x => x.MoveAway(x.TargetEntity, x.BaseSpeed))
                    .EndComposite()

                    //attack sequence
                    .Sequence(AbortTypes.LowerPriority)
                        .Action(x => x.StartAction())
                        .UntilSuccess()
                        .Sequence()
                            .Conditional(x => !x.IsActionExecuting())
                        .EndComposite()
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

            tree.UpdatePeriod = 0;

            return tree;
        }
    }

    public class TestBehaviorGuy
    {
        public Entity TargetEntity;
        public float BaseSpeed = 1f;
        bool _isActionExecuting = false;
        public bool IsPursued = false;

        public TaskStatus StartAction()
        {
            _isActionExecuting = true;
            Game1.Schedule(10f, timer => _isActionExecuting = false);
            Debug.Log("Action started");

            return TaskStatus.Success;
        }

        public TaskStatus Idle()
        {

           Debug.Log("Idling");
           return TaskStatus.Success;
        }

        public bool IsActionExecuting()
        {
            //Debug.Log($"Is Action Executing? {_isActionExecuting}");
            if (!_isActionExecuting)
                Debug.Log("Action finished executing");

            return _isActionExecuting;
        }

        public TaskStatus GetInRange()
        {

           Debug.Log("Getting in range");
            return TaskStatus.Running;
        }

        public TaskStatus MoveAway(Entity targetEntity, float baseSpeed)
        {
            Debug.Log("Moving away");
            return TaskStatus.Running;
        }
    }
}
