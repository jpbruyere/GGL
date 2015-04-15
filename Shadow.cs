using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace OTKGL
{
    public static class Shadow
    {
        
        
        static float[] lightProjection = new float[16];
        static float[] lightModelview = new float[16];

        public static int shadowTextureID;

        static float[] sPlane = new float[4] { 1.0f, 0.0f, 0.0f, 0.0f };
        static float[] tPlane = new float[4] { 0.0f, 1.0f, 0.0f, 0.0f };
        static float[] rPlane = new float[4] { 0.0f, 0.0f, 1.0f, 0.0f };
        static float[] qPlane = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };

        public static void initShadowMap()
        {
            // Set up some texture state that never changes
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GenTextures(1, out shadowTextureID);
            GL.BindTexture(TextureTarget.Texture2D, shadowTextureID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)(int)All.DepthTextureModeArb, (int)All.Intensity);
            //if (ambientShadowAvailable)
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)(int)All.TextureCompareFailValue, 0.5f);
            GL.TexGen(TextureCoordName.S, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.T, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.R, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.Q, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
        }

        public static void generateShadowMap()
        {
            GL.PolygonOffset(5.0f, 0.0f);
            float lightToSceneDistance, nearPlane, fieldOfView;

            // Save the depth precision for where it's useful
            lightToSceneDistance = (float)Math.Sqrt(vLight.X * vLight.X +
                                                    vLight.Y * vLight.Y +
                                                    vLight.Z * vLight.Z);
            float sceneBoundingRadius = 70;
            nearPlane = lightToSceneDistance - sceneBoundingRadius;

            fieldOfView = MathHelper.RadiansToDegrees(2.0f * (float)Math.Atan(sceneBoundingRadius / lightToSceneDistance));

            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GLU.Perspective(fieldOfView, 1.0f, nearPlane, nearPlane + 1.0f * sceneBoundingRadius);
            GL.GetFloat(GetPName.ProjectionMatrix, lightProjection);
            // Switch to light's point of view
            GL.PushMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GLU.LookAt(vLight.X, vLight.Y, vLight.Z,
                        64.0f, 64.0f, 0.0f, 0.0f, 0.0f, 1.0f);
            GL.GetFloat(GetPName.ModelviewMatrix, lightModelview);
            GL.Viewport(0, 0, ClientSize.Width, ClientSize.Width);

            // Clear the window with current clearing color
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // All we care about here is resulting depth values
            GL.ShadeModel(ShadingModel.Flat);
            GL.Disable(EnableCap.Lighting);
            //GL.Disable(EnableCap.ColorMaterial);
            GL.Disable(EnableCap.Normalize);
            GL.ColorMask(false, false, false, false);

            // Overcome imprecision
            GL.Enable(EnableCap.PolygonOffsetFill);

            World.CurrentWorld.render();

            // Copy depth values into depth texture
            GL.ActiveTexture(TextureUnit.Texture15);
            GL.BindTexture(TextureTarget.Texture2D, shadowTextureID);
            GL.Enable(EnableCap.Texture2D);
            GL.Uniform1(GL.GetUniformLocation(Terrain.terrainShader, "ShaderMap"), 15);

            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                     0, 0, ClientSize.Width, ClientSize.Width, 0);
            GL.Disable(EnableCap.Texture2D);
            // Restore normal drawing state
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Enable(EnableCap.Lighting);
            //GL.Enable(EnableCap.ColorMaterial);
            GL.Enable(EnableCap.Normalize);
            GL.ColorMask(true, true, true, true);
            GL.Disable(EnableCap.PolygonOffsetFill);

            // Set up texture matrix for shadow map projection
            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.Translate(0.5f, 0.5f, 0.5f);
            GL.Scale(0.5f, 0.5f, 0.5f);
            GL.MultMatrix(lightProjection);
            GL.MultMatrix(lightModelview);

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();

        }


        public static void drawShadowMap()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, shadowTextureID);
            GL.Enable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Texture);

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.Translate(0.5f, 0.5f, 0.5f);
            GL.Scale(0.5f, 0.5f, 0.5f);
            GL.MultMatrix(lightProjection);
            GL.MultMatrix(lightModelview);

            // Set up shadow comparison
            
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (float)TextureCompareMode.CompareRToTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);

            // Set up the eye plane for projecting the shadow map on the scene
            GL.Enable(EnableCap.TextureGenS);
            GL.Enable(EnableCap.TextureGenT);
            GL.Enable(EnableCap.TextureGenR);
            GL.Enable(EnableCap.TextureGenQ);
            GL.TexGen(TextureCoordName.S, TextureGenParameter.EyePlane, sPlane);
            GL.TexGen(TextureCoordName.T, TextureGenParameter.EyePlane, tPlane);
            GL.TexGen(TextureCoordName.R, TextureGenParameter.EyePlane, rPlane);
            GL.TexGen(TextureCoordName.Q, TextureGenParameter.EyePlane, qPlane);
            
            
        }        
    }
}
