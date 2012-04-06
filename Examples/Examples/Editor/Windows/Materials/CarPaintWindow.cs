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
using Microsoft.Xna.Framework.Input;
using XNAFinalEngine.Assets;
using XNAFinalEngine.UserInterface;
using Microsoft.Xna.Framework;
#endregion

namespace XNAFinalEngine.Editor
{
    public static class CarPaintWindow
    {

        /// <summary>
        /// Creates and shows the configuration window of this material.
        /// </summary>
        public static void Show(CarPaint asset)
        {

            #region Window

            var window = new AssetWindow
            {
                AssetName = asset.Name,
                AssetType = "Car-Pain"
            };
            window.AssetNameChanged += delegate
            {
                asset.Name = window.AssetName;
                window.AssetName = asset.Name; // If the new name is not unique
            };
            window.Draw += delegate { window.AssetName = asset.Name; };

            #endregion

            #region Group Diffuse

            GroupBox groupDiffuse = CommonControls.Group("Diffuse", window);

            #region Base Paint Color

            var sliderBasePaintColor = new SliderColor
            {
                Parent = groupDiffuse,
                Left = 10,
                Top = 20,
                Color = asset.BasePaintColor,
                Text = "Base Paint Color",
            };
            sliderBasePaintColor.ColorChanged += delegate { asset.BasePaintColor = sliderBasePaintColor.Color; };
            sliderBasePaintColor.Draw += delegate { sliderBasePaintColor.Color = asset.BasePaintColor; };

            #endregion

            #region Second Base Paint Color

            var sliderSecondBasePaintColor = new SliderColor
            {
                Parent = groupDiffuse,
                Left = 10,
                Top = sliderBasePaintColor.Top + sliderBasePaintColor.Height + 20,
                Color = asset.SecondBasePaintColor,
                Text = "Second Base Paint Color",
            };
            sliderSecondBasePaintColor.ColorChanged += delegate { asset.SecondBasePaintColor = sliderSecondBasePaintColor.Color; };
            sliderSecondBasePaintColor.Draw += delegate { sliderSecondBasePaintColor.Color = asset.SecondBasePaintColor; };

            #endregion

            #region Flake Layer Color 1

            var sliderFlakeLayerColor1 = CommonControls.SliderColor("Flake Layer Color 1", groupDiffuse, asset.FlakeLayerColor1);
            sliderFlakeLayerColor1.ColorChanged += delegate { asset.FlakeLayerColor1 = sliderFlakeLayerColor1.Color; };
            sliderFlakeLayerColor1.Draw += delegate { sliderFlakeLayerColor1.Color = asset.FlakeLayerColor1; };

            #endregion

            groupDiffuse.AdjustHeightFromChildren();

            #endregion

            #region Group Specular

            GroupBox groupSpecular = new GroupBox
            {
                Parent = window,
                Anchor = Anchors.Left | Anchors.Top | Anchors.Right,
                Width = window.ClientWidth - 16,
                Height = 160,
                Left = 8,
                Top = groupDiffuse.Top + groupDiffuse.Height + 15,
                Text = "Specular",
                TextColor = Color.Gray,
            };

            #region Specular Intensity

            var sliderSpecularIntensity = new SliderNumeric
            {
                Parent = groupSpecular,
                Left = 10,
                Top = 25,
                Value = asset.SpecularIntensity,
                Text = "Specular Intensity",
                IfOutOfRangeRescale = false,
                ValueCanBeOutOfRange = true,
                MinimumValue = 0,
                MaximumValue = 2,
            };
            sliderSpecularIntensity.ValueChanged += delegate
            {
                asset.SpecularIntensity = sliderSpecularIntensity.Value;
            };
            sliderSpecularIntensity.Draw += delegate { sliderSpecularIntensity.Value = asset.SpecularIntensity; };

            #endregion

            #region Specular Power

            var sliderSpecularPower = new SliderNumeric
            {
                Parent = groupSpecular,
                Left = 10,
                Top = 10 + sliderSpecularIntensity.Top + sliderSpecularIntensity.Height,
                Value = asset.SpecularPower,
                Text = "Specular Power",
                IfOutOfRangeRescale = false,
                ValueCanBeOutOfRange = true,
                MinimumValue = 0,
                MaximumValue = 100,
            };
            sliderSpecularPower.ValueChanged += delegate
            {
                asset.SpecularPower = sliderSpecularPower.Value;
            };
            sliderSpecularPower.Draw += delegate { sliderSpecularPower.Value = asset.SpecularPower; };

            #endregion

            #region Specular Texture

            var labelSpecularTexture = new Label
            {
                Parent = groupSpecular,
                Left = 10,
                Top = 10 + sliderSpecularPower.Top + sliderSpecularPower.Height,
                Width = 150,
                Text = "Specular Texture"
            };
            var comboBoxSpecularTexture = new ComboBox
            {
                Parent = groupSpecular,
                Left = labelSpecularTexture.Left + labelSpecularTexture.Width,
                Top = 10 + sliderSpecularPower.Top + sliderSpecularPower.Height,
                Height = 20,
                Anchor = Anchors.Left | Anchors.Top | Anchors.Right,
                MaxItemsShow = 25,
            };
            comboBoxSpecularTexture.Width = groupSpecular.Width - 10 - comboBoxSpecularTexture.Left;
            // Add textures name
            comboBoxSpecularTexture.Items.Add("No texture");
            comboBoxSpecularTexture.Items.AddRange(Texture.TexturesFilenames);
            // Events
            comboBoxSpecularTexture.ItemIndexChanged += delegate
            {
                if (comboBoxSpecularTexture.ItemIndex <= 0)
                    asset.SpecularTexture = null;
                else
                {
                    if (asset.SpecularTexture == null || asset.SpecularTexture.Name != (string)comboBoxSpecularTexture.Items[comboBoxSpecularTexture.ItemIndex])
                        asset.SpecularTexture = new Texture((string)comboBoxSpecularTexture.Items[comboBoxSpecularTexture.ItemIndex]);
                }
            };
            comboBoxSpecularTexture.Draw += delegate
            {
                if (comboBoxSpecularTexture.ListBoxVisible)
                    return;
                // Identify current index
                if (asset.SpecularTexture == null)
                    comboBoxSpecularTexture.ItemIndex = 0;
                else
                {
                    for (int i = 0; i < comboBoxSpecularTexture.Items.Count; i++)
                        if ((string)comboBoxSpecularTexture.Items[i] == asset.SpecularTexture.Name)
                        {
                            comboBoxSpecularTexture.ItemIndex = i;
                            break;
                        }
                }
            };

            #endregion

            #region Specular Texture Power Enabled

            var checkBoxSpecularTexturePowerEnabled = new CheckBox
            {
                Parent = groupSpecular,
                Left = 10,
                Top = 10 + comboBoxSpecularTexture.Top + comboBoxSpecularTexture.Height,
                Width = window.ClientWidth - 16,
                Checked = asset.SpecularTexturePowerEnabled,
                Text = " Specular Texture Power Enabled",
                ToolTip =
                {
                    Text = "Indicates if the specular power will be read from the texture (the alpha channel of the specular texture) or from the specular power property."
                }
            };
            checkBoxSpecularTexturePowerEnabled.CheckedChanged += delegate
            {
                asset.SpecularTexturePowerEnabled = checkBoxSpecularTexturePowerEnabled.Checked;
            };
            checkBoxSpecularTexturePowerEnabled.Draw += delegate { checkBoxSpecularTexturePowerEnabled.Checked = asset.SpecularTexturePowerEnabled; };

            #endregion

            groupSpecular.AdjustHeightFromChildren();
            
            #endregion

            #region Flakes

            var groupFlakes = CommonControls.Group("Flakes", window);

            #region Flakes Color

            var sliderFlakesColor = CommonControls.SliderColor("Flake Color", groupFlakes, asset.FlakesColor);
            sliderFlakesColor.ColorChanged += delegate { asset.FlakesColor = sliderFlakesColor.Color; };
            sliderFlakesColor.Draw += delegate { sliderFlakesColor.Color = asset.FlakesColor; };

            #endregion

            #region Flakes Scale

            var sliderFlakesScale = CommonControls.SliderNumeric("Flakes Scale", groupFlakes, asset.FlakesScale, true, true, 0, 500);
            sliderFlakesScale.ValueChanged += delegate { asset.FlakesScale = sliderFlakesScale.Value; };
            sliderFlakesScale.Draw += delegate { sliderFlakesScale.Value = asset.FlakesScale; };

            #endregion

            #region Flakes Exponent

            var sliderFlakesExponent = CommonControls.SliderNumeric("Flakes Exponent", groupFlakes, asset.FlakesExponent, true, true, 0, 500);
            sliderFlakesExponent.ValueChanged += delegate { asset.FlakesExponent = sliderFlakesExponent.Value; };
            sliderFlakesExponent.Draw += delegate { sliderFlakesExponent.Value = asset.FlakesExponent; };

            #endregion

            #region Flake Perturbation

            var sliderFlakePerturbation = CommonControls.SliderNumeric("Flake Perturbation", groupFlakes, asset.MicroflakePerturbation, false, false, -1, 1);
            sliderFlakePerturbation.ValueChanged += delegate { asset.MicroflakePerturbation = sliderFlakePerturbation.Value; };
            sliderFlakePerturbation.Draw += delegate { sliderFlakePerturbation.Value = asset.MicroflakePerturbation; };

            #endregion

            #region Flake Perturbation A

            var sliderFlakePerturbationA = CommonControls.SliderNumeric("Flake Perturbation A", groupFlakes, asset.MicroflakePerturbationA, false, false, 0, 1);
            sliderFlakePerturbationA.ValueChanged += delegate { asset.MicroflakePerturbationA = sliderFlakePerturbationA.Value; };
            sliderFlakePerturbationA.Draw += delegate { sliderFlakePerturbationA.Value = asset.MicroflakePerturbationA; };

            #endregion

            #region Normal Perturbation

            var sliderNormalPerturbation = CommonControls.SliderNumeric("Normal Perturbation", groupFlakes, asset.NormalPerturbation, false, false, -1, 1);
            sliderNormalPerturbation.ValueChanged += delegate { asset.NormalPerturbation = sliderNormalPerturbation.Value; };
            sliderNormalPerturbation.Draw += delegate { sliderNormalPerturbation.Value = asset.NormalPerturbation; };

            #endregion

            groupFlakes.AdjustHeightFromChildren();

            #endregion
            
            window.Height = 500;

        } // Show

    } // CarPaintWindow
} // XNAFinalEngine.Editor