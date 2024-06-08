using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.BehaviorTrees;
using Nez.Persistence;
using Nez.Sprites;
using Nez.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Components.Hitboxes;
using Threadlock.Entities;
using Threadlock.Entities.Characters.Enemies;
using Threadlock.Helpers;
using Threadlock.StaticData;
using static Nez.Content.Audio;

namespace Threadlock.Components.EnemyActions
{
    public class ScriptedEnemyAction
    {
        public List<ScriptAction> ScriptActions;
        public List<Requirement> Requirements;
        public float Cooldown;
        public int Priority;

        [JsonExclude]
        public bool IsActive;

        [JsonExclude]
        public Enemy Enemy;

        ICoroutine _coroutine;
        ITimer _cooldownTimer;
        bool _isOnCooldown;

        /// <summary>
        /// determine if we can execute this ability
        /// </summary>
        /// <returns></returns>
        public bool CanExecute()
        {
            if (_isOnCooldown)
                return false;

            if (Requirements == null)
                return true;

            return Requirements.All(r => r.IsMet(Enemy));
        }

        /// <summary>
        /// start executing this move
        /// </summary>
        /// <returns></returns>
        public IEnumerator ExecuteAttack()
        {
            //update state
            IsActive = true;

            //handle each action in the script
            foreach (var action in ScriptActions)
            {
                //handle specific json without manual parsing
                if (!string.IsNullOrWhiteSpace(action.Type))
                {
                    //get method
                    var methodName = action.Type;
                    var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method == null)
                        yield break;

                    var parameterInfo = method.GetParameters().FirstOrDefault();
                    if (parameterInfo == null)
                    {
                        //method takes no parameters
                        _coroutine = Game1.StartCoroutine((IEnumerator)method.Invoke(this, null));
                        yield return _coroutine;
                    }
                    else
                    {
                        //get param type
                        var parameterType = parameterInfo.ParameterType;

                        //convert to json
                        var paramsJson = Json.ToJson(action.Params, true);

                        //make generic method to deserialize json
                        var deserializeMethod = typeof(Json).GetMethods().Where(m => m.Name == "FromJson" && m.IsGenericMethodDefinition).FirstOrDefault();
                        var genericMethod = deserializeMethod.MakeGenericMethod(parameterType);

                        //get params
                        var parameters = genericMethod.Invoke(null, new object[] { paramsJson, null });

                        _coroutine = Game1.StartCoroutine((IEnumerator)method.Invoke(this, new[] { parameters }));
                        yield return _coroutine;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(action.Command)) //handle manual string command we can parse
                {
                    yield return ExecuteCommandString(action.Command);
                }
            }

            //null out the coroutine
            _coroutine = null;

            //start cooldown timer
            if (Cooldown > 0)
            {
                _isOnCooldown = true;
                _cooldownTimer = Game1.Schedule(Cooldown, timer => _isOnCooldown = false);
            }

            //update state
            IsActive = false;
        }

        public void Abort()
        {
            _coroutine?.Stop();
            _coroutine = null;

            IsActive = false;
        }

        IEnumerator ExecuteCommandString(string action)
        {
            //parse parameters
            var paramsDictionary = ParseParameters(action);

            //get method name
            var methodName = action.Substring(0, action.IndexOf('('));

            //retrieve method
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                var parameterInfo = method.GetParameters().FirstOrDefault();
                if (parameterInfo == null)
                {
                    //method takes no parameters
                    var coroutine = Game1.StartCoroutine((IEnumerator)method.Invoke(this, null));
                    yield return coroutine;
                }
                else
                {
                    var parameterType = parameterInfo.ParameterType;

                    //convert to json
                    var paramsJson = Json.ToJson(paramsDictionary, true);

                    //make generic method to deserialize json
                    var deserializeMethod = typeof(Json).GetMethods().Where(m => m.Name == "FromJson" && m.IsGenericMethodDefinition).FirstOrDefault();
                    var genericMethod = deserializeMethod.MakeGenericMethod(parameterType);

                    //get params
                    var parameters = genericMethod.Invoke(null, new object[] { paramsJson, null });

                    var coroutine = Game1.StartCoroutine((IEnumerator)method.Invoke(this, new[] { parameters }));
                    yield return coroutine;
                }
            }
        }

        #region PARSING

