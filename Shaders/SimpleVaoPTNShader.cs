using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace GameLib2
{
	public class SimpleVaoPTNShader : IDisposable
	{
		#region CTOR
		public SimpleVaoPTNShader ()
		{
			Compile ();
		}
		#endregion

		#region Sources
		protected string _vertSource = @"
			#version 130

			precision highp float;

			uniform mat4 projection_matrix;
			uniform mat4 modelview_matrix;
			uniform mat4 model_matrix;
			uniform mat4 normal_matrix;
			uniform vec4 lightPos;
			

			in vec3 in_position;
			in vec2 in_tex;
			in vec3 in_normal;
			out vec2 texCoord;
			out vec3 v;
			out vec3 N;
			out vec3 lPos;
			

			void main(void)
			{
				mat4 normalMatrix = transpose(inverse(modelview_matrix * model_matrix));

				texCoord = in_tex;
				N = normalize(vec3(normalMatrix * vec4(in_normal, 0)));

				v = vec3(modelview_matrix * model_matrix * vec4(in_position, 1));

				lPos = vec3(modelview_matrix * lightPos);
				gl_Position = projection_matrix * modelview_matrix * model_matrix * vec4(in_position, 1);
			}";

		protected string _fragSource = @"
			#version 130
			precision highp float;

			uniform vec4 color;
			uniform sampler2D tex;


			in vec2 texCoord;			
			in vec3 v;
			in vec3 N;
			in vec3 lPos;
			
			
			out vec4 out_frag_color;

			void main(void)
			{
				vec3 L = normalize(lPos-v);   
   				vec3 Idiff = vec3(1.0,1.0,1.0) * max(dot(N,L), 0.0);  
   				Idiff = clamp(Idiff, 0.0, 1.0);    
				vec4 diffTex = texture( tex, texCoord);

				out_frag_color =  vec4(diffTex.rgb * Idiff,diffTex.a); 
			}";
		string _geomSource = "";
		#endregion

		#region Private and protected fields
		protected int vsId, fsId, gsId, pgmId, savedPgmId = 0,
						modelviewMatrixLocation,
						modelMatrixLocation,
						normalMatrixLocation,
						projectionMatrixLocation,
						lightPosLocation,
						colorLocation,stencilTestLocation,resolutionLocation;

		Matrix4 projectionMatrix, 
				modelMatrix,
				modelviewMatrix;
		#endregion


		#region Public properties
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

		public Matrix4 ProjectionMatrix{
			set { 
				projectionMatrix = value;
				GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);  
			}
		}
		public Matrix4 ModelMatrix{
			set { 
				modelMatrix = value;
				GL.UniformMatrix4(modelMatrixLocation, false, ref modelMatrix);
				updateNormalMatrix ();
			}
		}
		public Matrix4 ModelViewMatrix {
			set { 
				modelviewMatrix = value;
				GL.UniformMatrix4 (modelviewMatrixLocation, false, ref modelviewMatrix);
				updateNormalMatrix ();
			}
		}
		void updateNormalMatrix()
		{
			Matrix4 normalMat = (modelviewMatrix * modelMatrix).Inverted();
			normalMat.Transpose ();
			GL.UniformMatrix4 (normalMatrixLocation, false, ref normalMat);
		}
		public Vector4 Color {
			set {GL.Uniform4 (colorLocation, value);}
		}

		public bool StencilTest {
			set {
				if (value)
					GL.Uniform1 (stencilTestLocation, 1);
				else
					GL.Uniform1 (stencilTestLocation, 0);
			}
		}

		public Vector2 Resolution {
			set { GL.Uniform2 (resolutionLocation, value); }
		}

		public Vector4 LightPos {
			set { GL.Uniform4 (lightPosLocation, value); }
		}

		#endregion

		#region Public functions
		public virtual void Compile()
		{
			Dispose ();

			pgmId = GL.CreateProgram();

			if (!string.IsNullOrEmpty(vertSource))
			{
				vsId = GL.CreateShader(ShaderType.VertexShader);
				compileShader(vsId, vertSource);
			}
			if (!string.IsNullOrEmpty(fragSource))
			{
				fsId = GL.CreateShader(ShaderType.FragmentShader);
				compileShader(fsId, fragSource);

			}
			if (!string.IsNullOrEmpty(geomSource))
			{
				gsId = GL.CreateShader(ShaderType.GeometryShader);
				compileShader(gsId,geomSource);                
			}

			if (vsId != 0)
				GL.AttachShader(pgmId, vsId);
			if (fsId != 0)
				GL.AttachShader(pgmId, fsId);
			if (gsId != 0)
				GL.AttachShader(pgmId, gsId);

			BindVertexAttributes ();

			GL.LinkProgram(pgmId);
			GL.ValidateProgram(pgmId);

			string info;
			GL.GetProgramInfoLog(pgmId, out info);
			Debug.WriteLine(info);

			Enable ();

			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}
		protected virtual void BindVertexAttributes()
		{
			GL.BindAttribLocation(pgmId, 0, "in_position");
			GL.BindAttribLocation(pgmId, 1, "in_tex");
			GL.BindAttribLocation(pgmId, 2, "in_normal");
		}
		protected virtual void GetUniformLocations()
		{
			projectionMatrixLocation = GL.GetUniformLocation(pgmId, "projection_matrix");
			modelviewMatrixLocation = GL.GetUniformLocation(pgmId, "modelview_matrix");
			modelMatrixLocation = GL.GetUniformLocation(pgmId, "model_matrix");
			normalMatrixLocation = GL.GetUniformLocation(pgmId, "normal_matrix");
			colorLocation = GL.GetUniformLocation (pgmId, "color");
			lightPosLocation = GL.GetUniformLocation (pgmId, "lightPos");
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"),0);
		}

		public virtual void Enable(){
			GL.GetInteger (GetPName.CurrentProgram, out savedPgmId);
			GL.UseProgram (pgmId);
		}
		public virtual void Disable(){
			GL.UseProgram (savedPgmId);
		}
		public static void Enable(SimpleVaoPTNShader s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(SimpleVaoPTNShader s)
		{
			if (s == null)
				return;
			s.Disable ();
		}
		#endregion

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
			
		#region IDisposable implementation
		public void Dispose ()
		{
			if (GL.IsProgram (pgmId))
				GL.DeleteProgram (pgmId);

			if (GL.IsShader (vsId))
				GL.DeleteShader (vsId);
			if (GL.IsShader (fsId))
				GL.DeleteShader (fsId);
			if (GL.IsShader (gsId))
				GL.DeleteShader (gsId);
		}
		#endregion
	}
}

