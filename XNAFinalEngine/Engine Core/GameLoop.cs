
#region License
/*
Copyright (c) 2008-2012, Laboratorio de Investigaci�n y Desarrollo en Visualizaci�n y Computaci�n Gr�fica - 
                         Departamento de Ciencias e Ingenier�a de la Computaci�n - Universidad Nacional del Sur.
All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

�	Redistributions of source code must retain the above copyright, this list of conditions and the following disclaimer.

�	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer
    in the documentation and/or other materials provided with the distribution.

�	Neither the name of the Universidad Nacional del Sur nor the names of its contributors may be used to endorse or promote products derived
    from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

-----------------------------------------------------------------------------------------------------------------------------------------------
Author: Schneider, Jos� Ignacio (jis@cs.uns.edu.ar)
-----------------------------------------------------------------------------------------------------------------------------------------------

*/
#endregion

#region Using directives
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using XNAFinalEngine.Animations;
using XNAFinalEngine.Assets;
using XNAFinalEngine.Audio;
using XNAFinalEngine.Components;
using XNAFinalEngine.Graphics;
using XNAFinalEngine.Helpers;
using XNAFinalEngine.Input;
using DirectionalLight = XNAFinalEngine.Components.DirectionalLight;
using Model = XNAFinalEngine.Assets.Model;
using RootAnimation = XNAFinalEngine.Components.RootAnimations;
using Texture = XNAFinalEngine.Assets.Texture;
#endregion

namespace XNAFinalEngine.EngineCore
{

    /// <summary>
    /// The XNA Final Engine pipeline is defined here. 
    /// There is no need for user input, all operations are carried automatically.
    /// </summary>
    /// <remarks>
    /// This class seems complex and in the pass I could agree.
    /// However to have the engine pipeline code in one class has its advantages.
    /// Besides, I don�t put everything in here, just the callers to the specifics tasks.
    /// </remarks>
    public static class GameLoop
    {

        #region Structs

        private struct MeshPartToRender
        {
            public Matrix WorldMatrix;
            public Model Model;
            public Matrix[] BoneTransform;
            public Material Material;
            public int MeshIndex;
            public int MeshPart;
        } // MeshPartToRender

        #endregion

        #region Variables

        // They are auxiliary values that helps avoiding garbage.
        private static readonly Vector3[] cornersViewSpace = new Vector3[4];
        private static readonly BoundingFrustum cameraBoundingFrustum = new BoundingFrustum(Matrix.Identity);

        // The system can have 0, 1, 2 and 3 audio lister.
        // These fields are used to avoid garbage in each sound update.
        private static AudioListener oneAudioListener;
        private static readonly AudioListener[] twoAudioListener   = new AudioListener[2];
        private static readonly AudioListener[] threeAudioListener = new AudioListener[3];
        private static readonly AudioListener[] fourAudioListener  = new AudioListener[4];

        // Frustum Culling.
        private static readonly List<ModelRenderer> modelsToRender = new List<ModelRenderer>(100);

        // G-Buffer ordered lists.
        private static readonly List<MeshPartToRender> gbufferSimple = new List<MeshPartToRender>(50);
        private static readonly List<MeshPartToRender> gBufferWithNormalMap = new List<MeshPartToRender>(50);
        private static readonly List<MeshPartToRender> gBufferWithParallax = new List<MeshPartToRender>(50);
        private static readonly List<MeshPartToRender> gBufferSkinnedSimple = new List<MeshPartToRender>(50);
        private static readonly List<MeshPartToRender> gBufferSkinnedWithNormalMap = new List<MeshPartToRender>(50);
        // Opaque ordered lists.
        private static readonly List<MeshPartToRender> opaqueBlinnPhong = new List<MeshPartToRender>(100);
        private static readonly List<MeshPartToRender> opaqueBlinnPhongSkinned = new List<MeshPartToRender>(100);
        private static readonly List<MeshPartToRender> opaqueCarPaint = new List<MeshPartToRender>(10);
        private static readonly List<MeshPartToRender> opaqueConstant = new List<MeshPartToRender>(10);
        
        #endregion

        #region Properties

        /// <summary>
        /// Current Scene.
        /// </summary>
        public static Scene CurrentScene { get; internal set; }

        /// <summary>
        /// This indicates the cameras to render and their order, ignoring the camera component settings.
        /// The camera component provides an interface to set this information but the editor and maybe some user testing will benefit with this functionality.
        /// </summary>
        public static List<Camera> CamerasToRender { get; set; }
        
        /// <summary>
        /// You can avoid the rendering of the main camera to the back buffer.
        /// </summary>
        public static bool RenderMainCameraToScreen { get; set; }

        #endregion

        #region Load Content

        /// <summary>
        /// Called when the application starts and when the device is disposed. 
        /// </summary>
        /// <remarks> 
        /// In application start up this method is called before Begin Run.
        /// </remarks>
        internal static void LoadContent()
        {
            RenderMainCameraToScreen = true;

            // Initialize managers that are related to the device.
            SpriteManager.Initialize();
            LineManager.Initialize();
            SoundManager.Initialize();
            
            // Recreate assets.
            ContentManager.RecreateContentManagers();

            // Call the DeviceDisposed method only when the the device was disposed.
            if (CurrentScene.ContentLoaded)
                CurrentScene.DeviceDisposed();

            // Collect all garbage.
            // Garbage collections are performed in XBOX 360 between 1 Mb of created data.
            // Collecting the garbage gives a little more room to have a little garbage periodically.
            GarbageCollector.CollectGarbage();
        } // LoadContent

        #endregion

        #region Begin Run

        /// <summary>
        /// Called when the application starts. 
        /// </summary>
        /// <remarks> 
        /// In application start up this method is called after Load Content.
        /// </remarks>
        internal static void BeginRun()
        {
            // Initialize managers that are not related to the device.
            InputManager.Initialize();
            MusicManager.Initialize();
            
            // Begin run the scene.
            CurrentScene.Initialize();
            CurrentScene.BeginRun();
            // Start scripts.
            for (int i = 0; i < Script.ScriptList.Count; i++) // The for sentence is needed because the script list could be modified by some script.
            {
                var script = Script.ScriptList[i];
                if (script.assignedToAGameObject && script.IsActive)
                {
                    if (!script.Started)
                        script.Start();
                }
            }

            // Init statistics counting.
            Statistics.InitStatistics();

            // Try to recover memory when the screen size changes.
            // Render Targets relative to the old screen resolution are not longer need it.
            Screen.ScreenSizeChanged += delegate
            {
                RenderTarget.ClearRenderTargetPool();
                RenderTarget.ClearMultpleRenderTargetPool();
            };

            // Collect all garbage.
            // Garbage collections are performed in XBOX 360 between 1 Mb of created data.
            // Collecting the garbage gives a little more room to have a little garbage periodically.
            GarbageCollector.CollectGarbage();

        } // BeginRun

        #endregion

        #region Update

