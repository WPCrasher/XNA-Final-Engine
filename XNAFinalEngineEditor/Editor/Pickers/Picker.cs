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
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAFinalEngine.Assets;
using XNAFinalEngine.Components;
using XNAFinalEngine.EngineCore;
using XNAFinalEngine.Graphics;
using XNAFinalEngine.Helpers;
using XNAFinalEngine.Input;
#endregion

namespace XNAFinalEngine.Editor
{

    /// <summary>
    /// A picker allows selecting an object from the screen.
    /// </summary>
    /// <remarks>
    /// It utilizes a texture method. This method produces better results that its alternative, the bounding volume method.
    /// One disadvantage is the the time that consumes the texture memory access.
    /// Possible addition: render in a render target of only one pixel.  Maybe it isn’t a good option.
    /// Besides, the picker efficiency is not critical.
    /// </remarks>
    internal class Picker
    {

        #region Variables

        // It’s the texture where the scene is render.
        private readonly RenderTarget pickerTexture;

        // I need a constant shader to render the picker.
        private readonly Shader constantShader;

        // For manual picking.
        private bool hasBegun;
        private Matrix viewMatrix, projectionMatrix;

        #endregion

        #region Constructor

        /// <summary>
        /// A picker allows selecting an object from the screen.
        /// </summary>
        /// <remarks>
        /// It utilizes a texture method. This method produces better results that its alternative, the bounding volume method.
        /// One disadvantage is the the time that consumes the texture memory access.
        /// Possible addition: render in a render target of only one pixel. 
        /// Maybe it isn’t a good option.
        /// Besides, the picker efficiency is not critical.
        /// </remarks>
        public Picker(Size size)
        {            
            // No antialiasing because the colors can change.
            pickerTexture = new RenderTarget(size);
            constantShader = new Shader("Materials\\PickerConstant");
        } // Picker

        #endregion

        #region Pick

        /// <summary>
        /// Pick the object that is on the mouse pointer.
        /// If no object was found the result is a null pointer.
        /// </summary>
        public GameObject Pick(Matrix viewMatrix, Matrix projectionMatrix, Viewport viewport)
        {
            return Pick(Mouse.Position.X, Mouse.Position.Y, viewMatrix, projectionMatrix, viewport);
        } // Pick

        /// <summary>
        /// Pick the object that is on the X Y coordinates.
        /// If no object was found the result is a null pointer.
        /// </summary>
        public GameObject Pick(int x, int y, Matrix viewMatrix, Matrix projectionMatrix, Viewport viewport)
        {
            try
            {

                RenderObjectsToPickerTexture(viewMatrix, projectionMatrix, viewport);
                
                #region Get the pixel from the texture
                
                Color[] color = new Color[1];
                pickerTexture.Resource.GetData(0, new Rectangle(x, y, 1, 1), color, 0, 1);
                Color pixel = color[0];

                #endregion

                #region Search the object

                byte red = 0, green = 0, blue = 0;
                foreach (GameObject obj in GameObject.GameObjects)
                {
                    // Select the next color
                    NextColor(ref red, ref green, ref blue);
                    if (pixel == new Color(red, green, blue))
                        return obj;
                }
                // Maybe it is an icon...
                foreach (GameObject obj in GameObject.GameObjects)
                {
                    // Select the next color
                    NextColor(ref red, ref green, ref blue);
                    if (pixel == new Color(red, green, blue))
                        return obj;
                }

                #endregion

            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Picker: operation failed: ", e);
            }
            return null;
        } // Pick

        /// <summary>
        /// Pick the object that is on the X Y coordinates.
        /// If no object was found the result is a null pointer.
        /// </summary>
        public List<GameObject> Pick(Rectangle region, Matrix viewMatrix, Matrix projectionMatrix, Viewport viewport)
        {
            List<GameObject> pickedObjects = new List<GameObject>();

            try
            {

                RenderObjectsToPickerTexture(viewMatrix, projectionMatrix, viewport);

                #region Get the pixel from the texture

                if (region.Width == 0)
                    region.Width = 1;
                if (region.Height == 0)
                    region.Height = 1;
                Color[] colors = new Color[region.Width * region.Height];
                pickerTexture.Resource.GetData(0, region, colors, 0, region.Width * region.Height);

                #endregion

                #region Search the object

                byte red = 0, green = 0, blue = 0;
                foreach (GameObject obj in GameObject.GameObjects)
                {
                    // Select the next color
                    NextColor(ref red, ref green, ref blue);
                    if (colors.Any(color => color == new Color(red, green, blue)))
                        pickedObjects.Add(obj);
                }
                // Maybe it is an icon
                foreach (GameObject obj in GameObject.GameObjects)
                {
                    // Select the next color
                    NextColor(ref red, ref green, ref blue);
                    if (colors.Any(color => color == new Color(red, green, blue)))
                        pickedObjects.Add(obj);
                }

                #endregion

            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Picker: operation failed: ", e);
            }
            return pickedObjects;
        } // Pick

        #endregion

        #region Next Color

