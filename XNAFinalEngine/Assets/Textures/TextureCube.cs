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
using System.IO;
using System.Linq;

#endregion

namespace XNAFinalEngine.Assets
{

	/// <summary>
	/// LDR or HDR Cube Maps.
	/// HDR can be stored in the RGBM format.
	/// </summary>
    public class TextureCube : Asset
    {

        #region Variables
                
        // XNA Texture.
        protected Microsoft.Xna.Framework.Graphics.TextureCube xnaTextureCube;
        
        // The size of this texture resource, in pixels.
	    protected int size;

        #endregion

        #region Properties

        #region Name

        /// <summary>
        /// The name of the asset.
        /// If the name already exists then we add one to its name and we call it again.
        /// </summary>
        public override string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    // Is the name unique?
                    bool isUnique = LoadedCubeTextures.All(assetFromList => assetFromList == this || assetFromList.Name != value);
                    if (isUnique)
                    {
                        name = value;
                        LoadedCubeTextures.Sort(CompareAssets);
                    }
                    // If not then we add one to its name and find out if is unique.
                    else
                        Name = NamePlusOne(value);
                }
            }
        } // Name

        #endregion

	    /// <summary>
	    /// XNA Texture.
	    /// </summary>
	    public virtual Microsoft.Xna.Framework.Graphics.TextureCube Resource { get { return xnaTextureCube; } }

        /// <summary>
        /// The size of this texture resource, in pixels.
        /// </summary>
        public int Size { get { return size; } }

        /// <summary>
        /// Is it in RGBM format?
        /// </summary>
        public bool IsRgbm { get; set; }

        /// <summary>
        /// RGBM Max Range.
        /// </summary>
        public float RgbmMaxRange { get; set; }

        /// <summary>
        ///  A list with all texture' filenames on the cube texture directory.
        /// </summary>
        public static string[] TexturesFilename { get; private set; }

        /// <summary>
        /// Loaded Cube Textures.
        /// </summary>
        public static List<TextureCube> LoadedCubeTextures { get; private set; }

        /// <summary>
        ///  A list with all texture' filenames on the cube texture directory.
        /// </summary>
        /// <remarks>
        /// If there are memory limitations, this list could be eliminated for the release version.
        /// This is use only for the editor.
        /// </remarks>
        public static string[] TexturesFilenames { get; private set; }
    
        #endregion

        #region Constructor

	    /// <summary>
	    /// Create cube map from given filename.
	    /// </summary>
	    /// <param name="filename">Set filename, must be relative and be a valid file in the textures directory.</param>
	    /// <param name="isRgbm">is in RGBM format?</param>
        /// <param name="rgbmMaxRange">RGBM Max Range.</param>
	    public TextureCube(string filename, bool isRgbm = false, float rgbmMaxRange = 50.0f)
		{
            Name = filename;
		    IsRgbm = isRgbm;
	        RgbmMaxRange = rgbmMaxRange;
            Filename = ContentManager.GameDataDirectory + "Textures\\CubeTextures\\" + filename;
            if (File.Exists(Filename + ".xnb") == false)
            {
                throw new ArgumentException("Failed to load cube map: File " + Filename + " does not exists!");
            }
            try
            {
                xnaTextureCube = ContentManager.CurrentContentManager.XnaContentManager.Load<Microsoft.Xna.Framework.Graphics.TextureCube>(Filename);
                ContentManager = ContentManager.CurrentContentManager;
                size = xnaTextureCube.Size;
                Resource.Name = filename;
            }
            catch (ObjectDisposedException)
            {
                throw new InvalidOperationException("Content Manager: Content manager disposed");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to load cube map: " + filename, e);
            }
            LoadedCubeTextures.Add(this);
            LoadedCubeTextures.Sort(CompareAssets);
		} // TextureCube

		#endregion

        #region Static Constructor

        /// <summary>
        /// Search the available cube textures.
        /// </summary>
        static TextureCube()
        {
            TexturesFilenames = SearchAssetsFilename(ContentManager.GameDataDirectory + "Textures\\CubeTextures");
            LoadedCubeTextures = new List<TextureCube>();
        } // Texture

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose managed resources.
        /// </summary>
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            LoadedCubeTextures.Remove(this);
        } // DisposeManagedResources

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            xnaTextureCube = ContentManager.CurrentContentManager.XnaContentManager.Load<Microsoft.Xna.Framework.Graphics.TextureCube>(Filename);
        } // RecreateResource

        #endregion

    } // TextureCube
} // XNAFinalEngine.Assets