        /// <summary>
        /// Called when the game has determined that game logic needs to be processed.
        /// </summary>
        internal static void Update(GameTime gameTime)
        {
            Time.GameDeltaTime = (float)(gameTime.ElapsedGameTime.TotalSeconds);
            
            #region Managers

            InputManager.Update();
            MusicManager.Update();
            // Update the chronometers that work in game delta time space.
            Chronometer.UpdateGameDeltaTimeChronometers();
            
            #endregion

            #region Root Animation Processing

            for (int i = 0; i < RootAnimation.ComponentPool.Count; i++)
            {
                RootAnimation component = RootAnimation.ComponentPool.Elements[i];
                if (component.IsActive)
                    component.Update();
            }

            #endregion

            #region Model Animation Processing

            // Update the individual active model animation players.
            AnimationManager.UpdateModelAnimationPlayers();

            // Compose the active model animations.
            for (int i = 0; i < ModelAnimations.ComponentPool.Count; i++)
            {
                ModelAnimations component = ModelAnimations.ComponentPool.Elements[i];
                if (component.IsActive)
                    component.Update();
            }

            // The global pose (world space) is generated.
            // However, if no post processing exist (IK, ragdolls, etc.) this stage could be merge with
            // the inverse bind pose multiplication stage in the mesh draw code. And for now the engine will do this.
            // TODO!!

            // The animation players of the individual animations that were finished in the model animation player update,
            // the individual animations that were discarded by the compose operation and the individual animations that were stopped
            // are release to be used by other future individual animations.
            AnimationManager.ReleaseUnusedAnimationPlayers();
            
            #endregion
            
            #region Logic Update
            
            // Update the scene
            CurrentScene.UpdateTasks();
            // Update the scripts
            for (int i = 0; i < Script.ScriptList.Count; i++) // The for sentence is needed because the script list could be modified by some script.
            {
                var script = Script.ScriptList[i];
                if (script.assignedToAGameObject && script.IsActive)
                {
                    if (!script.Started)
                        script.Start();
                    script.Update();
                }
            }
            // Perform the late update of the scene.
            CurrentScene.LateUpdateTasks();
            // Perform the late update of the scripts.
            for (int i = 0; i < Script.ScriptList.Count; i++) // The for sentence is needed because the script list could be modified by some script.
            {
                var script = Script.ScriptList[i];
                if (script.assignedToAGameObject && script.IsActive)
                {
                    if (!script.Started)
                        script.Start();
                    script.LateUpdate();
                }
            }

            #endregion

            #region Sound
         
            // Update the sound's general parameters.
            SoundManager.Update();

            #region Sound Listener

            // Count the active sound listeners.
            int audioListenerCount = 0;
            for (int i = 0; i < SoundListener.ComponentPool.Count; i++)
            {
                if (SoundListener.ComponentPool.Elements[i].IsActive)
                    audioListenerCount++;
            }
            if (audioListenerCount > 4)
                throw new InvalidOperationException("Sound Manager: The maximum number of active audio listener is 4");
            // Update and put into a list.
            int arrayindex = 0;
            for (int i = 0; i < SoundListener.ComponentPool.Count; i++)
            {
                SoundListener component = SoundListener.ComponentPool.Elements[i];
                if (component.IsActive)
                {
                    component.UpdateListenerProperties();
                    if (audioListenerCount == 1)
                        oneAudioListener = component.audioListener;
                    else if (audioListenerCount == 2)
                        twoAudioListener[arrayindex] = component.audioListener;
                    else if (audioListenerCount == 3)
                        threeAudioListener[arrayindex] = component.audioListener;
                    else if (audioListenerCount == 4)
                        fourAudioListener[arrayindex] = component.audioListener;
                    arrayindex++;
                }
            }

            #endregion
            
            #region Emitters
            
            // Update sound emitters.
            if (audioListenerCount <= 1)
                for (int i = 0; i < SoundEmitter.ComponentPool.Count; i++)
                {
                    if (SoundEmitter.ComponentPool.Elements[i].IsActive)
                        SoundEmitter.ComponentPool.Elements[i].Update(oneAudioListener);   
                }
            else
            {
                AudioListener[] audioListeners;
                if (audioListenerCount == 2)
                    audioListeners = twoAudioListener;
                else if (audioListenerCount == 3)   
                    audioListeners = threeAudioListener;
                else
                    audioListeners = fourAudioListener;
                for (int i = 0; i < SoundEmitter.ComponentPool.Count; i++)
                {
                    if (SoundEmitter.ComponentPool.Elements[i].IsActive)
                        SoundEmitter.ComponentPool.Elements[i].Update(audioListeners);   
                }
            }
            
            #endregion

            #endregion

        } // Update

        #endregion

        #region Draw

        /// <summary>
        /// Draw
        /// </summary>
        internal static void Draw(GameTime gameTime)
        {
            // Update frame time
            Time.FrameTime = (float)(gameTime.ElapsedGameTime.TotalSeconds);
            // Reset Frame Statistics
            Statistics.ReserFrameStatistics();
            // Update the chronometers that work in frame time space.
            Chronometer.UpdateFrameTimeChronometers();

            #region Logic Update

            // Perform the pre draw update of the scene and scripts.
            CurrentScene.PreRenderTasks();
            for (int i = 0; i < Script.ScriptList.Count; i++) // The for sentence is needed because the script list could be modified by some script.
            {
                var script = Script.ScriptList[i];
                if (script.assignedToAGameObject && script.IsActive)
                {
                    if (!script.Started)
                        script.Start();
                    script.PreRenderUpdate();
                }
            }

            #endregion 
            
            #region Particles Emitters

            // Update particle emitters.
            for (int i = 0; i < ParticleEmitter.ComponentPool.Count; i++)
            {
                if (ParticleEmitter.ComponentPool.Elements[i].IsActive)
                    ParticleEmitter.ComponentPool.Elements[i].Update();
            }

            #endregion

            Camera mainCamera;

            if (CamerasToRender == null || CamerasToRender.Count == 0)
            {

                #region Render Each Camera

                // For each camera we render the scene in it
                for (int cameraIndex = 0; cameraIndex < Camera.ComponentPool.Count; cameraIndex++)
                {
                    Camera currentCamera = Camera.ComponentPool.Elements[cameraIndex];
                    // Only active master cameras are renderer.
                    if (currentCamera.MasterCamera == null && currentCamera.IsActive)
                        RenderMasterCamera(currentCamera);
                }
            
                #endregion

                mainCamera = Camera.MainCamera;
            }
            else
            {
                #region Render CamerasToRender Cameras

                // For each camera we render the scene in it
                for (int cameraIndex = 0; cameraIndex < CamerasToRender.Count; cameraIndex++)
                {
                    Camera currentCamera = CamerasToRender[cameraIndex];
                    // Only active master cameras are renderer.
                    if (currentCamera.MasterCamera == null && currentCamera.IsActive)
                        RenderMasterCamera(currentCamera);
                }

                #endregion

                mainCamera = CamerasToRender[CamerasToRender.Count - 1];
            }

            #region Screenshot Preparations

            RenderTarget screenshotRenderTarget = null;
            if (ScreenshotCapturer.MakeScreenshot)
            {
                // Instead of render into the back buffer we render into a render target.
                ContentManager userContentManager = ContentManager.CurrentContentManager;
                ContentManager.CurrentContentManager = ContentManager.SystemContentManager;
                screenshotRenderTarget = new RenderTarget(Size.FullScreen, SurfaceFormat.Color, false);
                ContentManager.CurrentContentManager = userContentManager;
                screenshotRenderTarget.EnableRenderTarget();
            }

            #endregion

            #region Render Main Camera to Back Buffer

            if (RenderMainCameraToScreen)
            {
                EngineManager.Device.Clear(Color.Black);
                // Render the main camera onto back buffer.
                if (mainCamera != null && mainCamera.RenderTarget != null)
                    SpriteManager.DrawTextureToFullScreen(Camera.MainCamera.RenderTarget);
            }

            #endregion

            #region Logic Update

            CurrentScene.PostRenderTasks();
            for (int i = 0; i < Script.ScriptList.Count; i++) // The for sentence is needed because the script list could be modified by some script.
            {
                var script = Script.ScriptList[i];
                if (script.assignedToAGameObject && script.IsActive)
                {
                    if (!script.Started)
                        script.Start();
                    script.PostRenderUpdate();
                }
            }

            #endregion 

            #region Screenshot

            if (ScreenshotCapturer.MakeScreenshot)
            {
                screenshotRenderTarget.DisableRenderTarget();
                ScreenshotCapturer.MakeScreenshot = false;
                ScreenshotCapturer.SaveScreenshot(screenshotRenderTarget);
                SpriteManager.DrawTextureToFullScreen(screenshotRenderTarget);
                screenshotRenderTarget.Dispose();
            }

            #endregion
            
            #region Release Shadow Light Depth Textures

            // We can do this from time to time to reduce calculations.
            // The problem is that I have to store the result for each camera.
            // And how much cameras do the game will have?
            for (int i = 0; i < SpotLight.ComponentPool.Count; i++)
            {
                if (SpotLight.ComponentPool.Elements[i].Shadow != null)
                {
                    RenderTarget.Release(SpotLight.ComponentPool.Elements[i].Shadow.LightDepthTexture);
                    SpotLight.ComponentPool.Elements[i].Shadow.LightDepthTexture = null;
                }
            }
            for (int i = 0; i < DirectionalLight.ComponentPool.Count; i++)
            {
                if (DirectionalLight.ComponentPool.Elements[i].Shadow != null)
                {
                    RenderTarget.Release(DirectionalLight.ComponentPool.Elements[i].Shadow.LightDepthTexture);
                    DirectionalLight.ComponentPool.Elements[i].Shadow.LightDepthTexture = null;
                }
            }

            #endregion
            
        } // Draw

        #region Render Master Camera

