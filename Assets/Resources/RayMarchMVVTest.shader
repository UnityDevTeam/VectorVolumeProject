Shader "Custom/RayMarchMVVTest" 
{	
	Properties
	{
		_MipLevel ("Mip Level", Int) = 0
		_NumSteps ("Num Steps", Int) = 32	
		_VolumeSize ("Volume Size", Int) = 0				
		_VolumeTex ("Volume Texture", 3D) = "" {}
		_SurfaceColor ("Color", Color) = (1,0,0,1)
		_IntensityThreshold ("Intensity Threshold", Range (-1, 1)) = 0	
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 5.0
	
	int _VolumeSize;	

	int _MipLevel;
	float _NumSteps;
	float _IntensityThreshold;
	float4 _SurfaceColor;
	sampler3D _VolumeTex;

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

	bool isCoordValid(float3 p) {
		return (p.x <= 1 && p.x >= -1 &&
			p.y <= 1 && p.y >= -1 &&
			p.y <= 1 && p.z >= -1);
	}

	float sample_volume_interpolate(float3 p) {

		float v11 = tex3Dlod(_VolumeTex, float4(p + float3(0.01, 0, 0), 0)).a;
		float v12 = tex3Dlod(_VolumeTex, float4(p - float3(0.01, 0, 0), 0)).a;
		float v21 = tex3Dlod(_VolumeTex, float4(p + float3(0, 0.01, 0), 0)).a;
		float v22 = tex3Dlod(_VolumeTex, float4(p - float3(0, 0.01, 0), 0)).a;
		float v31 = tex3Dlod(_VolumeTex, float4(p + float3(0, 0, 0.01), 0)).a;
		float v32 = tex3Dlod(_VolumeTex, float4(p - float3(0, 0, 0.01), 0)).a;

		return (v11+v12+v21+v22+v31+v32)/6.0;

	}

	float sample_volume( float3 p )
	{	
		// Our texture is 64x128x96, map into corner of 256x128x128
		float3 new_point = clamp(p, float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0));
		new_point.x = new_point.x /2;// / 4.0;
		new_point.y = new_point.y/3;
		new_point.z = new_point.z/4;// *3.0 / 4.0;
		return //sample_volume_interpolate(new_point);
			   tex3Dlod(_VolumeTex, float4(new_point, _MipLevel)).a;	
	}

	float get_depth( float3 current_pos )
	{
		float4 pos = mul (UNITY_MATRIX_MVP, float4(current_pos - 0.5, 1));
		return (pos.z / pos.w) ;
	}

	float3 get_normal(float3 position, float dataStep)
	{
		float dx = sample_volume(position + float3(dataStep, 0, 0)) - sample_volume(position+float3(-dataStep, 0, 0)); 
		float dy = sample_volume(position + float3(0, dataStep, 0)) - sample_volume(position+float3(0, -dataStep, 0));			
		float dz = sample_volume(position + float3(0, 0, dataStep)) - sample_volume(position+float3(0, 0, -dataStep));		

		return normalize(float3(dx,dy,dz));
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
		
		// Offset to texture coordinates
		back_pos += 0.5;
		front_pos += 0.5;

		// Add noise
		float rand = frac(sin(dot(input.pos.xy ,float2(12.9898,78.233))) * 43758.5453);
		front_pos += view_dir * rand * 0.001;			
							
		float3 last_pos;
		float3 current_pos = front_pos;

		float delta_dir_length = 1 / _NumSteps;
		float3 delta_dir = view_dir * delta_dir_length;	
				
		float length_acc = 0;
		float current_intensity = 0.0;
		float max_length = length(front_pos - back_pos);
		
		// Linear search
		for( uint i = 0; i < _NumSteps ; i++ )
		{
			current_intensity = sample_volume(current_pos);
			if( current_intensity <= _IntensityThreshold ) break;	
			if( length_acc += delta_dir_length >= max_length ) break;	

			last_pos = current_pos;
			current_pos += delta_dir;
		}	

		if(current_intensity > _IntensityThreshold) discard;
		
		float texelSize = 1.0f / _VolumeSize;
		float3 normal = get_normal(current_pos, texelSize*2); // *2 to blur it a bit
		float ndotl = max(0.0, dot(-view_dir, normal));

		color = float4(_SurfaceColor.rgb * pow(ndotl, 0.5), 1); 
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