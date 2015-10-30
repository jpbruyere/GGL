using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Linq;

namespace GameLib
{
	public class Shader : IDisposable
	{
		class SubRoutine
		{
			public string Name;
			public int Id;
			public ShaderType ShaderStage;

			public SubRoutine(string _name, int _id, ShaderType _stage)
			{
				Name = _name;
				Id = _id;
				ShaderStage = _stage;
			}
		}

		#region CTOR
		public Shader ()
		{
			Compile ();
		}
		public Shader (string vertResId, string fragResId)
		{

			Stream s = tryGetStreamForResource (vertResId);
			if (s != null) {
				using (StreamReader sr = new StreamReader (s)) {				
					vertSource = sr.ReadToEnd ();
				}
			}

			s = tryGetStreamForResource (fragResId);
			if (s != null) {
				using (StreamReader sr = new StreamReader (s)) {				
					fragSource = sr.ReadToEnd ();
				}
			}

			Compile ();
		}
		Stream tryGetStreamForResource(string resId){
			if (string.IsNullOrEmpty (resId))
				return null;
			
			Stream s = Assembly.GetEntryAssembly ().
				GetManifestResourceStream (resId);
			return s == null ?
				Assembly.GetExecutingAssembly ().
					GetManifestResourceStream (resId) :
				s;
		}
		#endregion

		#region Sources
		protected string _vertSource = @"
			#version 330

			precision highp float;

			uniform mat4 Projection;
			uniform mat4 ModelView;
			uniform mat4 Model;
			uniform mat4 Normal;

			in vec3 in_position;
			in vec2 in_tex;

			out vec2 texCoord;
			

			void main(void)
			{
				texCoord = in_tex;
				gl_Position = Projection * ModelView * Model * vec4(in_position, 1);
			}";

		protected string _fragSource = @"
			#version 330
			precision highp float;

			uniform vec4 color;
			uniform sampler2D tex;

			in vec2 texCoord;
			out vec4 out_frag_color;

			void main(void)
			{
				out_frag_color = texture( tex, texCoord);
			}";
		string _geomSource = @"";
//			#version 330 
//			layout(triangles) in;
//			layout(triangle_strip, max_vertices=3) out;
//			void main()
//			{
//				for(int i=0; i<3; i++)
//				{
//					gl_Position = gl_in[i].gl_Position;
//					EmitVertex();
//				}
//				EndPrimitive();
//			}";
		#endregion

		#region Private and protected fields
		protected int vsId, fsId, gsId, pgmId, 
						modelViewLocation,
						modelLocation,
						projectionLocation,
						normalLocation,	
						colorLocation;

		Matrix4 projectionMat = Matrix4.Identity, 
				modelMat = Matrix4.Identity,
				modelViewMat = Matrix4.Identity;
		Vector4 color = new Vector4(1,1,1,1);

		List<SubRoutine> SubRoutines;
		SubRoutine activeSubRoutine;
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
			set { projectionMat = value; }
			get { return projectionMat; }
		}
		public Matrix4 ModelViewMatrix {
			set { modelViewMat = value; }
			get { return modelViewMat; }
		}
		public Matrix4 ModelMatrix {
			set { modelMat = value; }
			get { return modelMat; }
		}
		public Vector4 Color {
			set { color = value; }
			get { return color; }
		}

		#endregion

		void updateNormalMatrix()
		{
			Matrix4 normalMat = (modelViewMat).Inverted();
			normalMat.Transpose ();
			GL.UniformMatrix4 (normalLocation, false, ref normalMat);
		}

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

			string info;
			GL.LinkProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);

			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Linkage:");
				Debug.WriteLine (info);
			}

			info = null;

			GL.ValidateProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);
			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Validation:");
				Debug.WriteLine (info);
			}
				
			GL.UseProgram (pgmId);

			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}

		protected virtual void BindVertexAttributes()
		{
			GL.BindAttribLocation(pgmId, 0, "in_position");						
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected virtual void GetUniformLocations()
		{
			projectionLocation = GL.GetUniformLocation(pgmId, "Projection");
			modelViewLocation = GL.GetUniformLocation(pgmId, "ModelView");
			modelLocation = GL.GetUniformLocation(pgmId, "Model");
			normalLocation = GL.GetUniformLocation(pgmId, "Normal");
			colorLocation = GL.GetUniformLocation (pgmId, "color");

		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"), 0);
		}

		public virtual void Enable(){
			GL.UseProgram (pgmId);

			if (activeSubRoutine != null)
				GL.UniformSubroutines(activeSubRoutine.ShaderStage, 1, ref activeSubRoutine.Id);

			GL.UniformMatrix4(projectionLocation, false, ref projectionMat);
			GL.UniformMatrix4 (modelLocation, false, ref modelMat); 
			GL.UniformMatrix4 (modelViewLocation, false, ref modelViewMat);
			updateNormalMatrix ();
			GL.Uniform4 (colorLocation, color);


		}
		public virtual void Disable(){
			GL.UseProgram (0);
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
		#endregion

		public void RegisterSubRoutine(ShaderType shaderType, string subName)
		{
			GL.UseProgram (pgmId);
			if (SubRoutines == null)
				SubRoutines = new List<SubRoutine> ();

			int tmp = GL.GetSubroutineIndex (pgmId, shaderType, subName);
			if (tmp < 0) {
				Debug.WriteLine ("Subroutine registration failed: {0} not found.", subName);
				return;
			}
			GL.UseProgram (0);
			SubRoutines.Add (new SubRoutine(subName, tmp, shaderType));
		}

		public void SelectSubroutine(string subName)
		{
			SubRoutine tmp = SubRoutines.Where (s => s.Name == subName).FirstOrDefault();
			if (tmp == null) {
				Debug.WriteLine ("Subroutine {0} not found.", subName);
				return;
			}
			activeSubRoutine = tmp;
		}

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
		public virtual void Dispose ()
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