        /// <summary>
        /// Render a master camera and its slaves. 
        /// </summary>
        private static void RenderMasterCamera(Camera currentCamera)
        {
            // If the camera does not have a render target we create one for the user.
            if (currentCamera.RenderTarget == null)
                currentCamera.RenderTarget = new RenderTarget(currentCamera.RenderTargetSize, SurfaceFormat.Color, DepthFormat.None);
            // If it does not have slaves cameras and it occupied the whole render target...
            if (currentCamera.slavesCameras.Count == 0 && currentCamera.NormalizedViewport == new RectangleF(0, 0, 1, 1))
                RenderCamera(currentCamera, currentCamera.RenderTarget);
            else
            {

                #region Render Cameras

                // Render each camera to a render target and then merge.
                currentCamera.PartialRenderTarget = RenderTarget.Fetch(CalculatePartialRenderTargetSize(currentCamera), SurfaceFormat.Color, DepthFormat.None, 
                                                                       RenderTarget.AntialiasingType.NoAntialiasing);
                RenderCamera(currentCamera, currentCamera.PartialRenderTarget);
                foreach (Camera slaveCamera in currentCamera.slavesCameras)
                {
                    if (slaveCamera.IsActive)
                    {
                        // I store the render of the camera to a partial render target.
                        // This helps reduce the memory consumption (GBuffer, Light Pass, HDR pass)
                        // at the expense of a pass that copy this texture to a bigger render target
                        // and a last pass that copy the cameras� render target to the back buffer.
                        // If the performance is critical and there is more memory you should change this behavior.
                        // It also simplified the render of one camera. 
                        slaveCamera.PartialRenderTarget = RenderTarget.Fetch(CalculatePartialRenderTargetSize(currentCamera), SurfaceFormat.Color, DepthFormat.None,
                                                                             RenderTarget.AntialiasingType.NoAntialiasing);
                        RenderCamera(slaveCamera, slaveCamera.PartialRenderTarget);
                    }
                }

                #endregion

                #region Composite Cameras

                // Composite cameras
                currentCamera.RenderTarget.EnableRenderTarget();
                currentCamera.RenderTarget.Clear(currentCamera.ClearColor);

                // Composite using the rendering order
                bool masterCamerawasRendered = false;
                foreach (Camera slaveCamera in currentCamera.slavesCameras)
                {
                    // If the master camera needs to be rendered.
                    if (!masterCamerawasRendered && slaveCamera.RenderingOrder > currentCamera.RenderingOrder)
                    {
                        EngineManager.Device.Viewport = new Viewport(currentCamera.Viewport.X, currentCamera.Viewport.Y, currentCamera.Viewport.Width, currentCamera.Viewport.Height);
                        SpriteManager.DrawTextureToFullScreen(currentCamera.PartialRenderTarget, true);
                        RenderTarget.Release(currentCamera.PartialRenderTarget);
                        masterCamerawasRendered = true;
                    }
                    // Render slaves cameras (they are already ordered).
                    if (slaveCamera.IsActive)
                    {
                        EngineManager.Device.Viewport = new Viewport(slaveCamera.Viewport.X, slaveCamera.Viewport.Y, slaveCamera.Viewport.Width, slaveCamera.Viewport.Height);
                        SpriteManager.DrawTextureToFullScreen(slaveCamera.PartialRenderTarget, true);
                        RenderTarget.Release(slaveCamera.PartialRenderTarget);
                    }
                }
                // If the master camera was not rendered then we do it here.
                if (!masterCamerawasRendered)
                {
                    EngineManager.Device.Viewport = new Viewport(currentCamera.Viewport.X, currentCamera.Viewport.Y, currentCamera.Viewport.Width, currentCamera.Viewport.Height);
                    SpriteManager.DrawTextureToFullScreen(currentCamera.PartialRenderTarget, true);
                    RenderTarget.Release(currentCamera.PartialRenderTarget);
                }
                currentCamera.RenderTarget.DisableRenderTarget();

                #endregion

            }
        } // RenderMainCamera

        #endregion

        #region Frustum Culling

        /// <summary>
        /// Frustum Culling.
        /// </summary>
        /// <param name="boundingFrustum">Bounding Frustum.</param>
        /// <param name="modelsToRender">The result.</param>
        private static void FrustumCulling(BoundingFrustum boundingFrustum, List<ModelRenderer> modelsToRender)
        {
            for (int i = 0; i < ModelRenderer.ComponentPool.Count; i++)
            {
                ModelRenderer component = ModelRenderer.ComponentPool.Elements[i];
                if (component.CachedModel != null && component.IsVisible)
                {
                    if (boundingFrustum.Intersects(component.BoundingSphere))
                        modelsToRender.Add(component);
                }
            }
        } // FrustumCulling

        #endregion

        #region Calculate Render Target Size

        /// <summary>
        /// Calculate partial render target size.
        /// </summary>
        private static Size CalculatePartialRenderTargetSize(Camera camera)
        {
            Size targetSize;
            if (camera.NeedViewport)
            {
                targetSize = new Size(camera.Viewport.Width, camera.Viewport.Height);
                targetSize.MakeRelativeIfPosible();
            }
            else
                targetSize = camera.RenderTargetSize;
            return targetSize;
        } // CalculatePartialRenderTargetSize

        #endregion

        #region Render Camera

        // Render Targets used in the deferred lighting pipeline.
        private static RenderTarget.RenderTargetBinding gbufferTextures;
        private static RenderTarget.RenderTargetBinding gbufferHalfTextures;
        private static RenderTarget.RenderTargetBinding gbufferQuarterTextures;
        private static RenderTarget lightTexture;
        private static RenderTarget sceneTexture;
        private static RenderTarget ambientOcclusionTexture;

        /// <summary>
        /// Deferred lighting pipeline for one camera.
        /// </summary>
        private static void RenderCamera(Camera currentCamera, RenderTarget renderTarget)
        {
            if (renderTarget == null)
                throw new ArgumentNullException("renderTarget");

            // Calculate view space bounding frustum.
            currentCamera.BoundingFrustumViewSpace(cornersViewSpace);
            // Set camera culling mask.
            Layer.CurrentCameraCullingMask = currentCamera.CullingMask;

            #region Frustum Culling

            // The objective is implementing a culling management in a limited time framework.
            // In DICE�s presentation (Culling the Battlefield Data Oriented Design in Practice)
            // they find that a slightly modified simple frustum culling could work better than 
            // a tree based structure if a data oriented design is followed. 
            // The question is if C# could bring me the possibility to arrange data the way I need it or not.
            // Then they apply a software occlusion culling technique, an interesting approach
            // but unfortunately I don�t think I can make it work in the time that I have.

            // I will try to make a simple version and then optimized.
            // First I will try to do frustum culling with bounding spheres.
            // DICE talk about grids, I still do not understand why. It is to separate better the data send to the cores?
            // DICE also stores in an array only the bounding and entity information. This is something that I already find necessary to do some months before ago.
            // They also store AABB information to perform the software occlusion culling in the next pass.
            // They also improve a lot the performance of the intersect operation, however I will relay in the XNA implementation, at least for the time being.
            // Finally, I should implement a multi frustum culling (cameras and lights) to improve performance. 
            // Another reference about this: http://blog.selfshadow.com/publications/practical-visibility/
            // Reading this last link I concluded that probably understood incorrectly some part of the method.

            // CHC++ is a technique very used. In ShaderX7 there are a good article about it (it also includes the source code).

            // First Version (very simple)
            cameraBoundingFrustum.Matrix = currentCamera.ViewMatrix * currentCamera.ProjectionMatrix;
            modelsToRender.Clear();
            FrustumCulling(cameraBoundingFrustum, modelsToRender);

            #endregion

            #region GBuffer Pass

            GBufferPass.Begin(renderTarget.Size);
            GBufferShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, currentCamera.FarPlane);

            #region  Sorting

            gBufferSkinnedSimple.Clear();
            gBufferSkinnedWithNormalMap.Clear();
            gBufferWithParallax.Clear();
            gbufferSimple.Clear();
            gBufferWithNormalMap.Clear();

