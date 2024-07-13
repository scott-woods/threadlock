﻿using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using Threadlock.Components;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public class PlayerAction2 : BasicAction, ICloneable
    {
        //stats
        public string Description;
        public int ApCost;

        //icon
        public string IconName;

        //charge
        public string ChargeAnimation;

        //prep confirmation
        public ActionConfirmType ConfirmType;
        public float MinConfirmDistance = 0;
        public float MaxConfirmDistance = float.MaxValue;
        public PlayerActionWallBehavior WallBehavior;
        public bool SnapToEdgeIfOutsideRadius;
        public bool CanPassThroughWalls;
        public bool CanAimInsideWalls;

        public bool ShowSim;
        public SimPlayerType SimType;
        public string SimAnimation;

        public bool IsPrepared = false;

        //data saved after prep confirmation
        Vector2 _selectedPosition;
        Vector2 _selectedDirection
        {
            get
            {
                var dir = _selectedPosition - _baseEntity.Position;
                dir.Normalize();
                return dir;
            }
        }
        Entity _selectedEntity;

        EntitySelector _entitySelector;
        SimPlayer _simPlayer;

        Entity _baseEntity;

        public IEnumerator<TargetingInfo> Prepare(Entity prepEntity)
        {
            _baseEntity = prepEntity;

            //get necessary components from player
            var animator = prepEntity.GetComponent<SpriteAnimator>();

            //add entity selector if needed
            if (ConfirmType == ActionConfirmType.SelectEnemy)
            {
                var mouseCursor = Game1.Scene.FindEntity(MouseCursor.EntityName);
                if (mouseCursor != null)
                {
                    _entitySelector = mouseCursor.AddComponent(new EntitySelector(PhysicsLayers.Selectable));
                }
            }

            //play prep animation
            AnimatedSpriteHelper.PlayAnimation(ref animator, ChargeAnimation);

            //add sim player if necessary
            if (ShowSim)
                _simPlayer = Game1.Scene.AddEntity(new SimPlayer(SimType, SimAnimation, _selectedPosition));

            //wait for confirmation
            while (!TryConfirm())
                yield return null;

            //remove entity selector if necessary
            _entitySelector?.RemoveComponent(_entitySelector);
            _entitySelector = null;

            //remove sim player if necessary
            _simPlayer?.Destroy();
            _simPlayer = null;

            IsPrepared = true;

            yield return new TargetingInfo() { Position = new Vector2(50, 50) };
        }

        bool ValidateAim(Vector2 targetPosition, Entity prepEntity, out Vector2 finalPosition)
        {
            finalPosition = targetPosition;

            //get direction between aiming position and player
            var dir = finalPosition - prepEntity.Position;
            if (dir != Vector2.Zero)
                dir.Normalize();

            //get distance between aiming position and player
            var dist = Vector2.Distance(finalPosition, prepEntity.Position);

            //handle min and max radius
            if (dist < MinConfirmDistance || dist > MaxConfirmDistance)
            {
                if (SnapToEdgeIfOutsideRadius)
                {
                    if (dist < MinConfirmDistance)
                        finalPosition = prepEntity.Position + (dir * MinConfirmDistance);
                    else if (dist > MaxConfirmDistance)
                        finalPosition = prepEntity.Position + (dir * MaxConfirmDistance);
                }
                else
                    return false;
            }

            if (!CanPassThroughWalls)
            {
                //raycast to see if we hit a wall along the path
                var raycast = Physics.Linecast(prepEntity.Position, finalPosition, 1 << PhysicsLayers.Environment);
                if (raycast.Collider != null)
                {
                    switch (WallBehavior)
                    {
                        case PlayerActionWallBehavior.Disable:
                            return false;
                        case PlayerActionWallBehavior.Shorten:
                            finalPosition = raycast.Point + (dir * -1 * 8);
                            //check distance again. if shorter than min, there is no valid position. return false
                            if (Vector2.Distance(finalPosition, prepEntity.Position) < MinConfirmDistance)
                                return false;
                            break;
                    }
                }
            }

            if (!CanAimInsideWalls)
            {
                if (!TiledHelper.ValidatePosition(Game1.Scene, finalPosition))
                    return false;
            }

            return true;
        }

        bool TryConfirm()
        {
            var desiredPos = Game1.Scene.Camera.MouseToWorldPoint();
            if (ValidateAim(desiredPos, _baseEntity, out var finalPosition))
            {
                _selectedPosition = finalPosition;

                _simPlayer?.SetEnabled(true);
                _simPlayer?.UpdateTarget(finalPosition);

                if (Controls.Instance.Melee.IsPressed)
                    return true;
            }
            else
            {
                _simPlayer?.SetEnabled(false);
            }

            return false;
        }

        public void Reset()
        {
            _entitySelector?.RemoveComponent(_entitySelector);
            _entitySelector = null;

            _selectedEntity = null;

            _simPlayer?.Destroy();
            _simPlayer = null;
        }

        public Vector2 GetFinalPosition()
        {
            return _selectedPosition;
        }

        #region BASIC ACTION

        public override IEnumerator Execute()
        {
            var apComponent = Context.GetComponent<ApComponent>();
            apComponent.ActionPoints -= ApCost;

            _baseEntity = Context;

            return base.Execute();
        }

        protected override TargetingInfo GetTargetingInfo()
        {
            var player = Context as Player;

            return new TargetingInfo()
            {
                Position = _selectedPosition,
                Direction = _selectedDirection,
                TargetEntity = _selectedEntity
            };
        }

        public override void LoadAnimations(ref SpriteAnimator animator)
        {
            base.LoadAnimations(ref animator);

            AnimatedSpriteHelper.LoadAnimationsGlobal(ref animator, ChargeAnimation);
        }

        #endregion

        #region ICLONEABLE

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    public enum ActionConfirmType
    {
        Direction,
        Position,
        ExactPosition,
        Any, //clicking anywhere will confirm the action
        SelectEnemy, //must click on an enemy and select it
        FloorPosition, //must click somewhere where the player will be on a floor tile
        ClosestFloorPosition //must be on a floor tile. if cursor isn't on one, we'll take the closest floor in that direction
    }

    public enum SimPlayerType
    {
        AttachToCursor,
        Static
    }

    public enum PlayerActionWallBehavior
    {
        Allow,
        Shorten,
        Disable
    }
}
