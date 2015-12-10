Shader "Custom/RayMarchVolume" 
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

	uniform sampler2D _cubicLookup; // Lookup for fast cubic interpolation


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
		return (p.x <= 1 && p.x >= 0 &&
			p.y <= 1 && p.y >= 0 &&
			p.y <= 1 && p.z >= 0);
	}


	float sample_volume_cubic(float3 p) {
		float3 vCoordHG = (p*_VolumeSize - 0.5f.xxx);
		float3 hgX = tex2Dlod(_cubicLookup, float4(vCoordHG.x, 0.5, 0, 0)).xyz;
		float3 hgY = tex2Dlod(_cubicLookup, float4(vCoordHG.y, 0.5, 0, 0)).xyz;
		float3 hgZ = tex2Dlod(_cubicLookup, float4(vCoordHG.z, 0.5, 0, 0)).xyz;

		
		float3 cellSizeX = 1 / (float)_VolumeSize.xxx;
		float3 cellSizeY = 1 / (float)_VolumeSize.xxx;
		float3 cellSizeZ = 1 / (float)_VolumeSize.xxx;

		// offset -DX and +DX
		float3 vCoord000 = p - hgX.x * cellSizeX;
		float3 vCoord100 = p + hgX.y * cellSizeX;
		// offset +DY
		float3 vCoord010 = vCoord000 + hgY.y * cellSizeY;
		float3 vCoord110 = vCoord100 + hgY.y * cellSizeY;
		// offset +DZ
		float3 vCoord011 = vCoord010 + hgZ.y * cellSizeZ;
		float3 vCoord111 = vCoord110 + hgZ.y * cellSizeZ;
		// offset -DZ
		vCoord010 = vCoord010 - hgZ.x * cellSizeZ;
		vCoord110 = vCoord110 - hgZ.x * cellSizeZ;
		// offset -DY
		vCoord000 = vCoord000 - hgY.x * cellSizeY;
		vCoord100 = vCoord100 - hgY.x * cellSizeY;
		float3 vCoord001 = vCoord000 + hgZ.y * cellSizeZ;
		float3 vCoord101 = vCoord100 + hgZ.y * cellSizeZ;
		vCoord000 = vCoord000 - hgZ.x * cellSizeZ;
		vCoord100 = vCoord100 - hgZ.x * cellSizeZ;

		float value000 = tex3Dlod(_VolumeTex, float4(vCoord000, 0)).a;
		float value100 = tex3Dlod(_VolumeTex, float4(vCoord100, 0)).a;
		float value010 = tex3Dlod(_VolumeTex, float4(vCoord010, 0)).a;
		float value110 = tex3Dlod(_VolumeTex, float4(vCoord110, 0)).a;
		float value001 = tex3Dlod(_VolumeTex, float4(vCoord001, 0)).a;
		float value101 = tex3Dlod(_VolumeTex, float4(vCoord101, 0)).a;
		float value011 = tex3Dlod(_VolumeTex, float4(vCoord011, 0)).a;
		float value111 = tex3Dlod(_VolumeTex, float4(vCoord111, 0)).a;

		// interpolate along x
		value000 = lerp(value100, value000, hgX.z);
		value010 = lerp(value110, value010, hgX.z);
		value001 = lerp(value101, value001, hgX.z);
		value011 = lerp(value111, value011, hgX.z);

		// interpolate along y
		value000 = lerp(value010, value000, hgY.z);
		value001 = lerp(value011, value001, hgY.z);

		// interpolate along z
		value000 = lerp(value001, value000, hgZ.z);

		return value000;
		//return tex3Dlod(_VolumeTex, float4(p, 0));
	}

	float sample_volume( float3 p )
	{
		if (isCoordValid(p) == false) return 1;
		return sample_volume_cubic(p);//tex3Dlod(_VolumeTex, float4(p, _MipLevel)).a;	
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