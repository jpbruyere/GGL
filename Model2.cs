using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using GGL;
using System.Diagnostics;
using System.IO;
using Jitter.Collision.Shapes;

//namespace telmjmsdjqf
//{
//    [Serializable]
//    public abstract class Model2
//    {
//        public string Name;
//        public bool AlphaBlend = false;

//        public BoundingBox bounds = new BoundingBox();

//        public Model2(string _name = "Unamed")
//        {
//            Name = _name;
//        }

//        public virtual Texture texture
//        { get; set; }

//        public virtual int verticesCount
//        { get { return -1; } }


//        public abstract void Render();
//        public abstract void Prepare();

//    }

//    [Serializable]
//    public class ObjModel : Model2
//    {
//        public ObjModel(string _name = "Unamed") : base(_name) { }
//        public ObjModel(Mesh m, string _name = "")
//        {
//            meshes.Add(new Mesh[1]);
//            meshes[0][0] = m;
//            Name = _name;
//            Prepare();
//        }


//        public static ObjModel Load(string fileName, bool searchForLod = true)
//        {
//            ObjModel model = new ObjModel();
//            ObjMeshLoader.Load(fileName, model);
//            model.objPath = fileName;
//            model.Name = fileName;

//            //check for other lod
//            int l = 1;
//            if (searchForLod)
//            {
//                bool nextLodExist = true;

//                do
//                {
//                    string lod = System.IO.Path.GetDirectoryName(fileName) +
//                                    System.IO.Path.DirectorySeparatorChar +
//                                    System.IO.Path.GetFileNameWithoutExtension(fileName) + "_lod" + l + ".obj";
//                    l++;
//                    if (File.Exists(lod))
//                        ObjMeshLoader.Load(lod, model);
//                    else
//                        nextLodExist = false;
//                } while (nextLodExist);
//            }
//            model.Prepare();

//            return model;
//        }

//        public string objPath;

//        [NonSerialized]
//        public string mtllib;
//        [NonSerialized]
//        public List<Material> materials;
//        [NonSerialized]
//        public Mesh[] meshes;


//        public override int verticesCount
//        {
//            get
//            {
//                int vCount = 0;
//                foreach (Mesh m in meshes)
//                {
//                    vCount += m.Vertices.Length;
//                }
//                return vCount;
//            }
//        }

//        //single texture for the whole model
//        public int texture
//        {
//            get { return meshes[0].Faces[0].texture; }
//            set
//            {
//                foreach (Mesh[] ms in meshes)
//                {
//                    foreach (Mesh m in ms)
//                    {
//                        foreach (FaceGroup fg in m.Faces)
//                        {
//                            fg.texture = value;
//                        }
//                    }
//                }
//            }
//        }


//        public override void Prepare()
//        {
//            bounds = new BoundingBox();

//            bool firstLod = true;

//            foreach (Mesh[] ms in meshes)
//            {
//                for (int i = 0; i < ms.Length; i++)
//                {
//                    if (firstLod)
//                    {
//                        ms[i].ComputeBounds();
//                        bounds += ms[i].bounds;
//                    }
//                    ms[i].Prepare();
//                }
//                firstLod = false;
//            }
//        }



//        public override void Render()
//        {
//            //Game.PrintActiveTexturing("Model Render ");

//            for (int i = 0; i < meshes[LOD].Length; i++)
//            {
//                GL.PushAttrib(AttribMask.EnableBit);

//                if (AlphaBlend)
//                {
//                    GL.Enable(EnableCap.AlphaTest);
//                    GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
//                    GL.Enable(EnableCap.Blend);
//                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
//                }

//                meshes[LOD][i].Render();

//                GL.PopAttrib();
//            }
//        }
//    }

//}

