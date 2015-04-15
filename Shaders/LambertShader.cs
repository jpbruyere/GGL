using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GGL
{
    class LambertShader : Shader
    {
        public LambertShader()
        {
            vertSource = @"
                varying vec3 N;
                varying vec3 v;

                void main(void)
                {

                   v = vec3(gl_ModelViewMatrix * gl_Vertex);       
                   N = normalize(gl_NormalMatrix * gl_Normal);

                   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

                }
            ";

            fragSource = @"
                varying vec3 N;
                varying vec3 v;

                void main(void)
                {
                   vec3 L = normalize(gl_LightSource[0].position.xyz - v);   
                   vec4 Idiff = gl_FrontLightProduct[0].diffuse * max(dot(N,L), 0.0);  
                   Idiff = clamp(Idiff, 0.0, 1.0); 

                   gl_FragColor = Idiff;
                }
            ";

            Init();
            Compile();
        }
    }
}