            // Sort by G-Buffer techniques.
            foreach (ModelRenderer modelRenderer in modelsToRender)
            {
                if (modelRenderer.CachedModel != null && modelRenderer.IsVisible)
                {
                    int currentMeshPart = 0;
                    // Each mesh is sorted individually.
                    for (int mesh = 0; mesh < modelRenderer.CachedModel.MeshesCount; mesh++)
                    {
                        int meshPartsCount = 1;
                        if (modelRenderer.CachedModel is FileModel)
                            meshPartsCount = ((FileModel)modelRenderer.CachedModel).Resource.Meshes[mesh].MeshParts.Count;
                        // Each mesh part is sorted individiually.
                        for (int meshPart = 0; meshPart < meshPartsCount; meshPart++)
                        {
                            // Find material (the mesh part could have a custom material or use the model material)
                            Material material = null;
                            if (modelRenderer.MeshMaterial != null && currentMeshPart < modelRenderer.MeshMaterial.Length && modelRenderer.MeshMaterial[currentMeshPart] != null)
                                material = modelRenderer.MeshMaterial[currentMeshPart];
                            else if (modelRenderer.Material != null)
                                material = modelRenderer.Material;
                            // Once the material is felt then the classification begins.
                            if (material != null && material.AlphaBlending == 1) // Only opaque models are rendered on the G-Buffer.
                            {
                                // If it is a skinned model.
                                if (modelRenderer.CachedModel is FileModel && ((FileModel)modelRenderer.CachedModel).IsSkinned && modelRenderer.cachedBoneTransforms != null)
                                {
                                    MeshPartToRender meshPartToRender = new MeshPartToRender
                                    {
                                        WorldMatrix = modelRenderer.CachedWorldMatrix,
                                        Model = modelRenderer.CachedModel,
                                        BoneTransform = modelRenderer.cachedBoneTransforms,
                                        Material = modelRenderer.Material,
                                        MeshIndex = mesh,
                                        MeshPart = meshPart,
                                    };
                                    if (material.NormalTexture == null)
                                        gBufferSkinnedSimple.Add(meshPartToRender);
                                    else
                                        gBufferSkinnedWithNormalMap.Add(meshPartToRender);
                                }
                                else
                                {
                                    Matrix worldMatrix;
                                    if (modelRenderer.cachedBoneTransforms != null)
                                        worldMatrix = modelRenderer.cachedBoneTransforms[mesh + 1] * modelRenderer.CachedWorldMatrix;
                                    else
                                        worldMatrix = modelRenderer.CachedWorldMatrix;
                                    MeshPartToRender meshPartToRender = new MeshPartToRender
                                    {
                                        WorldMatrix = worldMatrix,
                                        Model = modelRenderer.CachedModel,
                                        BoneTransform = null,
                                        Material = modelRenderer.Material,
                                        MeshIndex = mesh,
                                        MeshPart = meshPart,
                                    };
                                    if (material.NormalTexture == null)
                                        gbufferSimple.Add(meshPartToRender);
                                    else if (material.ParallaxEnabled)
                                        gBufferWithParallax.Add(meshPartToRender);
                                    else
                                        gBufferWithNormalMap.Add(meshPartToRender);
                                }
                            }
                            currentMeshPart++;
                        }
                    }
                }
            }

            #endregion

            #region Render

            foreach (MeshPartToRender meshPartToRender in gbufferSimple)
            {
                GBufferShader.Instance.RenderModelSimple(meshPartToRender.WorldMatrix, meshPartToRender.Model, meshPartToRender.Material,
                                                         meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }
            foreach (MeshPartToRender meshPartToRender in gBufferWithNormalMap)
            {
                GBufferShader.Instance.RenderModelWithNormals(meshPartToRender.WorldMatrix, meshPartToRender.Model, meshPartToRender.Material,
                                                              meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }
            foreach (MeshPartToRender meshPartToRender in gBufferWithParallax)
            {
                GBufferShader.Instance.RenderModelWithParallax(meshPartToRender.WorldMatrix, meshPartToRender.Model, meshPartToRender.Material,
                                                               meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }
            foreach (MeshPartToRender meshPartToRender in gBufferSkinnedSimple)
            {
                GBufferShader.Instance.RenderModelSkinnedSimple(meshPartToRender.WorldMatrix, meshPartToRender.Model, meshPartToRender.Material,
                                                                meshPartToRender.MeshIndex, meshPartToRender.MeshPart, meshPartToRender.BoneTransform);
            }
            foreach (MeshPartToRender meshPartToRender in gBufferSkinnedWithNormalMap)
            {
                GBufferShader.Instance.RenderModelSkinnedWithNormals(meshPartToRender.WorldMatrix, meshPartToRender.Model, meshPartToRender.Material,
                                                                     meshPartToRender.MeshIndex, meshPartToRender.MeshPart, meshPartToRender.BoneTransform);
            }

            #endregion

            gbufferTextures = GBufferPass.End();
            
            // Downsample GBuffer
            gbufferHalfTextures = DownsamplerGBufferShader.Instance.Render(gbufferTextures.RenderTargets[0], gbufferTextures.RenderTargets[1]);
            
            /*renderTarget.EnableRenderTarget();
            SpriteManager.DrawTextureToFullScreen(gbufferTextures.RenderTargets[1]);
            if (currentCamera.RenderHeadUpDisplay)
                RenderHeadsUpDisplay();
            renderTarget.DisableRenderTarget();
            Layer.CurrentCameraCullingMask = uint.MaxValue;
            ReleaseUnusedRenderTargets();
            return;*/

            #endregion
            
            #region Light Pre Pass

            #region Ambient Occlusion
            
            // If the ambient occlusion pass is requested...
            if (currentCamera.AmbientLight != null && currentCamera.AmbientLight.Intensity > 0 &&
                currentCamera.AmbientLight.AmbientOcclusion != null && currentCamera.AmbientLight.AmbientOcclusion.Enabled)
            {
                RenderTarget aoDepthTexture, aoNormalTexture;
                // Select downsampled version or full version of the gbuffer textures.
                if (currentCamera.AmbientLight.AmbientOcclusion.TextureSize == Size.TextureSize.FullSize)
                {
                    aoDepthTexture = gbufferTextures.RenderTargets[0];
                    aoNormalTexture = gbufferTextures.RenderTargets[1];
                }
                else if (currentCamera.AmbientLight.AmbientOcclusion.TextureSize == Size.TextureSize.HalfSize)
                {
                    aoDepthTexture = gbufferHalfTextures.RenderTargets[0];
                    aoNormalTexture = gbufferHalfTextures.RenderTargets[1];
                }
                else
                {
                    // We need to downsample again the gbuffer.
                    gbufferQuarterTextures = DownsamplerGBufferShader.Instance.Render(gbufferHalfTextures.RenderTargets[0], gbufferHalfTextures.RenderTargets[1]);
                    aoDepthTexture = gbufferQuarterTextures.RenderTargets[0];
                    aoNormalTexture = gbufferQuarterTextures.RenderTargets[1];
                }
                // Produce occlusion texture. The result will be used in the light pass.
                if (currentCamera.AmbientLight.AmbientOcclusion is HorizonBasedAmbientOcclusion)
                {
                    ambientOcclusionTexture = HorizonBasedAmbientOcclusionShader.Instance.Render(aoDepthTexture,
                                                                                                 aoNormalTexture,
                                                                                                 (HorizonBasedAmbientOcclusion)currentCamera.AmbientLight.AmbientOcclusion,
                                                                                                 currentCamera.FieldOfView, renderTarget.Size,
                                                                                                 gbufferTextures.RenderTargets[0]);
                }
                if (currentCamera.AmbientLight.AmbientOcclusion is RayMarchingAmbientOcclusion)
                {
                    ambientOcclusionTexture = RayMarchingAmbientOcclusionShader.Instance.Render(aoDepthTexture,
                                                                                                aoNormalTexture,
                                                                                                (RayMarchingAmbientOcclusion)currentCamera.AmbientLight.AmbientOcclusion,
                                                                                                currentCamera.FieldOfView);
                }
                
                /*renderTarget.EnableRenderTarget();
                SpriteManager.DrawTextureToFullScreen(ambientOcclusionTexture);
                if (currentCamera.RenderHeadUpDisplay)
                    RenderHeadsUpDisplay();
                renderTarget.DisableRenderTarget();
                Layer.CurrentCameraCullingMask = uint.MaxValue;
                ReleaseUnusedRenderTargets();
                return;*/
            }

            #endregion
            
            #region Shadow Maps

            #region Directional Light Shadows);

            for (int i = 0; i < DirectionalLight.ComponentPool.Count; i++)
            {
                DirectionalLight directionalLight = DirectionalLight.ComponentPool.Elements[i];
                // If there is a shadow map...
                if (directionalLight.Shadow != null && directionalLight.Shadow.Enabled && directionalLight.IsVisible && directionalLight.Intensity > 0)
                {
                    RenderTarget depthTexture;
                    if (directionalLight.Shadow.TextureSize == Size.TextureSize.FullSize)
                        depthTexture = gbufferTextures.RenderTargets[0];
                    else if (directionalLight.Shadow.TextureSize == Size.TextureSize.HalfSize)
                        depthTexture = gbufferHalfTextures.RenderTargets[0];
                    else
                        depthTexture = gbufferQuarterTextures.RenderTargets[0];

                    #region Cascaded Shadow

                    // If the shadow map is a cascaded shadow map...
                    // A cascaded shadow is influenced by camera transformation.
                    if (directionalLight.Shadow is CascadedShadow)
                    {
                        CascadedShadow shadow = (CascadedShadow)directionalLight.Shadow;
                        if (shadow.LightDepthTexture != null)
                            RenderTarget.Release(shadow.LightDepthTexture);
                        CascadedShadowMapShader.Instance.Begin(shadow.LightDepthTextureSize, depthTexture, shadow.DepthBias, shadow.Filter);
                        CascadedShadowMapShader.Instance.SetLight(directionalLight.cachedDirection, currentCamera.ViewMatrix, currentCamera.ProjectionMatrix,
                                                                  currentCamera.NearPlane, currentCamera.FarPlane, cornersViewSpace);
                        // FrustumCulling(, modelsToRenderShadow);
                        // Render the opaque objects...
                        for (int j = 0; j < ModelRenderer.ComponentPool.Count; j++)
                        {
                            ModelRenderer modelRenderer = ModelRenderer.ComponentPool.Elements[j];
                            if (modelRenderer.CachedModel != null && modelRenderer.Material != null && modelRenderer.Material.AlphaBlending == 1 && modelRenderer.IsVisible)
                                CascadedShadowMapShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix, modelRenderer.CachedModel, modelRenderer.cachedBoneTransforms);
                        }
                        directionalLight.ShadowTexture = CascadedShadowMapShader.Instance.End(ref shadow.LightDepthTexture);
                    }

                    #endregion

                    #region Basic Shadow

                    // If the shadow map is a basic shadow map...
                    else if (directionalLight.Shadow is BasicShadow)
                    {
                        BasicShadow shadow = (BasicShadow)directionalLight.Shadow;
                        if (shadow.LightDepthTexture != null)
                            RenderTarget.Release(shadow.LightDepthTexture);
                        BasicShadowMapShader.Instance.Begin(shadow.LightDepthTextureSize, depthTexture, shadow.DepthBias, shadow.Filter);
                        BasicShadowMapShader.Instance.SetLight(directionalLight.cachedDirection, currentCamera.ViewMatrix, shadow.Range, cornersViewSpace);
                        //FrustumCulling(new BoundingFrustum(), modelsToRenderShadow);
                        // Render all the opaque objects...
                        for (int j = 0; j < ModelRenderer.ComponentPool.Count; j++)
                        {
                            ModelRenderer modelRenderer = ModelRenderer.ComponentPool.Elements[j];
                            if (modelRenderer.CachedModel != null && modelRenderer.Material != null && modelRenderer.Material.AlphaBlending == 1 && modelRenderer.IsVisible)
                            {
                                BasicShadowMapShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix, modelRenderer.CachedModel, modelRenderer.cachedBoneTransforms);
                            }
                        }
                        directionalLight.ShadowTexture = BasicShadowMapShader.Instance.End(ref shadow.LightDepthTexture);
                    }

                    #endregion

                }
            }

            #endregion

            #region Spot Light Shadows

            for (int i = 0; i < SpotLight.ComponentPool.Count; i++)
            {
                SpotLight spotLight = SpotLight.ComponentPool.Elements[i];
                // If there is a shadow map...
                if (spotLight.Shadow != null && spotLight.Shadow.Enabled && spotLight.IsVisible && spotLight.Intensity > 0)
                {
                    RenderTarget depthTexture;
                    if (spotLight.Shadow.TextureSize == Size.TextureSize.FullSize)
                        depthTexture = gbufferTextures.RenderTargets[0];
                    else if (spotLight.Shadow.TextureSize == Size.TextureSize.HalfSize)
                        depthTexture = gbufferHalfTextures.RenderTargets[0];
                    else
                        depthTexture = gbufferQuarterTextures.RenderTargets[0];

                    // If the shadow map is a cascaded shadow map...
                    if (spotLight.Shadow is BasicShadow)
                    {
                        BasicShadow shadow = (BasicShadow)spotLight.Shadow;
                        if (shadow.LightDepthTexture == null)
                        {
                            BasicShadowMapShader.Instance.Begin(shadow.LightDepthTextureSize, depthTexture, shadow.DepthBias, shadow.Filter);
                            BasicShadowMapShader.Instance.SetLight(spotLight.cachedPosition, spotLight.cachedDirection, currentCamera.ViewMatrix, spotLight.OuterConeAngle,
                                                                   spotLight.Range, cornersViewSpace);
                            //FrustumCulling(new BoundingFrustum(), modelsToRenderShadow);
                            // Render all the opaque objects...
                            for (int j = 0; j < ModelRenderer.ComponentPool.Count; j++)
                            {
                                ModelRenderer modelRenderer = ModelRenderer.ComponentPool.Elements[j];
                                if (modelRenderer.CachedModel != null && modelRenderer.Material != null && modelRenderer.Material.AlphaBlending == 1 && modelRenderer.IsVisible)
                                {
                                    BasicShadowMapShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix, modelRenderer.CachedModel, modelRenderer.cachedBoneTransforms);
                                }
                            }
                            spotLight.ShadowTexture = BasicShadowMapShader.Instance.End(ref shadow.LightDepthTexture);
                        }
                        else
                        {
                            spotLight.ShadowTexture = BasicShadowMapShader.Instance.ProcessWithPrecalculedLightDepthTexture(shadow.LightDepthTexture);
                        }
                    }
                }
            }

