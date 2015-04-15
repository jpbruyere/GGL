using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using go;
//using System.Drawing;

namespace GGL
{
    [Serializable]
    public class Forest
    {
        static Random rand = new Random();
        public static Shader shader = new ExternalShader("forest");

        public List<Vector3> Positions = new List<Vector3>();
        public Matrix4 mRot;

        public static List<Texture> textures;
        static TextureUnit texUnit = TextureUnit.Texture0;

        [NonSerialized]
        static int _selectetTreeIndex = 0;
        public static int selectetTreeIndex
        {
            get { return _selectetTreeIndex; }
            set
            {
                if (value < 0)
                    _selectetTreeIndex = textures.Count - 1;
                else if (value > textures.Count - 1)
                    _selectetTreeIndex = 0;
                else
                    _selectetTreeIndex = value;
            }
        }

        int _nbTrees;
        float _minSpace = 0.1f;
        public float texSize = 512;

        private Rectangle _bounds = Rectangle.Zero;

        public float minSpace
        {
            get { return _minSpace; }
            set { _minSpace = value; }
        }
        public Rectangle bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }
        public int nbTrees
        {
            get { return _nbTrees; }
            set { _nbTrees = value; }
        }
        public int texIndex = 0;

        private World _world;

        public enum treeEnum
        {
            firstTree,
            pine,
            cityTree
        }


