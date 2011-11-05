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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAFinalEngine.Assets;
using XNAFinalEngine.EngineCore;
using XNAFinalEngine.Helpers;
using Texture = XNAFinalEngine.Assets.Texture;
#endregion

namespace XNAFinalEngine.Graphics
{
    /// <summary>
    /// Über post processing shader.
    /// </summary>
    internal static class PostProcessingPass
    {

        #region Variables

        // Post process shader.
        private static PostProcessingShader postProcessingShader;

        #endregion
        
        #region Process

        /// <summary>
        /// Process
        /// </summary>
        /// <param name="sceneTexture">Linear space HDR scene texture.</param>
        /// <param name="depthTexture">Depth texture.</param>
        /// <param name="postProcess">Post process parameters.</param>
        /// <returns>The gamma space post process texture of the linear space scene texture.</returns>
        public static RenderTarget Process(Texture sceneTexture, Texture depthTexture, PostProcess postProcess)
        {
            if (sceneTexture == null || sceneTexture.Resource == null)
                throw new ArgumentNullException("sceneTexture");
            if (postProcess == null)
                throw new ArgumentNullException("postProcess");
            
            try
            {
                // Generate bloom texture
                RenderTarget bloomTexture = null;
                if (postProcess.Bloom != null && postProcess.Bloom.Enabled)
                    bloomTexture = BloomShader.Instance.Render(sceneTexture, postProcess);

                // If the shader was not created...
                if (postProcessingShader == null)
                    postProcessingShader = new PostProcessingShader();
                
                // Post process the scene texture.
                RenderTarget postProcessedSceneTexture = postProcessingShader.Render(sceneTexture, postProcess, bloomTexture);
                if (bloomTexture != null)
                    RenderTarget.Release(bloomTexture);

                // Process MLAA
                if (postProcess.MLAA != null && postProcess.MLAA.Enabled)
                {
                    RenderTarget mlaaTexture = MLAAShader.Instance.Render(postProcessedSceneTexture, depthTexture, postProcess);
                    RenderTarget.Release(postProcessedSceneTexture);
                    return mlaaTexture;
                }
                return postProcessedSceneTexture;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Post Process: Unable to render.", e);
            }
        } // Render

        #endregion
        
    } // PostProcessingPass
} // XNAFinalEngine.Graphics