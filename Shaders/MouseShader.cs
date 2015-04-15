using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GGL
{
    class MouseShader : Shader
    {
        public static float MouseX = 0f;
        public static float MouseY = 0f;
        public MouseShader()
            : base()
        {
            vertSource = @"
                

                void main() {
                    
                    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;                    
                }
                ";

            Compile();
        }
    }
}
