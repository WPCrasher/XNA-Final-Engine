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
using Microsoft.Xna.Framework.Graphics;
using XNAFinalEngine.Assets;
using XNAFinalEngine.UserInterface;
using Texture = XNAFinalEngine.Assets.Texture;
#endregion

namespace XNAFinalEngine.Editor
{
    internal static class TextureControls
    {

        #region Variables

        // Not all samplers states are the XNA predefined states.
        // I don't give the possibility (yet) of changing the values, but at least the user can maintained.
        private static SamplerState customSamplerState;

        #endregion

        #region Show

        /// <summary>
        /// Creates the configuration controls of this asset.
        /// </summary>
        public static void AddControls(Texture asset, Window owner, ComboBox comboBoxResource)
        {
            // In asset creation I need to look on the CurrentCreatedAsset property to have the last asset.
            // I can't use CurrentCreatedAsset in edit mode.
            // However I can use asset for creation (maybe in a disposed state but don't worry) and edit mode,
            // and only update the values when I know that CurrentCreatedAsset changes.
            
            #region Group Image

            var groupImage = CommonControls.Group("Image", owner);
            var imageBoxImage = CommonControls.ImageBox(asset, groupImage);
            groupImage.AdjustHeightFromChildren();

            #endregion
            
            #region Group Properties

            GroupBox groupProperties = CommonControls.Group("Properties", owner);

            var widthTextBox = CommonControls.TextBox("Width", groupProperties, asset.Width.ToString());
            widthTextBox.Enabled = false;

            var heightTextBox = CommonControls.TextBox("Height", groupProperties, asset.Height.ToString());
            heightTextBox.Enabled = false;

            #region Prefered Sampler State

            var comboBoxPreferredSamplerState = CommonControls.ComboBox("Prefered Sampler State", groupProperties);
            comboBoxPreferredSamplerState.Items.Add("AnisotropicClamp");
            comboBoxPreferredSamplerState.Items.Add("AnisotropicWrap");
            comboBoxPreferredSamplerState.Items.Add("LinearClamp");
            comboBoxPreferredSamplerState.Items.Add("LinearWrap");
            comboBoxPreferredSamplerState.Items.Add("PointClamp");
            comboBoxPreferredSamplerState.Items.Add("PointWrap");
            
            comboBoxPreferredSamplerState.ItemIndexChanged += delegate { asset.PreferredSamplerState = GetSamplerState(comboBoxPreferredSamplerState.ItemIndex); };
            comboBoxPreferredSamplerState.Draw += delegate
            {
                if (comboBoxPreferredSamplerState.ListBoxVisible)
                    return;
                // Identify current index
                if (asset.PreferredSamplerState == SamplerState.AnisotropicClamp)
                    comboBoxPreferredSamplerState.ItemIndex = 0;
                else if (asset.PreferredSamplerState == SamplerState.AnisotropicWrap)
                    comboBoxPreferredSamplerState.ItemIndex = 1;
                else if (asset.PreferredSamplerState == SamplerState.LinearClamp)
                    comboBoxPreferredSamplerState.ItemIndex = 2;
                else if (asset.PreferredSamplerState == SamplerState.LinearWrap)
                    comboBoxPreferredSamplerState.ItemIndex = 3;
                else if (asset.PreferredSamplerState == SamplerState.PointClamp)
                    comboBoxPreferredSamplerState.ItemIndex = 4;
                else if (asset.PreferredSamplerState == SamplerState.PointWrap)
                    comboBoxPreferredSamplerState.ItemIndex = 5;
                else
                {
                    if (customSamplerState == null)
                    {
                        comboBoxPreferredSamplerState.Items.Add("Custom");
                        customSamplerState = asset.PreferredSamplerState;
                    }
                    comboBoxPreferredSamplerState.ItemIndex = 6;
                }
            };

            #endregion

            groupProperties.AdjustHeightFromChildren();

            #endregion

            // If it is asset creation time.
            if (comboBoxResource != null)
            {
                comboBoxResource.ItemIndexChanged += delegate
                {
                    // Update properties if the resource changes.
                    imageBoxImage.Texture = ((Texture)AssetWindow.CurrentCreatedAsset);
                    widthTextBox.Text     = ((Texture)AssetWindow.CurrentCreatedAsset).Width.ToString();
                    heightTextBox.Text    = ((Texture)AssetWindow.CurrentCreatedAsset).Height.ToString();
                };
                // If the user creates the asset (pressed the create button) then update the changeable properties.
                owner.Closed += delegate
                {
                    if (owner.ModalResult != ModalResult.Cancel)
                        ((Texture)AssetWindow.CurrentCreatedAsset).PreferredSamplerState = GetSamplerState(comboBoxPreferredSamplerState.ItemIndex);
                };
            }
        } // AddControls

        #endregion

        #region Get Sampler State

        private static SamplerState GetSamplerState(int index)
        {
            switch (index)
            {
                case 0:  return SamplerState.AnisotropicClamp;
                case 1:  return SamplerState.AnisotropicWrap;
                case 2:  return SamplerState.LinearClamp;
                case 3:  return SamplerState.LinearWrap;
                case 4:  return SamplerState.PointClamp;
                case 5:  return SamplerState.PointWrap;
                case 6:  return customSamplerState;
                default: return SamplerState.AnisotropicWrap;
            }
        } // GetSamplerState

        #endregion

    } // TextureControls
} // XNAFinalEngine.Editor