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
#if (!XBOX)
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Xna.Framework.Content;
    using System.Windows.Forms;
#endif
#endregion

namespace XNAFinalEngineContentPipelineExtensionRuntime.UserInterface
{

    #if (!XBOX)

    /// <summary>
    /// Cursor reader.
    /// </summary>
    public class CursorReader : ContentTypeReader<Cursor>
    {

        protected override Cursor Read(ContentReader input, Cursor existingInstance)
        {
            if (existingInstance == null)
            {
                int count = input.ReadInt32();
                byte[] data = input.ReadBytes(count);

                string path = Path.GetTempFileName();
                File.WriteAllBytes(path, data);

                IntPtr handle = LoadCursor(path);
                Cursor cur = new Cursor(handle);
                File.Delete(path);

                return cur;
            }

            return existingInstance;
        } // Read

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(IntPtr instance, string fileName, uint type, int width, int height, uint load);

        private static IntPtr LoadCursor(string fileName)
        {
            return LoadImage(IntPtr.Zero, fileName, 2, 0, 0, 0x0010);
        } // LoadCursor

    } // CursorReader

    #endif

} // XNAFinalEngineContentPipelineExtensionRuntime.UserInterface

