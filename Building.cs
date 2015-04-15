using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using GGL;

namespace GGL
{

    [Serializable]
    public class Building : Model
    {
        [NonSerialized]
        public static List<Texture> textures = new List<Texture>();
        public static void loadTextures()
        {
			textures.Add(new Texture(directories.rootDir + @"Images\texture\Shops0011_thumbhuge.jpg", false));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0414_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0479_9_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0504_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0504_2_thumbhuge_reflection.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0545_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0547_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\BuildingsHighRise0547_2_thumbhuge_reflexion.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0026_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0026_2_thumblarge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0026_2_thumblarge_reflexion.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0032_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0069_2_thumbhuge.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\HighRiseGlass0069_2_thumbhuge_reflexion.jpg", true));
        }

        BOquads stage;
        BOquads shops;
        BOquads roof;

        [NonSerialized]
        public int id = 0;
        [NonSerialized]
        public int texture;

        public int texIndex = 1;
        public int nbStages = 5;
        public float textureTile = 2f;


        public float stageHeight = 5f;
        public float stageWidth = 5f;


        public Building(string _name, int _nbStages, int _texIndex)
        {
            texIndex = _texIndex;
            nbStages = _nbStages;
            Name = _name;

            CreateRoadVBO();
        }

        public override void Prepare()
        {
            stage.Prepare();
            shops.Prepare();
            roof.Prepare();

            bounds = stage.bounds + shops.bounds + roof.bounds;

        }

        public override void Render()
        {

            GL.PushAttrib(AttribMask.EnableBit);
            GL.PushMatrix();
            

            //GL.ActiveTexture(TextureUnit.Texture0);


            GL.LoadName(id);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();


            GL.BindTexture(TextureTarget.Texture2D, textures[0]);
            GL.Enable(EnableCap.Texture2D);
            
            shops.Render();

            float roofDiff = stageWidth * 0.025f;
            GL.Translate(-roofDiff, -roofDiff, shops.height);

            GL.BindTexture(TextureTarget.Texture2D, World.concrete);
            GL.Enable(EnableCap.Texture2D);

            roof.Render();

            //GL.Disable(EnableCap.ColorMaterial);

            GL.Translate(roofDiff, roofDiff, roof.height);

            GL.BindTexture(TextureTarget.Texture2D, textures[texIndex]);
            GL.Enable(EnableCap.Texture2D);

            for (int s = 0; s < nbStages; s++)
            {
                stage.Render();
                GL.Translate(0f, 0f, stageHeight);
            }

            GL.Translate(-roofDiff, -roofDiff, 0f);

            GL.BindTexture(TextureTarget.Texture2D, World.concrete);
            GL.Enable(EnableCap.Texture2D);

            roof.Render();

            GL.Disable(EnableCap.Texture2D);

            GL.Translate(roofDiff, roofDiff, roof.height);
            
            

            GL.PopMatrix();


            GL.PopAttrib();
            GL.PopMatrix();

        }


        void CreateRoadVBO()
        {
            stage = BOquads.createUncappedCube(stageWidth, stageHeight, 2f, 2f);
            shops = BOquads.createUncappedCube(stageWidth, 1f, 2f, 1f);

            float roofSize = stageWidth * 1.05f;
            roof = BOquads.createCappedCube(roofSize, 0.07f, 5f, 5f);
            

            Prepare();
        }


        //void CreateRoadVBO()
        //{
        //    Vertices = new Vertex[14];
        //    Quads = new Quad[4];

        //    Vertices[0].position = Vector3.Zero;
        //    Vertices[0].TexCoord = new Vector2(0, textureTile);
        //    Vertices[0].Normal = Vector3.UnitX;

        //    Vertices[1].position = new Vector3(0f, 0f, stageHeight);
        //    Vertices[1].TexCoord = new Vector2(0, 0);
        //    Vertices[1].Normal = Vector3.UnitX;

        //    Vertices[2].position = new Vector3(stageWidth, 0f, 0f);
        //    Vertices[2].TexCoord = new Vector2(textureTile, textureTile);
        //    Vertices[2].Normal = Vector3.UnitX;

        //    Vertices[3].position = new Vector3(stageWidth, 0f, stageHeight);
        //    Vertices[3].TexCoord = new Vector2(textureTile, 0);
        //    Vertices[3].Normal = Vector3.UnitX;

        //    Vertices[4].position = new Vector3(stageWidth, 0f, 0f);
        //    Vertices[4].TexCoord = new Vector2(0, textureTile);
        //    Vertices[4].Normal = Vector3.UnitY;

        //    Vertices[5].position = new Vector3(stageWidth, 0f, stageHeight);
        //    Vertices[5].TexCoord = new Vector2(0, 0);
        //    Vertices[5].Normal = Vector3.UnitY;

        //    Vertices[6].position = new Vector3(stageWidth, stageWidth, 0f);
        //    Vertices[6].TexCoord = new Vector2(textureTile, textureTile);
        //    Vertices[6].Normal = Vector3.UnitY;

        //    Vertices[7].position = new Vector3(stageWidth, stageWidth, stageHeight);
        //    Vertices[7].TexCoord = new Vector2(textureTile, 0);
        //    Vertices[7].Normal = Vector3.UnitY;

        //    Vertices[8].position = new Vector3(stageWidth, stageWidth, 0f);
        //    Vertices[8].TexCoord = new Vector2(textureTile, textureTile);
        //    Vertices[8].Normal = -Vector3.UnitX;

        //    Vertices[9].position = new Vector3(stageWidth, stageWidth, stageHeight);
        //    Vertices[9].TexCoord = new Vector2(textureTile, 0);
        //    Vertices[9].Normal = -Vector3.UnitX;

        //    Vertices[10].position = new Vector3(0f, stageWidth, 0f);
        //    Vertices[10].TexCoord = new Vector2(0, textureTile);
        //    Vertices[10].Normal = -Vector3.UnitY;

        //    Vertices[11].position = new Vector3(0f, stageWidth, stageHeight);
        //    Vertices[11].TexCoord = new Vector2(0, 0);
        //    Vertices[11].Normal = -Vector3.UnitY;

        //    Vertices[12].position = Vector3.Zero;
        //    Vertices[12].TexCoord = new Vector2(0, textureTile);
        //    Vertices[12].Normal = -Vector3.UnitY;

        //    Vertices[13].position = new Vector3(0f, 0f, stageHeight);
        //    Vertices[13].TexCoord = new Vector2(textureTile, 0);
        //    Vertices[13].Normal = -Vector3.UnitY;

        //    for (int q = 0; q < 4; q++)
        //    {
        //        Quad s = new Quad();
        //        s.Index0 = q * 4 + 1;
        //        s.Index1 = q * 4;
        //        s.Index2 = q * 4 + 2;
        //        s.Index3 = q * 4 + 3;
        //        Quads[q] = s;
        //    }

        //    Prepare();
        //}


    }

   
}
