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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using XNAFinalEngine.Assets;
using XNAFinalEngine.Audio;
using XNAFinalEngine.EngineCore;
using XNAFinalEngine.Helpers;
#endregion

namespace XNAFinalEngine.Components
{

    /// <summary>
    /// Sound Listener.
    /// </summary>
    /// <remarks>
    /// When a device is disposed the sound are stopped.
    /// This could be avoided but this scenario is very rare and the sound has to start over.
    /// </remarks>
    public class SoundEmitter : Component
    {
        
        #region Variables

        // Sound Effect Instance.
        private SoundEffectInstance soundEffectInstance;
        
        /// <summary>
        /// This is the cached world matrix from the transform component.
        /// This matrix represents the view matrix.
        /// </summary>
        internal Matrix cachedWorldMatrix;

        // Sound properties.
        private float volume, pan, pitch, dopplerScale;

        // XNA audio emitter, used for 3D sounds.
        private readonly AudioEmitter audioEmitter = new AudioEmitter();

        // For velocity calculations.
        private Vector3 oldPosition;

        // Sound asset.
        private Sound sound;

        // The Apply3D method needs an audio listener, but when I play a sound I don't have this information so I use an empty audio listener.
        // To avoid garbage I created beforehand.
        private static readonly AudioListener emptyAudioListener = new AudioListener();
        // Apply3D produces garbage if you don't use a audio listener array. Go figure.
        private static readonly AudioListener[] oneAudioListener = new AudioListener[1];

        #endregion

        #region Properties
        
        /// <summary>
        /// Sound Effect Instance.
        /// </summary>
        private SoundEffectInstance SoundEffectInstance
        {
            get
            {
                if (soundEffectInstance != null && soundEffectInstance.IsDisposed)
                    soundEffectInstance = null;
                return soundEffectInstance;
            }
            set { soundEffectInstance = value; }
        } // SoundEffectInstance

