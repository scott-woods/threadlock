using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Threadlock.StaticData;
using Threadlock.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.PostProcessors;
using Threadlock.Components;
using Threadlock.Entities;
using Threadlock.Renderers;
using Nez.DeferredLighting;
using Nez.Tiled;
using Threadlock.SceneComponents.Dungenerator;

namespace Threadlock.Scenes
{
    public abstract class BaseScene : Scene, IFinalRenderDelegate
    {
        public virtual Color SceneColor { get => Color.Black; }
        public virtual Color AmbientLightColor { get => new Color(200, 200, 200, 255); }

        readonly List<int> _lightRenderLayers = new List<int>()
        {
            RenderLayers.Back,
            RenderLayers.Walls,
            RenderLayers.YSort,
            RenderLayers.Front,
            RenderLayers.AboveFront,
            RenderLayers.Shadow
        };

        ScreenSpaceRenderer _uiRenderer;
        ScreenSpaceRenderer _cursorRenderer;
        RenderLayerExcludeRenderer _gameRenderer;
        YSortDeferredLightingRenderer _deferredLightingRenderer;

        public override void Initialize()
        {
            base.Initialize();

            ClearColor = SceneColor;

            //add cursor
            var mouseCursor = AddEntity(new MouseCursor());

            //handle camera
            Camera.Entity.SetUpdateOrder(int.MaxValue);
            var scale = Game1.ResolutionManager.ResolutionScale.X;
            Camera.MaximumZoom = (scale * 2) - 1;
            Camera.Zoom = .5f;
            var cameraShake = Camera.AddComponent(new CameraShake());
            cameraShake.SetUpdateOrder(int.MaxValue);

            //_gameRenderer = new YSortRenderer(0, RenderLayers.ScreenSpaceRenderLayer, RenderLayers.Cursor, RenderLayers.Light);
            //var size = Game1.ResolutionManager.DesignResolution;
            //var mainRenderTarget = new RenderTexture(size.X, size.Y);
            ////_gameRenderer.RenderTexture = mainRenderTarget;
            //AddRenderer(_gameRenderer);

            _uiRenderer = new ScreenSpaceRenderer(1, RenderLayers.ScreenSpaceRenderLayer);
            var uiSize = Game1.ResolutionManager.UIResolution;
            var uiRenderTarget = new RenderTexture(uiSize.X, uiSize.Y);
            //var uiRenderTarget = new RenderTexture(Game1.ResolutionManager.DesignResolution.X, Game1.ResolutionManager.DesignResolution.Y);
            uiRenderTarget.ResizeBehavior = RenderTexture.RenderTextureResizeBehavior.None;
            _uiRenderer.RenderTexture = uiRenderTarget;
            AddRenderer(_uiRenderer);

            _cursorRenderer = new ScreenSpaceRenderer(2, RenderLayers.Cursor);
            var cursorRenderTarget = new RenderTexture(Game1.ResolutionManager.DesignResolution.X, Game1.ResolutionManager.DesignResolution.Y);
            cursorRenderTarget.ResizeBehavior = RenderTexture.RenderTextureResizeBehavior.None;
            _cursorRenderer.RenderTexture = cursorRenderTarget;
            AddRenderer(_cursorRenderer);

            //lighting
            _deferredLightingRenderer = AddRenderer(new YSortDeferredLightingRenderer(0, RenderLayers.Light, _lightRenderLayers.ToArray()));
            _deferredLightingRenderer.SetClearColor(SceneColor);
            _deferredLightingRenderer.SetAmbientColor(AmbientLightColor);

            FinalRenderDelegate = this;
        }

        public override void Update()
        {
            base.Update();

            var dungenerator = GetSceneComponent<Dungenerator>();
            if (dungenerator != null && dungenerator.IsGenerationInProgress)
                return;

            List<TmxMap> visitedMaps = new List<TmxMap>();
            List<TmxTileset> visitedTilesets = new List<TmxTileset>();

            var mapRenderers = FindComponentsOfType<TiledMapRenderer>();
            foreach (var renderer in mapRenderers)
            {
                if (visitedMaps.Contains(renderer.TiledMap))
                    continue;

                visitedMaps.Add(renderer.TiledMap);

                foreach (var tileset in renderer.TiledMap.Tilesets)
                {
                    if (visitedTilesets.Contains(tileset))
                        continue;

                    visitedTilesets.Add(tileset);

                    tileset.Update();
                }
            }
        }

        #region FINAL RENDER DELEGATE

        private Scene _scene;

        public void OnAddedToScene(Scene scene) => _scene = scene;

        public void OnSceneBackBufferSizeChanged(int newWidth, int newHeight)
        {
            //_gameRenderer.OnSceneBackBufferSizeChanged(newWidth, newHeight);
            _uiRenderer.OnSceneBackBufferSizeChanged(newWidth, newHeight);
            _cursorRenderer.OnSceneBackBufferSizeChanged(newWidth, newHeight);
            //_cursorRenderer.RenderTexture.Resize(Screen.Width, Screen.Height);
            _deferredLightingRenderer.OnSceneBackBufferSizeChanged(newWidth, newHeight);
        }

        public void HandleFinalRender(RenderTarget2D finalRenderTarget, Color letterboxColor, RenderTarget2D source,
                                      Rectangle finalRenderDestinationRect, SamplerState samplerState)
        {
            Core.GraphicsDevice.SetRenderTarget(null);
            Core.GraphicsDevice.Clear(ClearColor);

            Vector2 offset = Vector2.Zero;
            if (Camera.Entity.TryGetComponent<CustomFollowCamera>(out var cam))
            {
                var scale = new Vector2(finalRenderDestinationRect.Width, finalRenderDestinationRect.Height) / Game1.ResolutionManager.DesignResolution.ToVector2();
                offset = scale * (cam.ActualPosition - cam.RoundedPosition - (new Vector2(4, 4) / 2));
            }

            finalRenderDestinationRect.X -= (int)offset.X;
            finalRenderDestinationRect.Y -= (int)offset.Y;

            Graphics.Instance.Batcher.Begin(BlendState.AlphaBlend, samplerState, DepthStencilState.None, RasterizerState.CullNone);

            Graphics.Instance.Batcher.Draw(source, finalRenderDestinationRect, Color.White);

            ////draw game
            //Graphics.Instance.Batcher.Draw(_gameRenderer.RenderTexture, finalRenderDestinationRect, Color.White);

            ////Graphics.Instance.Batcher.Draw(_deferredLightingRenderer.RenderTexture, finalRenderDestinationRect, Color.White);

            //render ui
            var uiRect = new Rectangle(0, 0, finalRenderDestinationRect.Width, finalRenderDestinationRect.Height);
            Graphics.Instance.Batcher.Draw(_uiRenderer.RenderTexture, uiRect, Color.White);

            //render cursor
            Graphics.Instance.Batcher.Draw(_cursorRenderer.RenderTexture, uiRect, Color.White);

            Graphics.Instance.Batcher.End();
        }

        #endregion
    }
}
