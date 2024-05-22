using Microsoft.Xna.Framework;
using Nez;
using Nez.Persistence;
using Nez.Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Helpers;
using Threadlock.Models;
using Threadlock.StaticData;

namespace Threadlock.Components.TiledComponents
{
    public class NPC : TiledComponent
    {
        const float _interactCooldown = 1f;

        public string Name;

        SpriteAnimator _animator;
        BoxCollider _collider;
        OriginComponent _origin;
        Interactable _interactable;

        Dictionary<string, Conversation> _dialogueDictionary;

        #region LIFECYCLE

        public override void Initialize()
        {
            base.Initialize();

            if (TmxObject.Properties == null)
                return;

            if (TmxObject.Properties.TryGetValue("Name", out var name))
                Name = name;

            _animator = Entity.AddComponent(new SpriteAnimator());
            _animator.SetRenderLayer(RenderLayers.YSort);

            //read animation file
            AnimatedSpriteHelper.ParseAnimationFile($"Content/Textures/Characters", $"{name}SpriteConfig", ref _animator);

            var json = File.ReadAllText($"Content/Textures/Characters/{name}SpriteConfig.json");
            var export = Json.FromJson<AsepriteExport>(json);

            //handle slices in the sprite
            foreach (var slice in export.Meta.Slices)
            {
                //create collider
                if (slice.Name == "Collider")
                {
                    var key = slice.Keys.FirstOrDefault();
                    if (key != null)
                    {
                        var colliderRect = key.Bounds.ToRectangle();
                        _collider = Entity.AddComponent(new BoxCollider(colliderRect.X - (_animator.Width / 2), colliderRect.Y - (_animator.Height / 2), colliderRect.Width, colliderRect.Height));
                        Flags.SetFlagExclusive(ref _collider.PhysicsLayer, PhysicsLayers.Environment);

                        _origin = Entity.AddComponent(new OriginComponent(_collider));

                        var diff = Entity.Position - _origin.Origin;
                        Entity.Position += diff;

                        _interactable = Entity.AddComponent(new Interactable(_collider));
                        _interactable.OnInteracted += OnInteracted;
                    }
                }
            }

            var dialogueJson = File.ReadAllText($"Content/Dialogue/{Name}.json");
            _dialogueDictionary = Json.FromJson<Dictionary<string, Conversation>>(dialogueJson);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_animator.Animations.ContainsKey("Idle"))
                _animator.Play("Idle");
        }

        #endregion

        void OnInteracted()
        {
            Game1.StartCoroutine(HandleInteraction());
        }

        IEnumerator HandleInteraction()
        {
            //disable interactable
            _interactable.SetEnabled(false);

            //pick a valid conversation
            var validConversations = new List<Conversation>();
            foreach (var conversation in  _dialogueDictionary?.Values)
            {
                if (conversation.Requirements == null || conversation.Requirements.Count == 0)
                {
                    validConversations.Add(conversation);
                    continue;
                }
                else
                {
                    var allRequirementsMet = true;
                    foreach (var requirement in conversation.Requirements)
                    {
                        if (!DialogueHelper.IsRequirementMet(requirement))
                        {
                            allRequirementsMet = false;
                            break;
                        }
                    }

                    if (allRequirementsMet)
                        validConversations.Add(conversation);
                }
            }


            if (validConversations.Count > 0)
            {
                var highestPriority = validConversations.Max(c => c.Priority);

                var highestPriorityConversations = validConversations.Where(c => c.Priority == highestPriority).ToList();
                var chosenConversation = highestPriorityConversations.RandomItem();

                yield return Game1.UIManager.ShowTextboxText(chosenConversation.Dialogue);
            }
            else
            {
                yield return Game1.UIManager.ShowTextboxText("Big Papa Pickle.");
            }

            Game1.Schedule(_interactCooldown, timer => _interactable.SetEnabled(true));
        }
    }
}
