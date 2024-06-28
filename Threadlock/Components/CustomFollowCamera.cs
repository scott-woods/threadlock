using Nez;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Nez.UI;

namespace Threadlock.Components
{
    public class CustomFollowCamera : Component, IUpdatable
    {
        float? _minX { get => Min != null ? Min?.X + (_camera.Bounds.Width / 2) : null; }
        float? _minY { get => Min != null ? Min?.Y + (_camera.Bounds.Height / 2) : null; }
        float? _maxX { get => Max != null ? Max?.X - (_camera.Bounds.Width / 2) : null; }
        float? _maxY { get => Max != null ? Max?.Y - (_camera.Bounds.Height / 2) : null; }

        Vector2 _actualPosition;
        public Vector2 ActualPosition
        {
            get => _actualPosition;
            set
            {
                var x = Math.Clamp(value.X, _minX ?? float.MinValue, _maxX ?? float.MaxValue);
                var y = Math.Clamp(value.Y, _minY ?? float.MinValue, _maxY ?? float.MaxValue);
                _actualPosition = new Vector2(x, y);
            }
        }
        public Vector2 RoundedPosition;
        Vector2? _min;
        public Vector2? Min
        {
            get => _min;
            set
            {
                _min = value;
                UpdateBounds();
            }
        }
        Vector2? _max;
        public Vector2? Max
        {
            get => _max;
            set
            {
                _max = value;
                UpdateBounds();
            }
        }

        float _lerpFactor = 15f;
        float _minDistance = .05f;
        Entity _targetEntity;
        Camera _camera;

        public CustomFollowCamera(Entity targetEntity)
        {
            _targetEntity = targetEntity;
        }

        public CustomFollowCamera(Entity targetEntity, Vector2 min, Vector2 max)
        {
            _targetEntity = targetEntity;

            Min = min;
            Max = max;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            if (_camera == null)
                _camera = Entity.Scene.Camera;

            UpdateBounds();

            ActualPosition = _targetEntity.Position;
            RoundedPosition = _targetEntity.Position;
        }

        public void Update()
        {
            //snap into position if within certain range
            if (Vector2.Distance(ActualPosition, _targetEntity.Position) < _minDistance)
            {
                _camera.Position = _targetEntity.Position;
                ActualPosition = _camera.Position;
                RoundedPosition = _camera.Position;
                return;
            }

            ActualPosition = Vector2.Lerp(ActualPosition, _targetEntity.Position, Time.DeltaTime * _lerpFactor);
            RoundedPosition = new Vector2((int)ActualPosition.X, (int)ActualPosition.Y);

            _camera.Position = ActualPosition;
        }

        public void SetFollowTarget(Entity targetEntity)
        {
            _targetEntity = targetEntity;
        }

        void UpdateBounds()
        {
            if (Min != null && Max != null && _camera != null)
            {
                var updatedMin = Min.Value;
                var updatedMax = Max.Value;

                var xDiff = Max.Value.X - Min.Value.X;
                if (xDiff < _camera.Bounds.Width)
                {
                    var amountToAdd = _camera.Bounds.Width - xDiff;

                    updatedMin.X -= (amountToAdd / 2);
                    updatedMax.X += (amountToAdd / 2);
                }

                var yDiff = Max.Value.Y - Min.Value.Y;
                if (yDiff < _camera.Bounds.Height)
                {
                    var amountToAdd = _camera.Bounds.Height - yDiff;

                    updatedMin.Y -= (amountToAdd / 2);
                    updatedMax.Y += (amountToAdd / 2);
                }

                if (updatedMin != Min)
                    _min = updatedMin;
                if (updatedMax != Max)
                    _max = updatedMax;
            }
        }
    }
}
