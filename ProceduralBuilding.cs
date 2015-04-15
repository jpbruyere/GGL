using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Drawing;

namespace GGL
{
    public class ProceduralBuilding : Model
    {
        public BOquads Structure;

        public float width = 2f;
        public float length = 2f;
        public int nbStages = 1;
        public float roofHeight = 1f;
        public float stageHeight = 1f;
        public float roofGap = 0.1f;
        public float roofTopGap = 0.1f;
        public Vector3 position;
        public Vector3 vDir = Vector3.UnitX;

        public float tileFactor = 20f;

        public int texIndex = 0;

        [NonSerialized]
        public static List<Texture> textures = new List<Texture>();
        public static void loadTextures()
        {
			textures.Add(new Texture(directories.rootDir + @"Images\texture\structures\HouseTest2.png", false));
        }

        public ProceduralBuilding()
        { }

        public ProceduralBuilding(float _length, float _width, float _height, float _roofHeight)
        {
            length = _length;
            width = _width;
            stageHeight = _height;
            roofHeight = _roofHeight;

            build();
        }
        public ProceduralBuilding(Vector3 dimensions, float _roofHeight = 0.5f)
        {
            length = dimensions.X;
            width = dimensions.Y;
            stageHeight = dimensions.Z;
            roofHeight = _roofHeight;

            build();            
        }

        public ProceduralBuilding(Vector3 _position, Vector3 _vdir, Vector3 dimensions, float _roofHeight)
        {
            position = _position;
            vDir = _vdir;
            length = dimensions.X;
            width = dimensions.Y;
            stageHeight = dimensions.Z;
            roofHeight = _roofHeight;

            build();            
        }

        public virtual void build()
        {
            Vector3 vDirPerp = new Vector3(new Vector2(vDir).PerpendicularLeft);


            Vector3 vHalfLength = vDir * length;
            Vector3 vHalfWidth = vDirPerp * width;
            Vector3 vHeightStage = new Vector3(0, 0, stageHeight);
            Vector3 vRoofHeight = new Vector3(0, 0, roofHeight);

            BOquads front = BOquads.createPlane
                (
                    position + vHalfLength + vHalfWidth,
                    position + vHeightStage + vHalfLength + vHalfWidth,
                    position + vHeightStage - vHalfLength + vHalfWidth,
                    position - vHalfLength + vHalfWidth,
                    0.0f, 0.75f, 0.0f, 0.25f
                );
            BOquads back = BOquads.createPlane
                (
                    position - vHalfLength - vHalfWidth,
                    position + vHeightStage - vHalfLength - vHalfWidth,
                    position + vHeightStage + vHalfLength - vHalfWidth,
                    position + vHalfLength - vHalfWidth,
                    0.0f, 0.75f, 0.25f, 0.5f
                );

            BOquads left1 = BOquads.createPlane
                (
                    position + vHalfLength - vHalfWidth,
                    position + vHeightStage + vHalfLength - vHalfWidth,
                    position + vHeightStage + vRoofHeight + vHalfLength,
                    position + vHalfLength,
                    0.75f, 1f, 0f, 0.5f, true
                );
            BOquads left2 = BOquads.createPlane
                (
                    position + vHalfLength,
                    position + vHeightStage + vRoofHeight + vHalfLength,
                    position + vHeightStage + vHalfLength + vHalfWidth,
                    position + vHalfLength + vHalfWidth,
                    0.75f, 1f, 0f, 0.5f, true
                );

            BOquads right1 = BOquads.createPlane
                (
                    position - vHalfLength + vHalfWidth,
                    position + vHeightStage - vHalfLength + vHalfWidth,
                    position + vHeightStage + vRoofHeight - vHalfLength,
                    position - vHalfLength,
                    0.75f, 1f, 0f, 0.5f, true
                );
            BOquads right2 = BOquads.createPlane
                (
                    position - vHalfLength,
                    position + vHeightStage + vRoofHeight - vHalfLength,
                    position + vHeightStage - vHalfLength - vHalfWidth,
                    position - vHalfLength - vHalfWidth,
                    0.75f, 1f, 0f, 0.5f, true
                );

            Vector3 roofDir = Vector3.Normalize(vRoofHeight - vHalfWidth);
            BOquads roofFront = BOquads.createPlane
            (
                position + vHeightStage + vHalfLength + vHalfWidth - Vector3.Normalize(vRoofHeight - vHalfWidth - vHalfLength) * roofGap,
                position + vHeightStage + vRoofHeight + vHalfLength + vDir * roofTopGap,
                position + vHeightStage + vRoofHeight - vHalfLength - vDir * roofTopGap,
                position + vHeightStage - vHalfLength + vHalfWidth - Vector3.Normalize(vRoofHeight - vHalfWidth + vHalfLength) * roofGap,
                0.0f, 0.7f, 0.68f, 1.0f
            );

            BOquads roofBack = BOquads.createPlane
            (
                position + vHeightStage - vHalfLength - vHalfWidth - Vector3.Normalize(vRoofHeight + vHalfWidth + vHalfLength) * roofGap,
                position + vHeightStage + vRoofHeight - vHalfLength - vDir * roofTopGap,
                position + vHeightStage + vRoofHeight + vHalfLength + vDir * roofTopGap,
                position + vHeightStage + vHalfLength - vHalfWidth - Vector3.Normalize(vRoofHeight + vHalfWidth - vHalfLength) * roofGap,
                0.0f, 0.7f, 0.68f, 1.0f
            );

            Structure = front + back + left1 + left2 + right1 + right2 + roofFront + roofBack;
        }

        public override void Prepare()
        {
            Structure.Prepare();

            bounds = Structure.bounds;

        }

        public override void Render()
        {

            GL.PushAttrib(AttribMask.EnableBit);

            GL.Disable(EnableCap.Lighting);

            //GL.ActiveTexture(TextureUnit.Texture0);


            GL.BindTexture(TextureTarget.Texture2D, textures[texIndex]);
            GL.Enable(EnableCap.Texture2D);
            GL.Color3(Color.White);

            Structure.Render();

            GL.PopAttrib();

        }



    }
}
