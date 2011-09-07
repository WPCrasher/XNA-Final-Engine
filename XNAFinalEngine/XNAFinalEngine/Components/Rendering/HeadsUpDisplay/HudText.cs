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
using System.Text;
using Microsoft.Xna.Framework;
using XNAFinalEngine.Assets;
using XNAFinalEngine.Helpers;
#endregion

namespace XnaFinalEngine.Components
{
    /// <summary>
    /// Display text into the HUD.
    /// This component works with 2D and 3D game objects.
    /// </summary>
    public class HudText : HudElement
    {

        #region Variables

        // The size value is arbitrary.
        private readonly StringBuilder text = new StringBuilder(20);

        #endregion

        #region Properties

        /// <summary>
        /// The Font to use when rendering the text.
        /// </summary>
        public Font Font { get; set; }

        /// <summary>
        /// Text Color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// The string to display.
        /// The reason why the set is not available is because we want to avoid garbage at any cost.
        /// Also don’t use the “+” operator with this type.
        /// </summary>
        /// <remarks>A String Builder type is used to avoid garbage collection.</remarks>
        public StringBuilder Text { get { return text; } }
        
        #endregion

        #region Initialize

        /// <summary>
        /// Initialize the component. 
        /// </summary>
        internal override void Initialize(GameObject owner)
        {
            Font = null;
            Color = Color.White;
            Text.Clear();
            base.Initialize(owner);
        } // Initialize

        #endregion

        #region Measure String

        /// <summary>
        /// Returns the width and height of a string as a Vector2.
        /// </summary>
        public Vector2 MeasureString(string text)
        {
            if (Font == null)
            {
                throw new InvalidOperationException("HudText: There is no Font set.");
            }
            return Font.MeasureString(text);
        } // MeasureString

        /// <summary>
        /// Returns the width and height of a string as a Vector2.
        /// http://msdn.microsoft.com/en-us/library/system.text.stringbuilder.aspx
        /// StringBuilder is good for avoid garbage.
        /// </summary>
        public Vector2 MeasureString(StringBuilder text)
        {
            if (Font == null)
            {
                throw new InvalidOperationException("HudText: There is no Font set.");
            }
            return Font.MeasureString(text);
        } // MeasureString

        #endregion

        #region Pool
        
        /// <summary>
        /// Pool for 2D HUD Text components
        /// </summary>
        private static readonly Pool<HudText> hudTextPool2D = new Pool<HudText>(20);

        /// <summary>
        /// Pool for 2D HUD Text components.
        /// </summary>
        internal static Pool<HudText> HudTextPool2D { get { return hudTextPool2D; } }

        #endregion

    } // HudText
} // XnaFinalEngine.Components