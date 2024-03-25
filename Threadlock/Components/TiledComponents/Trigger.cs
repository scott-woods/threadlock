using Nez.Tiled;
using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.StaticData;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Threadlock.Components.TiledComponents
{
    public class Trigger : TiledComponent, IUpdatable
    {
        public Collider Collider { get; private set; }
        public TriggerType TriggerType { get; private set; } = TriggerType.None;
        public Func<Trigger, IEnumerator> Handler { get; private set; }
        public List<string> Args { get; private set; } = new List<string>();

        public override void Initialize()
        {
            base.Initialize();

            switch (TmxObject.ObjectType)
            {
                case TmxObjectType.Basic:
                case TmxObjectType.Tile:
                    Collider = Entity.AddComponent(new BoxCollider(TmxObject.Width, TmxObject.Height));
                    Collider.SetLocalOffset(new Vector2(TmxObject.Width / 2, TmxObject.Height / 2));
                    break;
                case TmxObjectType.Ellipse:
                    Collider = Entity.AddComponent(new CircleCollider(TmxObject.Width));
                    break;
                case TmxObjectType.Polygon:
                    Collider = Entity.AddComponent(new PolygonCollider(TmxObject.Points));
                    var width = TmxObject.Points.Select(p => p.X).Max() - TmxObject.Points.Select(p => p.X).Min();
                    var height = TmxObject.Points.Select(p => p.Y).Max() - TmxObject.Points.Select(p => p.Y).Min();
                    Collider.SetLocalOffset(new Vector2(width / 2, height / 2));
                    break;
            }

            Collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref Collider.PhysicsLayer, PhysicsLayers.Trigger);
            Flags.SetFlagExclusive(ref Collider.CollidesWithLayers, PhysicsLayers.PlayerCollider);

            if (TmxObject.Properties != null && TmxObject.Properties.Count > 0)
            {
                //get trigger type
                if (TmxObject.Properties.TryGetValue("Type", out string type))
                {
                    if (Enum.TryParse(type, out TriggerType value))
                        TriggerType = value;
                }

                //try to get event handler
                if (TmxObject.Properties.TryGetValue("EventName", out string eventName))
                    if (Events.EventsMap.TryGetValue(eventName, out var handler))
                        Handler = handler;

                //get args
                if (TmxObject.Properties.TryGetValue("Args", out string args))
                    Args = args.Split(' ').ToList();
            }
        }

        public void Update()
        {
            if (TriggerType == TriggerType.Area)
            {
                var colliders = Physics.BoxcastBroadphaseExcludingSelf(Collider, Collider.CollidesWithLayers);
                if (colliders.Count > 0)
                {
                    if (colliders.Any(c =>
                    {
                        return Collider.Shape.CollidesWithShape(c.Shape, out var result);
                    }))
                        Game1.StartCoroutine(HandleTriggered());
                }
            }
            
            //if (TriggerType == TriggerType.Area && Collider.CollidesWithAny(out CollisionResult result))
            //    Game1.StartCoroutine(HandleTriggered());
        }

        public IEnumerator HandleTriggered()
        {
            //yield break;
            //if handler is null, there's nothing to trigger. break here
            if (Handler == null)
                yield break;

            //disable during event so it isn't triggered again
            SetEnabled(false);

            //call handler
            yield return Handler(this);

            //check just in case entity was destroyed during event
            if (Entity != null)
                SetEnabled(true);
        }
    }

    public enum TriggerType
    {
        None,
        Area,
        Interact
    }
}
