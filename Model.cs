using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using GGL;
using System.Diagnostics;
using System.IO;
using Jitter.Collision.Shapes;
using Jitter.LinearMath;
using Jitter.Collision;

public enum Category
{
    Building,
    Vehicle,
    Tree
}



[Serializable]
public abstract class Model
{
    public static Model activeModel
    {
        get { return modelList[selectetModelIndex]; }
    }    

    [NonSerialized]
    public static List<Model> modelList = new List<Model>();

    [NonSerialized]
    static int _selectetModelIndex = 0;
    public static int selectetModelIndex
    {
        get { return _selectetModelIndex; }
        set
        {
            if (value < 0)
                _selectetModelIndex = modelList.Count - 1;
            else if (value > modelList.Count - 1)
                _selectetModelIndex = 0;
            else
                _selectetModelIndex = value;
        }
    }

    public const int heolienne = 0;
    public const int bus = 1;
    public const int loco = 2;
    public const int wagon = 3;
    public const int tgva = 4;
    public static int proceduralHouse1;

    public static int registerModel(Model m)
    {
        int index = modelList.Count;
        modelList.Add(m);
        return index;
    }
    public static void loadModels()
    {
        modelList.Add(ObjModel.Load(directories.rootDir + @"obj\heolienne.obj"));
        modelList.Add(ObjModel.Load(directories.rootDir + @"blender\bus_Citaro_2.7.obj"));
        modelList.Add(ObjModel.Load(directories.rootDir + @"obj\loco1.obj"));
        modelList.Add(ObjModel.Load(directories.rootDir + @"obj\wagon1.obj"));
        modelList.Add(ObjModel.Load(directories.rootDir + @"obj\tgva.obj"));
        modelList.Add(ObjModel.Load(directories.rootDir + @"obj\tgva_v1.obj"));

        modelList[tgva].AlphaBlend = true;

        //modelList.Add(new Building("b1", 5, 1));
        //modelList.Add(new Building("b2", 2, 2));
        //modelList.Add(new Building("b3", 3, 3));
        //modelList.Add(new Building("b4", 4, 4));
        //modelList.Add(new Building("b5", 4, 5));
        //modelList.Add(new Building("b6", 4, 6));
        //modelList.Add(new Building("b7", 4, 7));
        //modelList.Add(new Building("b8", 4, 8));
        //modelList.Add(new Building("b9", 4, 9));
        //modelList.Add(new Building("b10", 4, 10));
        //modelList.Add(new Building("b11", 4, 11));
        //modelList.Add(new Building("b12", 4, 12));
        //modelList.Add(new Building("b13", 4, 13));


        Vector3 houseDim = new Vector3(1.5f, 0.8f, 1.0f);
        proceduralHouse1 = Model.registerModel(new ProceduralHouse(houseDim, 0.5f, true));


    }
    public static Model FindByName(string _name)
    {
        return modelList.Find(m => m.Name == _name);
    }

	[NonSerialized]
	public int LOD = 0;
    public Shape shape;
    public bool renderCollisionNormals = false;
    protected Vector3[] _collisionNormals;
    public virtual Vector3[] collisionNormals
    {
        get { return _collisionNormals; }
    }
    public virtual void createShape()
    {
        shape = new Jitter.Collision.Shapes.BoxShape(bounds.width, bounds.length, bounds.height);
    }
    

    public string Name;
    public bool AlphaBlend = false;

    public BoundingBox bounds = new BoundingBox();

    public Model(string _name = "Unamed")
    {
        Name = _name;
    }

    public Vector3 Axe;

    public virtual Texture texture{ get; set; }

    public virtual int verticesCount{ get { return -1; } }

    public abstract void Render();
    public abstract void Prepare();

    public virtual void drawCollisionNormals(){}

    public virtual Bitmap getPreviewImage(int iconSize = 256)
    {
        Bitmap icon = new Bitmap(iconSize, iconSize);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.PushAttrib(AttribMask.EnableBit);

        GL.Disable(EnableCap.Lighting);

        GL.Viewport(0, 0, iconSize, iconSize);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, 1f, 0.1f, 1000f);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.LoadMatrix(ref projection);

        Matrix4 modelview = Matrix4.LookAt(Vector3.One, Vector3.Zero, Vector3.UnitZ);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();
        GL.LoadMatrix(ref modelview);

        float ratio = 1f / (float)Math.Max((double)bounds.length, Math.Max((double)bounds.width, (double)bounds.height));
        Vector3 c = bounds.center;
        GL.Scale(ratio, ratio, ratio);
        GL.Translate(-c.X, -c.Y, -c.Z);


        Render();