        public static void initTextures()
        {
            textures = new List<Texture>();

            textures.Add(new Texture(directories.rootDir + @"obj\trees\tree1.png"));
            //textures[1] = new Texture(false, directories.rootDir + @"Images\texture\tree\tree2.jpg", true, shader, texUnit, "sprite_texture");
            //textures[2] = new Texture(false, directories.rootDir + @"Images\texture\tree\tree223 copia.png", true, shader, texUnit, "sprite_texture");
            textures.Add(new Texture(directories.rootDir + @"Images\texture\tree\USA WEST-8.png"));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\tree\USA WEST-39.png"));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\tree\USA WEST-9.png"));
            //textures[2] = new Texture(false, directories.rootDir + @"Images\texture\tree\autumn_tree_woods.jpg", true, shader, texUnit, "sprite_texture");
            //textures[3] = new Texture(false, directories.rootDir + @"Images\texture\tree\bush_tree_jungle.jpg", true, shader, texUnit, "sprite_texture");
            //textures[4] = new Texture(false, directories.rootDir + @"Images\texture\tree\erithrina01.png", true, shader, texUnit, "sprite_texture");
            //textures[5] = new Texture(false, directories.rootDir + @"Images\texture\tree\evergreen_tree_polepole.jpg", true, shader, texUnit, "sprite_texture");
            //textures[6] = new Texture(false, directories.rootDir + @"Images\texture\tree\flamboyan01.png", true, shader, texUnit, "sprite_texture");
            //textures[7] = new Texture(false, directories.rootDir + @"Images\texture\tree\fraxinus farbe.png", true, shader, texUnit, "sprite_texture");
            //textures[8] = new Texture(false, directories.rootDir + @"Images\texture\tree\pohon5.png", true, shader, texUnit, "sprite_texture");
            //textures[9] = new Texture(false, directories.rootDir + @"Images\texture\tree\tree1.png", true, shader, texUnit, "sprite_texture");
            //textures[10] = new Texture(false, directories.rootDir + @"Images\texture\tree\TREE03.jpg", true, shader, texUnit, "sprite_texture");
            //textures[12] = new Texture(false, directories.rootDir + @"Images\texture\tree\trees0042_2_s.jpg", true, shader, texUnit, "sprite_texture");

            //textures[14] = new Texture(false, directories.rootDir + @"Images\texture\tree\USA WEST-9.png", true, shader, texUnit, "sprite_texture");
            //textures[15] = new Texture(false, directories.rootDir + @"Images\texture\tree\USA WEST-11.png", true, shader, texUnit, "sprite_texture");
            //textures[16] = new Texture(false, directories.rootDir + @"Images\texture\tree\USA WEST-12.png", true, shader, texUnit, "sprite_texture");
            //textures[17] = new Texture(false, directories.rootDir + @"Images\texture\tree\USA WEST-11.png", true, shader, texUnit, "sprite_texture");
        
        }

        public static void createDefaultForest()
        {
            initTextures();

            World.CurrentWorld.addForest(new Forest(0));
            World.CurrentWorld.addForest(new Forest(1));
            World.CurrentWorld.addForest(new Forest(2));
            World.CurrentWorld.addForest(new Forest(3));
        }


        public Forest(int TreesNumber, int _textureIndex = 0, float size = 512, float MinimalSpaces = 0.1f)
        {
            nbTrees = TreesNumber;
            minSpace = MinimalSpaces;
            texIndex = _textureIndex;
            texSize = size;

        }
        public Forest(int _textureIndex)
        {
            minSpace = 0.1f;
            texIndex = _textureIndex;
            texSize = 512;
        }
        public void bind()
        {
            mRot = Matrix4.CreateRotationZ((float)(Math.PI));

			//GL.Uniform1(GL.GetUniformLocation(Shader.mainProgram, "texSize"), 1, ref texSize);

            //textures[0] = new Texture(false,directories.rootDir + @"obj\trees\tree1.png");
            //shader = null;

            if (bounds == Rectangle.Zero)
            {
                int s = World.worldSize * Terrain.size;
                bounds = new Rectangle(0, 0, s, s);
            }

            int invalidTriesCount = 0;

            List<Vector3> posList = new List<Vector3>();


            while (posList.Count < nbTrees && invalidTriesCount < 30)
            {
                float x = -1f,
                      y = -1f;

                x = (float)rand.NextDouble() * bounds.Width + bounds.Left;
                y = (float)rand.NextDouble() * bounds.Height + bounds.Top;

                Vector2 newPos = new Vector2(x, y);

                bool newPosIsValid = true;

                foreach (Vector3 pos in posList)
                {
                    Vector2 vDist = new Vector2(pos) - newPos;

                    if (vDist.Length < minSpace)
                    {
                        newPosIsValid = false;
                        break;
                    }
                }

                if (newPosIsValid)
                {
                    invalidTriesCount = 0;
                    int tex = 1;// rand.Next(textures.Length);
                    posList.Add(new Vector3(x, y, _world.getHeight(x, y)));
                }
                else
                    invalidTriesCount++;
            }
            nbTrees = posList.Count();
            Positions = posList;
        }

        public void bind(World world)
        {
            _world = world;
            bind();
        }

        public void addTree(Vector3 position)
        {
            Positions.Add(position);
        }
        public void render()
        {
            GL.PushMatrix();
            GL.PushAttrib(AttribMask.EnableBit);
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);

            GL.Enable(EnableCap.PointSprite);
            GL.Enable(EnableCap.VertexProgramPointSize);

            GL.PointParameter(PointSpriteCoordOriginParameter.LowerLeft);

            
			//GL.Uniform1(GL.GetUniformLocation(Shader.mainProgram, "texSize"), 1, ref texSize);
			//GL.Uniform3(GL.GetUniformLocation(Shader.mainProgram, "vEye"), World.vEye);


            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


            GL.ActiveTexture(texUnit);
            GL.BindTexture(TextureTarget.Texture2D, textures[texIndex]);

            GL.Enable(EnableCap.Texture2D);

            //float[] modelview = new float[16];
            //int i,j;

            //GL.GetFloat(GetPName.m,modelview);
            //// The only difference now is that
            //// the i variable will jump over the
            //// up vector, 2nd column in OpenGL convention

            //// undo all rotations
            //// beware all scaling is lost as well 
            //for (i = 0; i < 3; i++)
            //    for (j = 0; j < 3; j++)
            //    {
            //        if (i == j)
            //            modelview[i * 4 + j] = 1.0f;
            //        else
            //            modelview[i * 4 + j] = 0.0f;
            //    }
            //GL.LoadMatrix(modelview);


            //float[] quadratic =  { 0.1f, 0.1f, 0.1f };
            //GL.PointParameter(PointParameterName.PointDistanceAttenuation, quadratic );


            GL.Begin(BeginMode.Points);

            foreach (Vector3  v in Positions)
            {
                //GL.ActiveTexture(texUnit);
                //GL.BindTexture(TextureTarget.Texture2D, textures[(int)Positions[i].W]);
                //GL.Enable(EnableCap.Texture2D);
                GL.Vertex3(v);
                //GL.Disable(EnableCap.Texture2D);
            }
            GL.End();


            GL.Disable(EnableCap.Texture2D);
            GL.PopClientAttrib();
            GL.PopAttrib();
            GL.PopMatrix();
        }

    }
}
