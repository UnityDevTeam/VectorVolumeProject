Shader "Custom/MVVShader" 
{	
	Properties
	{

	}

	CGINCLUDE

	#include "UnityCG.cginc"
	#pragma target 5.0

	struct Node
	{
		int positiveId;
		int negativeId;
		int isLeaf;
		int sdfId;
		int regionId;
	};

	struct SDF
	{
		float3 index;
		float3 size;
		float4x4 transform;
		float4x4 aabb;
		int type;
		int3 index_size;
		float4x4 index_transform;
		int index_offset;
	};

	struct Region
	{
		int type;
		float3 color;
		float opacity;
		// TODO textures
		// public Vector3 scale;
		float3 index;
		float3 size;
		float4x4 bitmap_transform;
		// For now only support one index
		// if no index is used, define 1x1x1 index
        int embedded;
		int3 index_size;
		float4x4 index_transform;
		int index_offset;
	};

	struct Instance
	{
		float4x4 transform;
		int rootnode;
	};

	struct Indexcell
	{
		int instance; // -1 if no embedded objects in this index cell
		int max_instance;
	};

	uniform sampler3D _VolumeAtlas; // Atlas of all SDFs...
	uniform int3 _VolumeAtlasSize; // Size of Atlas for calculation...
	uniform int _rootInstance; // ID of First instance
	
	uniform sampler3D _BitmapAtlas;

	uniform StructuredBuffer<Node> nodeBuffer;
	uniform StructuredBuffer<SDF> sdfBuffer;
	uniform StructuredBuffer<Region> regionBuffer;
	uniform StructuredBuffer<Instance> instanceBuffer;
	uniform StructuredBuffer<Indexcell> indexcellBuffer;

	uniform sampler2D _cubicLookup; // Lookup for fast cubic interpolation

	float4x4 textureTrans = float4x4(float4(0.5,0,0,0.5), float4(0,0.5,0,0.5), float4(0,0,0.5,0.5), float4(0,0,0,1));

	float4 colorr;

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


	// Transforms a point with given matrix (must be already inverse)
	float3 transform(float3 p, float4x4 mat)
	{
		return (mul(mat, float4(p, 1))).xyz;
	}

	// Code adopted from Lvid Wang's Shader
	float sample_volume_cubic(float3 p) {
		return tex3Dlod(_VolumeAtlas, float4(p, 0)).a;
		float3 vCoordHG = p*_VolumeAtlasSize - 0.5f.xxx;
		//vCoordHG = p;// -0.5f.xxx;
		float3 hgX = tex2Dlod(_cubicLookup, float4(vCoordHG.x, 0, 0, 0)).xyz;
		float3 hgY = tex2Dlod(_cubicLookup, float4(vCoordHG.y, 0, 0, 0)).xyz;
		float3 hgZ = tex2Dlod(_cubicLookup, float4(vCoordHG.z, 0, 0, 0)).xyz;


		//float3 cellSizeX = 1 / (float)_VolumeAtlasSize.xxx;
		//float3 cellSizeY = 1 / (float)_VolumeAtlasSize.xxx;
		//float3 cellSizeZ = 1 / (float)_VolumeAtlasSize.xxx;
		float3 cellSizeX = float3(1 / (float)_VolumeAtlasSize.x, 0, 0);
		float3 cellSizeY = float3(0, 1 / (float)_VolumeAtlasSize.x, 0);
		float3 cellSizeZ = float3(0, 0, 1 / (float)_VolumeAtlasSize.x);

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

		float value000 = tex3Dlod(_VolumeAtlas, float4(vCoord000, 0)).a;
		float value100 = tex3Dlod(_VolumeAtlas, float4(vCoord100, 0)).a;
		float value010 = tex3Dlod(_VolumeAtlas, float4(vCoord010, 0)).a;
		float value110 = tex3Dlod(_VolumeAtlas, float4(vCoord110, 0)).a;
		float value001 = tex3Dlod(_VolumeAtlas, float4(vCoord001, 0)).a;
		float value101 = tex3Dlod(_VolumeAtlas, float4(vCoord101, 0)).a;
		float value011 = tex3Dlod(_VolumeAtlas, float4(vCoord011, 0)).a;
		float value111 = tex3Dlod(_VolumeAtlas, float4(vCoord111, 0)).a;

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
	}
	
	// Check if point is in standard cube (-1,-1,-1),(1,1,1)
	bool isCoordValid(float3 p) {
		return (p.x <= 1 && p.x >= -1 &&
			p.y <= 1 && p.y >= -1 &&
			p.z <= 1 && p.z >= -1);
	}

	// Sample volume at position p, with index and size of actual texture
	float sample_volume( float3 p, float3 index, float3 size)
	{	
	
		
		//size = int3(size.y, size.z, size.x);
		//index = int3(index.y, index.z, index.x);
		// point is between -1..1 in every direction
		// put between 0..1
		p = p/2.0f+0.5f.xxx;
		//float3 new_p = index / float3(_VolumeAtlasSize)+(size / float3(_VolumeAtlasSize))*p;
		float3 new_p = index + size*p;
		return sample_volume_cubic(new_p);
	}



	// Sample volume at psoition p for sdf with Id sdfId
	float sample_volume(float3 p, SDF sdf) {
		if (sdf.type == 0) {
			p = transform(p, sdf.transform);
			p = transform(p, sdf.aabb);
			// If p not in standard cube, return high value...
			if (isCoordValid(p) == false) {
				return 1;
			}
			return sample_volume(p, sdf.index, sdf.size);
		}
		//seed
		if (sdf.type == 1) {
			/*float mini = 1.0f;
			for (int i = sdf.first_transform; i < sdf.first_transform + sdf.max_transform; i++)
			{
				float3 newP = transform(p, transformBuffer[i]);
				newP = transform(newP, sdf.aabb);
				
				// Check range
				if (isCoordValid(newP))
				{
					//return 0;
					//return sample_volume(newP, sdf.index, sdf.size);
					mini = min(sample_volume(newP, sdf.index, sdf.size), mini);
				}
			}
			return mini;*/
			//Indexing
			//First check where we are in index
			float3 indexP = transform(p, sdf.index_transform);

			if (!isCoordValid(indexP)) {
				// No embedded will be in index, just return 1
				return 1;
			}
			
			// indexP is now in (-1,-1,-1)x(1,1,1) of index, time to check correct ccordinate
			// get in range 0..0.999
			indexP = indexP/2 + 0.5 - 0.001;
			int linear_index = (int)(sdf.index_size.x*indexP.x) * sdf.index_size.z * sdf.index_size.y +
						   (int)(sdf.index_size.y*indexP.y) * sdf.index_size.z +
						   (int)(sdf.index_size.z*indexP.z);


			int current_embedded_index = indexcellBuffer[sdf.index_offset + linear_index].instance;

			if (current_embedded_index < 0) {
				//No embedds in this index cell, just return 1
				return 1;
			}
			
			int current_embedded_length = indexcellBuffer[sdf.index_offset + linear_index].max_instance;
			
			float minValue = 1.0f;
			for (int i = current_embedded_index; i < current_embedded_index + current_embedded_length; i++){
				float3 newP = transform(p, instanceBuffer[i].transform);
				newP = transform(newP, sdf.aabb);
				
				// Check range
				if (isCoordValid(newP))
				{
					//return 0;
					//return sample_volume(newP, sdf.index, sdf.size);
					minValue = min(sample_volume(newP, sdf.index, sdf.size), minValue);
				}
			}
			return minValue;
			
		}
		//Tiling
		if (sdf.type == 2) {
			p = transform(p, sdf.transform);
			//Transform point to 0..1
			p = p/2.0f+0.5f.xxx;
			p = p - floor(p);
			//Transform back;
			p = p-0.5f;
			p = p*2.0f;
			p = transform(p, sdf.aabb);
			if (isCoordValid(p) == false) {
				return 1;
			}
			return sample_volume(p, sdf.index, sdf.size);
		}
		return 1;
	}

	// returns color for point and region
	float4 get_color(Region region, float3 p) {
		// Region type: 0: empty, 1: bitmap, 2: color, 3: rbf
		if (region.type == 0) {
			// empty:
			return float4(-1, -1, -1, -1);
		}
		else if (region.type == 1) {
			// bitmap:
			/*p = transform(p, region.bitmap_transform);
			p = p/2.0f+0.5f.xxx;
			p = p - floor(p);
			p = clamp(region.size.xxx*p, 0.001f.xxx, region.size.xxx-0.001f.xxx);
			float2 uv = region.index + float2(p.x, p.z + region.size*p.y);
			
			//p = float3(region.index,0) + float3(region.size,0)*p;*/
			p = transform(p, region.bitmap_transform);
			p = p/2.0f+0.5f.xxx;
			p = p - floor(p);
			//p = clamp(p, 0.01f.xxx, 0.99f.xxx);
			p = region.index + region.size*p;
			//return float4(p,1);
			return float4(tex3Dlod(_BitmapAtlas, float4(p, 0)).xyz,1);// region.opacity);
			//return float4(1, 0, 0, 1);
		}
		else if (region.type == 2) {
			// color
			return float4(region.color, 1);//, region.opacity);
		}
		else {
			return float4(0, 0, 0, 1);
		}
	}

	void frag_surf_opaque(v2f input, out float4 color : COLOR0) //, out float depth : SV_Depth
	{
		
		
		int in_embedded = 0; // Counts the embedded level
		int current_in_embedded = -1; //Current embedded level we are in...
		int current_embedded_index = 0; // Gives index of current embedded objects
		int current_embedded_length = 0; // Gives length of current embedded objects


		int i = _rootInstance;
		Instance inst = instanceBuffer[i];
		Node node = nodeBuffer[inst.rootnode];
		float3 p = input.worldPos;
		//p = float3(p.x,p.z,p.y);
		//p = float3(p.y, p.z, p.x);
		p = transform(p, inst.transform);
		float3 oldP = p;

		float3 indexP = p;
		int linear_index; //The linear index of the embeddedobjects start/length of the index z+y*size_z+x*size_z*size_y
		float4 cur_col;

		for (int b = 0; b < 512; b++) {
			if (isCoordValid(p) == false) {
				i++;
				if (i >= current_embedded_index + current_embedded_length) {
					// No more embedded objects here -> return region color
					break;
				}
				inst = instanceBuffer[i];
				node = nodeBuffer[inst.rootnode];
				p = transform(oldP, inst.transform);
				continue;
			}

			//current_in_embedded--; // We don't want to look for embedded objects, because we are already in a correct one...
			
			if (sample_volume(p, sdfBuffer[node.sdfId]) > 0.51) {
				node = nodeBuffer[node.positiveId];
			}
			else {
				node = nodeBuffer[node.negativeId];
			}

			if (node.isLeaf > 0) {
				// We are now in a region...
				Region r = regionBuffer[node.regionId];
				if (r.embedded == 0) {
					// No embedding
					cur_col = get_color(r, p);
					if (cur_col.x < 0) {
						// We are in an empty object, check for embedd
						i++;
						if (i >= current_embedded_index + current_embedded_length) {
							// No more embedded objects here -> return region color
							break;
						}
						inst = instanceBuffer[i];
						node = nodeBuffer[inst.rootnode];
						p = transform(oldP, inst.transform);
					}
					else {
						color = cur_col;
						break;
					}
				}
				else {
					// Embedded objects
					color = get_color(r, p); // If all embedded are empty, not reached, use that color
					//First check where we are in index
					indexP = transform(p, r.index_transform);

					if (!isCoordValid(indexP)) {
						// No embedded will be in index, just quit now
						break;
					}
					
					// indexP is now in (-1,-1,-1)x(1,1,1) of index, time to check correct ccordinate
					// get in range 0..0.999
					indexP = indexP/2 + 0.5 - 0.001;
					linear_index = (int)(r.index_size.x*indexP.x) * r.index_size.z * r.index_size.y +
					    		   (int)(r.index_size.y*indexP.y) * r.index_size.z +
								   (int)(r.index_size.z*indexP.z);


					current_embedded_index = indexcellBuffer[r.index_offset + linear_index].instance;

					
					if (current_embedded_index < 0) {
						//No embedds
						break;
					}

					current_embedded_length = indexcellBuffer[r.index_offset + linear_index].max_instance;
					i = current_embedded_index;

					oldP = p;
					inst = instanceBuffer[i];
					node = nodeBuffer[inst.rootnode];
					p = transform(p, inst.transform);

					//in_embedded++;
					//current_in_embedded = in_embedded;
				}
			}

		}

		// Ok we should have some color by now, else
		if (color.x < 0) color = float4(0,0,0,1);

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