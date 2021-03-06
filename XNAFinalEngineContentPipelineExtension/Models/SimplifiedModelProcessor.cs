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
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
#endregion

namespace XNAFinalEngineContentPipelineExtension.Models
{
    /// <summary>
    /// Custom processor that ignores texture and shader assets.
    /// It also hides some of the content processor properties of the father.
    /// </summary>
    public class SimplifiedModelProcessor : ModelProcessor
    {

        #region Hide Unnecessary Properties

        [Browsable(false)]
        public override bool ColorKeyEnabled { get { return false; } set { } }

        [Browsable(false)]
        public override Color ColorKeyColor { get { return base.ColorKeyColor; } set { } }

        [Browsable(false)]
        public override bool ResizeTexturesToPowerOfTwo { get { return false; } set { } }

        [Browsable(false)]
        public override bool GenerateMipmaps { get { return false; } set { } }

        [Browsable(false)]
        public override TextureProcessorOutputFormat TextureFormat { get { return TextureProcessorOutputFormat.NoChange; } set { } }

        [Browsable(false)]
        public override MaterialProcessorDefaultEffect DefaultEffect { get { return MaterialProcessorDefaultEffect.BasicEffect; } set { } }

        [Browsable(false)]
        public override bool PremultiplyTextureAlpha { get { return false; } set { } }

        [Browsable(false)]
        public override bool PremultiplyVertexColors { get { return false; } set { } }
        
        #endregion
        
        /// <summary>
        /// Ignores material and texture information.
        /// </summary>
        protected override MaterialContent ConvertMaterial(MaterialContent material, ContentProcessorContext context)
        {
            return null;
        } // ConvertMaterial

    } // SimplifiedModelProcessor
} // XNAFinalEngineContentPipelineExtension.Models
