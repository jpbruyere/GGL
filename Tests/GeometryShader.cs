﻿#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2009 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing details.
 */
#endregion

using System;
using System.Diagnostics;
using System.Drawing;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Examples.Tutorial {
	public class SimpleGeometryShader : GameWindow {
		public SimpleGeometryShader()
			: base(800, 600) {
		}

		int shaderProgram = 0;

		protected override void OnLoad(EventArgs e) {
//            if (!GL.GetString(StringName.Extensions).Contains("EXT_geometry_shader4"))
//            {
//                System.Windows.Forms.MessageBox.Show(
//                     "Your video card does not support EXT_geometry_shader4. Please update your drivers.",
//                     "EXT_geometry_shader4 not supported",
//                     System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
//                //Exit();
//            }

			// create a shader object.
			shaderProgram = GL.CreateProgram();
			// create shader objects for all three types.
			int vert = GL.CreateShader(ShaderType.VertexShader);
			int frag = GL.CreateShader(ShaderType.FragmentShader);
			int geom = GL.CreateShader(ShaderType.GeometryShaderExt);

			// GLSL for fragment shader.
			String fragSource = @"
				void main( void )
				{
					gl_FragColor = vec4(1, 1, 0, 1);
				}	
			";

			// GLSL for vertex shader.
			String vertSource = @"
				void main( void )
				{
					gl_Position = ftransform();
				}	
			";

			// GLSL for geometry shader.
			// Note this is a version 1.20 shader
			// Also note GL_EXT_geometry_shader4 must be enabled explicitly, correct
			// OpenGL implementations should only have the new tokens, like
			// EmitVertex and EndPrimitive, when this extension is enabled.
			String geomSource = @"
				#version 120 
				//#extension GL_EXT_geometry_shader4 : enable
				void main(void)
				{
					// variable to use in for loops
					int i;

					// Emit the original vertices without changing, making
					// this part exactly the same as if no geometry shader
					// was used.
					for(i=0; i< gl_VerticesIn; i++)
					{
						gl_Position = gl_PositionIn[i];
						EmitVertex();
					}
					// End the one primitive with the original vertices
					EndPrimitive();

					// Now we generate some more! This translates 0.2 over
					// the positive x axis.
					for(i=0; i< gl_VerticesIn; i++)
					{
						gl_Position = gl_PositionIn[i];
						gl_Position.x += 0.2;
						EmitVertex();
					}
					EndPrimitive();
				}
			";

			// compile shaders.
			compileShader(frag, fragSource);
			compileShader(vert, vertSource);
			compileShader(geom, geomSource);

			// attach shaders and link the program.
			GL.AttachShader(shaderProgram, frag);
			GL.AttachShader(shaderProgram, vert);
			//GL.AttachShader(shaderProgram, geom);
			GL.LinkProgram(shaderProgram);

			// output link info log.
			string info;
			GL.GetProgramInfoLog(shaderProgram, out info);
			Debug.WriteLine(info);

			// Set the input type of the primitives we are going to feed the geometry shader, this should be the same as
			// the primitive type given to GL.Begin. If the types do not match a GL error will occur (todo: verify GL_INVALID_ENUM, on glBegin)
			GL.Ext.ProgramParameter(shaderProgram, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Lines);
			// Set the output type of the geometry shader. Becasue we input Lines we will output LineStrip(s).
			GL.Ext.ProgramParameter(shaderProgram, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.LineStrip);

			// We must tell the shader program how much vertices the geometry shader will output (at most).
			// The simple way is to query the maximum and use that.
			int tmp;
			// Get the maximum amount of vertices into tmp.
			GL.GetInteger((GetPName)ExtGeometryShader4.MaxGeometryOutputVerticesExt, out tmp);
			// And feed amount that to the shader program. (0x0400 on a HD3850, with catalyst 9.8)
			GL.Ext.ProgramParameter(shaderProgram, ExtGeometryShader4.GeometryVerticesOutExt, tmp);

			// Set clearcolor and bind the shader program.
			GL.ClearColor(0.1f, 0.1f, 0.1f, 0.1f);
			GL.UseProgram(shaderProgram);
			// Set color to red. If the shader fails the fixed pipeline will be used and 
			// the lines will be red, if all is ok the fragment shader is used and they will be green.
			GL.Color3(1.0f, 0, 0);

			// Clean up resources. Note the program object is not deleted.
			if (frag != 0)
				GL.DeleteShader(frag);
			if (vert != 0)
				GL.DeleteShader(vert);
			if (geom != 0)
				GL.DeleteShader(geom);
		}

		/// <summary>
		/// Helper method to avoid code duplication.
		/// Compiles a shader and prints results using Debug.WriteLine.
		/// </summary>
		/// <param name="shader">A shader object, gotten from GL.CreateShader.</param>
		/// <param name="source">The GLSL source to compile.</param>
		void compileShader(int shader, string source) {
			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);

			string info;
			GL.GetShaderInfoLog(shader, out info);
			Debug.WriteLine(info);

			int compileResult;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);
			if (compileResult != 1) {
				Debug.WriteLine("Compile Error!");
				Debug.WriteLine(source);
			}
		}

        protected override void OnUnload(EventArgs e)
        {
            if (shaderProgram != 0)
                GL.DeleteProgram(shaderProgram);
            base.OnUnload(e);
        }

		/// <summary>
		/// Sets the viewport and projection matrix for orthographic projection.
		/// </summary>
		/// <param name="e">resize event args</param>
 		protected override void OnResize(EventArgs e) {
			GL.Viewport(0, 0, Width, Height);

			// Set projection matrix
			GL.MatrixMode(MatrixMode.Projection);
			OpenTK.Matrix4 ortho = OpenTK.Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, 1, -1);
			GL.LoadMatrix(ref ortho);

			// Set selector state back to matrix mode
			GL.MatrixMode(MatrixMode.Modelview);
			base.OnResize(e);

		}

		protected override void OnUpdateFrame(FrameEventArgs e) {
			base.OnUpdateFrame(e);

			if (Keyboard[Key.Space]) {
				ErrorCode err = GL.GetError();
				//Console.WriteLine(err + "  " + Glu.ErrorString((GluErrorCode)err));
				Console.WriteLine("GL error code: {0}", err);
			}

			if (Keyboard[Key.Escape])
				this.Exit();
		}

		protected override void OnRenderFrame(FrameEventArgs e) {
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// draw two vertical lines
			GL.Begin(BeginMode.Lines);
			{
				// line one
				GL.Vertex2(-0.5f, -0.5f);
				GL.Vertex2(-0.5f, 0.5f);
				// line two
				GL.Vertex2(0.5f, 0.5f);
				GL.Vertex2(0.5f, -0.5f);
			}
			GL.End();

			this.SwapBuffers();
		}

		#region public static void Main()

		/// <summary>
		/// Entry point of this example.
		/// </summary>
		[STAThread]
		public static void Main() {
			using (SimpleGeometryShader example = new SimpleGeometryShader()) {
				example.Run(30.0, 0.0);
			}
		}

		#endregion
	}
}