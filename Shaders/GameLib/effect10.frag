//electric arc
//by c64cosmin@gmail.com

#version 330
precision highp float;

in vec2 texCoord;
out vec4 out_frag_color;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

float lineDist(vec2 a,vec2 b,vec2 point){
	vec2 line=b-a;
	vec2 d=point-a;
	vec2 p=dot(normalize(line),d)*normalize(line);
	float percent=length(p)/length(line)*sign(dot(p,line));
	vec2 h=d-p;
	float r=percent;
	if(percent>=0.0&&percent<1.0)r=length(h);
	if(percent>1.0)r=length(point-b);
	if(percent<0.0)r=length(point-a);
	return r;//distance(point,h+a);
}
float thunder(vec2 a,vec2 b,vec2 point){
	float r=100.0;
	const int n=10;
	const int m=3;
	float random=fract(sin(time));
	for(int i=0;i<n;i++){
		vec2 aa=a;
		vec2 bb=b;
		float h=float(n)/2.0;
		float middle=float(n)/2.0;
		for(int it=0;it<m;it++){
			float radius=0.2*length(aa-bb);
			float angle=time*10.0*(.4+float(it)*0.7)*(0.7+float(it))+random*0.3*cos(time);
			vec2 mid=aa*0.5+bb*0.5+vec2(radius*cos(angle),radius*sin(angle));
			h/=2.0;
			if(float(i)<middle){
				bb=mid;
				middle-=h;
			}else{
				aa=mid;
				middle+=h;
			}
		}
		float dist=lineDist(aa,bb,point);
		if(dist<r)r=dist;
	}
	return r;
}

void main( void ) {
	vec2 position = texCoord.xy;
	vec2 a=vec2(0.,0.5);
	vec2 b=vec2(1.0,0.5);
	float d=thunder(a,b,position);
	float color = 0.1/d;
	out_frag_color = vec4( color*0.3,color*0.3,color, 1.0 );

}
