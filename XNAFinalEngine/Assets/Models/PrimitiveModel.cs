
#region License
/*
Copyright (c) 2008-2012, Laboratorio de Investigaci�n y Desarrollo en Visualizaci�n y Computaci�n Gr�fica - 
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAFinalEngine.EngineCore;
#endregion

namespace XNAFinalEngine.Assets
{

    /// <summary>
    /// Base class to represent the geometrical information of primitive models (spheres, cubes, cylinders, cones)
    /// Limitation: the model created doesn't have the tangent information.
    /// In this case the best is to create the primitives in any 3D program and import them with "create tangent" activated in the content pipeline property.
    /// </summary>
    public abstract class PrimitiveModel : Model
    {

        #region Variables

        /// <summary>
        /// Number of vertices.
        /// </summary>
        protected int numberVertices;

        /// <summary>
        /// Vertex Buffer.
        /// </summary>
        protected VertexBuffer vertexBuffer;

        /// <summary>
        /// Number of indices.
        /// </summary>
        protected int numberIndices;

        /// <summary>
        /// Index Buffer.
        /// </summary>
        protected IndexBuffer indexBuffer;
                
        #endregion

        #region Properties

        /// <summary>
        /// Get the vertices' positions of the model.
        /// </summary>        
        /// <remarks>This is a slow operation that generates garbage. We could store the vertices here, but there is no need to do this� for now.</remarks>
        public override Vector3[] Vertices
        {
            get
            {
                Vector3[] verticesPosition = new Vector3[vertexBuffer.VertexCount];

                VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[vertexBuffer.VertexCount];
                vertexBuffer.GetData(vertices);

                for (int index = 0; index < vertices.Length; index++)
                {
                    verticesPosition[index] = vertices[index].Position;
                }

                return verticesPosition;
            }
        } // Vertices

        #endregion

        #region Constructor

        protected PrimitiveModel()
        {
            MeshesCount = 1;
            MeshPartsTotalCount = 1;
            MeshPartsCountPerMesh = new int[1];
            MeshPartsCountPerMesh[0] = 1;
            IsSkinned = false;
        } // PrimitiveModel

        #endregion

        #region Render

        /// <summary>
        /// Render the model.
        /// </summary>
        /// <remarks>
        /// Don't call it excepting see the model on the screen.
        /// This is public to allow doing some specific tasks not implemented in the engine.
        /// </remarks>
        public override void Render()
        {
            EngineManager.Device.Indices = indexBuffer;
            EngineManager.Device.SetVertexBuffer(vertexBuffer);
            EngineManager.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numberVertices, 0, numberIndices / 3);
            // Update statistics
            Statistics.DrawCalls++;
            Statistics.TrianglesDrawn += numberIndices / 3;
            Statistics.VerticesProcessed += numberVertices;
        } // Render

        /// <summary>
        /// Render a mesh of the model.
        /// </summary>
        /// <remarks>
        /// Don't call it excepting see the model on the screen.
        /// This is public to allow doing some specific tasks not implemented in the engine.
        /// </remarks>
        public override void RenderMeshPart(int meshIndex, int meshPart)
        {
            if (meshIndex != 0)
                throw new IndexOutOfRangeException("meshIndex");
            if (meshPart != 0)
                throw new IndexOutOfRangeException("meshPart");
            Render();
        } // RenderMeshPart

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose managed resources.
        /// </summary>
        protected override void DisposeManagedResources()
        {
            // This type of resource can be disposed ignoring the content manager.
            ContentManager = null; // This is done to avoid an exception.
            base.DisposeManagedResources();
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();
        } // DisposeManagedResources

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();
        } // RecreateResource

        #endregion

    } // PrimitiveModel

    #region Sphere Class

    public class Sphere : PrimitiveModel
    {

        #region Variables

        private int stacks;
        private int slices;
        private float radius;

        #endregion

        #region Properties

        /// <summary>
        /// Stacks.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public int Stacks
        {
            get { return stacks; }
            set
            {
                stacks = value;
                RecreateResource();
            }
        } // Stacks

        /// <summary>
        /// Slices.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public int Slices
        {
            get { return slices; }
            set
            {
                slices = value;
                RecreateResource();
            }
        } // Slices
        
        /// <summary>
        /// Radius.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                RecreateResource();
            }
        } // Radius

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a sphere model.
        /// </summary>
        public Sphere() : this(20, 20, 1) { }

        /// <summary>
        /// Creates a sphere model.
        /// </summary>
        /// <param name="stacks">Stacks</param>
        /// <param name="slices">Slices</param>
        /// <param name="radius">Radius</param>
        public Sphere(int stacks, int slices, float radius)
        {
            Name = "Sphere Primitive";
            this.stacks = stacks;
            this.slices = slices;
            this.radius = radius;
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Sphere

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            base.RecreateResource();
            // Calculates the resulting number of vertices and indices
            numberVertices = (Stacks + 1) * (Slices + 1);
            numberIndices = (3 * Stacks * (Slices + 1)) * 2;
            int[] indices = new int[numberIndices];
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[numberVertices];

            float stackAngle = MathHelper.Pi / Stacks;
            float sliceAngle = (float)(Math.PI * 2.0) / Slices;

            // Generate the group of Stacks for the sphere  
            int wVertexIndex = 0;
            int vertexCount = 0;
            int indexCount = 0;

            for (int stack = 0; stack < (Stacks + 1); stack++)
            {

                float r = (float)Math.Sin(stack * stackAngle);
                float y = (float)Math.Cos(stack * stackAngle);

                // Generate the group of segments for the current Stack  
                for (int slice = 0; slice < (Slices + 1); slice++)
                {
                    float x = r * (float)Math.Sin(slice * sliceAngle);
                    float z = r * (float)Math.Cos(slice * sliceAngle);
                    vertices[vertexCount].Position = new Vector3(x * Radius, y * Radius, z * Radius);

                    vertices[vertexCount].Normal = Vector3.Normalize(new Vector3(x, y, z));

                    vertices[vertexCount].TextureCoordinate = new Vector2((float)slice / (float)Slices, (float)stack / (float)Stacks);
                    vertexCount++;
                    if (stack != (Stacks - 1))
                    {
                        // First Face
                        indices[indexCount++] = wVertexIndex + (Slices + 1);

                        indices[indexCount++] = wVertexIndex;

                        indices[indexCount++] = wVertexIndex + 1;

                        // Second Face
                        indices[indexCount++] = wVertexIndex + (Slices);

                        indices[indexCount++] = wVertexIndex;

                        indices[indexCount++] = wVertexIndex + (Slices + 1);

                        wVertexIndex++;
                    }
                }
            }
            vertexBuffer = new VertexBuffer(EngineManager.Device, typeof(VertexPositionNormalTexture), numberVertices, BufferUsage.None);
            vertexBuffer.SetData(vertices, 0, vertices.Length);
            indexBuffer = new IndexBuffer(EngineManager.Device, typeof(int), numberIndices, BufferUsage.None);
            indexBuffer.SetData(indices, 0, indices.Length);
        } // RecreateResource

        #endregion

    } // Sphere

    #endregion

    #region Box Class

    public class Box : PrimitiveModel
    {

        #region Variables

        private float width;
        private float height;
        private float depth;

        #endregion

        #region Properties

        /// <summary>
        /// Width.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Width
        {
            get { return width; }
            set
            {
                width = value;
                RecreateResource();
            }
        } // Width

        /// <summary>
        /// Height.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Height
        {
            get { return height; }
            set
            {
                height = value;
                RecreateResource();
            }
        } // Height

        /// <summary>
        /// Depth.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Depth
        {
            get { return depth; }
            set
            {
                depth = value;
                RecreateResource();
            }
        } // Depth

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a box model.
        /// </summary>
        public Box() : this(1, 1, 1) { }

        /// <summary>
        /// Creates a box model.
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="depth">Depth</param>
        public Box(float width, float height, float depth)
        {
            Name = "Box Primitive";
            this.width = width;
            this.height = height;
            this.depth = depth;
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Box

        /// <summary>
        /// Creates a box model.
        /// </summary>
        /// <param name="size">Size</param>
        public Box(float size) : this(size, size, size)
        {
        } // Box
        
        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            base.RecreateResource();
            // Calculates the resulting number of vertices and indices  
            numberVertices = 36;
            numberIndices = 36;
            int[] indices = new int[numberIndices];
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[numberVertices];

            // Because the box is centered at the origin, we need to divide by two to find the + and - offsets
            float halfWidth = Width / 2.0f;
            float halfHeight = Height / 2.0f;
            float halfDepth = Depth / 2.0f;

            Vector3 topLeftFront = new Vector3(-halfWidth, halfHeight, halfDepth);
            Vector3 bottomLeftFront = new Vector3(-halfWidth, -halfHeight, halfDepth);
            Vector3 topRightFront = new Vector3(halfWidth, halfHeight, halfDepth);
            Vector3 bottomRightFront = new Vector3(halfWidth, -halfHeight, halfDepth);
            Vector3 topLeftBack = new Vector3(-halfWidth, halfHeight, -halfDepth);
            Vector3 topRightBack = new Vector3(halfWidth, halfHeight, -halfDepth);
            Vector3 bottomLeftBack = new Vector3(-halfWidth, -halfHeight, -halfDepth);
            Vector3 bottomRightBack = new Vector3(halfWidth, -halfHeight, -halfDepth);

            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            // Front face.
            vertices[1] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            vertices[0] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            vertices[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            vertices[4] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            vertices[3] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            vertices[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // Back face.
            vertices[7] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            vertices[6] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            vertices[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            vertices[10] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            vertices[9] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            vertices[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // Top face.
            vertices[13] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            vertices[12] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            vertices[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            vertices[16] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            vertices[15] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            vertices[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // Bottom face. 
            vertices[19] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            vertices[18] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            vertices[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            vertices[22] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            vertices[21] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            vertices[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // Left face.
            vertices[25] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            vertices[24] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            vertices[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            vertices[28] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            vertices[27] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            vertices[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // Right face. 
            vertices[31] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            vertices[30] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            vertices[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            vertices[34] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            vertices[33] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            vertices[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            for (int i = 0; i < 36; i++)
            {
                indices[i] = i;
            }

            vertexBuffer = new VertexBuffer(EngineManager.Device, typeof(VertexPositionNormalTexture), numberVertices, BufferUsage.None);
            vertexBuffer.SetData(vertices, 0, vertices.Length);
            indexBuffer = new IndexBuffer(EngineManager.Device, typeof(int), numberIndices, BufferUsage.None);
            indexBuffer.SetData(indices, 0, indices.Length);
        } // RecreateResource

        #endregion

    } // Box

    #endregion

    #region Plane Class

    public class Plane : PrimitiveModel
    {

        #region Variables

        private Vector3 topLeft, bottomLeft, topRight, bottomRight;
        private float width, height;

        #endregion

        #region Properties

        /// <summary>
        /// Width.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Width
        {
            get { return width; }
            set
            {
                width = value;
                topLeft = new Vector3(-width / 2.0f, 0, height / 2.0f);
                bottomLeft = new Vector3(-width / 2.0f, 0, -height / 2.0f);
                topRight = new Vector3(width / 2.0f, 0, height / 2.0f);
                bottomRight = new Vector3(width / 2.0f, 0, -height / 2.0f);
                RecreateResource();
            }
        } // Width

        /// <summary>
        /// Height.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Height
        {
            get { return height; }
            set
            {
                height = value;
                topLeft = new Vector3(-width / 2.0f, 0, height / 2.0f);
                bottomLeft = new Vector3(-width / 2.0f, 0, -height / 2.0f);
                topRight = new Vector3(width / 2.0f, 0, height / 2.0f);
                bottomRight = new Vector3(width / 2.0f, 0, -height / 2.0f);
                RecreateResource();
            }
        } // Height

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a XY plane model.
        /// </summary>
        public Plane() : this(1, 1) { }
                
        /// <summary>
        /// Creates a XY plane model.
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public Plane(float width, float height)
        {
            Name = "Plane Primitive";
            // Because the plane is centered at the origin, need to divide by two to find the + and - offsets
            this.width = width;
            this.height = height;

            topLeft = new Vector3(-width / 2.0f, 0, height / 2.0f);
            bottomLeft = new Vector3(-width / 2.0f, 0, -height / 2.0f);
            topRight = new Vector3(width / 2.0f, 0, height / 2.0f);
            bottomRight = new Vector3(width / 2.0f, 0, -height / 2.0f);
            
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Plane

        /// <summary>
        /// Creates a XY plane model.
        /// </summary>
        /// <param name="size">Size</param>
        public Plane(float size) : this(size, size)
        {
        } // Plane

        /// <summary>
        /// Creates a plane model.
        /// </summary>
        /// <param name="topLeft">Top left vertex's position</param>
        /// <param name="bottomLeft">Bottom left vertex's position</param>
        /// <param name="topRight">Top right vertex's position</param>
        /// <param name="bottomRight">Bottom right vertex's position</param>
        public Plane(Vector3 topLeft, Vector3 bottomLeft, Vector3 topRight, Vector3 bottomRight)
        {
            Name = "Plane Primitive";
            this.topLeft = topLeft;
            this.bottomLeft = bottomLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Plane

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            base.RecreateResource();
            // Calculates the resulting number of vertices and indices  
            numberVertices = 4;
            numberIndices = 6;
            int[] indices = new int[numberIndices];
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[numberVertices];

            Vector2 textureTopLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 1.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 0.0f);

            Vector3 normal = Vector3.Cross(topLeft - bottomLeft, bottomRight - bottomLeft);
            normal.Normalize();

            vertices[0] = new VertexPositionNormalTexture(topLeft, normal, textureTopLeft);
            vertices[1] = new VertexPositionNormalTexture(bottomLeft, normal, textureBottomLeft);
            vertices[2] = new VertexPositionNormalTexture(topRight, normal, textureTopRight);
            vertices[3] = new VertexPositionNormalTexture(bottomRight, normal, textureBottomRight);
            
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;

            vertexBuffer = new VertexBuffer(EngineManager.Device, typeof(VertexPositionNormalTexture), numberVertices, BufferUsage.None);
            vertexBuffer.SetData(vertices, 0, vertices.Length);
            indexBuffer = new IndexBuffer(EngineManager.Device, typeof(int), numberIndices, BufferUsage.None);
            indexBuffer.SetData(indices, 0, indices.Length);
        } // CreatePlane

        #endregion

    } // Plane

    #endregion

    #region Cylinder Class

    public class Cylinder : PrimitiveModel
    {

        #region Variables

        private float length;
        private int slices;
        private float radius;

        #endregion

        #region Properties

        /// <summary>
        /// Length.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Length
        {
            get { return length; }
            set
            {
                length = value;
                RecreateResource();
            }
        } // Length

        /// <summary>
        /// Slices.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public int Slices
        {
            get { return slices; }
            set
            {
                slices = value;
                RecreateResource();
            }
        } // Slices

        /// <summary>
        /// Radius.
        /// </summary>
        /// <remarks>
        /// This operation requires the disposal of the vertex and index buffer.
        /// </remarks>
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                RecreateResource();
            }
        } // Radius

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a cylinder model.
        /// </summary>
        public Cylinder() : this(1, 1, 20) { }

        /// <summary>
        /// Creates a cylinder model.
        /// </summary>
        /// <param name="radius">Radius</param>
        /// <param name="length">Length</param>
        /// <param name="slices">Slices</param>
        public Cylinder(float radius, float length, int slices)
        {
            Name = "Cylinder Primitive";
            this.radius = radius;
            this.length = length;
            this.slices = slices;
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Cylinder

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            base.RecreateResource();
            float sliceStep = MathHelper.TwoPi / Slices;
            float textureStep = 1.0f / Slices;
            // Calculates the resulting number of vertices and indices
            numberVertices = 2 + (Slices * 4) + 2;
            numberIndices = Slices * 3 * 2 + Slices * 3 * 2;
            int[] indices = new int[numberIndices];
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[numberVertices];

            // The center top and center bottom vertices //
            vertices[0] = new VertexPositionNormalTexture(new Vector3(0, Length / 2.0f, 0), Vector3.Up, new Vector2(0.5f, 0.5f));
            vertices[1] = new VertexPositionNormalTexture(new Vector3(0, -Length / 2.0f, 0), Vector3.Down, new Vector2(0.5f, 0.5f));

            // The other vertices
            int currentVertex = 2;
            int indexCount = 0;

            float sliceAngle = 0;
            for (int i = 0; i < Slices; i++)
            {
                float x = (float)Math.Cos(sliceAngle);
                float z = (float)Math.Sin(sliceAngle);

                #region Top
                vertices[currentVertex] = new VertexPositionNormalTexture(new Vector3(Radius * x, Length / 2, Radius * z),
                                                                          Vector3.Up,
                                                                          new Vector2(x / 2.0f + 0.5f, z / 2.0f + 0.5f));

                indices[indexCount++] = 0;
                indices[indexCount++] = currentVertex;
                if (i == Slices - 1)
                    indices[indexCount++] = 2;
                else
                    indices[indexCount++] = currentVertex + 1;
                #endregion

                #region Bottom

                vertices[currentVertex + Slices] = new VertexPositionNormalTexture(new Vector3(Radius * x, -Length / 2, Radius * z),
                                                                                   Vector3.Down,
                                                                                   new Vector2(-x / 2.0f + 0.5f, z / 2.0f + 0.5f));

                indices[indexCount++] = 1;
                if (i == Slices - 1)
                    indices[indexCount++] = Slices + 2;
                else
                    indices[indexCount++] = currentVertex + Slices + 1;
                indices[indexCount++] = currentVertex + Slices;

                #endregion

                #region Side

                vertices[currentVertex + 2 * Slices] = new VertexPositionNormalTexture(new Vector3(Radius * x, Length / 2, Radius * z),
                                                                                          new Vector3(x, 0, z),
                                                                                          new Vector2(textureStep * i, 1));

                vertices[currentVertex + 3 * Slices] = new VertexPositionNormalTexture(new Vector3(Radius * x, -Length / 2, Radius * z),
                                                                                       new Vector3(x, 0, z),
                                                                                       new Vector2(textureStep * i, 0));
                // First Face
                indices[indexCount++] = currentVertex + 2 * Slices;
                indices[indexCount++] = currentVertex + 3 * Slices;
                if (i == Slices - 1)
                {
                    vertices[currentVertex + 3 * Slices + 1] = new VertexPositionNormalTexture(new Vector3(Radius, Length / 2, 0),
                                                                                                  new Vector3(x, 0, z),
                                                                                                  new Vector2(1, 1));

                    vertices[currentVertex + 3 * Slices + 2] = new VertexPositionNormalTexture(new Vector3(Radius, -Length / 2, 0),
                                                                                               new Vector3(x, 0, z),
                                                                                               new Vector2(1, 0));
                    indices[indexCount++] = currentVertex + 3 * Slices + 1;
                }
                else
                    indices[indexCount++] = currentVertex + 2 * Slices + 1;
                // Second Face                
                indices[indexCount++] = currentVertex + 3 * Slices;
                if (i == Slices - 1)
                {
                    indices[indexCount++] = currentVertex + 3 * Slices + 2;
                    indices[indexCount++] = currentVertex + 3 * Slices + 1;
                }
                else
                {
                    indices[indexCount++] = currentVertex + 3 * Slices + 1;
                    indices[indexCount++] = currentVertex + 2 * Slices + 1;
                }
                #endregion

                currentVertex++;
                sliceAngle += sliceStep;
            }

            vertexBuffer = new VertexBuffer(EngineManager.Device, typeof(VertexPositionNormalTexture), numberVertices, BufferUsage.None);
            vertexBuffer.SetData(vertices, 0, vertices.Length);
            indexBuffer = new IndexBuffer(EngineManager.Device, typeof(int), numberIndices, BufferUsage.None);
            indexBuffer.SetData(indices, 0, indices.Length);
        } // RecreateResource

        #endregion

    } // Cylinder

    #endregion

    #region Cone Class

    public class Cone : PrimitiveModel
    {

        #region Variables

        private float length;
        private int slices;
        private float radius;

        #endregion

        #region Properties

        /// <summary>
        /// Length.
        /// </summary>
        public float Length
        {
            get { return length; }
            set
            {
                length = value;
                RecreateResource();
            }
        } // Length

        /// <summary>
        /// Slices.
        /// </summary>
        public int Slices
        {
            get { return slices; }
            set
            {
                slices = value;
                RecreateResource();
            }
        } // Slices

        /// <summary>
        /// Radius.
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                RecreateResource();
            }
        } // Radius

        #endregion

        #region Constructor

         /// <summary>
        /// Creates a cone model
        /// </summary>
        public Cone() : this(1, 1, 20) { }

        /// <summary>
        /// Creates a cone model
        /// </summary>
        /// <param name="radius">Radius</param>
        /// <param name="length">Length</param>
        /// <param name="slices">Slices</param>
        public Cone(float radius, float length, int slices)
        {
            Name = "Cone Primitive";
            this.radius = radius;
            this.length = length;
            this.slices = slices;
            RecreateResource();
            boundingSphere = BoundingSphere.CreateFromPoints(Vertices);
            boundingBox = BoundingBox.CreateFromPoints(Vertices);
        } // Cone

        #endregion

        #region Recreate Resource

        /// <summary>
        /// Useful when the XNA device is disposed.
        /// </summary>
        internal override void RecreateResource()
        {
            base.RecreateResource();
            float sliceStep = MathHelper.TwoPi / Slices;
            // Calculates the resulting number of vertices and indices  
            numberVertices = 2 + (Slices * 2);// +2;
            numberIndices = Slices * 3 * 2;
            int[] indices = new int[numberIndices];
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[numberVertices];

            // The center top and center bottom vertices //
            vertices[0] = new VertexPositionNormalTexture(new Vector3(0, Length, 0), Vector3.Up, new Vector2(0.5f, 0.5f));
            vertices[1] = new VertexPositionNormalTexture(new Vector3(0, 0, 0), Vector3.Down, new Vector2(0.5f, 0.5f));

            // The other vertices
            int currentVertex = 2;
            int indexCount = 0;
            float sliceAngle = 0;

            for (int i = 0; i < Slices; i++)
            {
                float x = (float)Math.Cos(sliceAngle);
                float z = (float)Math.Sin(sliceAngle);

                #region Top

                vertices[currentVertex] = new VertexPositionNormalTexture(new Vector3(Radius * x, 0, Radius * z),
                                                                          Vector3.Up,
                                                                          new Vector2(x / 2.0f + 0.5f, z / 2.0f + 0.5f));

                indices[indexCount++] = 0;
                indices[indexCount++] = currentVertex;
                if (i == Slices - 1)
                    indices[indexCount++] = 2;
                else
                    indices[indexCount++] = currentVertex + 1;

                #endregion

                #region Bottom

                vertices[currentVertex + Slices] = new VertexPositionNormalTexture(new Vector3(Radius * x, 0, Radius * z),
                                                                                   Vector3.Down,
                                                                                   new Vector2(-x / 2.0f + 0.5f, z / 2.0f + 0.5f));

                indices[indexCount++] = 1;
                if (i == Slices - 1)
                    indices[indexCount++] = Slices + 2;
                else
                    indices[indexCount++] = currentVertex + Slices + 1;
                indices[indexCount++] = currentVertex + Slices;

                #endregion

                currentVertex++;
                sliceAngle += sliceStep;
            }

            vertexBuffer = new VertexBuffer(EngineManager.Device, typeof(VertexPositionNormalTexture), numberVertices, BufferUsage.None);
            vertexBuffer.SetData(vertices, 0, vertices.Length);
            indexBuffer = new IndexBuffer(EngineManager.Device, typeof(int), numberIndices, BufferUsage.None);
            indexBuffer.SetData(indices, 0, indices.Length);
        } // RecreateResource

        #endregion

    } // Cone

    #endregion

} // XNAFinalEngine.Assets
