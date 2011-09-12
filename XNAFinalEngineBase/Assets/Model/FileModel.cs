
#region License
/*
Copyright (c) 2008-2011, Laboratorio de Investigaci�n y Desarrollo en Visualizaci�n y Computaci�n Gr�fica - 
                         Departamento de Ciencias e Ingenier�a de la Computaci�n - Universidad Nacional del Sur.
All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

�	Redistributions of source code must retain the above copyright, this list of conditions and the following disclaimer.

�	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer
    in the documentation and/or other materials provided with the distribution.

�	Neither the name of the Universidad Nacional del Sur nor the names of its contributors may be used to endorse or promote products derived
    from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

-----------------------------------------------------------------------------------------------------------------------------------------------
Author: Schneider, Jos� Ignacio (jis@cs.uns.edu.ar)
-----------------------------------------------------------------------------------------------------------------------------------------------

*/
#endregion

#region Using directives
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaModel = Microsoft.Xna.Framework.Graphics.Model;
#endregion

namespace XNAFinalEngine.Assets
{
    /// <summary>
    /// Support .X and fbx formats.
    /// The internal resource can�t be disposed individually. Use the Unload method from the Content Manager instead.
    /// </summary>
    public class FileModel : Model
    {

        #region Variables

        /// <summary>
        /// Transform matrices for each mesh part, we only have to get them once if the model is not animated.
        /// </summary>
        public Matrix[] transforms;
        
        #endregion

        #region Properties

        /// <summary>
        /// Underlying xna model object. Loaded with the content system.
        /// </summary>
        public XnaModel XnaModel { get; private set; }

        #region Vertices

        /// <summary>
        /// Get the vertices' positions of the model.
        /// </summary>   
        /// <remarks>This is a slow operation that generates garbage. We could store the vertices here, but there is no need to do this� for now.</remarks>
        public override Vector3[] Vectices
        {
            get
            {
                #region Vertex Count

                int vertexCount = 0;

                foreach (ModelMesh mesh in XnaModel.Meshes) // Foreach is faster and does not produce garbage.
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        vertexCount += part.NumVertices;
                    }
                }

                #endregion

                // The array of vertices positions
                Vector3[] verticesPosition = new Vector3[vertexCount];

                // Indicates where to write in the vertices array.
                int currentIndex = 0;

                foreach (ModelMesh mesh in XnaModel.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        // Read the format of the vertex buffer  
                        VertexDeclaration declaration = part.VertexBuffer.VertexDeclaration;
                        VertexElement[] vertexElements = declaration.GetVertexElements();
                        // Find the element that holds the position  
                        VertexElement vertexPosition = new VertexElement();
                        foreach (VertexElement vert in vertexElements)
                        {
                            if (vert.VertexElementUsage == VertexElementUsage.Position &&
                                vert.VertexElementFormat == VertexElementFormat.Vector3)
                            {
                                vertexPosition = vert;
                                // There should only be one  
                                break;
                            }
                        }
                        // Check the position element found is valid  
                        if (vertexPosition.VertexElementUsage != VertexElementUsage.Position || vertexPosition.VertexElementFormat != VertexElementFormat.Vector3)
                        {
                            throw new InvalidOperationException("Model's Vertices: Model uses unsupported vertex format!");
                        }
                        // This where we store the vertices until transformed  
                        Vector3[] partVertices = new Vector3[part.NumVertices];
                        // Read the vertices from the buffer in to the array  
                        part.VertexBuffer.GetData(part.VertexOffset * declaration.VertexStride + vertexPosition.Offset, partVertices, 0, part.NumVertices, declaration.VertexStride);
                        // Copy part vertices to the array of vertices of the model.
                        partVertices.CopyTo(verticesPosition, currentIndex);
                        currentIndex += part.NumVertices;
                    }
                }
                return verticesPosition;
            }
        } // Vertices

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Load a .x or fbx file.
        /// </summary>
        public FileModel(String filename)
        {
            Name = filename;
            string fullFilename = ContentManager.GameDataDirectory + "Models\\" + filename;
            if (File.Exists(fullFilename + ".xnb") == false)
            {
                throw new ArgumentException("Failed to load model: File " + fullFilename + " does not exists!", "filename");
            }
            try
            {
                XnaModel = ContentManager.CurrentContentManager.XnaContentManager.Load<XnaModel>(fullFilename);
                // Get matrices for each mesh part
                transforms = new Matrix[XnaModel.Bones.Count];
                XnaModel.CopyAbsoluteBoneTransformsTo(transforms);
                // Calcuate bounding volumes
                Vector3[] vectices = Vectices;
                boundingSphere = BoundingSphere.CreateFromPoints(vectices);
                boundingBox = BoundingBox.CreateFromPoints(vectices);
            }
            catch (ObjectDisposedException)
            {
                throw new InvalidOperationException("Content Manager: Content manager disposed");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to load model: " + filename, e);
            }
        } // FileModel

        #endregion

        #region Render
        /*
        /// <summary>
        /// Render the model.
        /// </summary>
        internal override void Render()
        {
            // Go through all meshes in the model
            foreach (ModelMesh mesh in XnaModel.Meshes)
			{
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Set vertex buffer and index buffer
                    EngineManager.Device.SetVertexBuffer(part.VertexBuffer);
                    EngineManager.Device.Indices = part.IndexBuffer;
                    // And render all primitives
                    EngineManager.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                }
            }
        } // Render
        */
        #endregion

    } // FileModel
} // XNAFinalEngine.Assets