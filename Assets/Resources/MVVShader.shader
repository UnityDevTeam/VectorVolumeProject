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
		uint positiveId;
		uint negativeId;
		uint isLeaf;
		uint sdfId;
		uint regionId;
	};

	struct SDF
	{
		uint3 index;
		uint3 size;
		float4x4 transform;
		uint type;
		// TODO seeded SDFs
	};

	struct Region
	{
		uint type;
		float3 color;
		float opacity;
		// TODO textures
		// public Vector3 scale;

		// For now only support one index
		// if no index is used, define 1x1x1 index
        int embedded;
		uint3 index_size;
		float4x4 index_transform;
		uint index_offset;
	};

	struct Instance
	{
		float4x4 transform;
		uint rootnode;
	};

	struct Indexcell
	{
		int instance; // -1 if no embedded objects in this index cell
		int max_instance;
	};

	uniform sampler3D _VolumeAtlas; // Atlas of all SDFs...
	uniform int3 _VolumeAtlasSize; // Size of Atlas for calculation...
	uniform int _rootInstance; // ID of First instance

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
		float3 vCoordHG = p*_VolumeAtlasSize - 0.5f.xxx;
		float3 hgX = tex2Dlod(_cubicLookup, float4(vCoordHG.x, 0, 0, 0)).xyz;
		float3 hgY = tex2Dlod(_cubicLookup, float4(vCoordHG.y, 0, 0, 0)).xyz;
		float3 hgZ = tex2Dlod(_cubicLookup, float4(vCoordHG.z, 0, 0, 0)).xyz;


		float3 cellSizeX = 1 / (float)_VolumeAtlasSize;
		float3 cellSizeY = 1 / (float)_VolumeAtlasSize;
		float3 cellSizeZ = 1 / (float)_VolumeAtlasSize;

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


		return tex3Dlod(_VolumeAtlas, float4(p, 0)).a;
	}

	// Sample volume at position p, with index and size of actual texture
	float sample_volume( float3 p, int3 index, int3 size )
	{	
		index = int3(index.y, index.z, index.x);
		p = p*0.5 + 0.5.xxx;
		// point is between 0..1 in every direction
		//p = float3(p.z, p.x, p.y);
		//colorr = float4(p, 1);
		float3 new_p = index / float3(_VolumeAtlasSize)+(size / float3(_VolumeAtlasSize))*p;
		return sample_volume_cubic(p);
	}


	// Check if point is in standard cube (-1,-1,-1),(1,1,1)
	bool isCoordValid(float3 p) {
		return (p.x <= 1 && p.x >= -1 &&
			p.y <= 1 && p.y >= -1 &&
			p.z <= 1 && p.z >= -1);
	}

	// Sample volume at psoition p for sdf with Id sdfId
	float sample_volume(float3 p, SDF sdf) {
		p = transform(p, sdf.transform);
		// If p not in standard cube, return high value...
		if (isCoordValid(p) == false) {
			return 1;
		}
		// bring p in 0..1 space
		//p = transform(p, textureTrans);
		return sample_volume(p, sdf.index, sdf.size);

		//float current_value;

		//p = transform(p, _sdfTransforms[sdfId]);

		//if (_sdfType[sdfId] == 0) {
		//	// Simple SDF
		//	current_value = sample_volume(p, _sdfIndices[sdfId], _sdfDimensions[sdfId]);
		//}
		//else if (_sdfType[sdfId] == 1) {
		//	// Seeding
		//	for (int i = 0; i < _sdfSeedTransformLenght[sdfId]; i++)
		//	{
		//		current_value = min(current_value,
		//			sample_volume(
		//				transform(
		//					p, _sdfSeedTransforms[_sdfSeedTransformIndices[sdfId] + i]),
		//				_sdfIndices[sdfId],
		//				_sdfDimensions[sdfId]));
		//	}
		//}
		//else current_value = sample_volume(p, _sdfIndices[sdfId], _sdfDimensions[sdfId]); // Tiling not supported yet...

		//return current_value;
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
			// TODO
			return float4(1, 0, 0, region.opacity);
		}
		else if (region.type == 2) {
			// color
			return float4(region.color, region.opacity);
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
		float3 p = transform(input.worldPos, inst.transform);
		float3 oldP = p;

		float3 indexP = p;
		int linear_index; //The linear index of the embeddedobjects start/length of the index z+y*size_z+x*size_z*size_y


		for (int b = 0; b < 512; b++) {
			if (in_embedded == current_in_embedded && isCoordValid(p) == false) {
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

			current_in_embedded--; // We don't want to look for embedded objects, because we are already in a correct one...

			if (sample_volume(p, sdfBuffer[node.sdfId]) > 0) {
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
					color = get_color(r, p);
					if (color.x == -1) {
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
					else break;
				}
				else {
					// Embedded objects
					color = get_color(r, p); // If all embedded are empty, not reached, use that color
					//First check where we are in index
					indexP = transform(p, r.index_transform);

					if (indexP.x < -1 || indexP.x > 1 ||
						indexP.y < -1 || indexP.y > 1 ||
						indexP.z < -1 || indexP.z > 1) {
						// No embedded will be in index, just quit now
						break;
					}

					// indexP is now in (-1,-1,-1)x(1,1,1) of index, time to check correct ccordinate
					// get in range 0..0.999
					indexP = smoothstep(float3(-1, -1, -1), float3(1.0001, 1.0001, 1.0001),indexP);
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

					in_embedded++;
					current_in_embedded = in_embedded;
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