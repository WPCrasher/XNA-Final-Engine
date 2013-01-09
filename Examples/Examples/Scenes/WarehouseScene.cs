﻿
#region License
/*
Copyright (c) 2008-2012, Laboratorio de Investigación y Desarrollo en Visualización y Computación Gráfica - 
                         Departamento de Ciencias e Ingeniería de la Computación - Universidad Nacional del Sur.
All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

•	Redistributions of source code must retain the above copyright, this list of conditions and the following disclaimer.

•	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer
    in the documentation and/or other materials provided with the distribution.

•	Neither the name of the Universidad Nacional del Sur nor the names of its contributors may be used to endorse or promote products derived
    from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

-----------------------------------------------------------------------------------------------------------------------------------------------
Author: Schneider, José Ignacio (jis@cs.uns.edu.ar)
-----------------------------------------------------------------------------------------------------------------------------------------------

*/
#endregion

#region Using directives
using Microsoft.Xna.Framework;
using XNAFinalEngine.Assets;
using XNAFinalEngine.Components;
//using XNAFinalEngine.Editor;
using XNAFinalEngine.EngineCore;
using XNAFinalEngine.Graphics;
using XNAFinalEngine.Input;
using DirectionalLight = XNAFinalEngine.Components.DirectionalLight;
using Size = XNAFinalEngine.Helpers.Size;
using TextureCube = XNAFinalEngine.Assets.TextureCube;
#endregion

namespace XNAFinalEngineExamples
{

    /// <summary>
    /// Lighthouse scene.
    /// </summary>
    public class WarehouseScene : Scene
    {

        #region Variables
        
        // Now every entity is a game object and the entity’s behavior is defined by the components attached to it.
        // There are several types of components, components related to models, to sound, to particles, to physics, etc.
        private static GameObject3D // Models
                                    warehouseWalls, warehouseRoof, warehouseRoof1, warehouseWood, warehouseWood2, warehouseBrokenWindow, warehouseWindow, warehouseGround,
                                    // Lights
                                    directionalLight, pointLight, pointLight2, spotLight,
                                    // Cameras
                                    camera,
                                    skydome;

        private static GameObject2D statistics;
        
        #endregion

        #region Load
        
