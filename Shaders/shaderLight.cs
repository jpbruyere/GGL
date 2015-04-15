using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GGL
{
    class shaderLight : Shader
    {
        public shaderLight(Vector4[] lightsDir, Vector3 vEye) : base()
        {
            fragSource = @"
                #define MAX_LIGHTS 8
                #define NUM_LIGHTS 1
                varying vec3 normal;
                uniform vec4 lightDir[MAX_LIGHTS];
                uniform vec3 eyeVec;

                void main (void)
                {
                  vec4 final_color = gl_FrontLightModelProduct.sceneColor;
                  vec3 N = normalize(normal);
                  int i;
                  for (i=0; i<NUM_LIGHTS; ++i)
                  {  
                    vec3 L = normalize(lightDir[i]);
                    float lambertTerm = dot(N,L);
                    if (lambertTerm > 0.0)
                    {
                      final_color += gl_LightSource[i].diffuse * gl_FrontMaterial.diffuse * lambertTerm;	
                      vec3 E = normalize(eyeVec);
                      vec3 R = reflect(-L, N);
                      float specular = pow( max(dot(R, E), 0.0), gl_FrontMaterial.shininess );
                      final_color += gl_LightSource[i].specular * gl_FrontMaterial.specular * specular;	
                    }
                  }
                  gl_FragColor = final_color;
                } 
            ";
            vertSource = @"
                #define MAX_LIGHTS 8
                #define NUM_LIGHTS 1

                varying vec3 normal;
                uniform vec4 lightDir[MAX_LIGHTS];
                uniform vec3 eyeVec;

                void main()
                {	
                  gl_Position = ftransform();		
                  normal = gl_NormalMatrix * gl_Normal;
                  vec4 vVertex = gl_ModelViewMatrix * gl_Vertex;
                  eyeVec = -vVertex.xyz;
                  int i;
                  for (i=0; i<NUM_LIGHTS; ++i)
                    lightDir[i] = vec3(gl_LightSource[i].position.xyz - vVertex.xyz);
                }
                ";

//            GL.BindAttribLocation(Shader.mainProgram, 0, "texcoord");
//            if (GL.GetError() != ErrorCode.NoError)
//                throw new Exception();
//            GL.BindAttribLocation(Shader.mainProgram, 1, "normal");
//            GL.BindAttribLocation(Shader.mainProgram, 2, "position");
//            
//            GL.Uniform3(GL.GetUniformLocation(Shader.mainProgram, "eyeVec"), vEye);
//            GL.Uniform4(GL.GetUniformLocation(Shader.mainProgram, "lightDir[0]"),lightsDir[0]);

            Compile();
        }
    }
}
