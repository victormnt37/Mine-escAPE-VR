Shader "Custom/CaveCeilingCutout"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _RoughnessMap("Roughness Map", 2D) = "white" {}

        _BumpScale("Normal Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Parallax Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0

        [Header(Cutout Settings)]
        _CutoutShape("Cutout Shape (0=Cylinder, 1=Square)", Float) = 0
        _CutoutWidth("Cutout Width (Half-Width)", Float) = 2.5
        _CutoutLength("Cutout Length (Half-Length)", Float) = 3.5
        _CutoutForwardOffset("Forward Shift", Float) = 0.0
        _CutoutHeightMin("Min Height Above Ground", Float) = 0.8
        _CutoutCurvature("Curvature (-0.5 Left, +0.5 Right)", Range(-0.5, 0.5)) = 0.0
        _CutoutPitch("Pitch Angle Y", Range(-45.0, 45.0)) = 0.0
        _CutoutCenterFallback("Fallback Cart Pos", Vector) = (2.502, -0.611, 2.791, 1.0)
        _CutoutForwardFallback("Fallback Cart Forward", Vector) = (0.0, 0.0, 1.0, 0.0)
        _CutoutUpFallback("Fallback Cart Up", Vector) = (0.0, 1.0, 0.0, 1.0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _Cutoff;
            half _Smoothness;
            half _Metallic;
            half _BumpScale;
            half _Parallax;
            half _OcclusionStrength;
            half _EnvironmentReflections;
            half _SpecularHighlights;
            float _CutoutShape;
            float _CutoutWidth;
            float _CutoutLength;
            float _CutoutForwardOffset;
            float _CutoutHeightMin;
            float _CutoutCurvature;
            float _CutoutPitch;
            float4 _CutoutCenterFallback;
            float4 _CutoutForwardFallback;
            float4 _CutoutUpFallback;
        CBUFFER_END

        float4 _CaveCutoutPos;
        float4 _CaveCutoutForward;
        float4 _CaveCutoutUp;
        float4 _CaveCutoutParams;
        float4 _CaveCutoutOffset;
        float _CaveCutoutCurvature;
        float _CaveCutoutPitch;

        void ApplyCaveCutout(float3 posWS)
        {
            float3 center = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutPos.xyz : _CutoutCenterFallback.xyz;
            float hMin = (_CaveCutoutPos.w > 0.5 && _CaveCutoutParams.w > 0.0) ? _CaveCutoutParams.w : _CutoutHeightMin;

            float3 up = (_CaveCutoutUp.w > 0.5) ? _CaveCutoutUp.xyz : _CutoutUpFallback.xyz;
            if (dot(up, up) < 0.0001) up = float3(0, 1, 0);
            up = normalize(up);

            float3 fwd = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutForward.xyz : _CutoutForwardFallback.xyz;
            fwd = fwd - up * dot(fwd, up);
            if (dot(fwd, fwd) < 0.0001) fwd = float3(0, 0, 1);
            fwd = normalize(fwd);

            float3 right = cross(up, fwd);

            float fwdShift = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutOffset.x : _CutoutForwardOffset;
            float lateralShift = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutOffset.y : 0.0;

            float3 shiftCenter = center + fwd * fwdShift + right * lateralShift;
            float3 delta = posWS - shiftCenter;

            float signedDistRight = dot(delta, right);
            float signedDistForward = dot(delta, fwd);
            float heightAboveFloor = dot(posWS - center, up);

            // --- APLICACIÓN DE INCLINACIÓN (PITCH EN Y) ---
            float pitch = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutPitch : _CutoutPitch;
            if (abs(pitch) > 0.01)
            {
                float rad = radians(pitch);
                float cosP = cos(rad);
                float sinP = sin(rad);
                
                float newFwd = signedDistForward * cosP - heightAboveFloor * sinP;
                float newUp  = signedDistForward * sinP + heightAboveFloor * cosP;
                
                signedDistForward = newFwd;
                heightAboveFloor = newUp;
            }

            // Comprobación de altura mínima tras aplicar el pitch
            if (heightAboveFloor > hMin)
            {
                // --- APLICACIÓN DE CURVATURA EN Z ---
                float curve = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutCurvature : _CutoutCurvature;
                if (abs(curve) > 0.0001)
                {
                    signedDistRight -= curve * (signedDistForward * signedDistForward);
                }

                float distRight = abs(signedDistRight);
                float distForward = abs(signedDistForward);

                float halfWidth = (_CaveCutoutPos.w > 0.5 && _CaveCutoutParams.y > 0.05) ? _CaveCutoutParams.y : max(0.1, _CutoutWidth);
                float halfLength = (_CaveCutoutPos.w > 0.5 && _CaveCutoutParams.z > 0.05) ? _CaveCutoutParams.z : max(0.1, _CutoutLength);
                float shape = (_CaveCutoutPos.w > 0.5) ? _CaveCutoutParams.x : _CutoutShape;

                float normDist;
                if (shape > 0.5)
                {
                    normDist = max(distRight / halfWidth, distForward / halfLength);
                }
                else
                {
                    normDist = sqrt((distRight / halfWidth) * (distRight / halfWidth) + 
                                    (distForward / halfLength) * (distForward / halfLength));
                }

                clip(normDist - 1.0);
            }
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _ROUGHNESSMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_RoughnessMap);       SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
            TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 tangentWS    : TEXCOORD2;
                float2 uv           : TEXCOORD3;
                float fogFactor     : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                ApplyCaveCutout(input.positionWS);

                // Construcción y normalización del espacio tangente para Parallax y Normal Mapping
                half3 normalWS_geom = NormalizeNormalPerPixel(input.normalWS);
                half3 tangentWS_geom = normalize(input.tangentWS.xyz);
                half3 bitangentWS = input.tangentWS.w * cross(normalWS_geom, tangentWS_geom);
                half3x3 tangentToWorld = half3x3(tangentWS_geom, bitangentWS, normalWS_geom);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 viewDirTS = mul(tangentToWorld, viewDirWS);

                float2 uv = input.uv;
                #if defined(_PARALLAXMAP)
                    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // 3. Normal Mapping con Normal Scale (_BumpScale)
                #if defined(_NORMALMAP)
                    half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                    half3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
                    normalWS = normalize(normalWS);
                #else
                    half3 normalTS = half3(0, 0, 1);
                    half3 normalWS = normalWS_geom;
                #endif

                // Doble cara: normal invertida para caras traseras para que el techo exterior se ilumine
                normalWS = IS_FRONT_VFACE(isFrontFace, normalWS, -normalWS);

                // 4. Metallic & Smoothness / Roughness
                half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
                half metallic = metallicGloss.r * _Metallic;

                half smoothness = _Smoothness;
                #if defined(_ROUGHNESSMAP)
                    half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                    smoothness = saturate((1.0 - roughness) * _Smoothness);
                #elif defined(_METALLICSPECGLOSSMAP)
                    smoothness = metallicGloss.a * _Smoothness;
                #endif

                half occlusion = 1.0;
                #if defined(_OCCLUSIONMAP)
                    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                    occlusion = LerpWhiteTo(occ, _OcclusionStrength);
                #endif

                half4 emission = half4(0, 0, 0, 0);
                #if defined(_EMISSION)
                    emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv) * _EmissionColor;
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission.rgb;
                surfaceData.occlusion = occlusion;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct AttributesShadow
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsShadow
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 _LightDirection;

            VaryingsShadow VertShadow(AttributesShadow input)
            {
                VaryingsShadow output = (VaryingsShadow)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 FragShadow(VaryingsShadow input) : SV_Target
            {
                ApplyCaveCutout(input.positionWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma multi_compile_instancing

            struct AttributesDepth
            {
                float4 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VaryingsDepth VertDepth(AttributesDepth input)
            {
                VaryingsDepth output = (VaryingsDepth)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(input.positionOS.xyz);

                return output;
            }

            half4 FragDepth(VaryingsDepth input) : SV_Target
            {
                ApplyCaveCutout(input.positionWS);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}