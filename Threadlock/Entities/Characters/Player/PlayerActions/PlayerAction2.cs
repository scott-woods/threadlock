using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components;
using Threadlock.Components.EnemyActions;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.SaveData;
using Threadlock.StaticData;

namespace Threadlock.Entities.Characters.Player.PlayerActions
{
    public class PlayerAction2 : ICloneable
    {
        //stats
        public string Name;
        public string Description;
        public int ApCost;

        //icon
        public string IconName;

        //charge
        public string ChargeAnimation;

        //execute
        public string ExecuteAnimation;
        public bool WaitForExecuteAnimation;
        public float ExecuteDuration;
        public PlayerActionMovementConfig ExecutionMovement;
        public string ExecutionSound;

        //projectiles
        public List<PlayerProjectile> Projectiles = new List<PlayerProjectile>();

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

        //data saved after prep confirmation
        Vector2 _selectedPosition;
        Vector2 _selectedDirection
        {
            get
            {
                var dir = _selectedPosition - Player.Instance.Position;
                dir.Normalize();
                return dir;
            }
        }
        Entity _selectedEntity;

        EntitySelector _entitySelector;
        SimPlayer _simPlayer;

        public void LoadAnimations()
        {
            var player = Player.Instance;
            var animator = player.GetComponent<SpriteAnimator>();

            AnimatedSpriteHelper.LoadAnimationsGlobal(ref animator, ChargeAnimation, ExecuteAnimation);
        }

        public IEnumerator Prepare()
        {
            //grab player instance
            var player = Player.Instance;

            //get necessary components from player
            var animator = player.GetComponent<SpriteAnimator>();

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
        }

        bool ValidateAim(Vector2 targetPosition, Player player, out Vector2 finalPosition)
        {
            finalPosition = targetPosition;

            //get direction between aiming position and player
            var dir = finalPosition - player.Position;
            if (dir != Vector2.Zero)
                dir.Normalize();

            //get distance between aiming position and player
            var dist = Vector2.Distance(finalPosition, player.Position);

            //handle min and max radius
            if (dist < MinConfirmDistance || dist > MaxConfirmDistance)
            {
                if (SnapToEdgeIfOutsideRadius)
                {
                    if (dist < MinConfirmDistance)
                        finalPosition = player.Position + (dir * MinConfirmDistance);
                    else if (dist > MaxConfirmDistance)
                        finalPosition = player.Position + (dir * MaxConfirmDistance);
                }
                else
                    return false;
            }

            if (!CanPassThroughWalls)
            {
                //raycast to see if we hit a wall along the path
                var raycast = Physics.Linecast(player.Position, finalPosition, 1 << PhysicsLayers.Environment);
                if (raycast.Collider != null)
                {
                    switch (WallBehavior)
                    {
                        case PlayerActionWallBehavior.Disable:
                            return false;
                        case PlayerActionWallBehavior.Shorten:
                            finalPosition = raycast.Point + (dir * -1 * 8);
                            //check distance again. if shorter than min, there is no valid position. return false
                            if (Vector2.Distance(finalPosition, player.Position) < MinConfirmDistance)
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
            if (ValidateAim(desiredPos, Player.Instance, out var finalPosition))
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

        public IEnumerator Execute()
        {
            var player = Player.Instance;
            var animator = player.GetComponent<SpriteAnimator>();
            var velocityComponent = player.GetComponent<VelocityComponent>();

            //handle projectiles
            foreach (var playerProjectile in Projectiles)
            {
                if (Projectiles2.TryGetProjectile(playerProjectile.ProjectileName, out var projectileConfig))
                {
                    //create projectile entity
                    var projectileEntity = ProjectileEntity.CreateProjectileEntity(projectileConfig, _selectedDirection);

                    //determine if we should start from enemy or target pos
                    Vector2 pos = Vector2.Zero;
                    if (!projectileConfig.AttachToOwner)
                        pos = Player.Instance.Position;
                    else
                        projectileEntity.SetParent(Player.Instance);

                    //add entity offset
                    pos += playerProjectile.EntityOffset;

                    //add offset to starting pos
                    pos += (_selectedDirection * playerProjectile.OffsetInDirection);

                    //set the projectile position
                    projectileEntity.SetPosition(pos);

                    //handle rotation
                    if (projectileConfig.ShouldRotate)
                    {
                        var rotationRadians = DirectionHelper.GetClampedAngle(_selectedDirection, projectileConfig.MaxRotation);
                        projectileEntity.SetRotation(rotationRadians);
                    }

                    Game1.Scene.AddEntity(projectileEntity);
                }
            }

            //play execute animation
            AnimatedSpriteHelper.PlayAnimation(ref animator, ExecuteAnimation);
            var executeAnimDuration = WaitForExecuteAnimation ? AnimatedSpriteHelper.GetAnimationDuration(animator) : ExecuteDuration;

            //play execution sound
            Game1.AudioManager.PlaySound(ExecutionSound);

            //prepare execution movement
            float executionMovementSpeed = 0;
            if (ExecutionMovement != null)
            {
                switch (ExecutionMovement.MovementType)
                {
                    case PlayerActionMovementType.InDirection:
                        executionMovementSpeed = ExecutionMovement.Speed;
                        break;
                    case PlayerActionMovementType.ToTarget:
                        var dist = Vector2.Distance(_selectedPosition, player.Position);
                        var time = ExecutionMovement.UseAnimationDuration ? executeAnimDuration : ExecutionMovement.Duration;
                        executionMovementSpeed = dist / time;
                        break;
                    case PlayerActionMovementType.Instant:
                        player.Position = _selectedPosition;
                        break;
                }
            }

            //wait for execution phase to finish
            var executeTimer = 0f;
            while ((!WaitForExecuteAnimation && (executeTimer < ExecuteDuration)) || (WaitForExecuteAnimation && (animator.AnimationState != SpriteAnimator.State.Completed)))
            {
                //increment timer
                executeTimer += Time.DeltaTime;

                if (ExecutionMovement != null && ExecutionMovement.MovementType != PlayerActionMovementType.Instant)
                {
                    //only move if still within movement duration, or set to move during entire animation
                    if (ExecutionMovement.UseAnimationDuration || executeTimer < ExecutionMovement.Duration)
                    {
                        velocityComponent.Move(_selectedDirection, executionMovementSpeed, true);
                    }
                }

                yield return null;
            }
        }

        public void Reset()
        {
            _entitySelector?.RemoveComponent(_entitySelector);
            _entitySelector = null;

            _selectedEntity = null;

            _simPlayer?.Destroy();
            _simPlayer = null;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
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
    }

    public enum PlayerActionWallBehavior
    {
        Allow,
        Shorten,
        Disable
    }
}
