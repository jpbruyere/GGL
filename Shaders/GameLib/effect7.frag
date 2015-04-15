#version 330
precision highp float;


#define pi2_inv 0.159154943091895335768883763372

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 resolution;

float border(vec2 uv, float thickness){
	uv = fract(uv - vec2(0.5));
	uv = min(uv, vec2(1.)-uv)*2.;
//	return 1.-length(uv-0.5)/thickness;
	return clamp(max(uv.x,uv.y)-1.+thickness,0.,1.)/thickness;;
}

vec2 spiralzoom(vec2 domain, vec2 center, float n, float spiral_factor, float zoom_factor, vec2 pos){
	vec2 uv = domain - center;
	float d = length(uv);
	return vec2( atan(uv.y, uv.x)*n*pi2_inv + log(d)*spiral_factor, -log(d)*zoom_factor) + pos;
}

void main( void ) {
	vec2 uv = texCoord.xy;
	uv = 0.5 + (uv - 0.5)*vec2(resolution.x/resolution.y,1.);
	
	uv = uv-0.5;

	vec2 spiral_uv = spiralzoom(uv,vec2(0.),8.,-.5,1.8,vec2(0.5,0.5)*time*0.5);
	vec2 spiral_uv2 = spiralzoom(uv,vec2(0.),3.,.9,1.2,vec2(-0.5,0.5)*time*.8);
	vec2 spiral_uv3 = spiralzoom(uv,vec2(0.),5.,.75,4.0,-vec2(0.5,0.5)*time*.7);

	out_frag_color = vec4(border(spiral_uv,0.9), border(spiral_uv2,0.9) ,border(spiral_uv3,0.9),1.);

	out_frag_color *= 1.;
}
