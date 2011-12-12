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
using System.IO;
using Microsoft.Xna.Framework.Audio;
#endregion

namespace XNAFinalEngine.Assets
{
    /// <summary>
    /// Sound.
    /// </summary>
    public class Sound : Asset
    {
        
        #region Properties

        /// <summary>
        /// XNA sound effect.
        /// </summary>
        public SoundEffect Resource { get; private set; }

        /// <summary>
        /// Gets the duration of the sound (in seconds)
        /// </summary>
        public float Duration { get { return (float)Resource.Duration.TotalSeconds; } }

        /// <summary>
        /// Returns the speed of sound: 343.5 meters per second.
        /// </summary>
        public static float SpeedOfSound { get { return SoundEffect.SpeedOfSound; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Load sound.
        /// </summary>
        /// <param name="filename">The filename must be relative and be a valid file in the sound directory.</param>
        public Sound(string filename)
        {
            Name = filename;
            string fullFilename = ContentManager.GameDataDirectory + "Sounds\\" + filename;
            if (File.Exists(fullFilename + ".xnb") == false)
            {
                throw new Exception("Failed to load sound: File " + fullFilename + " does not exists!");
            }
            try
            {
                Resource = ContentManager.CurrentContentManager.XnaContentManager.Load<SoundEffect>(fullFilename);
            }
            catch (ObjectDisposedException e)
            {
                throw new Exception("Content Manager: Content manager disposed", e);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load sound: " + filename, e);
            }
        } // Sound

        #endregion

        #region Play

        /// <summary>
        /// Plays a sound.
        /// </summary>
        /// <remarks>
        /// Play returns false if too many sounds already are playing. 
        /// 
        /// To loop a sound or apply 3D effects, call CreateInstance instead of Play, and SoundEffectInstance.Play. 
        /// 
        /// Sounds play in a "fire and forget" fashion with Play.
        /// These sounds will play once, and then stop. They cannot be looped or 3D positioned.
        /// To loop a sound or apply 3D effects, use the sound components.
        /// 
        /// In the XBOX 360 only 300 sound instances could be exist at the same time.
        /// The sound system will not allow more than 290 instances at the same time, so you can throw some fire and forget sounds, but be careful.
        /// 
        /// With the sound’s duration you can implement a system that checks fire and forget sound, but is unnecessary and it hurts the performance a little.
        /// Besides, the sound emitter brings everything you need. Just use fire and forget sounds in testing or something similar.
        /// </remarks>
        /// <returns>true if the sound is playing back successfully; otherwise, false.</returns>
        public bool Play ()
        {
            return Resource.Play();
        } // Play

        /// <summary>
        /// Plays a sound.
        /// </summary>
        /// <remarks>
        /// Play returns false if too many sounds already are playing. 
        /// 
        /// To loop a sound or apply 3D effects, call CreateInstance instead of Play, and SoundEffectInstance.Play. 
        /// 
        /// Sounds play in a "fire and forget" fashion with Play.
        /// These sounds will play once, and then stop. They cannot be looped or 3D positioned.
        /// To loop a sound or apply 3D effects, use the sound components.
        /// 
        /// In the XBOX 360 only 300 sound instances could be exist at the same time.
        /// The sound system will not allow more than 290 instances at the same time, so you can throw some fire and forget sounds, but be careful.
        /// 
        /// With the sound’s duration you can implement a system that checks fire and forget sound, but is unnecessary and it hurts the performance a little.
        /// Besides, the sound emitter brings everything you need. Just use fire and forget sounds in testing or something similar.
        /// </remarks>
        /// <param name="volume">Volume, ranging from 0.0f (silence) to 1.0f (full volume). </param>
        /// <param name="pitch">Pitch adjustment, ranging from -1.0f (down one octave) to 1.0f (up one octave). 0.0f is unity (normal) pitch.</param>
        /// <param name="pan">Panning, ranging from -1.0f (full left) to 1.0f (full right). 0.0f is centered.</param>
        /// <returns>true if the sound is playing back successfully; otherwise, false.</returns>
        public bool Play(float volume, float pitch, float pan)
        {
            return Resource.Play(volume, pitch, pan);
        } // Play

        #endregion

    } // Sound
} // XNAFinalEngineBase.Assets