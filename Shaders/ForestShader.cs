using System;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace GameLib
{
	public class ForestShader : IDisposable
	{
		#region CTOR
		public ForestShader ()
		{
			Compile ();
		}
		#endregion

		#region Sources
		protected string _vertSource = @"
			#version 330

			precision highp float;

			uniform mat4 Projection;
			uniform mat4 ModelView;	
			uniform float SpriteSize;					

			in vec3 in_position;

			out float A;						

			void main(void)
			{
				vec4 eyePos = ModelView * vec4(in_position, 1);
				vec4 projVoxel = Projection * vec4(SpriteSize,SpriteSize,eyePos.z,eyePos.w);
    			vec2 projSize = projVoxel.xy / projVoxel.w;
				
				gl_PointSize = (projSize.x+projSize.y);

				mat4 normalMatrix = transpose(inverse(ModelView));
				vec3 N = normalize(vec3(normalMatrix * vec4(1.0,0.0,0.0,0.0)));
				vec3 E = normalize(vec3(eyePos));
				//vec3 N = vec3(E.x,E.y,0.0);
				//vec3 N = vec3(0.0,0.0,-1.0);
				A =max(dot(N,E),0.0);

				gl_Position = Projection * eyePos;
			}";

		protected string _fragSource = @"
			#version 330
			precision highp float;

			uniform vec4 color;
			uniform sampler2D tex;
			
			in float A;
			out vec4 out_frag_color;

			void main(void)
			{
				float t = gl_PointCoord.t/A;
				if (t>1.0)
					discard;
				vec4 c = texture( tex, vec2(gl_PointCoord.s,t) );
				if (c.a < 0.01)
					discard;                          

				out_frag_color = c;
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
		protected int vsId, fsId, gsId, pgmId, savedPgmId = 0,
						ModelViewLocation,
						ModelLocation,
						ProjectionLocation,
						NormalLocation,
						SpriteSizeLocation,
						colorLocation;

		Matrix4 Projection, 				
				ModelView;
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
				Projection = value;
				GL.UniformMatrix4(ProjectionLocation, false, ref Projection);  
			}
		}
		public Matrix4 ModelViewMatrix {
			set { 
				ModelView = value;
				GL.UniformMatrix4 (ModelViewLocation, false, ref ModelView); 
			}
		}
		public Vector4 Color {
			set {GL.Uniform4 (colorLocation, value);}
		}
		public float SpriteSize {
			set {GL.Uniform1 (SpriteSizeLocation, value);}
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
		}
		protected virtual void GetUniformLocations()
		{
			ProjectionLocation = GL.GetUniformLocation(pgmId, "Projection");
			ModelViewLocation = GL.GetUniformLocation(pgmId, "ModelView");
			colorLocation = GL.GetUniformLocation (pgmId, "color");
			SpriteSizeLocation = GL.GetUniformLocation (pgmId, "SpriteSize");
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
		public static void Enable(ForestShader s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(ForestShader s)
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

