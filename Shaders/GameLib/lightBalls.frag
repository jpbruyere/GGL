#version 330
#ifdef GL_ES
precision mediump float;
#endif

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 resolution;

#define pi 3.141592
#define num_balls 7.

void main( void ) 
{	
	vec2 p=(gl_FragCoord.xy-.5*resolution)/min(resolution.x,resolution.y);
	vec3 c = vec3(0);
	
	for (float i=0.; i<=num_balls; i++) 
	{
		float a = pi/2. + pi * 2. * i / num_balls + time;
		
		vec2 pt = vec2(cos(a), tan(a)) * (0.1 + 0.2*sin(time*2.));
		
		float dist = length(p-pt);
		
		
		vec3 newval;
		
		
		if(i==1.){newval = 0.015 /dist * vec3(209./255.,0,0);}
		if(i==2.){newval = 0.015 /dist * vec3(255./255.,102./255.,34./255.);}
		if(i==3.){newval = 0.015 /dist * vec3(255./255.,218./255.,33./255.);}
		if(i==4.){newval = 0.015 /dist * vec3(51./255.,221./255.,0);}
		if(i==5.){newval = 0.015 /dist * vec3(17./255.,51./255.,204./255.);}
		if(i==6.){newval = 0.015 /dist * vec3(34./255.,0,102./255.);}
		if(i==7.){newval = 0.015 /dist * vec3(51./255.,0,68./255.);}

		
		c += newval;
	}
	
	for (float i=0.; i<=num_balls; i++) 
	{
		float a = pi/2. + pi * 2. * i / num_balls + time;
		
		vec2 pt = vec2(tan(a), sin(a)) * (0.1 + 0.2*sin(time*2.));
		
		float dist = length(p-pt);
		
		
		vec3 newval;
		
		
		if(i==1.){newval = 0.015 /dist * vec3(209./255.,0,0);}
		if(i==2.){newval = 0.015 /dist * vec3(255./255.,102./255.,34./255.);}
		if(i==3.){newval = 0.015 /dist * vec3(255./255.,218./255.,33./255.);}
		if(i==4.){newval = 0.015 /dist * vec3(51./255.,221./255.,0);}
		if(i==5.){newval = 0.015 /dist * vec3(17./255.,51./255.,204./255.);}
		if(i==6.){newval = 0.015 /dist * vec3(34./255.,0,102./255.);}
		if(i==7.){newval = 0.015 /dist * vec3(51./255.,0,68./255.);}

		
		c += newval;
	}
	
	
	out_frag_color = vec4(c,c.g*c.r);
}