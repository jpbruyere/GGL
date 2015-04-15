using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace GGL
{

    [Serializable]
    public class BOquads
    {
        public Quad[] Quads { get; set; }
        public Vertex[] Vertices { get; set; }
        [NonSerialized]
        int verticesBufferId;
        [NonSerialized]
        int quadsBufferId;

        public float height = 0f;
        public float width = 0f;
        public float length = 0f;

        public BoundingBox bounds;

        public void ChangeHeight(float zDelta)
        {
            for (int i = 0; i < this.Vertices.Length; i++)
            {
                Vertices[i].position.Z += zDelta;
            }
        }

        public void Prepare()
        {
            bounds = new BoundingBox();

            bounds.X0Y0Z0 = new Vector3(10000, 10000, 10000);
            bounds.X1Y1Z1 = Vector3.Zero;

            for (int i = 0; i < Vertices.Length; i++)
            {
                bounds.X0Y0Z0 = Vector3.ComponentMin(bounds.X0Y0Z0, Vertices[i].position);
                bounds.X1Y1Z1 = Vector3.ComponentMax(bounds.X1Y1Z1, Vertices[i].position);
            }

            if (Vertices != null)
            {
                GL.GenBuffers(1, out verticesBufferId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.StaticDraw);
            }

            if (Quads != null)
            {
                GL.GenBuffers(1, out quadsBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Quads.Length * Marshal.SizeOf(typeof(Quad))), Quads, BufferUsageHint.StaticDraw);
            }
        }
        public void Render()
        {
            GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
            GL.EnableClientState(EnableCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.InterleavedArrays(InterleavedArrayFormat.T2fN3fV3f, Marshal.SizeOf(typeof(Vertex)), IntPtr.Zero);

            if (Quads.Length > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.DrawElements(BeginMode.Quads, Quads.Length * 4, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            GL.PopClientAttrib();
        }
        public void drawNormals()
        {
			GL.Color3(go.Color.Blue);
                GL.LineWidth(1.0f);
                GL.Begin(BeginMode.Lines);
                foreach (Vertex v in Vertices)
                {
                    GL.Vertex3(v.position);
                    GL.Vertex3(v.position + v.Normal * 0.1f);
                }
                GL.End();
        }
        public static BOquads operator +(BOquads b1, BOquads b2)
        {
            BOquads result = new BOquads();

            result.Quads = new Quad[b1.Quads.Length + b2.Quads.Length];
            result.Vertices = new Vertex[b1.Vertices.Length + b2.Vertices.Length];

            b1.Quads.CopyTo(result.Quads, 0);
            b1.Vertices.CopyTo(result.Vertices, 0);

            for (int i = 0; i < b2.Quads.Length; i++)
            {
                b2.Quads[i].Index0 += b1.Vertices.Length;
                b2.Quads[i].Index1 += b1.Vertices.Length;
                b2.Quads[i].Index2 += b1.Vertices.Length;
                b2.Quads[i].Index3 += b1.Vertices.Length;
            }

            b2.Quads.CopyTo(result.Quads, b1.Quads.Length);
            b2.Vertices.CopyTo(result.Vertices, b1.Vertices.Length);
            return result;
        }
        public static BOquads createBox(float _width, float _length, float _height, float texTileX = 1, float texTileY = 1, float texTileZ = 1)
        {
            Vector3 vWidth = new Vector3(_width, 0, 0);
            Vector3 vLenght = new Vector3(0, _length, 0);
            Vector3 vHeight = new Vector3(0, 0, _height);

            Vector3 p0 = new Vector3(-_width/2f, -_length/2f, -_height/2f); ;
            Vector3 p1 = p0 + vWidth;
            Vector3 p2 = p1 + vHeight;
            Vector3 p3 = p0 + vHeight;

            Vector3 p4 = p3 + vLenght;
            Vector3 p5 = p2 + vLenght;
            Vector3 p6 = p1 + vLenght;
            Vector3 p7 = p0 + vLenght;

            BOquads result = createPlane(p0, p1, p2, p3, texTileX, texTileY);
            result += createPlane(p3, p2, p5, p4, texTileX, texTileY);
            result += createPlane(p4, p5, p6, p7, texTileX, texTileY);
            result += createPlane(p7, p6, p1, p0, texTileX, texTileY);
            result += createPlane(p1, p6, p5, p2, texTileX, texTileY);
            result += createPlane(p3, p4, p7, p0, texTileX, texTileY);

            result.height = _height;
            result.width = _width;

            return result;
        }
        public static BOquads createBox2(float _width, float _length, float _height, float texTileX = 1, float texTileY = 1, float texTileZ = 1)
        {
            Vector3 vWidth = new Vector3(_width, 0, 0);
            Vector3 vLenght = new Vector3(0, _length, 0);
            Vector3 vHeight = new Vector3(0, 0, _height);

            Vector3 p0 = new Vector3(-_width / 2f, -_length / 2f, -_height / 2f); ;
            Vector3 p1 = p0 + vWidth;
            Vector3 p2 = p1 + vHeight;
            Vector3 p3 = p0 + vHeight;

            Vector3 p4 = p3 + vLenght;
            Vector3 p5 = p2 + vLenght;
            Vector3 p6 = p1 + vLenght;
            Vector3 p7 = p0 + vLenght;

            BOquads result = createPlane2(p0, p1, p2, p3, texTileX, texTileY);
            result += createPlane2(p3, p2, p5, p4, texTileX, texTileY);
            result += createPlane2(p4, p5, p6, p7, texTileX, texTileY);
            result += createPlane2(p7, p6, p1, p0, texTileX, texTileY);
            result += createPlane2(p1, p6, p5, p2, texTileX, texTileY);
            result += createPlane2(p3, p4, p7, p0, texTileX, texTileY);

            result.height = _height;
            result.width = _width;

            return result;
        }
        public static BOquads createCappedCube(float _width, float _height, float texTileX, float texTileY)
        {
            BOquads q = createUncappedCube(_width, _height, texTileX, texTileY);
            BOquads cap = createPlaneZup(_width, _width, 5f, 5f, _height);
            BOquads result = new BOquads();

            result.Quads = new Quad[5];
            result.Vertices = new Vertex[20];

            q.Quads.CopyTo(result.Quads, 0);
            q.Vertices.CopyTo(result.Vertices, 0);

            cap.Quads[0].Index0 += 16;
            cap.Quads[0].Index1 += 16;
            cap.Quads[0].Index2 += 16;
            cap.Quads[0].Index3 += 16;

            cap.Quads.CopyTo(result.Quads, 4);
            cap.Vertices.CopyTo(result.Vertices, 16);

            result.height = _height;
            result.width = _width;

            return result;
        }
        public static BOquads createCappedCube(int x1, int y1, int x2, int y2, float z = 0f, float zHeight = 1f, float texTileY = 1f, float texTileX = 1f)
        {
            float height = Math.Abs(x1 - x2);
            float width = Math.Abs(y1 - y2);

            BOquads q = createUncappedCube(x1, y1, x2, y2, z, zHeight, texTileX, texTileY);
            BOquads cap = createPlaneZup(x1, y1, x2, y2, z + zHeight, texTileX, texTileY);
            BOquads result = new BOquads();

            result.Quads = new Quad[5];
            result.Vertices = new Vertex[20];

            q.Quads.CopyTo(result.Quads, 0);
            q.Vertices.CopyTo(result.Vertices, 0);

            cap.Quads[0].Index0 += 16;
            cap.Quads[0].Index1 += 16;
            cap.Quads[0].Index2 += 16;
            cap.Quads[0].Index3 += 16;

            cap.Quads.CopyTo(result.Quads, 4);
            cap.Vertices.CopyTo(result.Vertices, 16);


            result.height = height;
            result.width = width;

            return result;
        }

        public static BOquads createMultiPlaneZup(int x1, int y1, int x2, int y2, int nbSubX, int nbSubY, float z = 0f, float texTileY = 1f, float texTileX = 1f)
        {
            float height = Math.Abs(x1 - x2);
            float width = Math.Abs(y1 - y2);

            float heightStep = height / nbSubY;
            float widthStep = height / nbSubY;

            int nbQuads = nbSubX * nbSubY;

            Quad[] Quads = new Quad[nbQuads];
            Vertex[] Vertices = new Vertex[(nbSubX + 1) * (nbSubY + 1)];

            for (int y = 0; y < nbSubY + 1; y++)
            {
                for (int x = 0; x < nbSubX + 1; x++)
                {
                    int i = x + y * (nbSubY + 1);

                    Vertices[i].position = new Vector3(x1 + x * widthStep, y1 + y * heightStep, z);
                    Vertices[i].TexCoord = new Vector2(x / texTileX, y / texTileY);
                    Vertices[i].Normal = Vector3.UnitZ;
                }
            }

            for (int y = 0; y < nbSubY; y++)
            {
                for (int x = 0; x < nbSubX; x++)
                {
                    int q = x + y * (nbSubY);

                    Quad s = new Quad();
                    s.Index0 = x + (y + 1) * (nbSubY + 1);
                    s.Index1 = x + y * (nbSubY + 1);
                    s.Index2 = x + 1 + y * (nbSubY + 1);
                    s.Index3 = x + 1 + (y + 1) * (nbSubY + 1);

                    Quads[q] = s;
                }
            }

            return new BOquads { Quads = Quads, Vertices = Vertices, height = height, width = width };
        }
        public static BOquads createPlaneZup(int x1, int y1, int x2, int y2, float z = 0f, float texTileY = 1f, float texTileX = 1f)
        {
            float height = Math.Abs(x1 - x2);
            float width = Math.Abs(y1 - y2);

            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];

            Vertices[0].position = new Vector3(x1, y1, z);
            Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = Vector3.UnitZ;

            Vertices[1].position = new Vector3(x1, y2, z);
            Vertices[1].TexCoord = new Vector2(0, 0);
            Vertices[1].Normal = Vector3.UnitZ;

            Vertices[2].position = new Vector3(x2, y1, z);
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[2].Normal = Vector3.UnitZ;

            Vertices[3].position = new Vector3(x2, y2, z);
            Vertices[3].TexCoord = new Vector2(texTileX, 0);
            Vertices[3].Normal = Vector3.UnitZ;

            Quad s = new Quad();
            s.Index0 = 1;
            s.Index1 = 0;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices, height = height, width = width };
        }
        public static BOquads createPlaneZup(float _width, float _height, float texTileX, float texTileY, float z = 0f)
        {
            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];

			Vertices[0].position = new Vector3(-_width/2, -_height/2, z);
            Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = Vector3.UnitZ;

			Vertices[1].position = new Vector3(-_width/2, _height/2, z);
            Vertices[1].TexCoord = new Vector2(0, 0);
            Vertices[1].Normal = Vector3.UnitZ;

			Vertices[2].position = new Vector3(_width/2, -_height/2, z);
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[2].Normal = Vector3.UnitZ;

			Vertices[3].position = new Vector3(_width/2, _height/2, z);
            Vertices[3].TexCoord = new Vector2(texTileX, 0);
            Vertices[3].Normal = Vector3.UnitZ;

            Quad s = new Quad();
            s.Index0 = 1;
            s.Index1 = 0;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices, height = _height, width = _width };

        }
        public static void createPlaneZUpDL(ref int dlId, float width = 1f, float height = 1f, float zPos = 0f)
        {
            if (dlId != 0)
                GL.DeleteLists(dlId, 1);

            dlId = GL.GenLists(1);
            GL.NewList(dlId, ListMode.Compile);
            {
                GL.Begin(PrimitiveType.Quads);
                {
                    GL.TexCoord2(0, 0); GL.Vertex3(-width, -height, zPos);
                    GL.TexCoord2(2, 0); GL.Vertex3(width, -height, zPos);
                    GL.TexCoord2(2, 2); GL.Vertex3(width, height, zPos);
                    GL.TexCoord2(0, 2); GL.Vertex3(-width, height, zPos);
                }
                GL.End();
            }
            GL.EndList();
        }

        //counterclockwize plane   
        public void selectSubTexture(float x1, float y1, float x2, float y2)
        {
            Vertices[1].TexCoord += new Vector2(x1, y1);
            //point 0,0
            Vertices[1].TexCoord += new Vector2(x1, y1);


        }
        public static BOquads createPlane(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float tu0 = 0f, float tu1 = 1f, float tv0 = 0f, float tv1 = 1f, bool HorizontalAlign = false)
        {
            if (HorizontalAlign)
                return createPlaneHorizontalyAlignedTextured(v1, v2, v3, v4, tu0, tu1, tv0, tv1);
            else
                return createPlane(v1, v2, v3, v4, tu0, tu1, tv0, tv1);

        }
        //create plane with texture aligned horizontaly with uv coordonate        
        private static BOquads createPlaneHorizontalyAlignedTextured(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float tu0 = 0f, float tu1 = 1f, float tv0 = 0f, float tv1 = 1f)
        {
            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];

            Vector3 normal = new Vector3();

            Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

            Vector3 vMinBase = Vector3.ComponentMin(v1, v4);
            Vector3 vMaxBase = Vector3.ComponentMax(v1, v4);
            Vector3 vMinTop = Vector3.ComponentMin(v2, v3);
            Vector3 vMaxTop = Vector3.ComponentMax(v2, v3);

            float MinHeight = Math.Min((v2 - v1).Length, (v3 - v4).Length);
            //float MaxHeight = 

            float diffZBase = v1.Z - v4.Z;
            float diffZTop = v2.Z - v3.Z;
            float texLengthLeft = v2.Z - v1.Z;
            float texLengthRight = v3.Z - v4.Z;



            float texHeight = Math.Min(v2.Z, v3.Z) - Math.Max(v1.Z, v4.Z);

            float uvTexHeightV = tv1 - tv0;
            float uvTewWidthU = tu1 - tu0;

            Vertices[0].position = v1;
            if (v1.Z < v4.Z)
                Vertices[0].TexCoord = new Vector2(0, tv0 + texLengthLeft / texHeight * uvTexHeightV);
            else
                Vertices[0].TexCoord = new Vector2(tu0, tv1);
            Vertices[0].Normal = normal;

            Vertices[1].position = v2;
            if (v2.Z < v3.Z)
                Vertices[1].TexCoord = new Vector2(tu0, tv0);
            else
                Vertices[1].TexCoord = new Vector2(tu0, tv0 + (v3.Z - v2.Z) / texHeight * uvTexHeightV);
            Vertices[1].Normal = normal;

            Vertices[2].position = v3;
            if (v2.Z < v3.Z)
                Vertices[2].TexCoord = new Vector2(tu1, tv0 + (v2.Z - v3.Z) / texHeight * uvTexHeightV);
            else
                Vertices[2].TexCoord = new Vector2(tu1, tv0);
            Vertices[2].Normal = normal;

            Vertices[3].position = v4;
            if (v1.Z < v4.Z)
                Vertices[3].TexCoord = new Vector2(tu1, tv1);// + texTileY * ratio / texLengthLeft * diffZBase);
            else
                Vertices[3].TexCoord = new Vector2(tu1, tv0 + (texHeight + v1.Z - v4.Z) / texHeight * uvTexHeightV);
            Vertices[3].Normal = normal;

            Quad s = new Quad();
            s.Index0 = 0;
            s.Index1 = 1;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices };
        }
        //create plane with texture aligned horizontaly with uv coordonate        
        private static BOquads createPlane(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float tu0 = 0f, float tu1 = 1f, float tv0 = 0f, float tv1 = 1f)
        {
            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];

            Vector3 normal = new Vector3();

            Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

            Vector3 vMinBase = Vector3.ComponentMin(v1, v4);
            Vector3 vMaxBase = Vector3.ComponentMax(v1, v4);
            Vector3 vMinTop = Vector3.ComponentMin(v2, v3);
            Vector3 vMaxTop = Vector3.ComponentMax(v2, v3);

            float MinHeight = Math.Min((v2 - v1).Length, (v3 - v4).Length);
            //float MaxHeight = 

            float diffZBase = v1.Z - v4.Z;
            float diffZTop = v2.Z - v3.Z;
            float texLengthLeft = v2.Z - v1.Z;
            float texLengthRight = v3.Z - v4.Z;



            float texHeight = Math.Min(v2.Z, v3.Z) - Math.Max(v1.Z, v4.Z);

            float ratio = texLengthLeft / texHeight;

            Vertices[0].position = v1;
            Vertices[0].TexCoord = new Vector2(tu0, tv1);
            Vertices[0].Normal = normal;

            Vertices[1].position = v2;
            Vertices[1].TexCoord = new Vector2(tu0, tv0);
            Vertices[1].Normal = normal;

            Vertices[2].position = v3;
            Vertices[2].TexCoord = new Vector2(tu1, tv0);
            Vertices[2].Normal = normal;

            Vertices[3].position = v4;
            Vertices[3].TexCoord = new Vector2(tu1, tv1);// + texTileY * ratio / texLengthLeft * diffZBase);
            Vertices[3].Normal = normal;

            Quad s = new Quad();
            s.Index0 = 0;
            s.Index1 = 1;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices };
        }
        //create plane with texture aligned horizontaly
        public static BOquads createPlane(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float texTileY = 1f, float texTileX = 1f)
        {
            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];


            Vector3 normal = Vector3.Normalize(Vector3.Cross(v2 - v1,v3 - v1));

            Vector3 vMinBase = Vector3.ComponentMin(v1, v4);
            Vector3 vMaxBase = Vector3.ComponentMax(v1, v4);
            Vector3 vMinTop = Vector3.ComponentMin(v2, v3);
            Vector3 vMaxTop = Vector3.ComponentMax(v2, v3);

            float MinHeight = Math.Min((v2 - v1).Length, (v3 - v4).Length);
            //float MaxHeight = 

            float diffZBase = v1.Z - v4.Z;
            float diffZTop = v2.Z - v3.Z;
            float texLengthLeft = v2.Z - v1.Z;
            float texLengthRight = v3.Z - v4.Z;



            float texHeight = Math.Min(v2.Z, v3.Z) - Math.Max(v1.Z, v4.Z);

            float ratio = texLengthLeft / texHeight;

            Vertices[0].position = v1;
            if (v1.Z < v4.Z)
                Vertices[0].TexCoord = new Vector2(0, texLengthLeft / texHeight * texTileY);
            else
                Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = normal;

            Vertices[1].position = v2;
            if (v2.Z < v3.Z)
                Vertices[1].TexCoord = new Vector2(0, 0);
            else
                Vertices[1].TexCoord = new Vector2(0, (v3.Z - v2.Z) / texHeight * texTileY);
            Vertices[1].Normal = normal;

            Vertices[2].position = v3;
            if (v2.Z < v3.Z)
                Vertices[2].TexCoord = new Vector2(texTileX, -texTileY / texLengthLeft * Math.Abs(diffZTop));
            else
                Vertices[2].TexCoord = new Vector2(texTileX, 0);
            Vertices[2].Normal = normal;

            Vertices[3].position = v4;
            if (v1.Z < v4.Z)
                Vertices[3].TexCoord = new Vector2(texTileX, texTileY);// + texTileY * ratio / texLengthLeft * diffZBase);
            else
                Vertices[3].TexCoord = new Vector2(texTileX, (texHeight + v1.Z - v4.Z) / texHeight * texTileY);
            Vertices[3].Normal = normal;

            Quad s = new Quad();
            s.Index0 = 0;
            s.Index1 = 1;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices };
        }
        public static BOquads createPlane2(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float texTileY = 1f, float texTileX = 1f)
        {
            Quad[] Quads = new Quad[1];
            Vertex[] Vertices = new Vertex[4];


            Vector3 normal = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

            Vector3 vMinBase = Vector3.ComponentMin(v1, v4);
            Vector3 vMaxBase = Vector3.ComponentMax(v1, v4);
            Vector3 vMinTop = Vector3.ComponentMin(v2, v3);
            Vector3 vMaxTop = Vector3.ComponentMax(v2, v3);

            float MinHeight = Math.Min((v2 - v1).Length, (v3 - v4).Length);
            //float MaxHeight = 

            float diffZBase = v1.Z - v4.Z;
            float diffZTop = v2.Z - v3.Z;
            float texLengthLeft = v2.Z - v1.Z;
            float texLengthRight = v3.Z - v4.Z;



            float texHeight = Math.Min(v2.Z, v3.Z) - Math.Max(v1.Z, v4.Z);

            float ratio = texLengthLeft / texHeight;

            Vertices[0].position = v1;
            Vertices[0].TexCoord = new Vector2(0, 0);
            Vertices[0].Normal = normal;

            Vertices[1].position = v2;
            Vertices[1].TexCoord = new Vector2(texTileX, 0);
            Vertices[1].Normal = normal;

            Vertices[2].position = v3;
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY); 
            Vertices[2].Normal = normal;

            Vertices[3].position = v4;
            Vertices[3].TexCoord = new Vector2(0, texTileY); 
            Vertices[3].Normal = normal;

            Quad s = new Quad();
            s.Index0 = 0;
            s.Index1 = 1;
            s.Index2 = 2;
            s.Index3 = 3;

            Quads[0] = s;


            return new BOquads { Quads = Quads, Vertices = Vertices };
        }
        public static BOquads createPlane(Vector3[] v, float texTileY = 1f, float texTileX = 1f)
        {
            return BOquads.createPlane(v[0], v[1], v[2], v[3], texTileX, texTileY);
        }

        public static BOquads createUncappedCube(int x1, int y1, int x2, int y2, float z = 0f, float zHeight = 1f, float texTileY = 1f, float texTileX = 1f)
        {
            float height = Math.Abs(x1 - x2);
            float width = Math.Abs(y1 - y2);

            Quad[] Quads = new Quad[4];
            Vertex[] Vertices = new Vertex[16];


            Vertices[0].position = new Vector3(x1, y1, z);
            Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = Vector3.UnitX;

            Vertices[1].position = new Vector3(x1, y1, z + zHeight);
            Vertices[1].TexCoord = new Vector2(0, 0);
            Vertices[1].Normal = Vector3.UnitX;

            Vertices[2].position = new Vector3(x2, y1, z);
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[2].Normal = Vector3.UnitX;

            Vertices[3].position = new Vector3(x2, y1, z + zHeight);
            Vertices[3].TexCoord = new Vector2(texTileX, 0);
            Vertices[3].Normal = Vector3.UnitX;

            Vertices[4].position = new Vector3(x2, y1, z);
            Vertices[4].TexCoord = new Vector2(0, texTileY);
            Vertices[4].Normal = Vector3.UnitY;

            Vertices[5].position = new Vector3(x2, y1, z + zHeight);
            Vertices[5].TexCoord = new Vector2(0, 0);
            Vertices[5].Normal = Vector3.UnitY;

            Vertices[6].position = new Vector3(x2, y2, z);
            Vertices[6].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[6].Normal = Vector3.UnitY;

            Vertices[7].position = new Vector3(x2, y2, z + zHeight);
            Vertices[7].TexCoord = new Vector2(texTileX, 0);
            Vertices[7].Normal = Vector3.UnitY;

            Vertices[8].position = new Vector3(x2, y2, z);
            Vertices[8].TexCoord = new Vector2(0, texTileY);
            Vertices[8].Normal = -Vector3.UnitX;

            Vertices[9].position = new Vector3(x2, y2, z + zHeight);
            Vertices[9].TexCoord = new Vector2(0, 0);
            Vertices[9].Normal = -Vector3.UnitX;

            Vertices[10].position = new Vector3(x1, y2, z);
            Vertices[10].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[10].Normal = -Vector3.UnitY;

            Vertices[11].position = new Vector3(x1, y2, z + zHeight);
            Vertices[11].TexCoord = new Vector2(texTileX, 0);
            Vertices[11].Normal = -Vector3.UnitY;

            Vertices[12].position = new Vector3(x1, y2, z);
            Vertices[12].TexCoord = new Vector2(0, texTileY);
            Vertices[12].Normal = -Vector3.UnitY;

            Vertices[13].position = new Vector3(x1, y2, z + zHeight);
            Vertices[13].TexCoord = new Vector2(0, 0);
            Vertices[13].Normal = -Vector3.UnitY;

            Vertices[14].position = new Vector3(x1, y1, z);
            Vertices[14].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[14].Normal = -Vector3.UnitY;

            Vertices[15].position = new Vector3(x1, y1, z + zHeight);
            Vertices[15].TexCoord = new Vector2(texTileX, 0);
            Vertices[15].Normal = -Vector3.UnitY;

            for (int q = 0; q < 4; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4 + 1;
                s.Index1 = q * 4;
                s.Index2 = q * 4 + 2;
                s.Index3 = q * 4 + 3;
                Quads[q] = s;
            }

            return new BOquads { Quads = Quads, Vertices = Vertices, height = height, width = width };
        }
        public static BOquads createUncappedRectangle(Vector3 x1, Vector3 x2, Vector3 y1, Vector3 y2, float _height, float texTileX, float texTileY)
        {
            Quad[] Quads = new Quad[4];
            Vertex[] Vertices = new Vertex[16];

            Vector3 vHeight = new Vector3(0, 0, _height);
            float _width = (x1 - x2).Length;
            float _length = (y1 - y2).Length;



            Vertices[0].position = x1;
            Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = Vector3.UnitX;

            Vertices[1].position = x1 + vHeight;
            Vertices[1].TexCoord = new Vector2(0, 0);
            Vertices[1].Normal = Vector3.UnitX;

            Vertices[2].position = x2;
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[2].Normal = Vector3.UnitX;

            Vertices[3].position = x2 + vHeight;
            Vertices[3].TexCoord = new Vector2(texTileX, 0);
            Vertices[3].Normal = Vector3.UnitX;

            Vertices[4].position = x2;
            Vertices[4].TexCoord = new Vector2(0, texTileY);
            Vertices[4].Normal = Vector3.UnitY;

            Vertices[5].position = x2 + vHeight;
            Vertices[5].TexCoord = new Vector2(0, 0);
            Vertices[5].Normal = Vector3.UnitY;

            Vertices[6].position = y2;
            Vertices[6].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[6].Normal = Vector3.UnitY;

            Vertices[7].position = y2 + vHeight;
            Vertices[7].TexCoord = new Vector2(texTileX, 0);
            Vertices[7].Normal = Vector3.UnitY;

            Vertices[8].position = y2;
            Vertices[8].TexCoord = new Vector2(0, texTileY);
            Vertices[8].Normal = -Vector3.UnitX;

            Vertices[9].position = y2 + vHeight;
            Vertices[9].TexCoord = new Vector2(0, 0);
            Vertices[9].Normal = -Vector3.UnitX;

            Vertices[10].position = y1;
            Vertices[10].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[10].Normal = -Vector3.UnitY;

            Vertices[11].position = y1 + vHeight;
            Vertices[11].TexCoord = new Vector2(texTileX, 0);
            Vertices[11].Normal = -Vector3.UnitY;

            Vertices[12].position = y1;
            Vertices[12].TexCoord = new Vector2(0, texTileY);
            Vertices[12].Normal = -Vector3.UnitY;

            Vertices[13].position = y1 + vHeight;
            Vertices[13].TexCoord = new Vector2(0, 0);
            Vertices[13].Normal = -Vector3.UnitY;

            Vertices[14].position = x1;
            Vertices[14].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[14].Normal = -Vector3.UnitY;

            Vertices[15].position = x1 + vHeight;
            Vertices[15].TexCoord = new Vector2(texTileX, 0);
            Vertices[15].Normal = -Vector3.UnitY;

            for (int q = 0; q < 4; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4 + 1;
                s.Index1 = q * 4;
                s.Index2 = q * 4 + 2;
                s.Index3 = q * 4 + 3;
                Quads[q] = s;
            }

            return new BOquads { Quads = Quads, Vertices = Vertices, length = _length, height = _height, width = _width };
        }
        public static BOquads createUncappedRectangle(float _width, float _length, float _height, float texTileX, float texTileY)
        {
            Quad[] Quads = new Quad[4];
            Vertex[] Vertices = new Vertex[16];


            Vertices[0].position = Vector3.Zero;
            Vertices[0].TexCoord = new Vector2(0, texTileY);
            Vertices[0].Normal = Vector3.UnitX;

            Vertices[1].position = new Vector3(0f, 0f, _height);
            Vertices[1].TexCoord = new Vector2(0, 0);
            Vertices[1].Normal = Vector3.UnitX;

            Vertices[2].position = new Vector3(_width, 0f, 0f);
            Vertices[2].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[2].Normal = Vector3.UnitX;

            Vertices[3].position = new Vector3(_width, 0f, _height);
            Vertices[3].TexCoord = new Vector2(texTileX, 0);
            Vertices[3].Normal = Vector3.UnitX;

            Vertices[4].position = new Vector3(_width, 0f, 0f);
            Vertices[4].TexCoord = new Vector2(0, texTileY);
            Vertices[4].Normal = Vector3.UnitY;

            Vertices[5].position = new Vector3(_width, 0f, _height);
            Vertices[5].TexCoord = new Vector2(0, 0);
            Vertices[5].Normal = Vector3.UnitY;

            Vertices[6].position = new Vector3(_width, _length, 0f);
            Vertices[6].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[6].Normal = Vector3.UnitY;

            Vertices[7].position = new Vector3(_width, _length, _height);
            Vertices[7].TexCoord = new Vector2(texTileX, 0);
            Vertices[7].Normal = Vector3.UnitY;

            Vertices[8].position = new Vector3(_width, _length, 0f);
            Vertices[8].TexCoord = new Vector2(0, texTileY);
            Vertices[8].Normal = -Vector3.UnitX;

            Vertices[9].position = new Vector3(_width, _length, _height);
            Vertices[9].TexCoord = new Vector2(0, 0);
            Vertices[9].Normal = -Vector3.UnitX;

            Vertices[10].position = new Vector3(0f, _length, 0f);
            Vertices[10].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[10].Normal = -Vector3.UnitY;

            Vertices[11].position = new Vector3(0f, _length, _height);
            Vertices[11].TexCoord = new Vector2(texTileX, 0);
            Vertices[11].Normal = -Vector3.UnitY;

            Vertices[12].position = new Vector3(0f, _length, 0f);
            Vertices[12].TexCoord = new Vector2(0, texTileY);
            Vertices[12].Normal = -Vector3.UnitY;

            Vertices[13].position = new Vector3(0f, _length, _height);
            Vertices[13].TexCoord = new Vector2(0, 0);
            Vertices[13].Normal = -Vector3.UnitY;

            Vertices[14].position = Vector3.Zero;
            Vertices[14].TexCoord = new Vector2(texTileX, texTileY);
            Vertices[14].Normal = -Vector3.UnitY;

            Vertices[15].position = new Vector3(0f, 0f, _height);
            Vertices[15].TexCoord = new Vector2(texTileX, 0);
            Vertices[15].Normal = -Vector3.UnitY;

            for (int q = 0; q < 4; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4 + 1;
                s.Index1 = q * 4;
                s.Index2 = q * 4 + 2;
                s.Index3 = q * 4 + 3;
                Quads[q] = s;
            }

            return new BOquads { Quads = Quads, Vertices = Vertices, length = _length, height = _height, width = _width };
        }
        public static BOquads createUncappedCube(float _width, float _height, float texTileX, float texTileY)
        {
            return createUncappedRectangle(_width, _width, _height, texTileX, texTileY);
        }
    }

}