        /// <summary>
        /// Gives the next color.
        /// </summary>
        private static void NextColor(ref byte red, ref byte green, ref byte blue)
        {
            if (red < 255)
                red++;
            else
            {
                red = 0;
                if (green < 255)
                    green++;
                else
                {
                    green = 0;
                    blue++; // If blue is bigger than 255 then overflow.
                }
            }
        } // NextColor

        #endregion

        #region Render All Object to Picker

        /// <summary>
        /// Render the object using a constant shasder to picker texture.
        /// Each object will be render using a unique color.
        /// </summary>
        private void RenderObjectsToPickerTexture(Matrix viewMatrix, Matrix projectionMatrix, Viewport viewport)
        {
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;

            byte red = 0, green = 0, blue = 0;
            Color colorMaterial;

            // Start rendering onto the picker map
            pickerTexture.EnableRenderTarget();

            // Set Render States.
            EngineManager.Device.BlendState        = BlendState.NonPremultiplied;
            EngineManager.Device.RasterizerState   = RasterizerState.CullCounterClockwise;
            EngineManager.Device.DepthStencilState = DepthStencilState.Default;

            EngineManager.Device.Viewport = viewport;

            // Clear render target
            pickerTexture.Clear(Color.Black);

            constantShader.Resource.CurrentTechnique = constantShader.Resource.Techniques["ConstantsRGB"];

            // Render every object, one at a time
            foreach (GameObject obj in GameObject.GameObjects)
            {
                // Select the next color
                NextColor(ref red, ref green, ref blue);
                colorMaterial = new Color(red, green, blue);
                // Editor elements or not visible game objects should not be picked.
                if (obj.Layer != Layer.GetLayerByNumber(30) && obj.Layer != Layer.GetLayerByNumber(31) && Layer.IsVisible(obj.Layer.Mask) && obj.Active)
                    RenderObjectToPicker(obj, colorMaterial);
            }
            // Render icons
            LineManager.Begin2D(PrimitiveType.TriangleList);
            foreach (GameObject obj in GameObject.GameObjects)
            {
                // Select the next color
                NextColor(ref red, ref green, ref blue);
                colorMaterial = new Color(red, green, blue);
                // Editor elements or not visible game objects should not be picked.
                if (obj.Layer != Layer.GetLayerByNumber(30) && obj.Layer != Layer.GetLayerByNumber(31) && Layer.IsVisible(obj.Layer.Mask) && obj.Active)
                    RenderIconToPicker(obj, colorMaterial);
            }
            LineManager.End();

            // Activate the frame buffer again.
            pickerTexture.DisableRenderTarget();

        } // RenderObjectsToPickerTexture

        #endregion

        #region Render Object To Picker

