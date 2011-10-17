﻿
#region License
/*
Copyright (c) 2008-2011, Laboratorio de Investigación y Desarrollo en Visualización y Computación Gráfica - 
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAFinalEngine.Assets;
using XNAFinalEngine.EngineCore;
using XNAFinalEngine.Helpers;
#endregion

namespace XNAFinalEngine.Components
{

    /// <summary>
    /// Camera component.
    /// </summary>
    public class Camera : Component
    {

        #region Enumerates

        public enum RenderingType
        {
            /// <summary>
            /// Deferred lighting (or Light Pre-Pass) rendering.
            /// The default rendering type.
            /// Use it for the rendering of the scene, or any king of rendering.
            /// </summary>
            DeferredLighting,
            /// <summary>
            /// Classic forward rendering.
            /// Some graphic effects and material don't work in this renderer.
            /// Use it for auxiliary rendering, like reflections.
            /// </summary>
            ForwardRendering,
        } // RenderingType

        #endregion

        #region Variables

        /// <summary>
        /// This is the cached world matrix from the transform component.
        /// This matrix represents the view matrix.
        /// </summary>
        internal Matrix cachedWorldMatrix;

        // Clear color
        private Color clearColor = new Color(20, 20, 20, 255);

        // Where on the screen is the camera rendered in clip space.
        private RectangleF normalizedViewport = new RectangleF(0, 0, 1, 1);

        // Where on the screen is the camera rendered in screen space.
        private Rectangle viewport = Rectangle.Empty;

        // The viewport is expressed in clip space or screen space?
        private bool viewportExpressedInClipSpace = true;

        // The master of this slave camera. A camera can't be master and slave simultaneity.
        private Camera masterCamera;

        // The slaves of this camera. A camera can't be master and slave simultaneity.
        internal readonly List<Camera> slavesCameras = new List<Camera>();

        // Destination render texture.
        private RenderTarget renderTarget;

        // Deferred lighting or forward rendering?
        private RenderingType renderer;
        
        #region Projection

        // Projection matrix.
        private Matrix projectionMatrix;

        // Use the projection matrix that the user set. 
        // If a projection's value changes there will no change in the projection matrix until the user call the reset projection matrix method.
        private bool useUserProjectionMatrix;

        /// <summary>
        /// Aspect Ratio. O means system aspect ratio.
        /// </summary>
        private float aspectRatio = 0;

        /// <summary>
        /// Field of view, near plane and far plane.
        /// </summary>
        private float nearPlane = 0.1f,
                      farPlane = 1000.0f,
                      fieldOfView = 36;
        
        // Is the camera orthographic (true) or perspective (false)?
        private bool orthographic;

        // Camera's vertical size when in orthographic mode.
        private int orthographicVerticalSize = 10;
        
        #endregion

        #endregion

        #region Properties

        #region Clear Color

        /// <summary>
        /// The color with which the screen will be cleared.
        /// </summary>
        public Color ClearColor
        {
            get { return clearColor; }
            set
            {
                if (masterCamera != null)
                    masterCamera.ClearColor = value; // So it updates its children.
                else
                {
                    clearColor = value;
                    if (slavesCameras.Count > 0) // If is a master camera update its childrens.
                        for (int i = 0; i < slavesCameras.Count; i++)
                            slavesCameras[i].clearColor = value;
                }
            }
        } // ClearColor

        #endregion

        #region Renderer

        /// <summary>
        /// Deferred lighting or forward rendering?
        /// </summary>
        public RenderingType Renderer
        {
            get { return renderer; }
            set 
            {
                if (masterCamera != null)
                    masterCamera.Renderer = value; // So it updates its children.
                else
                {
                    renderer = value;
                    if (slavesCameras.Count > 0) // If is a master camera update its childrens.
                        for (int i = 0; i < slavesCameras.Count; i++)
                            slavesCameras[i].renderer = value;
                }
            }
        } // Renderer

        #endregion

        #region Render Target

        /// <summary>
        /// Destination render texture.
        /// XNA Final Engine works in linear space and in High Dynamic Range.
        /// If you want proper results use a floating point texture.
        /// I recommend using the SetRenderTarget method except you know very well what are doing. 
        /// </summary>
        public RenderTarget RenderTarget
        {
            get { return renderTarget; }
            set
            {
                if (masterCamera != null)
                    masterCamera.RenderTarget = value; // So it updates its children.
                else
                {
                    renderTarget = value;
                    if (slavesCameras.Count > 0) // If is a master camera update its childrens.
                        for (int i = 0; i < slavesCameras.Count; i++)
                            slavesCameras[i].renderTarget = value;
                }
            }
        } // RenderTarget

        #endregion

        #region Master Camera

        /// <summary>
        /// If you want to render the scene using more than one camera in a single render target (for example: split screen) you have to relate the cameras.
        /// It does not matter who is the master, however, the rendering type, clear color and render target values come from the master camera.
        /// </summary>
        public Camera MasterCamera
        {
            get { return masterCamera; }
            set
            {
                if (slavesCameras.Count != 0 || (value != null && value.masterCamera != null))
                    throw new InvalidOperationException("Camera: A camera can't be master and slave simultaneity.");
                // Remove this camera from the old master.
                if (masterCamera != null)
                    masterCamera.slavesCameras.Remove(this);
                // Set new master.
                masterCamera = value;
                // Update master with its new slave.
                if (value != null)
                {
                    value.slavesCameras.Add(this);
                    // Just to be robust...
                    // I update the children values so that, in the case of a unparent, the values remain the same as the father.
                    renderTarget = value.RenderTarget;
                    clearColor = value.clearColor;
                    renderer = value.Renderer;
                }
            }
        } // MasterCamera

        #endregion

        #region View

        /// <summary>
        /// View Matrix.
        /// If you change this matrix, the camera no longer updates its rendering based on its Transform.
        /// This lasts until you call ResetWorldToCameraMatrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get
            {
                return cachedWorldMatrix;
            }
            set 
            {
                // Aca tendriamos que actualizar el transform pero teniendo en cuenta que la view matrix mira al resve, no?
                cachedWorldMatrix = value;
            }
        } // ViewMatrix

        #endregion

        #region Projection

        /// <summary> 
        /// We can set the projection matrix.
        /// If a projection's value changes there will no change in the projection matrix until the user call the reset projection matrix method.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
            set
            {
                projectionMatrix = value;
                useUserProjectionMatrix = true;
            }
        } // ProjectionMatrix

        /// <summary>
        /// The camera's aspect ratio (width divided by height).
        /// Default value: system aspect ratio.
        /// If you modify the aspect ratio of the camera, the value will stay until you call camera.ResetAspect(); which resets the aspect to the screen's aspect ratio.
        /// If the aspect ratio is set to system aspect ratio then the result will consider the viewport selected.
        /// </summary>
        public float AspectRatio
        {
            get
            {
                if (aspectRatio == 0)
                {
                    RectangleF normalizedViewport = NormalizedViewport;
                    return Screen.AspectRatio * normalizedViewport.Width / normalizedViewport.Height;
                }
                return aspectRatio;
            }
            set
            {
                if (value <= 0)
                    throw new Exception("Camera: the aspect ratio has to be a positive real number.");
                if (aspectRatio == 0)
                    Screen.AspectRatioChanged -= OnAspectRatioChanged;
                if (value == 0)
                    Screen.AspectRatioChanged += OnAspectRatioChanged;
                aspectRatio = value;
                CalculateProjectionMatrix();
            }
        } // AspectRatio

        /// <summary>
        /// Field of View.
        /// This is the vertical field of view; horizontal FOV varies depending on the viewport's aspect ratio.
        /// Field of view is ignored when camera is orthographic 
        /// Unit: degrees.
        /// Default value: 36
        /// </summary>
        public float FieldOfView
        {
            get { return fieldOfView; }
            set
            {
                fieldOfView = value;
                CalculateProjectionMatrix();
            }
        } // FieldOfView

        /// <summary>
        /// Near Plane.
        /// Default Value: 0.1
        /// </summary>
        public float NearPlane
        {
            get { return nearPlane; }
            set
            {
                nearPlane = value;
                CalculateProjectionMatrix();
            }
        } // NearPlane

        /// <summary>
        /// Far Plane.
        /// Defautl Value: 1000
        /// </summary>
        public float FarPlane
        {
            get { return farPlane; }
            set
            {
                farPlane = value;
                CalculateProjectionMatrix();
            }
        } // FarPlane

        /// <summary> 
        /// Is the camera orthographic (true) or perspective (false)?
        /// Unlike perspective projection, in orthographic projection there is no perspective foreshortening.
        /// </summary>
        public bool OrthographicProjection
        {
            get { return orthographic; }
            set
            {
                orthographic = value;
                CalculateProjectionMatrix();
            }
        } // OrthographicProjection

        /// <summary>
        /// Camera's vertical size when in orthographic mode. 
        /// The horizontal value} is calculated automaticaly with the aspect ratio property. 
        /// </summary>
        public int OrthographicVerticalSize
        {
            get { return orthographicVerticalSize; }
            set
            {
                orthographicVerticalSize = value;
                CalculateProjectionMatrix();
            }
        } // OrthographicVerticalSize

        #endregion

        #region Viewport

        /// <summary>
        /// Where on the screen is the camera rendered in clip space.
        /// Values: left, bottom, width, height.
        /// The normalized values should update with screen size changes.
        /// </summary>
        public RectangleF NormalizedViewport
        {
            get
            {
                if (viewportExpressedInClipSpace)
                    return normalizedViewport;
                return new RectangleF((float)viewport.X / (float)Screen.Width, (float)viewport.Y / (float)Screen.Height,
                                      (float)viewport.Width / (float)Screen.Width, (float)viewport.Height / (float)Screen.Height);
            }
            set
            {
                if (RenderTarget == null)
                    throw new InvalidOperationException("Camera: there is not render target set.");
                if (value.X < 0 || value.Y < 0 || (value.X + value.Width) > 1 || (value.Y + value.Height) > 1)
                    throw new ArgumentException("Camera: viewport size invalid.", "value");
                viewportExpressedInClipSpace = true;
                normalizedViewport = value;
                CalculateProjectionMatrix(); // The viewport could affect the aspect ratio.
            }
        } // NormalizedViewport

        /// <summary>
        /// Where on the screen is the camera rendered in screen space.
        /// Values: left, bottom, width, height.
        /// These values won't be updated with screen size changes.
        /// </summary>
        public Rectangle Viewport
        {
            get
            {
                if (viewportExpressedInClipSpace)
                    return new Rectangle((int)(normalizedViewport.X * Screen.Width), (int)(normalizedViewport.Y * Screen.Height),
                                         (int)(normalizedViewport.Width * Screen.Width), (int)(normalizedViewport.Height * Screen.Height));
                // Check for viewport dimensions? Is the correct...
                return viewport;
            }
            set
            {
                if (RenderTarget == null)
                    throw new InvalidOperationException("Camera: there is not render target set.");
                if (value == Rectangle.Empty || value.X < 0 || value.Y < 0 || (value.X + value.Width) > RenderTarget.Width || (value.Y + value.Height) > RenderTarget.Height)
                    throw new ArgumentException("Camera: viewport size invalid.", "value");
                viewportExpressedInClipSpace = false;
                viewport = value;
                CalculateProjectionMatrix(); // The viewport could affect the aspect ratio.
            }
        } // Viewport

        /// <summary>
        /// True if the camera needs a viewport (split screen) for render.
        /// </summary>
        public bool NeedViewport { get { return NormalizedViewport != new RectangleF(0, 0, 1, 1); } }

        #endregion
        
        #endregion

        #region Initialize

        /// <summary>
        /// Initialize the component. 
        /// </summary>
        internal override void Initialize(GameObject owner)
        {
            base.Initialize(owner);
            // Generate the projection matrix.
            CalculateProjectionMatrix();
            Screen.AspectRatioChanged += OnAspectRatioChanged;
            // Cache transform matrix. It will be the view matrix.
            cachedWorldMatrix = ((GameObject3D)Owner).Transform.WorldMatrix;
            ((GameObject3D)Owner).Transform.WorldMatrixChanged += OnWorldMatrixChanged;
        } // Initialize
        
        #endregion

        #region Uninitialize

        /// <summary>
        /// Uninitialize the component.
        /// Is important to remove event associations and any other reference.
        /// </summary>
        internal override void Uninitialize()
        {
            base.Uninitialize();
            if (aspectRatio == 0)
                Screen.AspectRatioChanged -= OnAspectRatioChanged;
            ((GameObject3D)Owner).Transform.WorldMatrixChanged -= OnWorldMatrixChanged;
        } // Uninitialize

        #endregion

        #region Set Render Target

        /// <summary>
        /// Creates and assign a render target for the camera.
        /// The render target properties are the most addecuate for the task.
        /// </summary>
        /// <param name="size">Render Target size.</param>
        public void SetRenderTarget(RenderTarget.SizeType size)
        {
            // It's in linear space. In this same render target the transparent object will be rendered. Maybe an RGBM encoding could work, but how?
            // Multisampling could generate indeseable artifacts. Be careful!
            RenderTarget = new RenderTarget(size, SurfaceFormat.HdrBlendable, DepthFormat.Depth24, RenderTarget.AntialiasingType.NoAntialiasing);
        } // SetRenderTarget

        /// <summary>
        /// Creates and assign a render target for the camera.
        /// The render target properties are the most addecuate for the task.
        /// </summary>
        /// <param name="size">>Render Target size.</param>
        public void SetRenderTarget(Size size)
        {
            // It's in linear space. In this same render target the transparent object will be rendered. Maybe an RGBM encoding could work, but how?
            // Multisampling could generate indeseable artifacts. Be careful!
            RenderTarget = new RenderTarget(size, SurfaceFormat.HdrBlendable, DepthFormat.Depth24, RenderTarget.AntialiasingType.NoAntialiasing);
        } // SetRenderTarget

        #endregion

        #region Calculate and Reset Projection Matrix

        /// <summary>
        /// Update projection matrix based in the camera's projection properties.
        /// </summary>
        public void ResetProjectionMatrix()
        {
            useUserProjectionMatrix = false;
            CalculateProjectionMatrix();
        } // ResetProjectionMatrix

        /// <summary>
        /// Update projection matrix based in the camera's projection properties.
        /// This is only executed if the user does not set a projection matrix.
        /// </summary>
        private void CalculateProjectionMatrix()
        {
            if (!useUserProjectionMatrix)
            {
                if (OrthographicProjection)
                    projectionMatrix = Matrix.CreateOrthographic(OrthographicVerticalSize * AspectRatio, OrthographicVerticalSize, NearPlane, FarPlane);
                else
                    projectionMatrix = Matrix.CreatePerspectiveFieldOfView(3.1416f * FieldOfView / 180.0f, AspectRatio, NearPlane, FarPlane);
            }
        } // CalculateProjectionMatrix

        #endregion
        
        #region Bounding Frustum

        /// <summary>
        /// Camera Far Plane Bounding Frustum (in view space). 
        /// With the help of the bounding frustum, the position can be cheaply reconstructed from a depth value.
        /// </summary>
        public Vector3[] BoundingFrustum()
        {
            BoundingFrustum boundingFrustum = new BoundingFrustum(ViewMatrix * ProjectionMatrix);
            Vector3[] cornersWorldSpace = boundingFrustum.GetCorners();
            Vector3[] cornersViewSpace = new Vector3[4];
            // Transform form world space to view space
            for (int i = 0; i < 4; i++)
            {
                cornersViewSpace[i] = Vector3.Transform(cornersWorldSpace[i + 4], ViewMatrix);
            }

            // Swap the last 2 values.
            Vector3 temp = cornersViewSpace[3];
            cornersViewSpace[3] = cornersViewSpace[2];
            cornersViewSpace[2] = temp;

            return cornersViewSpace;
        } // BoundingFrustum

        #endregion

        #region On Aspect Ratio Changed

        /// <summary>
        /// When the system aspect ratio changes then the projection matrix has to be recalculated.
        /// </summary>
        private void OnAspectRatioChanged(object sender, EventArgs e)
        {
            CalculateProjectionMatrix();
        } // OnAspectRatioChanged

        #endregion

        #region On World Matrix Changed

        /// <summary>
        /// On transform's world matrix changed.
        /// </summary>
        protected virtual void OnWorldMatrixChanged(Matrix worldMatrix)
        {
            // The view matrix is the invert
            cachedWorldMatrix = Matrix.Invert(worldMatrix);
        } // OnWorldMatrixChanged

        #endregion

        #region Pool

        // Pool for this type of components.
        private static readonly Pool<Camera> componentPool = new Pool<Camera>(20);

        /// <summary>
        /// Pool for this type of components.
        /// </summary>
        internal static Pool<Camera> ComponentPool { get { return componentPool; } }

        #endregion

    } // Camera
} // XNAFinalEngine.Components