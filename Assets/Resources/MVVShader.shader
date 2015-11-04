Shader "Custom/MVVShader" 
{	
	Properties
	{

	}

	CGINCLUDE

	#include "UnityCG.cginc"
	#pragma target 5.0

	const static int MAX_SDFS = 32;
	const static int MAX_REGIONS = 32;
	const static int MAX_SDFSEEDS = 16384;
	const static int MAX_INDICES = 32;
	const static int MAX_EMBEDDED = 16384;
	const static int MAX_INDICESPARTS = 8192;
	
	sampler3D _VolumeAtlas; // Atlas of all SDFs...
	int3 _VolumeAtlasSize; // Size of Atlas for calculation...
	int _rootSDF; // ID of First sdf
	// SDF stuff
	int3 _sdfIndices[MAX_SDFS]; // Indices of sdfs in atlas
	int3 _sdfDimensions[MAX_SDFS]; // Dimensions of sdfs in atlas
	int _sdfPositiveId[MAX_SDFS]; // Stores region/sdf id of positive branch
	int _sdfPositiveRegion[MAX_SDFS]; // True if this branch is a region
	int _sdfNegativeId[MAX_SDFS]; // Stores region/sdf id of negative branch
	int _sdfNegativeRegion[MAX_SDFS]; // True if this branch is a region
	float4x4 _sdfTransforms[MAX_SDFS]; // Stores Transform of SDF (inverse)
	int _sdfType[MAX_SDFS]; // Type of sdf: 0: default, 1: seed, 2: tiling
	float4x4 _sdfSeedTransforms[MAX_SDFSEEDS]; // Transform of seeds (inverse)
	int _sdfSeedTransformIndices[MAX_SDFS]; // Starting index of seedTransforms
	int _sdfSeedTransformLenght[MAX_SDFS]; // Length of indices
	float _sdfOffset[MAX_SDFS]; // iso-surface offset
	// Region stuff
	int _regionType[MAX_REGIONS]; // Region type: 0: empty, 1: bitmap, 2: color, 3: rbf
	sampler2D _regionTextures[MAX_REGIONS]; // Textures of array
	float3 _regionColor[MAX_REGIONS]; // Color of region
	float _regionOpacity[MAX_REGIONS]; // Opacity of region
	float3 _regionScales[MAX_REGIONS]; // For Bitmap transform
	int _regionHasEmbedd[MAX_REGIONS]; // Does region have embedds
	//There are two kinds of embedds: with and without index
	int _regionIndices[MAX_REGIONS]; // IDs of Index for Embedded Objects (-1 no index)
	int _regionEmbeddedIndices[MAX_REGIONS]; // Where embedded objects start (-1 no element in index)
	int _regionEmbeddedLength[MAX_REGIONS]; // how many objects are there in this part?
	// Indices
	float4x4 _indexTransform[MAX_INDICES]; // Transform of index (inverse)
	int3 _indexSizes[MAX_INDICES]; // Size in each dimenstion
	int _indexOffset[MAX_INDICES]; // Offset in indexEmbeddedIndices/Length
	int _embeddedSDFs[MAX_EMBEDDED]; // index of sdfs that are embedded
	float4x4 _embeddedTransforms[MAX_EMBEDDED]; // transform for embedded objects (inverse)
	//float4x4 _embeddedBoundingBox[MAX_EMBEDDED]; // boundingbox for embedded objects (inverse)
	//The following will also include direct embedds in region without index...
	int _indexEmbeddedIndices[MAX_INDICESPARTS]; // Where embedded objects start (-1 no element in index)
	int _indexEmbeddedLength[MAX_INDICESPARTS]; // how many objects are there in this part?
	

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

	// Sample volume at position p, with index and size of actual texture
	float sample_volume( float3 p, int3 index, int3 size )
	{	

		float3 new_p = index / float3(_VolumeAtlasSize)+(size / float3(_VolumeAtlasSize))*p;
		return tex3Dlod(_VolumeAtlas, float4(new_p, 0)).a;	
	}

	// Transforms a point with given matrix (must be already inverse)
	float3 transform(float3 p, float4x4 mat)
	{
		return (mul(mat, float4(p, 1))).xyz;
	}

	// Sample volume at psoition p for sdf with Id sdfId
	float sample_volume(float3 p, int sdfId) {

		return sample_volume(p, _sdfIndices[sdfId], _sdfDimensions[sdfId]);

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

	// Check if point is in standard cube (-1,-1,-1),(1,1,1)
	bool isCoordValid(float3 p) {
		return (p.x <= 1 && p.x >= -1 && 
				p.y <= 1 && p.y >= -1 && 
				p.y <= 1 && p.z >= -1);
	}

	// returns color for point and region
	float4 get_color(int regionId, float3 p) {
		// Region type: 0: empty, 1: bitmap, 2: color, 3: rbf
		if (_regionType[regionId] == 0) {
			// empty:
			return float4(-1, -1, -1, -1);
		}
		else if (_regionType[regionId] == 1) {
			// bitmap:
			//how to get correct coordinates?
			return float4(1, 0, 0, _regionOpacity[regionId]);
		}
		else if (_regionType[regionId] == 2) {
			// color
			return float4(_regionColor[regionId], _regionOpacity[regionId]);
		}
		else {
			return float4(0, 0, 0, 1);
		}
	}

	void frag_surf_opaque(v2f input, out float4 color : COLOR0, out float depth : SV_Depth)
	{
		
		color = float4(normalize(input.worldPos.xyz),1.0);
		
		// Tranform with first SDF
		//float3 p = transform(input.worldPos, _sdfTransforms[_rootSDF]);
		// We are now in the root SDF
		bool fin = false;
		int current_sdf = _rootSDF;
		int current_value = 0;
		float3 p = input.worldPos;
		float3 oldP = p;
		int in_embedded = 0; // Counts the embedded level
		int current_in_embedded = -1; //Current embedded level we are in...
		int current_embedded = 0;
		int current_embedded_index = 0; // Gives index of current embedded objects
		int current_embedded_length = 0; // Gives length of current embedded objects
		int node = 0; // ID for sdf/region
		bool is_leaf = false; // true if we have a leaf...
		float3 indexP = p;
		int index_index; //Ahhh... yeah... the linear index of the embeddedobjects start/length of the index z+y*size_z+x*size_z*size_y
		
		for (int i = 0; i < 512; i++)
		{
			// Using algorithm from paper
			if (in_embedded == current_in_embedded && isCoordValid(p) == false) {
				// p not inside current embedded instance, try next...
				current_embedded++; // Increment embedded index
				if (current_embedded >= current_embedded_index + current_embedded_length) {
					// No more embedded objects here -> return region color
					break;
				}
				current_sdf = _embeddedSDFs[current_embedded];
				p = transform(oldP, _embeddedTransforms[current_sdf]);
				continue;
			}
			current_in_embedded--; // We don't want to look for embedded objects, because we are already in a correct one...
			if (sample_volume(p, current_sdf) > 0) {
				node = _sdfPositiveId[current_sdf];
				is_leaf = _sdfPositiveRegion[current_sdf] > 0;
			}
			else {
				node = _sdfNegativeId[current_sdf];
				is_leaf = _sdfNegativeRegion[current_sdf] > 0;
			}

			if (is_leaf) {
				if (_regionHasEmbedd[node] == 0) {
					//normal region
					if (_regionType[node] == 0) {
						// this is empty, when we are in an embedded object, go to next one:
						current_embedded++; // Increment embedded index
						if (current_embedded >= current_embedded_index + current_embedded_length) {
							// No more embedded objects here -> return region color
							break;
						}
						current_sdf = _embeddedSDFs[current_embedded];
						p = transform(oldP, _embeddedTransforms[current_sdf]);
					} else {
						color = get_color(node, p);
						break;
					}
				}
				else {
					// Embedded stuff
					color = get_color(node, p); // If all embedded are empty, not reached, use that color
					//First check where we are in index

					current_embedded_index = _regionIndices[node];

					if (current_embedded_index < 0) {
						// No index, just embedds
						current_embedded_index = _regionEmbeddedIndices[node];
						if (current_embedded_index < 0) return;

						// Okay loop through embedded objects
						current_embedded_length = _regionEmbeddedLength[node];
						current_embedded = current_embedded_index;
						oldP = p;
						current_sdf = _indexEmbeddedIndices[current_embedded];
						p = transform(p, _embeddedTransforms[current_sdf]);
						continue;
					}


					indexP = transform(p, _indexTransform[current_embedded_index]);

					if (indexP.x < -1 || indexP.x > 1 ||
						indexP.y < -1 || indexP.y > 1 ||
						indexP.z < -1 || indexP.z > 1) {
						// No embedded will be in index, just quit now
						return;
					}

					// indexP is now in (-1,-1,-1)x(1,1,1) of index, time to check correct ccordinate
					// get in range 0..0.999
					indexP = smoothstep(float3(-1, -1, -1), float3(1.0001, 1.0001, 1.0001),indexP);
					index_index = (int)(_indexSizes[current_embedded_index].x*indexP.x) * _indexSizes[current_embedded_index].z * _indexSizes[current_embedded_index].y +
								  (int)(_indexSizes[current_embedded_index].y*indexP.y) * _indexSizes[current_embedded_index].z +
								  (int)(_indexSizes[current_embedded_index].z*indexP.z);

					current_embedded_index = _indexEmbeddedIndices[_indexOffset[current_embedded_index] + index_index];
					current_embedded_length = _indexEmbeddedLength[_indexOffset[current_embedded_index] + index_index];
					current_embedded = current_embedded_index;

					oldP = p;
					current_sdf = _indexEmbeddedIndices[current_embedded];
					p = transform(p, _embeddedTransforms[current_sdf]);

					in_embedded++;
					current_in_embedded = in_embedded;
				}
			}
			else {
				// no leaf... prepare for new sdf
				current_sdf = node; 
			}

			// Ok we should have some color by now, else
			if (color.x < 0) discard;

			//// Check sdf
			//// Check sdf type
			//p = transform(p, _sdfTransforms[current_sdf]);
			//if (_sdfType[current_sdf] == 0) {
			//	// Simple SDF
			//	current_value = sample_volume(p, _sdfIndices[current_sdf], _sdfDimensions[current_sdf]);
			//}
			//else if (_sdfType[current_sdf] == 1) {
			//	// Seeding
			//	for (int i = 0; i < _sdfSeedTransfomrLenght[current_sdf]; i++)
			//	{
			//		current_value = min(current_value,

			//			sample_volume(
			//				transform(
			//					p, _sdfSeedTransforms[_sdfSeedTransformIndices[current_sdf] + i]),
			//				_sdfIndices[current_sdf],
			//				_sdfDimensions[current_sdf]));
			//	}
			//}
			//else discard; // Tiling not supported yet...
			//
			//// Decide what to do...
			//if (current_value >= 0) {
			//	// Do positive stuff

			//}
			//else {
			//	// Do negative stuff
			//}
		}
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