        /// <summary>
        /// Sound Volume.
        /// </summary>
        /// <value>Volume, ranging from 0.0f (silence) to 1.0f (full volume).</value>
        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                if (volume < 0)
                    volume = 0;
                if (volume > 1)
                    volume = 1;
            }
        } // Volume
        
        /// <summary>
        /// Sound asset.
        /// </summary>
        public Sound Sound
        {
            get { return sound; }
            set
            {
                if (SoundEffectInstance != null)
                    Stop();
                sound = value;
            }
        } // Sound

        /// <summary>
        /// Gets or sets the panning.
        /// </summary>
        /// <value>Panning, ranging from -1.0f (full left) to 1.0f (full right). 0.0f is centered.</value>
        public float Pan
        {
            get { return pan; }
            set
            {
                pan = value;
                if (pan < -1)
                    pan = -1;
                if (pan > 1)
                    pan = 1;
            }
        } // Pan

        /// <summary>
        /// Gets or sets the pitch.
        /// </summary>
        /// <value>Pitch adjustment, ranging from -1.0f (down one octave) to 1.0f (up one octave). 0.0f is unity (normal) pitch.</value>
        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = value;
                if (pitch < -1)
                    pitch = -1;
                if (pitch > 1)
                    pitch = 1;
            }
        } // Pitch

        /// <summary>
        /// Gets or sets a value that adjusts the effect of doppler calculations on the sound (emitter).
        /// </summary>
        /// <value>Value that scales doppler calculations on the sound (emitter).</value>
        /// <remarks>
        /// DopplerScale changes the relative velocities of emitters and listeners.
        /// If sounds are shifting (pitch) too much for the given relative velocity of the emitter and listener, decrease the DopplerScale.
        /// If sounds are not shifting enough for the given relative velocity of the emitter and listener, increase the DopplerScale.
        /// 
        /// 0.0 < DopplerScale <= 1.0 --> Decreases the effect of Doppler
        /// DopplerScale > 1.0 --> Increase the effect of Doppler
        /// </remarks>
        public float DopplerScale
        {
            get { return dopplerScale; }
            set
            {
                dopplerScale = value;
                if (dopplerScale < 0)
                    dopplerScale = 0;
            }
        } // DopplerScale
       
        /// <summary>
        /// Gets the current state (playing, paused, or stopped) 
        /// </summary>
        public SoundState State
        {
            get
            {
                if (SoundEffectInstance == null)
                    return SoundState.Stopped;
                return SoundEffectInstance.State;
            }
        } // State
        
        #endregion

        #region Initialize

        /// <summary>
        /// Initialize the component. 
        /// </summary>
        internal override void Initialize(GameObject owner)
        {
            base.Initialize(owner);
            // Values //
            volume = 1;
            Pan = 0;
            Pitch = 0;
            dopplerScale = 1;
            // Cache transform matrix. It will be the view matrix.
            cachedWorldMatrix = ((GameObject3D)Owner).Transform.WorldMatrix;
            ((GameObject3D)Owner).Transform.WorldMatrixChanged += OnWorldMatrixChanged;
            oldPosition = cachedWorldMatrix.Translation;
        } // Initialize
        
        #endregion

        #region Uninitialize

        /// <summary>
        /// Uninitialize the component.
        /// Is important to remove event associations and any other reference.
        /// </summary>
        internal override void Uninitialize()
        {
            if (SoundEffectInstance != null)
                SoundEffectInstance.Stop();
            SoundEffectInstance = null;
            Sound = null;
            ((GameObject3D)Owner).Transform.WorldMatrixChanged -= OnWorldMatrixChanged;
            // Call this last because the owner information is needed.
            base.Uninitialize();
        } // Uninitialize

        #endregion

        #region Play, Pause, Resume, Stop

        /// <summary>
        /// Plays or resumes a sound. 
        /// </summary>
        /// <remarks>
        /// An emitter could only play one sound instance at a time.
        /// </remarks>
        public void Play()
        {
            if (SoundEffectInstance == null || SoundEffectInstance.State == SoundState.Stopped)
            {
                SoundEffectInstance = SoundManager.FetchSoundInstance(Sound);
                // If the sound instance could not be created then do nothing.
                if (SoundEffectInstance == null)
                    return;
                SoundEffectInstance.Pitch = Pitch;
                SoundEffectInstance.Volume = Volume;
                // We need to activate the 3D support before play it, but we don’t want to know anything about the listener in this stage.
                if (Sound.Type == Sound.SoundType.Sound3D)
                {
                    audioEmitter.DopplerScale = DopplerScale;
                    audioEmitter.Forward = cachedWorldMatrix.Forward;
                    audioEmitter.Up = cachedWorldMatrix.Up;
                    audioEmitter.Position = cachedWorldMatrix.Translation;
                    if (Time.SmoothFrameTime > 0)
                        audioEmitter.Velocity = (audioEmitter.Position - oldPosition) / Time.SmoothFrameTime;
                    else
                        audioEmitter.Velocity = Vector3.Zero;
                    // Distance / Time
                    oldPosition = audioEmitter.Position;
                    // Apply3D produces garbage if you don't use a audio listener array. Go figure.
                    oneAudioListener[0] = emptyAudioListener;
                    SoundEffectInstance.Apply3D(oneAudioListener, audioEmitter);
                }
                else
                {
                    SoundEffectInstance.Pan = Pan;
                }
                SoundEffectInstance.Play();
            }
            else if (SoundEffectInstance.State == SoundState.Paused)
            {
                SoundEffectInstance.Resume();
            }
        } // Play

        /// <summary>
        /// Stops playing the sound.
        /// </summary>
        /// <param name="immediate">
        /// Specifies whether to stop playing immediately, or to break out of the loop region and play the release.
        /// Specify true to stop playing immediately, or false to break out of the loop region and play the release phase (the remainder of the sound).
        /// </param>
        public void Stop(bool immediate = true)
        {
            if (SoundEffectInstance != null)
                SoundEffectInstance.Stop(immediate);
            if (immediate)
                SoundEffectInstance = null;
        } // Stop

        /// <summary>
        /// Pauses the sound.
        /// </summary>
        public void Pause()
        {
            if (SoundEffectInstance != null && SoundEffectInstance.State == SoundState.Playing)
            {
                SoundEffectInstance.Pause();
            }
        } // Pause

        /// <summary>
        /// Resumes playback for this sound.
        /// </summary>
        public void Resume()
        {
            if (SoundEffectInstance != null && SoundEffectInstance.State == SoundState.Paused)
            {
                SoundEffectInstance.Resume();
            }
        } // Resume
        
        #endregion

        #region Update
        
        /// <summary>
        /// Update sound emitter.
        /// </summary>
        internal void Update(AudioListener audioListener)
        {
            if (SoundEffectInstance != null)
            {
                // If the sound ends.
                if (SoundEffectInstance.State == SoundState.Stopped)
                {
                    Stop();
                }
                else if (SoundEffectInstance.State == SoundState.Playing)
                {
                    SoundEffectInstance.Pitch = Pitch;
                    SoundEffectInstance.Volume = Volume;
                    // Update 3D information.
                    if (Sound.Type == Sound.SoundType.Sound3D)
                    {
                        audioEmitter.DopplerScale = DopplerScale;
                        audioEmitter.Forward = cachedWorldMatrix.Forward;
                        audioEmitter.Up = cachedWorldMatrix.Up;
                        audioEmitter.Position = cachedWorldMatrix.Translation;
                        if (Time.SmoothFrameTime > 0)
                            audioEmitter.Velocity = (audioEmitter.Position - oldPosition) / Time.SmoothFrameTime;
                        else
                            audioEmitter.Velocity = Vector3.Zero;
                        // Distance / Time
                        oldPosition = audioEmitter.Position;
                        if (audioListener == null)
                        {
                            throw new Exception("Sound Manager: 3D sounds need at least one sound listener.");
                        }
                        // Apply3D produces garbage if you don't use a audio listener array. Go figure.
                        oneAudioListener[0] = audioListener;
                        SoundEffectInstance.Apply3D(oneAudioListener, audioEmitter);
                    }
                    else
                    {
                        SoundEffectInstance.Pan = Pan;
                    }
                }
            }
            else
            {
                // Check distance
                // TODO!!
            }
        } // Update

        /// <summary>
        /// Update sound emitter.
        /// </summary>
        internal void Update(AudioListener[] audioListeners)
        {
            if (SoundEffectInstance != null)
            {
                // If the sound ends.
                if (SoundEffectInstance.State == SoundState.Stopped)
                    Stop();
                else if (SoundEffectInstance.State == SoundState.Playing)
                {
                    SoundEffectInstance.Pitch = Pitch;
                    SoundEffectInstance.Volume = Volume;
                    // Update 3D information.
                    if (Sound.Type == Sound.SoundType.Sound3D)
                    {
                        audioEmitter.DopplerScale = DopplerScale;
                        audioEmitter.Forward = cachedWorldMatrix.Forward;
                        audioEmitter.Up = cachedWorldMatrix.Up;
                        audioEmitter.Position = cachedWorldMatrix.Translation;
                        if (Time.SmoothFrameTime > 0)
                            audioEmitter.Velocity = (audioEmitter.Position - oldPosition)/Time.SmoothFrameTime;
                        else
                            audioEmitter.Velocity = Vector3.Zero;
                        // Distance / Time
                        oldPosition = audioEmitter.Position;
                        if (SoundEffectInstance.State == SoundState.Playing)
                        {
                            SoundEffectInstance.Apply3D(audioListeners, audioEmitter);
                        }
                    }
                    else
                    {
                        SoundEffectInstance.Pan = Pan;
                    }
                }
            }
            else
            {
                // Check distance
                // TODO!!
            }
        } // Update

        #endregion

        #region On World Matrix Changed

        /// <summary>
        /// On transform's world matrix changed.
        /// </summary>
        protected virtual void OnWorldMatrixChanged(Matrix worldMatrix)
        {
            cachedWorldMatrix = worldMatrix;            
        } // OnWorldMatrixChanged

        #endregion

        #region Pool

        // Pool for this type of components.
        private static readonly Pool<SoundEmitter> componentPool = new Pool<SoundEmitter>(20);

        /// <summary>
        /// Pool for this type of components.
        /// </summary>
        internal static Pool<SoundEmitter> ComponentPool { get { return componentPool; } }

        #endregion

    } // SoundEmitter
} // XNAFinalEngine.Components