        /// <summary>
        /// Load the resources.
        /// </summary>
        public override void LoadContent()
        {

            #region Camera
            
            camera = new GameObject3D();
            camera.AddComponent<Camera>();
            camera.AddComponent<SoundListener>();
            camera.Camera.RenderTargetSize = Size.FullScreen;
            camera.Camera.FarPlane = 1000;
            camera.Camera.NearPlane = 1f; // Do not place a small value here, you can destroy performance, not just precision.
            camera.Transform.LookAt(new Vector3(5, 0, 15), Vector3.Zero, Vector3.Up);
            ScriptCustomCamera script = (ScriptCustomCamera)camera.AddComponent<ScriptCustomCamera>();
            script.SetPosition(new Vector3(0, 13, 22), Vector3.Zero);
            camera.Camera.ClearColor = Color.Black;
            camera.Camera.FieldOfView = 180 / 7f;
            camera.Camera.PostProcess = new PostProcess();
            camera.Camera.PostProcess.ToneMapping.AutoExposureEnabled = false;
            camera.Camera.PostProcess.ToneMapping.LensExposure = -1.5f;
            camera.Camera.PostProcess.ToneMapping.ToneMappingFunction = ToneMapping.ToneMappingFunctionEnumerate.FilmicALU;
            camera.Camera.PostProcess.MLAA.EdgeDetection = MLAA.EdgeDetectionType.Both;
            camera.Camera.PostProcess.MLAA.Enabled = false;
            camera.Camera.PostProcess.Bloom.Enabled = false;
            camera.Camera.PostProcess.Bloom.Threshold = 2;
            camera.Camera.PostProcess.FilmGrain.Enabled = false;
            camera.Camera.PostProcess.FilmGrain.Strength = 0.1f;
            camera.Camera.PostProcess.AnamorphicLensFlare.Enabled = false;
            camera.Camera.AmbientLight = new AmbientLight
            {
                SphericalHarmonicLighting = SphericalHarmonicL2.GenerateSphericalHarmonicFromCubeMap(new TextureCube("FactoryCatwalkRGBM") { IsRgbm = true, RgbmMaxRange = 50, }),
                                                            Color = new Color(30, 30, 30),
                                                            Intensity = 1.5f, //6
                                                            AmbientOcclusionStrength = 2f };
            
            //camera.Camera.Sky = new Skydome { Texture = new Texture("HotPursuitSkydome") };

            /*camera.Camera.AmbientLight.AmbientOcclusion = new HorizonBasedAmbientOcclusion
            {
                NumberSteps = 16, //12, // Don't change this.
                NumberDirections = 12, // 16, // Don't change this.
                Radius = 0.001f, // Bigger values produce more cache misses and you don’t want GPU cache misses, trust me.
                LineAttenuation = 1.0f,
                Contrast = 1.3f,
                AngleBias = 0.1f,
                Quality = HorizonBasedAmbientOcclusion.QualityType.HighQuality,
                TextureSize = Size.TextureSize.HalfSize,
            };*/

            #endregion
            
            #region Models
            
            warehouseWalls = new GameObject3D(new FileModel("Warehouse\\WarehouseWalls"),
                                              new BlinnPhong
                                                  {
                                                      DiffuseTexture = new Texture("Warehouse\\Warehouse-Diffuse"),
                                                      SpecularTexture = new Texture("Warehouse\\Warehouse-Specular"),
                                                      SpecularIntensity = 3,
                                                      SpecularPower = 30000,
                                                  });
            warehouseRoof  = new GameObject3D(new FileModel("Warehouse\\WarehouseRoof"),  
                                              new BlinnPhong
                                                  {
                                                      DiffuseTexture = new Texture("Warehouse\\MetalRoof-Diffuse")
                                                  });
            warehouseRoof1 = new GameObject3D(new FileModel("Warehouse\\WarehouseRoof1"),
                                              new BlinnPhong
                                              {
                                                  DiffuseTexture = new Texture("Warehouse\\MetalRoof2-Diffuse")
                                              });

            warehouseWood = new GameObject3D(new FileModel("Warehouse\\WarehouseWood"),
                                             new BlinnPhong
                                             {
                                                 DiffuseTexture = new Texture("Warehouse\\Wood-Diffuse")
                                             });

            warehouseWood2 = new GameObject3D(new FileModel("Warehouse\\WarehouseWood2"),
                                              new BlinnPhong
                                              {
                                                  DiffuseTexture = new Texture("Warehouse\\Wood2-Diffuse")
                                              });

            warehouseBrokenWindow = new GameObject3D(new FileModel("Warehouse\\WarehouseBrokenWindow"),
                                             new BlinnPhong
                                             {
                                                 DiffuseTexture = new Texture("Warehouse\\Window-Diffuse")
                                             });

            warehouseWindow = new GameObject3D(new FileModel("Warehouse\\WarehouseWindow"),
                                              new BlinnPhong
                                              {
                                                  DiffuseTexture = new Texture("Warehouse\\Window-Diffuse")
                                              });
            warehouseGround = new GameObject3D(new FileModel("Warehouse\\WarehouseGround"),
                                              new BlinnPhong
                                              {
                                                  DiffuseTexture = new Texture("Warehouse\\Ground-Diffuse"),
                                                  NormalTexture = new Texture("Warehouse\\Ground-Normals"),
                                                  SpecularIntensity = 0.7f
                                              });
            
            #endregion
            
            #region Shadows and Lights
            
            directionalLight = new GameObject3D();
            directionalLight.AddComponent<DirectionalLight>();
            directionalLight.DirectionalLight.Color = new Color(250, 250, 220);
            directionalLight.DirectionalLight.Intensity = 1.2f;
            directionalLight.Transform.LookAt(new Vector3(0.3f, 0.95f, -0.3f), Vector3.Zero, Vector3.Forward);
            directionalLight.DirectionalLight.Shadow = new CascadedShadow
            {
                Filter = Shadow.FilterType.PcfPosion,
                LightDepthTextureSize = Size.Square512X512,
                TextureSize = Size.TextureSize.FullSize, // Lower than this could produce artifacts if the light is to intense.
                DepthBias = 0.0025f,
                FarPlaneSplit1 = 15,
                FarPlaneSplit2 = 40,
                FarPlaneSplit3 = 100,
                //FarPlaneSplit4 = 150
            };
            
            pointLight = new GameObject3D();
            pointLight.AddComponent<PointLight>();
            pointLight.PointLight.Color = new Color(10, 200, 10); // new Color(240, 235, 200);
            pointLight.PointLight.Intensity = 0;
            pointLight.PointLight.Range = 60;
            pointLight.Transform.Position = new Vector3(4.8f, 1.5f, 10); // new Vector3(8f, -1f, 10);
            //pointLight.PointLight.Shadow = new CubeShadow { LightDepthTextureSize = 1024, };

            //for (int i = 0; i < 50; i++)
            {
                pointLight2 = new GameObject3D();
                pointLight2.AddComponent<PointLight>();
                pointLight2.PointLight.Color = new Color(10, 10, 190);
                pointLight2.PointLight.Intensity = 0.3f;// 0.05f;
                pointLight2.PointLight.Range = 30;
                pointLight2.Transform.Position = new Vector3(12f, 2, -3);
            }

            spotLight = new GameObject3D();
            spotLight.AddComponent<SpotLight>();
            spotLight.SpotLight.Color = Color.Green;
            spotLight.SpotLight.Intensity = 0;// 100f;
            spotLight.SpotLight.Range = 40; // I always forget to set the light range lower than the camera far plane.
            spotLight.Transform.Position = new Vector3(0, 15f, 10);
            spotLight.Transform.Rotate(new Vector3(-45, 0, 0));
            spotLight.SpotLight.LightMaskTexture = new Texture("LightMasks\\Crysis2TestLightMask");
            /*spotLight.SpotLight.Shadow = new BasicShadow
            {
                Filter = Shadow.FilterType.Pcf3X3,
                LightDepthTextureSize = Size.Square1024X1024,
                TextureSize = Size.TextureSize.FullSize
            };*/
            
            #endregion
            
            #region Statistics
            
            statistics = new GameObject2D();
            statistics.AddComponent<ScriptStatisticsDrawer>();
            
            #endregion
            
            #region Lamborghini Murcielago LP640
            
            // To test performance.
            //for (int i = 0; i < 10; i++)
            {
                LamborghiniMurcielagoLoader lamborghiniMurcielagoLoader = new LamborghiniMurcielagoLoader();
                lamborghiniMurcielagoLoader.LoadContent();

                lamborghiniMurcielagoLoader.LamborghiniMurcielago.Transform.LocalScale = new Vector3(1.2f, 1.2f, 1.2f);
                lamborghiniMurcielagoLoader.LamborghiniMurcielago.Transform.LocalPosition = new Vector3(0, 1.4f, 2.35f);
            }
            
            #endregion
            
        } // Load

        #endregion

        #region Update Tasks

        /// <summary>
        /// Tasks executed during the update.
        /// This is the place to put the application logic.
        /// </summary>
        public override void UpdateTasks()
        {
            if (GamePad.PlayerOne.AJustPressed)
                EngineManager.ExitApplication();
            base.UpdateTasks();
        } // UpdateTasks

        #endregion

        #region Render Tasks

        /// <summary>
        /// Tasks before the engine render.
        /// Some tasks are more related to the frame rendering than the update,
        /// or maybe the update frequency is too high to waste time in this kind of tasks,
        /// for that reason the pre render task exists.
        /// For example, is more correct to update the HUD information here because is related with the rendering.
        /// </summary>
        public override void PreRenderTasks()
        {
            
        } // PreRenderTasks

        /// <summary>
        /// Tasks after the engine render.
        /// Probably you won’t need to place any task here.
        /// </summary>
        public override void PostRenderTasks()
        {
            
        } // PostRenderTasks

        #endregion

    } // WarehouseScene
} // XNAFinalEngineExamples