        Dictionary<string, object> ParseParameters(string action)
        {
            //get parameters substring
            var openIndex = action.IndexOf('(');
            var closeIndex = action.LastIndexOf(')');
            var parametersString = action.Substring(openIndex + 1, closeIndex - openIndex - 1);

            //init params dictionary
            var parameters = new Dictionary<string, object>();

            var currentKey = "";
            var currentValue = "";
            var inKey = true;
            var nestedLevel = 0;

            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];

                //we've finished reading the key once we hit an =
                if (c == '=' && nestedLevel == 0)
                {
                    inKey = false;
                    continue;
                }

                //comma indicates we've finished reading a key value pair
                if (c == ',' && nestedLevel == 0)
                {
                    parameters[currentKey.Trim()] = ParseValue(currentValue.Trim());
                    currentKey = "";
                    currentValue = "";
                    inKey = true;
                    continue;
                }

                //opening brackets indicates a nested section
                if (new char[] { '{', '[' }.Contains(c))
                    nestedLevel++;

                //closed brackets closes a nested section
                if (new char[] { '}', ']' }.Contains(c))
                    nestedLevel--;

                //handle key or value
                if (inKey)
                    currentKey += c;
                else
                    currentValue += c;
            }

            //add the last parameter
            if (!string.IsNullOrWhiteSpace(currentKey) && !string.IsNullOrWhiteSpace(currentValue))
                parameters[currentKey.Trim()] = ParseValue(currentValue.Trim());

            return parameters;
        }

        object ParseValue(string value)
        {
            //handle objects
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                value = value.Substring(1, value.Length - 2);
                var nestedParams = ParseParameters($"dummy({value})");
                return nestedParams;
            }

            //handle lists
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                //init list of objects
                var list = new List<object>();

                //get value substring
                value = value.Substring(1, value.Length - 2);

                //loop through characters of value string
                var itemBuilder = new StringBuilder();
                var nestedLevel = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];

                    //handle nested elements
                    if (new char[] { '{', '[' }.Contains(c))
                        nestedLevel++;
                    if (new char[] { '}', ']' }.Contains(c))
                        nestedLevel--;

                    //comma with no nesting means we're at the end of this object
                    if (c == ',' && nestedLevel == 0)
                    {
                        list.Add(ParseValue(itemBuilder.ToString().Trim()));
                        itemBuilder.Clear();
                        continue;
                    }

                    itemBuilder.Append(c);
                }

                //handle the last item
                if (itemBuilder.Length > 0)
                    list.Add(ParseValue(itemBuilder.ToString().Trim()));

                return list;
            }

            //handle strings
            if (value.StartsWith("'") && value.EndsWith("'"))
            {
                return value.Substring(1, value.Length - 2);
            }

            //handle ints
            if (int.TryParse(value, out int intValue))
                return intValue;

            //handle floats
            if (float.TryParse(value, out float floatValue))
                return floatValue;

            //handle bools
            if (bool.TryParse(value, out bool boolValue))
                return boolValue;

            return value;
        }

        #endregion

        #region COROUTINES

        IEnumerator MeleeAttack(MeleeAttackParams parameters)
        {
            //create hitbox
            Collider hitbox = null;
            if (parameters.Hitbox.Size != Vector2.Zero)
                hitbox = new BoxHitbox(parameters.Hitbox.Damage, parameters.Hitbox.Size.X, parameters.Hitbox.Size.Y);
            else if (parameters.Hitbox.Radius != 0)
                hitbox = new CircleHitbox(parameters.Hitbox.Damage, parameters.Hitbox.Radius);
            else if (parameters.Hitbox.Points != null)
                hitbox = new PolygonHitbox(parameters.Hitbox.Damage, parameters.Hitbox.Points.ToArray());
            else if (!parameters.Hitbox.Rect.IsEmpty)
                hitbox = new BoxHitbox(parameters.Hitbox.Damage, parameters.Hitbox.Rect);

            //handle hitbox physics layers
            Flags.SetFlagExclusive(ref hitbox.PhysicsLayer, PhysicsLayers.EnemyHitbox);
            Flags.SetFlagExclusive(ref hitbox.CollidesWithLayers, PhysicsLayers.PlayerHurtbox);

            //create hitbox entity
            var hitboxEntity = Game1.Scene.CreateEntity("enemy-hitbox");
            hitboxEntity.AddComponent(hitbox);
            hitboxEntity.SetParent(Enemy);
            hitboxEntity.SetLocalPosition(parameters.Hitbox.Offset);
            hitbox.SetLocalOffset(parameters.Hitbox.LocalOffset);

            //make sure hitbox is disabled to start
            hitbox.SetEnabled(false);

            //handle animation
            var animationName = parameters.Animation.Name;
            var hitboxActiveFrames = parameters.Hitbox.ActiveFrames;
            //var animationName = parameters.TryGetValue("animationName", out var animationNameObj) ? animationNameObj.ToString() : "";
            //var hitboxActiveFrames = parameters.TryGetValue("hitboxActiveFrames", out var hitboxActiveFramesObj) && hitboxActiveFramesObj is List<object> activeFramesList ? activeFramesList.Select(x => Convert.ToInt32(x)).ToList() : new List<int>();

            if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                if (animator.Animations.ContainsKey(animationName))
                {
                    //handle direction
                    var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);

                    var movementDir = GetMovementDirection(parameters.Movement);

                    //rotate hitbox if necessary
                    if (parameters.Hitbox.Rotate)
                        hitboxEntity.SetRotation((float)Math.Atan2(dir.Y, dir.X));

                    //if (hitbox.LocalOffset.Y != 0 && (Math.Sign(dir.X) != Math.Sign(hitbox.LocalOffset.Y)))
                    //    hitbox.SetLocalOffset(new Vector2(hitbox.LocalOffset.X, hitbox.LocalOffset.Y * -1));

                    //play animation
                    var actualName = animationName;
                    if (parameters.Animation.Frames != null)
                    {
                        actualName = "Temp";
                        animator.AddAnimation("Temp", animator.Animations[animationName].Sprites.Where((s, i) => parameters.Animation.Frames.Contains(i)).ToArray());
                        animator.Play("Temp", SpriteAnimator.LoopMode.Once);
                    }
                    else
                        animator.Play(animationName, SpriteAnimator.LoopMode.Once);
                    
                    //handle what we doing while animation is playing
                    var currentFrame = -1;
                    var timer = 0f;
                    while (animator.CurrentAnimationName == actualName && animator.AnimationState != SpriteAnimator.State.Completed)
                    {
                        HandleAnimation(parameters.Animation, parameters.Movement, animator, movementDir, ref timer, ref currentFrame);

                        //enable or disable hitbox based on frame
                        hitbox.SetEnabled(hitboxActiveFrames.Contains(animator.CurrentFrame));

                        yield return null;
                    }

                    hitbox.SetEnabled(false);

                    animator.Animations.Remove("Temp");
                }
            }

            hitbox.SetEnabled(false);
            hitboxEntity.Destroy();
        }

        IEnumerator PlayAnimation(PlayAnimationParams parameters)
        {
            //var animationName = parameters.TryGetValue("name", out var nameObj) ? Convert.ToString(nameObj) : "";
            //var loop = parameters.TryGetValue("loop", out object loopObj) ? Convert.ToBoolean(loopObj) : false;
            //var sounds = parameters.TryGetValue("sounds", out var soundsObj) ? ((List<object>)soundsObj).Cast<Dictionary<string, object>>().ToList() : null;

            var animationName = parameters.Animation.Name;
            var loop = parameters.Animation.Loop;
            var sounds = parameters.Animation.Sounds;

            if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
            {
                if (loop)
                {
                    animator.Play(animationName);

                    if (parameters.Animation.Duration > 0)
                        yield return Coroutine.WaitForSeconds(parameters.Animation.Duration);
                }
                else
                {
                    //play animation
                    var actualName = animationName;
                    if (parameters.Animation.Frames != null)
                    {
                        actualName = "Temp";
                        animator.AddAnimation("Temp", animator.Animations[animationName].Sprites.Where((s, i) => parameters.Animation.Frames.Contains(i)).ToArray());
                        animator.Play("Temp", SpriteAnimator.LoopMode.Once);
                    }
                    else
                        animator.Play(animationName, SpriteAnimator.LoopMode.Once);

                    //get movement dir
                    var movementDir = GetMovementDirection(parameters.Movement);

                    var currentFrame = -1;
                    var timer = 0f;
                    while (animator.CurrentAnimationName == actualName && animator.AnimationState != SpriteAnimator.State.Completed)
                    {
                        HandleAnimation(parameters.Animation, parameters.Movement, animator, movementDir, ref timer, ref currentFrame);

                        yield return null;
                    }
                }
            }
        }

        IEnumerator Wait(WaitParams parameters)
        {
            yield return Coroutine.WaitForSeconds(parameters.Time);
        }

        IEnumerator FireProjectile(FireProjectileParams parameters)
        {
            var dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
            var projectileEntity = new Projectile(parameters.Name, parameters.Speed, parameters.Hitbox.Radius, parameters.Hitbox.Damage, dir, parameters.DestroyOnWall, PhysicsLayers.EnemyHitbox, PhysicsLayers.PlayerHurtbox);
            projectileEntity.SetPosition(Enemy.Position);

            //handle animation
            if (parameters.Animation != null)
            {
                if (Enemy.TryGetComponent<SpriteAnimator>(out var animator))
                {
                    if (animator.Animations.ContainsKey(parameters.Animation.Name))
                    {
                        //play animation and wait for launch frame
                        animator.Play(parameters.Animation.Name, SpriteAnimator.LoopMode.Once);
                        while (animator.CurrentFrame < parameters.LaunchFrame)
                            yield return null;

                        //launch projectile
                        Game1.Scene.AddEntity(projectileEntity);
                        projectileEntity.SetPosition(Enemy.Position);

                        //wait for animation to finish
                        while (animator.CurrentAnimationName == parameters.Animation.Name && animator.AnimationState != SpriteAnimator.State.Completed)
                            yield return null;
                    }
                }
            }
        }

        #endregion

        #region HELPERS

        /// <summary>
        /// should be called every frame an animation is running
        /// </summary>
        /// <param name="config"></param>
        /// <param name="movement"></param>
        /// <param name="animator"></param>
        /// <param name="timer"></param>
        /// <param name="currentFrame"></param>
        void HandleAnimation(AnimationConfig config, Movement movement, SpriteAnimator animator, Vector2 dir, ref float timer, ref int currentFrame)
        {
            //get total animation duration
            var animDuration = AnimatedSpriteHelper.GetAnimationDuration(animator);

            //increment timer
            timer += Time.DeltaTime;

            //check if this is the first time we're on this frame
            if (animator.CurrentFrame != currentFrame)
            {
                //update previous frame
                currentFrame = animator.CurrentFrame;

                //handle sounds
                if (config.Sounds != null && config.Sounds.TryGetValue(animator.CurrentFrame, out var soundsToPlay))
                {
                    foreach (var sound in soundsToPlay)
                        Game1.AudioManager.PlaySound($"Content/Audio/Sounds/{sound}.wav");
                }

                //handle vfx
                if (config.Vfx != null && config.Vfx.Count > 0)
                {
                    var vfxToPlay = config.Vfx.Where(v => v.StartFrame == animator.CurrentFrame);
                    foreach (var vfx in vfxToPlay)
                    {
                        var vfxEntity = Game1.Scene.AddEntity(new VfxEntity(vfx));
                        vfxEntity.SetPosition(Enemy.Position + (dir * vfx.Offset));
                        if (vfx.Rotate)
                            vfxEntity.SetRotation((float)Math.Atan2(dir.Y, dir.X));
                        if (vfx.Flip)
                            vfxEntity.Animator.FlipY = (dir.X < 0);
                    }
                }
            }

            //handle movement
            if (movement != null)
            {
                if (timer < animDuration)
                {
                    var speed = Lerps.Ease(movement.EaseType, movement.Speed, movement.FinalSpeed, timer, animDuration);

                    if (Enemy.TryGetComponent<VelocityComponent>(out var vc))
                        vc.Move(dir, speed, true);
                }
            }
        }

        Vector2 GetMovementDirection(Movement movement)
        {
            Vector2 dir = Vector2.Zero;
            if (movement != null)
            {
                switch (movement.MovementDirection)
                {
                    case MovementDirection.AwayFromTarget:
                        dir = EntityHelper.DirectionToEntity(Enemy.TargetEntity, Enemy);
                        break;
                    case MovementDirection.ToTarget:
                    default:
                        dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);
                        break;
                }
            }
            else
                dir = EntityHelper.DirectionToEntity(Enemy, Enemy.TargetEntity);

            return dir;
        }

        #endregion
    }
}
