// Simplified SDF shader:
// - Lit with Emission
// - No Shading Option (bevel / bump / env map) - (Maintained from original)
// - No Glow Option - (Maintained from original, emission serves a similar purpose)
// - Softness is applied on both side of the outline - (Maintained from original)

Shader "TextMeshPro/Custom/LitEmission" {

Properties {
	[HDR]_FaceColor		("Face Color (Albedo)", Color) = (1,1,1,1) // Renamed for clarity
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

	[HDR]_OutlineColor	("Outline Color", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness", Range(0,1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

    [HDR]_EmissionColor ("Emission Color", Color) = (0,0,0,1)   // New Emission Property
    _EmissionStrength   ("Emission Strength", Float) = 0        // New Emission Property (0=Lit, 1=Unlit Emission, >1=Boosted Emission)

	[HDR]_UnderlayColor	("Border Color", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Border OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Border OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate", Range(-1,1)) = 0
	_UnderlaySoftness 	("Border Softness", Range(0,1)) = 0

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = .5

	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			("Font Atlas", 2D) = "white" {}
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5
	_ScaleX				("Scale X", float) = 1
	_ScaleY				("Scale Y", float) = 1
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0

	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_MaskSoftnessX		("Mask SoftnessX", float) = 0
	_MaskSoftnessY		("Mask SoftnessY", float) = 0

	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 1
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255

	_CullMode			("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15
}

SubShader {
	Tags
	{
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
        "LightMode"="ForwardBase" // Necessary for receiving lighting in Forward Rendering
	}

	Stencil
	{
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting On             // Enable lighting calculations
	Fog { Mode Off }
	ZTest [unity_GUIZTestMode]
	Blend One OneMinusSrcAlpha // Standard alpha blending (expects premultiplied alpha from fragment shader)
	ColorMask [_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex VertShader
		#pragma fragment PixShader
		#pragma shader_feature __ OUTLINE_ON
		#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP
        #pragma multi_compile_fwdbase // For ForwardBase pass (main directional light, ambient, lightmaps, SH)

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"
        #include "AutoLight.cginc"      // For lighting macros like LIGHTING_COORDS and TRANSFER_VERTEX_TO_FRAGMENT
        #include "Lighting.cginc"       // For _WorldSpaceLightPos0, _LightColor0, UNITY_LIGHTMODEL_AMBIENT
		#include "TMPro_Properties.cginc"

        // New Emission Properties
        uniform fixed4 _EmissionColor;
        uniform half _EmissionStrength;

		struct vertex_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			float4	vertex			: POSITION;
			float3	normal			: NORMAL;
			fixed4	color			: COLOR;        // Vertex color
			float2	texcoord0		: TEXCOORD0;    // UV for Font Atlas
			float2	texcoord1		: TEXCOORD1;    // Additional per-vertex data (e.g., boldness)
		};

		struct pixel_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
			float4	vertex			: SV_POSITION;
			fixed4	faceColor		: COLOR;        // Albedo for face (post-vertex color and _FaceColor modulation, pre-lighting)
			fixed4	outlineColor	: COLOR1;       // Albedo for outline (post-vertex color and _OutlineColor modulation, pre-lighting, potentially mixed with faceColor)
			float4	texcoord0		: TEXCOORD0;	// Texture UV (xy), Mask UV (zw)
			half4	param			: TEXCOORD1;	// SDF Scale(x), BiasIn(y), BiasOut(z), Bias(w)
			half4	mask			: TEXCOORD2;	// Position in clip space(xy), Softness(zw) for masking
            float3  worldNormal     : TEXCOORD5;    // World space normal for lighting
            float3  worldPos        : TEXCOORD6;    // World space position for lighting
            LIGHTING_COORDS(7,8)                    // Macro for lightmap UVs and/or shadow coordinates

			#if (UNDERLAY_ON | UNDERLAY_INNER)
			float4	texcoord1		: TEXCOORD3;	// Texcoord for underlay (xy), Original Vertex Alpha (z)
			half2	underlayParam	: TEXCOORD4;	// Underlay SDF Scale(x), Bias(y)
			#endif
		};


		pixel_t VertShader(vertex_t input)
		{
			pixel_t output;

			UNITY_INITIALIZE_OUTPUT(pixel_t, output);
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_TRANSFER_INSTANCE_ID(input, output);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // --- World space calculations for lighting ---
            output.worldNormal = UnityObjectToWorldNormal(input.normal);
            output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;

			float bold = step(input.texcoord1.y, 0);

			float4 vert = input.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;
			float4 vPosition = UnityObjectToClipPos(vert);

			float2 pixelSize = vPosition.w;
			pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

			float scale = rsqrt(dot(pixelSize, pixelSize));
			scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
			if(UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

			float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
			weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

			float layerScale = scale; // For Underlay

			scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
			float bias = (0.5 - weight) * scale - 0.5;
			float outlineWidthFactor = _OutlineWidth * _ScaleRatioA * 0.5; // Renamed for clarity

            // --- Color preparations (Albedos without premultiplied alpha) ---
            // Opacity combines vertex alpha and material alpha (_FaceColor.a or _OutlineColor.a)
			float generalOpacity = input.color.a; // Base opacity from vertex color

			fixed4 faceAlbedo = fixed4(input.color.rgb * _FaceColor.rgb, generalOpacity * _FaceColor.a);
			fixed4 outlineAlbedoProperty = fixed4(input.color.rgb * _OutlineColor.rgb, generalOpacity * _OutlineColor.a);

            // Mix outlineAlbedo with faceAlbedo based on outlineWidth (original TMP logic)
            // This 'mixedOutlineColor' is the albedo for the outline area, it will not be lit directly.
            // The 'outlineWidthFactor * scale' is related to the visual thickness of the outline.
			fixed4 mixedOutlineAlbedo = lerp(faceAlbedo, outlineAlbedoProperty, sqrt(min(1.0, (outlineWidthFactor * scale * 2.0))));

			output.faceColor = faceAlbedo;          // This will be lit in the fragment shader
			output.outlineColor = mixedOutlineAlbedo; // This will be used as is (unlit) for the outline part

			#if (UNDERLAY_ON | UNDERLAY_INNER)
			layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
			float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);
			float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
			float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
			float2 layerOffset = float2(x, y);
			#endif

			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

			output.vertex = vPosition;
			output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
			output.param = half4(scale, bias - (outlineWidthFactor * scale), bias + (outlineWidthFactor * scale), bias); // y = bias-outline, z = bias+outline
			output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));

			#if (UNDERLAY_ON || UNDERLAY_INNER)
			// Store original vertex alpha for potential use with underlay opacity modulation (though underlay has its own alpha)
			output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
			output.underlayParam = half2(layerScale, layerBias);
			#endif

            TRANSFER_VERTEX_TO_FRAGMENT(output); // Transfer lighting related data to fragment shader

			return output;
		}


		fixed4 PixShader(pixel_t input) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(input);

            // --- SDF Sample ---
			half sdfDist = tex2D(_MainTex, input.texcoord0.xy).a;
            half scaledSDF = sdfDist * input.param.x; // d in original shader

            // --- SDF Alphas ---
            // input.param.w = bias (center of face)
            // input.param.z = bias + outline (inner edge of outline, outer_edge of face)
            // input.param.y = bias - outline (outer edge of outline)
			half faceSDFAlpha = saturate(scaledSDF - input.param.w);

            fixed3 finalRgb;
            half finalAlpha;

            // --- Lighting Calculation (Basic Lambertian) ---
            float3 normalDir = normalize(input.worldNormal);
            float3 lightDir = normalize(UnityWorldSpaceLightDir(input.worldPos)); // Main directional light
            half NdotL = max(0.0, dot(normalDir, lightDir));
            fixed3 diffuseLight = _LightColor0.rgb * NdotL;
            fixed3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb;

            fixed3 litFaceComponent = input.faceColor.rgb * (diffuseLight + ambientLight);

            // --- Determine base color and alpha based on SDF (Face or Outline) ---
            fixed3 baseColorToShow; // This will be either lit face or unlit outline
            half combinedAlpha;     // Alpha considering SDF and material alphas

            #ifdef OUTLINE_ON
                half outlineInnerEdgeSDF = input.param.z; // Corresponds to "d - param.z" for lerp factor
                half outlineOuterEdgeSDF = input.param.y; // Corresponds to "d - param.y" for overall outline alpha mask
                
                // Factor to lerp between outline and face: 1 for face, 0 for outline.
                half faceFactor = saturate(scaledSDF - outlineInnerEdgeSDF); 

                baseColorToShow = lerp(input.outlineColor.rgb, litFaceComponent, faceFactor);
                combinedAlpha = lerp(input.outlineColor.a, input.faceColor.a, faceFactor);
                combinedAlpha *= saturate(scaledSDF - outlineOuterEdgeSDF); // Apply outer softness/edge of outline
            #else
                baseColorToShow = litFaceComponent;
                combinedAlpha = input.faceColor.a * faceSDFAlpha;
            #endif

            // --- Emission Logic ---
            // _EmissionStrength = 0: baseColorToShow (lit or outline)
            // _EmissionStrength = 1: _EmissionColor.rgb (unlit emission)
            // _EmissionStrength > 1: _EmissionColor.rgb * (_EmissionStrength value)
            if (_EmissionStrength <= 1.0h)
            {
                finalRgb = lerp(baseColorToShow, _EmissionColor.rgb, _EmissionStrength);
            }
            else
            {
                finalRgb = _EmissionColor.rgb * _EmissionStrength;
            }
            finalAlpha = combinedAlpha;


            // --- Final Color Structure ---
			fixed4 finalColor = fixed4(finalRgb, finalAlpha);


			// --- Underlay (Applied on top, does not affect/is not affected by lighting or emission of main text) ---
			#if UNDERLAY_ON
			half d_underlay = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
            // Premultiply underlay color by its own alpha
			fixed4 underlayColor = fixed4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a);
            // Add underlay, considering transparency of existing pixels
			finalColor += underlayColor * saturate(d_underlay - input.underlayParam.y) * (1.0h - finalColor.a);
			#endif

			#if UNDERLAY_INNER
            // Inner underlay is applied within the text shape (where sd is > 0)
			half sd_inner_mask = saturate(scaledSDF - input.param.z); // Mask for inside the outline's inner edge
			half d_inner_underlay = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
            fixed4 innerUnderlayColor = fixed4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a);
            // Apply inverse SDF for inner underlay, masked by sd_inner_mask and transparency of existing pixels
			finalColor += innerUnderlayColor * (1.0h - saturate(d_inner_underlay - input.underlayParam.y)) * sd_inner_mask * (1.0h - finalColor.a);
			#endif

            // --- Premultiply Alpha (Required for Blend One OneMinusSrcAlpha) ---
            finalColor.rgb *= finalColor.a;

			// --- Masking ---
			#if UNITY_UI_CLIP_RECT
			half2 clipMask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
			finalColor *= clipMask.x * clipMask.y;
			#endif

            // --- Alpha Clipping (Optional, from original shader) ---
			#if UNITY_UI_ALPHACLIP
			clip(finalColor.a - 0.001h);
			#endif

			return finalColor;
		}
		ENDCG
	}
}

CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}