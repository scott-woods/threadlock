using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Actions;
using Threadlock.Components;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Enemies
{
    public class EnemyActionManager : Component
    {
        public bool IsExecutingAction { get; private set; } = false;

        List<EnemyAction> _actions = new List<EnemyAction>();
        Dictionary<EnemyAction, bool> _actionCooldownDict = new Dictionary<EnemyAction, bool>();

        ICoroutine _executionCoroutine;
        ICoroutine _actionCoroutine;

        EnemyAction _activeAction;

        List<string> _actionNames;

        public EnemyActionManager(List<string> actionNames)
        {
            _actionNames = actionNames;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            foreach (var actionString in _actionNames)
            {
                if (AllEnemyActions.TryCreateEnemyAction(actionString, Entity, out var action))
                {
                    _actions.Add(action);
                    _actionCooldownDict.Add(action, false);
                }
            }

            if (Entity.TryGetComponent<SpriteAnimator>(out var animator))
            {
                foreach (var action in _actions)
                    action.LoadAnimations(ref animator);
            }

            if (Entity.TryGetComponent<StatusComponent>(out var statusComponent))
                statusComponent.Emitter.AddObserver(StatusEvents.Changed, OnStatusChanged);
        }

        public bool TryAction()
        {
            var actions = GetValidActions();

            if (actions.Count == 0)
                return false;

            var groups = actions.OrderByDescending(a => a.Priority).GroupBy(a => a.Priority).Select(g => g.ToList());
            var action = groups.First().RandomItem();
            _executionCoroutine = Game1.StartCoroutine(ExecuteAction(action));

            return true;
        }

        public List<EnemyAction> GetValidActions()
        {
            var actions = new List<EnemyAction>();

            foreach (var action in _actions)
            {
                if (action.CanExecute() && !_actionCooldownDict[action])
                    actions.Add(action);
            }

            return actions;
        }

        public Vector2 GetTargetPosition()
        {
            var currentPos = Entity.Position;
            if (Entity.TryGetComponent<OriginComponent>(out var oc))
                currentPos = oc.Origin;

            var idealPositions = new List<Vector2>();

            foreach (var action in _actions)
            {
                if (action.IsOnCooldown)
                    continue;

                idealPositions.Add(action.GetIdealPosition());
            }

            var targetPos = idealPositions.MinBy(p => Vector2.Distance(p, currentPos));

            return targetPos;
        }

        void EndAction()
        {
            if (_activeAction != null)
            {
                //start cooldown
                if (_activeAction.Cooldown > 0)
                {
                    _actionCooldownDict[_activeAction] = true;
                    Game1.Schedule(_activeAction.Cooldown, timer => _actionCooldownDict[_activeAction] = false);
                }

                _activeAction?.Abort();
                _activeAction = null;
            }

            _executionCoroutine?.Stop();
            _executionCoroutine = null;

            _actionCoroutine?.Stop();
            _actionCoroutine = null;

            IsExecutingAction = false;


        }

        IEnumerator ExecuteAction(EnemyAction action)
        {
            IsExecutingAction = true;

            _actionCoroutine = Game1.StartCoroutine(action.Execute());
            yield return _actionCoroutine;

            EndAction();
        }

        void OnStatusChanged(StatusPriority priority)
        {
            if (priority != StatusPriority.Normal)
                EndAction();
        }
    }
}
