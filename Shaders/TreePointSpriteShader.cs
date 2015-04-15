using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GGL
{
    class TreePointSpriteShader : Shader
    {
        public TreePointSpriteShader()
            : base()
        {
            fragSource = @"
                #version 330

                uniform sampler2D sprite_texture;

                in float angle;
               
 
                void main(void)
                {
                    const float sin_theta = sin(angle);
                    const float cos_theta = cos(angle);
                    const mat2 rotation_matrix = mat2(cos_theta, sin_theta,
                                                        -sin_theta, cos_theta);
                    const vec2 pt = gl_PointCoord;
                    pt.x += 20f;
                    gl_FragColor = texture2D(sprite_texture, pt);
                }
			";
            vertSource = @"
                

                void main() {
                    
                    vec4 outPos = gl_ModelViewProjectionMatrix * gl_Vertex;
                    outPos.x += 10000f;// gl_PointSize / 2f;
                    gl_Position = outPos;
                    gl_PointCoord = outPos;
                    gl_PointSize = gl_PointSize / gl_Position.w;
                    
                }
                ";

            Compile();
        }
    }
    
}