            #endregion

            #endregion

            #region Light Texture

            LightPrePass.Begin(renderTarget.Size);

            #region Ambient Light

            // Render ambient light
            if (currentCamera.AmbientLight != null && currentCamera.AmbientLight.Intensity > 0)
            {
                AmbientLightShader.Instance.RenderLight(gbufferTextures.RenderTargets[1], // Normal Texture
                                                        currentCamera.AmbientLight,
                                                        ambientOcclusionTexture,
                                                        currentCamera.ViewMatrix);
            }
            RenderTarget.Release(ambientOcclusionTexture);

            #endregion

            #region Directional Lights

            DirectionalLightShader.Instance.Begin(gbufferTextures.RenderTargets[0], // Depth Texture
                                                  gbufferTextures.RenderTargets[1], // Normal Texture
                                                  currentCamera.ViewMatrix,
                                                  cornersViewSpace);
            for (int i = 0; i < DirectionalLight.ComponentPool.Count; i++)
            {
                DirectionalLight directionalLight = DirectionalLight.ComponentPool.Elements[i];
                if (directionalLight.Intensity > 0 && directionalLight.IsVisible)
                {
                    DirectionalLightShader.Instance.RenderLight(directionalLight.Color, directionalLight.cachedDirection,
                                                                directionalLight.Intensity, directionalLight.ShadowTexture);
                }
            }

            #endregion

            #region Point Lights

            PointLightShader.Instance.Begin(gbufferTextures.RenderTargets[0], // Depth Texture
                                            gbufferTextures.RenderTargets[1], // Normal Texture
                                            currentCamera.ViewMatrix,
                                            currentCamera.ProjectionMatrix,
                                            currentCamera.NearPlane,
                                            currentCamera.FarPlane);
            for (int i = 0; i < PointLight.ComponentPool.Count; i++)
            {
                PointLight pointLight = PointLight.ComponentPool.Elements[i];
                if (pointLight.Intensity > 0 && pointLight.IsVisible)
                {
                    PointLightShader.Instance.RenderLight(pointLight.Color, pointLight.cachedPosition, pointLight.Intensity, pointLight.Range);
                }
            }

            #endregion

            #region Spot Lights

            SpotLightShader.Instance.Begin(gbufferTextures.RenderTargets[0], // Depth Texture
                                           gbufferTextures.RenderTargets[1], // Normal Texture
                                           currentCamera.ViewMatrix,
                                           currentCamera.ProjectionMatrix,
                                           currentCamera.NearPlane,
                                           currentCamera.FarPlane);
            for (int i = 0; i < SpotLight.ComponentPool.Count; i++)
            {
                SpotLight spotLight = SpotLight.ComponentPool.Elements[i];
                if (spotLight.Intensity > 0 && spotLight.IsVisible)
                {
                    SpotLightShader.Instance.RenderLight(spotLight.Color, spotLight.cachedPosition,
                                                         spotLight.cachedDirection, spotLight.Intensity,
                                                         spotLight.Range, spotLight.InnerConeAngle,
                                                         spotLight.OuterConeAngle, spotLight.ShadowTexture, spotLight.LightMaskTexture);
                }
            }

            #endregion

            lightTexture = LightPrePass.End();

            #endregion

            #region Release Shadow Textures