        int iconTexID;
        GL.GenTextures(1, out iconTexID);
        GL.BindTexture(TextureTarget.Texture2D, iconTexID);
        GL.Enable(EnableCap.Texture2D);
        GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                 0, 0, iconSize, iconSize, 0);

        BitmapData data = icon.LockBits(new System.Drawing.Rectangle(0, 0, iconSize, iconSize),
            ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

        icon.UnlockBits(data);
        icon.RotateFlip(RotateFlipType.RotateNoneFlipY);

        //icon.Save(Name + ".bmp");

        GL.Disable(EnableCap.Texture2D);

        GL.PopAttrib();
        return icon;
    }
}

[Serializable]
public class ObjModel : Model
{
    public ObjModel(string _name = "Unamed") : base(_name) { }
    public ObjModel(Mesh m, string _name = "")
    {
        meshes.Add(new Mesh[1]);
        meshes[0][0] = m;
        Name = _name;
        Prepare();
    }


    public static ObjModel Load(string fileName, bool searchForLod = true)
    {
        ObjModel model = new ObjModel();
        ObjMeshLoader.Load(fileName, model);
        model.objPath = fileName;
        model.Name = fileName;

        //check for other lod
        int l = 1;
        if (searchForLod)
        {
            bool nextLodExist = true;

            do
            {
                string lod = System.IO.Path.GetDirectoryName(fileName) +
                                System.IO.Path.DirectorySeparatorChar +
                                System.IO.Path.GetFileNameWithoutExtension(fileName) + "_lod" + l + ".obj";
                l++;
                if (File.Exists(lod))
                    ObjMeshLoader.Load(lod, model);
                else
                    nextLodExist = false;
            } while (nextLodExist);
        }
        model.Prepare();

        return model;
    }

    public string objPath;

    [NonSerialized]
    public string mtllib;
    [NonSerialized]
    public List<Material> materials;
    [NonSerialized]
    public List<Mesh[]> meshes = new List<Mesh[]>();

    public override void createShape()
    {
        List<JVector> positions = new List<JVector>();
        List<TriangleVertexIndices> indices = new List<TriangleVertexIndices>();

        foreach (Mesh[] mlist in meshes)
        {
            positions.AddRange(new List<Vertex>(mlist[0].Vertices).Select(v => new JVector(v.position.X, v.position.Y, v.position.Z)).ToList());
            foreach (FaceGroup fg in mlist[0].Faces)
	        {
                foreach (Triangle t in fg.Triangles)
                {
                    indices.Add(new TriangleVertexIndices
                            (
                                t.Index0,
                                t.Index1,
                                t.Index2
                            ));
                }
	        }
        }        
       
        // Build an octree of it
        Octree octree = new Octree(
            positions,
            indices);

        //octree.BuildOctree();

        // Pass it to a new instance of the triangleMeshShape

        shape = new TriangleMeshShape(octree);
        //shape.MakeHull(ref positions, 1);
    }
    public override int verticesCount
    {
        get
        {
            int vCount = 0;
            foreach (Mesh m in meshes[LOD])
            {
                vCount += m.Vertices.Length;
            }
            return vCount;
        }
    }

    //single texture for the whole model
    public int texture
    {
        get { return meshes[0][0].Faces[0].texture; }
        set
        {
            foreach (Mesh[] ms in meshes)
            {
                foreach (Mesh m in ms)
                {
                    foreach (FaceGroup fg in m.Faces)
                    {
                        fg.texture = value;
                    }
                }
            }
        }
    }


    public override void Prepare()
    {
        bounds = new BoundingBox();

        bool firstLod = true;

        foreach (Mesh[] ms in meshes)
        {
            for (int i = 0; i < ms.Length; i++)
            {
                if (firstLod)
                {
                    ms[i].ComputeBounds();
                    bounds += ms[i].bounds;
                }
                ms[i].Prepare();
            }
            firstLod = false;
        }
    }
		
    public override void Render()
    {
        for (int i = 0; i < meshes[LOD].Length; i++)
        {
            GL.PushAttrib(AttribMask.EnableBit);

            if (AlphaBlend)
            {
                GL.Enable(EnableCap.AlphaTest);
                GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }

            meshes[LOD][i].Render();

            GL.PopAttrib();
        }
    }

	public static ObjModel operator +(ObjModel om, BOquads q)
	{
		for (int i = 0; i < om.meshes.Count; i++) {
			Mesh[] ms = om.meshes [i];
			Array.Resize<Mesh> (ref ms, ms.Length + 1);
			ms[ms.Length-1] = new Mesh(q);
		} 

		om.Prepare ();
		return om;
	}
	public static ObjModel operator +(ObjModel om, Mesh m)
	{
		for (int i = 0; i < om.meshes.Count; i++) {
			Mesh[] ms = om.meshes [i];
			Array.Resize<Mesh> (ref ms, ms.Length + 1);
			ms[ms.Length-1] = m;
			om.meshes [i] = ms;
		} 

		om.Prepare ();
		return om;
	}
}



