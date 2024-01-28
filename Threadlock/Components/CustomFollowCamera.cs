using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Threadlock.Components
{
    public class CustomFollowCamera : Component, IUpdatable
    {
        public Vector2 ActualPosition;
        public Vector2 RoundedPosition;

        Entity _targetEntity;
        Camera _camera;

        public CustomFollowCamera(Entity targetEntity)
        {
            _targetEntity = targetEntity;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_camera == null)
                _camera = Entity.Scene.Camera;

            ActualPosition = _targetEntity.Position;
            RoundedPosition = _targetEntity.Position;
        }

        public void Update()
        {
            if (Vector2.Distance(ActualPosition, _targetEntity.Position) < .05f)
            {
                _camera.Position = _targetEntity.Position;
                ActualPosition = _camera.Position;
                RoundedPosition = _camera.Position;
                return;
            }

            ActualPosition = Vector2.Lerp(ActualPosition, _targetEntity.Position, Time.DeltaTime * 10);
            RoundedPosition = new Vector2((int)ActualPosition.X, (int)ActualPosition.Y);

            _camera.Position = RoundedPosition;
        }
    }
}