            // We can do this from time to time to reduce calculations.
            for (int i = 0; i < SpotLight.ComponentPool.Count; i++)
            {
                SpotLight spotLight = SpotLight.ComponentPool.Elements[i];
                if (spotLight.ShadowTexture != null)
                {
                    RenderTarget.Release(spotLight.ShadowTexture);
                    spotLight.ShadowTexture = null;
                }
            }
            for (int i = 0; i < DirectionalLight.ComponentPool.Count; i++)
            {
                DirectionalLight directionalLight = DirectionalLight.ComponentPool.Elements[i];
                if (directionalLight.ShadowTexture != null)
                {
                    RenderTarget.Release(directionalLight.ShadowTexture);
                    directionalLight.ShadowTexture = null;
                }
            }
            for (int i = 0; i < PointLight.ComponentPool.Count; i++)
            {
                PointLight pointLight = PointLight.ComponentPool.Elements[i];
                if (pointLight.ShadowTexture != null)
                {
                    RenderTarget.Release(pointLight.ShadowTexture);
                    pointLight.ShadowTexture = null;
                }
            }

            #endregion
            /*
            renderTarget.EnableRenderTarget();
            SpriteManager.DrawTextureToFullScreen(lightTexture);
            if (currentCamera.RenderHeadUpDisplay)
                RenderHeadsUpDisplay();
            renderTarget.DisableRenderTarget();
            Layer.CurrentCameraCullingMask = uint.MaxValue;
            ReleaseUnusedRenderTargets();
            return;*/

            #endregion

            #region HDR Linear Space Pass

            ScenePass.Begin(renderTarget.Size, currentCamera.ClearColor);

            #region Opaque Objects

            #region  Sorting
            
            opaqueBlinnPhong.Clear();
            opaqueBlinnPhongSkinned.Clear();
            opaqueCarPaint.Clear();
            opaqueConstant.Clear();

            // Sort by material.
            foreach (ModelRenderer modelRenderer in modelsToRender)
            {
                if (modelRenderer.CachedModel != null && modelRenderer.IsVisible)
                {
                    int currentMeshPart = 0;
                    // Each mesh is sorted individually.
                    for (int mesh = 0; mesh < modelRenderer.CachedModel.MeshesCount; mesh++)
                    {
                        int meshPartsCount = 1;
                        if (modelRenderer.CachedModel is FileModel)
                            meshPartsCount = ((FileModel)modelRenderer.CachedModel).Resource.Meshes[mesh].MeshParts.Count;
                        // Each mesh part is sorted individiually.
                        for (int meshPart = 0; meshPart < meshPartsCount; meshPart++)
                        {
                            // Find material (the mesh part could have a custom material or use the model material)
                            Material material = null;
                            if (modelRenderer.MeshMaterial != null && currentMeshPart < modelRenderer.MeshMaterial.Length && modelRenderer.MeshMaterial[currentMeshPart] != null)
                                material = modelRenderer.MeshMaterial[currentMeshPart];
                            else if (modelRenderer.Material != null)
                                material = modelRenderer.Material;
                            // Once the material is felt then the classification begins.
                            if (material != null && material.AlphaBlending == 1) // Only opaque models are rendered on the G-Buffer.
                            {
                                // If it is a skinned model.
                                if (modelRenderer.CachedModel is FileModel && ((FileModel)modelRenderer.CachedModel).IsSkinned && modelRenderer.cachedBoneTransforms != null)
                                {
                                    MeshPartToRender meshPartToRender = new MeshPartToRender
                                    {
                                        WorldMatrix = modelRenderer.CachedWorldMatrix,
                                        Model = modelRenderer.CachedModel,
                                        BoneTransform = modelRenderer.cachedBoneTransforms,
                                        Material = modelRenderer.Material,
                                        MeshIndex = mesh,
                                        MeshPart = meshPart,
                                    };
                                    opaqueBlinnPhongSkinned.Add(meshPartToRender);
                                }
                                else
                                {
                                    Matrix worldMatrix;
                                    if (modelRenderer.cachedBoneTransforms != null)
                                        worldMatrix = modelRenderer.cachedBoneTransforms[mesh + 1] * modelRenderer.CachedWorldMatrix;
                                    else
                                        worldMatrix = modelRenderer.CachedWorldMatrix;
                                    MeshPartToRender meshPartToRender = new MeshPartToRender
                                    {
                                        WorldMatrix = worldMatrix,
                                        Model = modelRenderer.CachedModel,
                                        BoneTransform = null,
                                        Material = modelRenderer.Material,
                                        MeshIndex = mesh,
                                        MeshPart = meshPart,
                                    };
                                    if (material is BlinnPhong)
                                        opaqueBlinnPhong.Add(meshPartToRender);
                                    else if (material is Constant)
                                        opaqueConstant.Add(meshPartToRender);
                                    else if (material is CarPaint)
                                        opaqueCarPaint.Add(meshPartToRender);
                                }
                            }
                            currentMeshPart++;
                        }
                    }
                }
            }

            #endregion

            #region Render

            foreach (var meshPartToRender in opaqueConstant)
            {
                ConstantShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);
                ConstantShader.Instance.RenderModel(meshPartToRender.WorldMatrix,
                                                    meshPartToRender.Model,
                                                    meshPartToRender.BoneTransform,
                                                    (Constant)meshPartToRender.Material,
                                                    meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }
            foreach (var meshPartToRender in opaqueBlinnPhong)
            {
                BlinnPhongShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, lightTexture);
                BlinnPhongShader.Instance.RenderModel(meshPartToRender.WorldMatrix,
                                                    meshPartToRender.Model,
                                                    meshPartToRender.BoneTransform,
                                                    (BlinnPhong)meshPartToRender.Material,
                                                    meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }
            foreach (var meshPartToRender in opaqueCarPaint)
            {
                CarPaintShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, lightTexture);
                CarPaintShader.Instance.RenderModel(meshPartToRender.WorldMatrix,
                                                    meshPartToRender.Model,
                                                    meshPartToRender.BoneTransform,
                                                    (CarPaint)meshPartToRender.Material,
                                                    meshPartToRender.MeshIndex, meshPartToRender.MeshPart);
            }

            #endregion
            
            #endregion

            #region Sky

            // The sky is render later so that the GPU can avoid fragment processing. But it has to be done before the transparent objects.
            if (currentCamera.Sky != null)
            {
                if (currentCamera.Sky is Skybox && ((Skybox)currentCamera.Sky).TextureCube != null)
                {
                    SkyboxShader.Instance.Render(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, currentCamera.FarPlane, (Skybox)(currentCamera.Sky));
                }
                if (currentCamera.Sky is Skydome && ((Skydome)currentCamera.Sky).Texture != null)
                {
                    SkydomeShader.Instance.Render(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, currentCamera.FarPlane, (Skydome)(currentCamera.Sky));
                }
            }

            #endregion

            #region Particles

            ParticleShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, currentCamera.AspectRatio, currentCamera.FarPlane,
                                          new Size(currentCamera.Viewport.Width, currentCamera.Viewport.Height), gbufferTextures.RenderTargets[0]);
            for (int i = 0; i < ParticleRenderer.ComponentPool.Count; i++)
            {
                ParticleRenderer particleRenderer = ParticleRenderer.ComponentPool.Elements[i];

                if (particleRenderer.cachedParticleSystem != null && particleRenderer.Texture != null && particleRenderer.IsVisible)
                    ParticleShader.Instance.Render(particleRenderer.cachedParticleSystem, particleRenderer.Duration,
                                                   particleRenderer.BlendState, particleRenderer.DurationRandomness, particleRenderer.Gravity,
                                                   particleRenderer.EndVelocity, particleRenderer.MinimumColor, particleRenderer.MaximumColor,
                                                   particleRenderer.RotateSpeed, particleRenderer.StartSize, particleRenderer.EndSize,
                                                   particleRenderer.Texture, particleRenderer.SoftParticles, particleRenderer.FadeDistance);
            }

            #endregion

            #region Transparent Objects
            
            // The transparent objects will be render in forward fashion.
            foreach (ModelRenderer modelRenderer in modelsToRender)
            {
                if (modelRenderer.CachedModel != null && modelRenderer.IsVisible)
                {
                    int currentMeshPart = 0;
                    // Render each mesh
                    for (int j = 0; j < modelRenderer.CachedModel.MeshesCount; j++)
                    {
                        // I need to know the total index of the current part for the materials.
                        int meshPartsCount = 1;
                        if (modelRenderer.CachedModel is FileModel)
                        {
                            meshPartsCount = ((FileModel)modelRenderer.CachedModel).Resource.Meshes[j].MeshParts.Count;
                        }
                        for (int k = 0; k < meshPartsCount; k++)
                        {
                            // Find material
                            Material material = null;
                            if (modelRenderer.MeshMaterial != null && currentMeshPart < modelRenderer.MeshMaterial.Length &&
                                modelRenderer.MeshMaterial[currentMeshPart] != null)
                            {
                                material = modelRenderer.MeshMaterial[currentMeshPart];
                            }
                            else if (modelRenderer.Material != null)
                            {
                                material = modelRenderer.Material;
                            }
                            // Render mesh with finded material.
                            if (material != null && material.AlphaBlending < 1)
                            {

                                if (modelRenderer.Material is Constant)
                                {
                                    ConstantShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);
                                    ConstantShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix,
                                                                        modelRenderer.CachedModel,
                                                                        modelRenderer.cachedBoneTransforms,
                                                                        (Constant)material, j, k);
                                }
                                else if (modelRenderer.Material is BlinnPhong)
                                {
                                    ForwardBlinnPhongShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, lightTexture);
                                    ForwardBlinnPhongShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix,
                                                                                 modelRenderer.CachedModel,
                                                                                 modelRenderer.cachedBoneTransforms,
                                                                                 (BlinnPhong)material,
                                                                                 currentCamera.AmbientLight, j, k);
                                }
                                else if (modelRenderer.Material is CarPaint)
                                {
                                    CarPaintShader.Instance.Begin(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, lightTexture);
                                    CarPaintShader.Instance.RenderModel(modelRenderer.CachedWorldMatrix,
                                                                        modelRenderer.CachedModel,
                                                                        modelRenderer.cachedBoneTransforms,
                                                                        (CarPaint)material, j, k);
                                }
                            }
                            currentMeshPart++;
                        }
                    }
                }
            }
            
            #endregion

            #region Textures and Text

            if (HudText.ComponentPool3D.Count != 0 || HudTexture.ComponentPool3D.Count != 0)
            {
                SpriteManager.Begin3DLinearSpace(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);
                for (int i = 0; i < HudTexture.ComponentPool3D.Count; i++)
                {
                    HudTexture hudTexture = HudTexture.ComponentPool3D.Elements[i];
                    if (hudTexture.Texture != null && hudTexture.PostProcessed && hudTexture.IsVisible)
                    {
                        if (hudTexture.Billboard)
                        {
                            if (hudTexture.DestinationRectangle != null)
                                SpriteManager.Draw3DBillboardTexture(hudTexture.Texture,
                                                                     hudTexture.CachedWorldMatrix,
                                                                     hudTexture.Color,
                                                                     currentCamera.Position,
                                                                     currentCamera.Up,
                                                                     currentCamera.Forward);
                            else
                                SpriteManager.Draw3DBillboardTexture(hudTexture.Texture,
                                                                     hudTexture.CachedWorldMatrix,
                                                                     hudTexture.SourceRectangle,
                                                                     hudTexture.Color,
                                                                     currentCamera.Position,
                                                                     currentCamera.Up,
                                                                     currentCamera.Forward);
                        }
                        else
                        {
                            if (hudTexture.DestinationRectangle != null)
                                SpriteManager.Draw3DTexture(hudTexture.Texture,
                                                          hudTexture.CachedWorldMatrix,
                                                          hudTexture.SourceRectangle,
                                                          hudTexture.Color);
                            else
                                SpriteManager.Draw3DTexture(hudTexture.Texture,
                                                          hudTexture.CachedWorldMatrix,
                                                          hudTexture.Color);
                        }
                    }
                }
                for (int i = 0; i < HudText.ComponentPool3D.Count; i++)
                {
                    HudText hudText = HudText.ComponentPool3D.Elements[i];
                    if (hudText.PostProcessed && hudText.IsVisible)
                    {
                        if (hudText.Billboard)
                            SpriteManager.Draw3DBillboardText(hudText.Font ?? Font.DefaultFont,
                                                              hudText.Text,
                                                              hudText.CachedWorldMatrix,
                                                              hudText.Color,
                                                              currentCamera.Position,
                                                              currentCamera.Up,
                                                              currentCamera.Forward);
                        else
                            SpriteManager.Draw3DText(hudText.Font ?? Font.DefaultFont, hudText.Text, hudText.CachedWorldMatrix, hudText.Color);

                    }
                }
                SpriteManager.End();
            }

            #endregion

            #region 3D Lines (Line List)

            LineManager.Begin3D(PrimitiveType.LineList, currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);

            for (int i = 0; i < LineRenderer.ComponentPool3D.Count; i++)
            {
                LineRenderer lineRenderer = LineRenderer.ComponentPool3D.Elements[i];
                if (lineRenderer.Vertices != null && lineRenderer.IsVisible && lineRenderer.PostProcessed && lineRenderer.PrimitiveType == PrimitiveType.LineList)
                {
                    for (int j = 0; j < lineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(lineRenderer.Vertices[j].Position, lineRenderer.CachedWorldMatrix), lineRenderer.Vertices[j].Color);
                }
            }

            LineManager.End();
            #endregion

            sceneTexture = ScenePass.End();
            RenderTarget.Release(lightTexture);

            #endregion

            #region Post Process Pass

            PostProcessingPass.BeginAndProcess(sceneTexture, gbufferTextures.RenderTargets[0], currentCamera.PostProcess, ref currentCamera.LuminanceTexture, renderTarget);
            // Render in gamma space

            #region Textures and Text

            if (HudText.ComponentPool3D.Count != 0 || HudTexture.ComponentPool3D.Count != 0)
            {
                SpriteManager.Begin3DGammaSpace(currentCamera.ViewMatrix, currentCamera.ProjectionMatrix, gbufferTextures.RenderTargets[0], currentCamera.FarPlane);
                for (int i = 0; i < HudTexture.ComponentPool3D.Count; i++)
                {
                    HudTexture hudTexture = HudTexture.ComponentPool3D.Elements[i];
                    if (hudTexture.IsVisible && hudTexture.Texture != null && !hudTexture.PostProcessed)
                    {
                        if (hudTexture.Billboard)
                        {
                            if (hudTexture.DestinationRectangle != null)
                                SpriteManager.Draw3DBillboardTexture(hudTexture.Texture,
                                                                     hudTexture.CachedWorldMatrix,
                                                                     hudTexture.Color,
                                                                     currentCamera.Position,
                                                                     currentCamera.Up,
                                                                     currentCamera.Forward);
                            else
                                SpriteManager.Draw3DBillboardTexture(hudTexture.Texture,
                                                                     hudTexture.CachedWorldMatrix,
                                                                     hudTexture.SourceRectangle,
                                                                     hudTexture.Color,
                                                                     currentCamera.Position,
                                                                     currentCamera.Up,
                                                                     currentCamera.Forward);
                        }
                        else
                        {
                            if (hudTexture.DestinationRectangle != null)
                                SpriteManager.Draw3DTexture(hudTexture.Texture,
                                                          hudTexture.CachedWorldMatrix,
                                                          hudTexture.SourceRectangle,
                                                          hudTexture.Color);
                            else
                                SpriteManager.Draw3DTexture(hudTexture.Texture,
                                                          hudTexture.CachedWorldMatrix,
                                                          hudTexture.Color);
                        }
                    }
                }
                for (int i = 0; i < HudText.ComponentPool3D.Count; i++)
                {
                    HudText hudText = HudText.ComponentPool3D.Elements[i];
                    if (hudText.IsVisible && !hudText.PostProcessed)
                    {
                        if (hudText.Billboard)
                            SpriteManager.Draw3DBillboardText(hudText.Font ?? Font.DefaultFont,
                                                              hudText.Text,
                                                              hudText.CachedWorldMatrix,
                                                              hudText.Color,
                                                              currentCamera.Position,
                                                              currentCamera.Up,
                                                              currentCamera.Forward);
                        else
                            SpriteManager.Draw3DText(hudText.Font ?? Font.DefaultFont, hudText.Text, hudText.CachedWorldMatrix, hudText.Color);

                    }
                }
                SpriteManager.End();
            }

            #endregion

            #region Lines (Line List)

            LineManager.Begin3D(PrimitiveType.LineList, currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);

            #region Bounding Volumes

            for (int i = 0; i < ModelRenderer.ComponentPool.Count; i++)
            {
                ModelRenderer modelRenderer = ModelRenderer.ComponentPool.Elements[i];
                if (modelRenderer.CachedModel != null && 
                    (modelRenderer.RenderNonAxisAlignedBoundingBox || modelRenderer.RenderBoundingSphere || modelRenderer.RenderAxisAlignedBoundingBox) &&
                    modelRenderer.IsVisible)
                {
                    if (modelRenderer.RenderNonAxisAlignedBoundingBox)
                    {
                        // Doing this allows to show a more correct bounding box.
                        // But be aware that the axis aligned bounding box calculated in the model renderer component does not match this.
                        LineManager.DrawBoundingBox(modelRenderer.CachedModel.BoundingBox, Color.Gray, modelRenderer.CachedWorldMatrix);
                    }
                    if (modelRenderer.RenderAxisAlignedBoundingBox)
                    {
                        LineManager.DrawBoundingBox(modelRenderer.BoundingBox, Color.Gray);
                    }
                    if (modelRenderer.RenderBoundingSphere)
                    {
                        LineManager.DrawBoundingSphere(modelRenderer.BoundingSphere, Color.Gray);
                    }
                }
            }

            #endregion

            #region 3D Lines (Line List)

            for (int i = 0; i < LineRenderer.ComponentPool3D.Count; i++)
            {
                LineRenderer lineRenderer = LineRenderer.ComponentPool3D.Elements[i];
                if (lineRenderer.Vertices != null && lineRenderer.IsVisible && lineRenderer.PrimitiveType == PrimitiveType.LineList && !lineRenderer.PostProcessed)
                {
                    for (int j = 0; j < lineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(lineRenderer.Vertices[j].Position, lineRenderer.CachedWorldMatrix), lineRenderer.Vertices[j].Color);
                }
            }

            #endregion

            LineManager.End();

            #endregion

            #region 3D Lines (Triangle List)

            LineManager.Begin3D(PrimitiveType.TriangleList, currentCamera.ViewMatrix, currentCamera.ProjectionMatrix);

            for (int i = 0; i < LineRenderer.ComponentPool3D.Count; i++)
            {
                LineRenderer lineRenderer = LineRenderer.ComponentPool3D.Elements[i];
                if (lineRenderer.Vertices != null && lineRenderer.IsVisible && lineRenderer.PrimitiveType == PrimitiveType.TriangleList)
                {
                    for (int j = 0; j < lineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(lineRenderer.Vertices[j].Position, lineRenderer.CachedWorldMatrix), lineRenderer.Vertices[j].Color);
                }
            }

            LineManager.End();

            #endregion

            #region Head Up Display
            
            if (currentCamera.RenderHeadUpDisplay)
                RenderHeadsUpDisplay();

            #endregion

            PostProcessingPass.End();

            #endregion

            ReleaseUnusedRenderTargets();

            // Reset Camera Culling Mask
            Layer.CurrentCameraCullingMask = uint.MaxValue;
            
        } // RenderCamera

        /// <summary>
        /// Release Unused Render Targets.
        /// </summary>
        private static void ReleaseUnusedRenderTargets()
        {
            RenderTarget.Release(gbufferTextures);
            RenderTarget.Release(gbufferHalfTextures);
            RenderTarget.Release(gbufferQuarterTextures);
            if (lightTexture != null)
            {
                RenderTarget.Release(lightTexture);
                lightTexture = null;
            }
            if (sceneTexture != null)
            {
                RenderTarget.Release(sceneTexture);
                sceneTexture = null;
            }
            if (ambientOcclusionTexture != null)
            {
                RenderTarget.Release(ambientOcclusionTexture);
                ambientOcclusionTexture = null;
            }
        } // ReleaseUnusedRenderTargets

        #endregion

        #region Render Heads Up Display

        /// <summary>
        /// Render the Head Up Display
        /// </summary>
        private static void RenderHeadsUpDisplay()
        {
            // If depth is important then a depth buffer should be generated in the back buffer.
            SpriteManager.Begin2D();
            {

                #region Videos

                VideoRenderer video;
                for (int i = 0; i < VideoRenderer.ComponentPool.Count; i++)
                {
                    video = VideoRenderer.ComponentPool.Elements[i];
                    video.Update();
                    if (video.IsVisible && video.State != MediaState.Stopped)
                    {
                        // Aspect ratio
                        Rectangle screenRectangle;
                        float videoAspectRatio = (float)video.Texture.Width / (float)video.Texture.Height,
                              screenAspectRatio = (float)Screen.Width / (float)Screen.Height;

                        if (videoAspectRatio > screenAspectRatio)
                        {
                            float vsAspectRatio = videoAspectRatio / screenAspectRatio;
                            int blackStripe = (int)((Screen.Height - (Screen.Height / vsAspectRatio)) / 2);
                            screenRectangle = new Rectangle(0, 0 + blackStripe, Screen.Width, Screen.Height - blackStripe * 2);
                        }
                        else
                        {
                            float vsAspectRatio = screenAspectRatio / videoAspectRatio;
                            int blackStripe = (int)((Screen.Width - (Screen.Width / vsAspectRatio)) / 2);
                            screenRectangle = new Rectangle(0 + blackStripe, 0, Screen.Width - blackStripe * 2, Screen.Height);
                        }
                        SpriteManager.Draw2DTexture(video.Texture, video.CachedPosition.Z, screenRectangle, null, Color.White, 0, Vector2.Zero);
                    }
                }

                #endregion

            }
            SpriteManager.End();

            #region 2D Lines

            LineManager.Begin2D(PrimitiveType.TriangleList);
            for (int i = 0; i < LineRenderer.ComponentPool2D.Count; i++)
            {
                LineRenderer lineRenderer = LineRenderer.ComponentPool2D.Elements[i];
                if (lineRenderer.Vertices != null && lineRenderer.IsVisible && lineRenderer.PrimitiveType == PrimitiveType.TriangleList)
                {
                    for (int j = 0; j < lineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(lineRenderer.Vertices[j].Position, lineRenderer.Vertices[j].Color);
                }
            }
            LineManager.End();

            LineManager.Begin2D(PrimitiveType.LineList);
            for (int i = 0; i < LineRenderer.ComponentPool2D.Count; i++)
            {
                LineRenderer currentLineRenderer = LineRenderer.ComponentPool2D.Elements[i];
                if (currentLineRenderer.Vertices != null && currentLineRenderer.IsVisible && currentLineRenderer.PrimitiveType == PrimitiveType.LineList)
                {
                    for (int j = 0; j < currentLineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(currentLineRenderer.Vertices[j].Position, currentLineRenderer.Vertices[j].Color);
                }
            }
            LineManager.End();

            #endregion

            SpriteManager.Begin2D();
            {

                #region HUD Text

                HudText hudText;
                for (int i = 0; i < HudText.ComponentPool2D.Count; i++)
                {
                    hudText = HudText.ComponentPool2D.Elements[i];
                    if (hudText.IsVisible)
                    {
                        SpriteManager.Draw2DText(hudText.Font ?? Font.DefaultFont, hudText.Text, hudText.CachedPosition, hudText.Color, 
                                                 hudText.CachedRotation, Vector2.Zero, hudText.CachedScale);
                    }
                }

                #endregion

                #region HUD Texture

                HudTexture hudTexture;
                for (int i = 0; i < HudTexture.ComponentPool2D.Count; i++)
                {
                    hudTexture = HudTexture.ComponentPool2D.Elements[i];
                    if (hudTexture.IsVisible && hudTexture.Texture != null)
                    {
                        if (hudTexture.DestinationRectangle != null)
                            SpriteManager.Draw2DTexture(hudTexture.Texture, hudTexture.CachedPosition.Z, hudTexture.DestinationRectangle.Value, hudTexture.SourceRectangle,
                                                        hudTexture.Color, hudTexture.CachedRotation, Vector2.Zero);
                        else
                            SpriteManager.Draw2DTexture(hudTexture.Texture, hudTexture.CachedPosition, hudTexture.SourceRectangle, hudTexture.Color, hudTexture.CachedRotation,
                                                        Vector2.Zero, hudTexture.CachedScale);
                    }
                }

                #endregion

            }
            SpriteManager.End();
        } // RenderHeadsUpDisplay

        #endregion

        #endregion

        #region End Run

        internal static void EndRun()
        {
            CurrentScene.EndRun();
            CurrentScene.Unitialize();
            // Disable wiimote and keyboard hook.
            InputManager.UnloadInputDevices();
        } // UnloadContent

        #endregion

        #region Remove Unused Resources

        /// <summary>
        /// Remove Unused Resources.
        /// This is intended to be used when you load a level.
        /// </summary>
        public static void RemoveUnusedResources()
        {
            SoundManager.RemoveNotReservedUnusedSoundInstances();
            GarbageCollector.CollectGarbage();
        } // RemoveUnusedResources

        #endregion

    } // GameLoop
} // XNAFinalEngine.EngineCore