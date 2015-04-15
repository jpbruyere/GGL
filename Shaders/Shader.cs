using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace GGL
{
    public class Shader
    {        
        public int shaderProgram = 0;

		#region Sources

        string _vertSource =
			@"  #version 120
                void main()
                {
                  gl_TexCoord[0] = gl_MultiTexCoord0;
                  gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				  gl_FrontColor = gl_Color;
                }
            ";
        string _fragSource =
            @"
            #version 130
 
            void main(void) {            
                gl_FragColor = gl_Color;  
            }";
        string _geomSource = @"";

		#endregion
		        
        public virtual string vertSource
        {
            get { return _vertSource;}
            set { _vertSource = value; }
        }
        public virtual string fragSource 
        {
            get { return _fragSource;}
            set { _fragSource = value; }
        }
        public virtual string geomSource
        { 
            get { return _geomSource; }          
            set { _geomSource = value; }
        }

        int vertShaderID = 0,
            fragShaderID = 0,
            geomShaderID = 0;
		bool isAttached = false;

        public Shader()
        {
            Init();            
        }


        public void Init()
        {
            if (!GL.GetString(StringName.Extensions).Contains("EXT_geometry_shader4") && !string.IsNullOrEmpty(geomSource))
            {
                System.Windows.Forms.MessageBox.Show(
                     "Your video card does not support EXT_geometry_shader4. Please update your drivers.",
                     "EXT_geometry_shader4 not supported",
                     System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }

            shaderProgram = GL.CreateProgram();
        }

        public void Compile()
        {
			if (!string.IsNullOrEmpty(vertSource))
            {
                vertShaderID = GL.CreateShader(ShaderType.VertexShader);
				compileShader(vertShaderID, vertSource);
            }
			if (!string.IsNullOrEmpty(fragSource))
            {
                fragShaderID = GL.CreateShader(ShaderType.FragmentShader);
				compileShader(fragShaderID, fragSource);
                
            }
			if (!string.IsNullOrEmpty(geomSource))
            {
                geomShaderID = GL.CreateShader(ShaderType.GeometryShader);
				compileShader(geomShaderID,geomSource);                
            }

			if (fragShaderID != 0)
				GL.AttachShader(shaderProgram, fragShaderID);
			if (vertShaderID != 0)
				GL.AttachShader(shaderProgram, vertShaderID);
			if (geomShaderID != 0)
				GL.AttachShader(shaderProgram, geomShaderID);

			GL.LinkProgram(shaderProgram);
			GL.ValidateProgram(shaderProgram);

			string info;
			GL.GetProgramInfoLog(shaderProgram, out info);
			Debug.WriteLine(info);
		}

        /// <summary>
        /// Helper method to avoid code duplication.
        /// Compiles a shader and prints results using Debug.WriteLine.
        /// </summary>
        /// <param name="shader">A shader object, gotten from GL.CreateShader.</param>
        /// <param name="source">The GLSL source to compile.</param>
        void compileShader(int shader, string source)
        {
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            string info;
            GL.GetShaderInfoLog(shader, out info);
            Debug.WriteLine(info);

            int compileResult;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);
            if (compileResult != 1)
            {
                Debug.WriteLine("Compile Error!");
                Debug.WriteLine(source);
            }
        }

        public static implicit operator int(Shader s)
        {
            return s == null ? 0 : s.shaderProgram;
        }

        ~Shader()
        {
			GL.DeleteProgram (shaderProgram);

            //// Clean up resources. Note the program object is not deleted.
            //if (fragShaderID != 0)
            //    GL.DeleteShader(fragShaderID);
            //if (vertShaderID != 0)
            //    GL.DeleteShader(vertShaderID);
            //if (geomShaderID != 0)
            //    GL.DeleteShader(geomShaderID);
        }

		int savedPgm = 0;
		public virtual void Enable()
		{
			GL.GetInteger (GetPName.CurrentProgram, out savedPgm);
			GL.UseProgram (shaderProgram);
		}
		public virtual void Disable()
		{
			GL.UseProgram (savedPgm);
		}
    
		public static void Enable(Shader s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(Shader s)
		{
			if (s == null)
				return;
			s.Disable ();
		}
	}
}
