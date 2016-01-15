Shader "Custom/DisplayVolume" 
{	
	Properties
	{
		_MipLevel ("Mip Level", Int) = 0			
		_VolumeTex ("Volume Texture", 3D) = "" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 5.0
	
	int _MipLevel;
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

	void frag_surf_opaque(v2f input, out float4 color : COLOR0, out float depth : SV_Depth) //out float depth : SV_DepthLessEqual)
	{
		color = tex3Dlod(_VolumeTex, float4(input.worldPos, 0));
		
		depth = 0;			
	}

	ENDCG
	
	Subshader 
	{
		ZWrite On
		Cull Back 	

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