        /// <summary>
        /// Render Object To Picker.
        /// </summary>
        public void RenderObjectToPicker(GameObject gameObject)
        {
            if (gameObject is GameObject3D)
            {
                GameObject3D gameObject3D = (GameObject3D)gameObject;
                if (gameObject3D.LineRenderer != null)
                {
                    LineManager.Begin3D(gameObject3D.LineRenderer.PrimitiveType, viewMatrix, projectionMatrix);
                    for (int j = 0; j < gameObject3D.LineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(gameObject3D.LineRenderer.Vertices[j].Position, gameObject3D.Transform.WorldMatrix), gameObject3D.LineRenderer.Vertices[j].Color);
                    LineManager.End();
                }
            }
            else if (gameObject is GameObject2D)
            {
                GameObject2D gameObject2D = (GameObject2D)gameObject;
                if (gameObject2D.LineRenderer != null)
                {
                    LineManager.Begin2D(gameObject2D.LineRenderer.PrimitiveType);
                    for (int j = 0; j < gameObject2D.LineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(gameObject2D.LineRenderer.Vertices[j].Position, gameObject2D.LineRenderer.Vertices[j].Color);
                    LineManager.End();
                }
            }
        } // RenderObjectToPicker

        /// <summary>
        /// Render Object To Picker.
        /// </summary>
        public void RenderObjectToPicker(GameObject gameObject, Color color)
        {
            if (gameObject is GameObject3D)
            {
                GameObject3D gameObject3D = (GameObject3D)gameObject;
                // Model Renderer)
                if (gameObject3D.ModelFilter != null && gameObject3D.ModelFilter.Model != null)
                {
                    constantShader.Resource.Parameters["diffuseColor"].SetValue(new Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
                    constantShader.Resource.Parameters["worldViewProj"].SetValue(gameObject3D.Transform.WorldMatrix * viewMatrix * projectionMatrix);
                    constantShader.Resource.CurrentTechnique.Passes[0].Apply();
                    gameObject3D.ModelFilter.Model.Render();
                }
                // Lines
                else if (gameObject3D.LineRenderer != null)
                {
                    LineManager.Begin3D(gameObject3D.LineRenderer.PrimitiveType, viewMatrix, projectionMatrix);
                    for (int j = 0; j < gameObject3D.LineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(gameObject3D.LineRenderer.Vertices[j].Position, gameObject3D.Transform.WorldMatrix), color);
                    LineManager.End();
                }
            }
            else
            {
                GameObject2D gameObject2D = (GameObject2D)gameObject;
                if (gameObject2D.LineRenderer != null)
                {
                    LineManager.Begin2D(gameObject2D.LineRenderer.PrimitiveType);
                    for (int j = 0; j < gameObject2D.LineRenderer.Vertices.Length; j++)
                        LineManager.AddVertex(Vector3.Transform(gameObject2D.LineRenderer.Vertices[j].Position, gameObject2D.Transform.WorldMatrix), color);
                    LineManager.End();
                }
            }
        } // RenderObjectToPicker

        #endregion

        #region Render Icon To Picker

        /// <summary>
        /// Render Icon To Picker.
        /// </summary>
        public void RenderIconToPicker(GameObject gameObject, Color color)
        {
            if (gameObject is GameObject3D)
            {
                GameObject3D gameObject3D = (GameObject3D)gameObject;
                if (gameObject3D.Light != null || gameObject3D.Camera != null)
                {
                    // Component's screen position.
                    Vector3 screenPositions = EngineManager.Device.Viewport.Project(gameObject3D.Transform.Position, projectionMatrix, viewMatrix, Matrix.Identity);
                    // Center the icon.
                    screenPositions.X -= 16;
                    screenPositions.Y -= 16;
                    // Draw.
                    LineManager.DrawSolid2DPlane(new Rectangle((int)screenPositions.X, (int)screenPositions.Y, 32, 32), color);
                }
            }
        } // RenderIconToPicker

        #endregion

        #region Manual Pick

        /// <summary>
        /// Manualy render the picker texture.
        /// This allow us to control deeply the pick operation.
        /// The black color is reserved.
        /// </summary>
        public void BeginManualPicking(Matrix viewMatrix, Matrix projectionMatrix, Viewport viewport)
        {
            if (hasBegun)
            {
                throw new InvalidOperationException("Picker: Begin has been called before calling End after the last call to Begin. Begin cannot be called again until End has been successfully called.");
            }
            hasBegun = true;
            try
            {
                this.viewMatrix = viewMatrix;
                this.projectionMatrix = projectionMatrix;
                // Set Render States.
                EngineManager.Device.BlendState = BlendState.NonPremultiplied;
                EngineManager.Device.RasterizerState = RasterizerState.CullCounterClockwise;
                EngineManager.Device.DepthStencilState = DepthStencilState.Default;

                // Start rendering onto the picker map
                pickerTexture.EnableRenderTarget();

                EngineManager.Device.Viewport = viewport;

                // Clear render target
                pickerTexture.Clear(Color.Black);

                constantShader.Resource.CurrentTechnique = constantShader.Resource.Techniques["ConstantsRGB"];
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Picker: operation failed: ", e);
            }
        } // BeginManualPicking

        /// <summary>
        /// End manual render of the picker texture.
        /// </summary>
        public Color[] EndManualPicking(Rectangle region)
        {
            if (!hasBegun)
                throw new InvalidOperationException("Line Manager: End was called, but Begin has not yet been called. You must call Begin successfully before you can call End.");
            hasBegun = false;
            Color[] colors = new Color[region.Width * region.Height];
            try
            {
                // Activate the frame buffer again.
                pickerTexture.DisableRenderTarget();
                
                #region Fix out of region

                // Left side
                if (region.X < 0)
                {
                    region.Width += region.X;
                    region.X = 0;
                }
                // Top side
                if (region.Y < 0)
                {
                    region.Height += region.Y;
                    region.Y = 0;
                }
                // Right side
                if (region.X + region.Width > pickerTexture.Width)
                    region.Width = pickerTexture.Width - Mouse.Position.X;
                // Bottom side
                if (region.Y + region.Height > pickerTexture.Height)
                    region.Height = pickerTexture.Height - Mouse.Position.Y;

                #endregion

                pickerTexture.Resource.GetData(0, region, colors, 0, region.Width * region.Height);
            }
            catch (Exception e)
            {
                //throw new InvalidOperationException("Picker: operation failed: ", e);
            }
            return colors;
        } // EndManualPicking

        #endregion
        
        #region Bounding Mesh Methods
        /*
        /// <summary>
        /// Select the objects using the bounding volume method. 
        /// It wasn't finished.
        /// </summary>
        private void PickWithBoundingSpheres(int x, int y)
        {   
            Matrix world = Matrix.Identity;
            Vector3 nearsource = new Vector3(x, y, 0f),
                    farsource = new Vector3(x, y, 1f),
                    nearPoint = EngineManager.Device.Viewport.Unproject(nearsource, ApplicationLogic.Camera.ProjectionMatrix, ApplicationLogic.Camera.ViewMatrix, world),
                    farPoint = EngineManager.Device.Viewport.Unproject(farsource, ApplicationLogic.Camera.ProjectionMatrix, ApplicationLogic.Camera.ViewMatrix, world),
                    direction = farPoint - nearPoint;

            direction.Normalize();
            
            Ray pickRay = new Ray(nearPoint, direction);

            // TODO!!! It wasn't finished.

        } // PickWithBoundingSpheres
        */
        #endregion
        
    } // Picker
} // XNAFinalEngine.Editor
