Shader "Custom/RaytraceSDF" 
{	
	Properties
	{
		_MaxSteps ("Max Steps", Int) = 100
		_MinStepSize ("Min Step size", Float) = 0.01
		_SurfaceColor ("Color", Color) = (1,0,0,1)
		_IntensityThreshold ("Intensity Threshold", Range (-1, 1)) = 0
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 5.0
	
	int _MaxSteps;
	float4 _SurfaceColor;
	float _IntensityThreshold; 
	float _MinStepSize;

	struct v2f
	{
		float4 pos : SV_POSITION;		
		float3 viewRay : C0LOR0;				
		float3 worldPos : C0LOR1;		
		float3 localPos : C0LOR2;				
	};
	
	v2f vert(appdata_base v)  
	{
		v2f output;
		output.pos = mul (UNITY_MATRIX_MVP, v.vertex);		
		output.localPos =  v.vertex;
		output.worldPos =  mul (_Object2World, v.vertex);
		output.viewRay = mul (_World2Object, output.worldPos - _WorldSpaceCameraPos);	
        return output;
    }

	float get_depth( float3 current_pos )
	{
		float4 pos = mul (UNITY_MATRIX_MVP, float4(current_pos - 0.5, 1));
		return (pos.z / pos.w) ;
	}

	float calculate(float3 pos){
		//Do some simple sphere
		//float result = length(pos) - 0.5;

		//Or torus :)
		float2 q = float2(length(pos.xz)-0.3,pos.y);
		float result = length(q)-0.2;

		//Prism
		//float3 q = abs(pos);
		//float result = max(q.z-0.4,max(q.x*0.866025+pos.y*0.5,-pos.y)-0.10);

		return -_IntensityThreshold + result;

	
	}

	float3 get_normal(float3 position)
	{
		//TODO
		float e = 0.0001;
		float dx = calculate(float3(position.x+e, position.y, position.z)) - calculate(float3(position.x-e, position.y, position.z)); 
		float dy = calculate(float3(position.x, position.y+e, position.z)) - calculate(float3(position.x, position.y-e, position.z));			
		float dz = calculate(float3(position.x, position.y, position.z+e)) - calculate(float3(position.x, position.y, position.z-e));			

		return -float3(dx,dy,dz)/2/e;
	}	

	void frag_surf_opaque(v2f input, out float4 color : COLOR0, out float depth : SV_Depth) //out float depth : SV_DepthLessEqual)
	{
		depth = 0;

		// Get back position from vertex shader
		float3 back_pos = input.localPos;	
		float3 view_dir = normalize(input.viewRay);
		float3 view_dir_inv = -view_dir;

		// Find the front pos
		float3 t = max((0.5-back_pos)/view_dir_inv, (-0.5-back_pos)/view_dir_inv);				
		float3 front_pos = back_pos + (min(t.x, min(t.y, t.z)) * view_dir_inv);
		

		// Add noise
		float rand = frac(sin(dot(input.pos.xy ,float2(12.9898,78.233))) * 43758.5453);
		front_pos += view_dir * rand * 0.001;			
							
		float3 last_pos;
		float3 current_pos = front_pos;

				
		float length_acc = 0;
		float current_intensity = 0.0;
		float max_length = length(front_pos - back_pos);
		
		float delta_dir_length = 0.1;
		float3 delta_dir = view_dir * delta_dir_length;	
		// Linear search
		for( uint i = 0; i < _MaxSteps ; i++ )
		{
			current_intensity = calculate(current_pos);
			delta_dir_length = max(current_intensity, _MinStepSize);
			delta_dir = view_dir * delta_dir_length;	
			if( current_intensity <= 0 ) break;	
			if( length_acc += delta_dir_length >= max_length ) break;	

			last_pos = current_pos;
			current_pos += delta_dir;
		}	

		if(current_intensity > 0) discard;
		
		float3 normal = get_normal(current_pos);
		float ndotl = max(0.0, dot(view_dir, normal));

		color = float4(_SurfaceColor.rgb * pow(ndotl, 0.8), 1); 
		depth = 0;			
	}

	ENDCG
	
	Subshader 
	{
		ZWrite On
		Cull Front 	

		Pass 
		{
			CGPROGRAM
			#pragma vertex vert		
			#pragma fragment frag_surf_opaque		
			ENDCG
		}				
	}

	Fallback off
} 