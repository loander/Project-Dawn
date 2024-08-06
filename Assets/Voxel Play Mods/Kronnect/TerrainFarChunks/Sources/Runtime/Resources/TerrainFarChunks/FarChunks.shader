Shader "Unlit/FarChunks"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TerrainFarChunksTex ("Terrain Info", 2D) = "black" {}
        _SnapshotData ("_SnapshotData", Vector) = (0,0,0)
        _WaterColor ("_WaterColor", Color) = (0,0,0)
        _WaterLevel ("_WaterLevel", Float) = 0
        _TerrainMaxAltitude ("_TerrainMaxAltitude", Float) = 100
        _ShadowIntensity ("_ShadowIntensity", Float) = 0.2
        
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _SHADOWS

            #include "UnityCG.cginc"

            #define RAY_STEPS 200
            #define BSEARCH_STEPS 8
            #define RAY_SHADOW_STEPS 100

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
            };

            sampler2D _TerrainFarChunksTex;
            float4 _SnapshotData;
            half4 _WaterColor;
            float _WaterLevel;
            float _TerrainMaxAltitude;
            half _ShadowIntensity;
            half _VPDaylightShadowAtten, _VPAmbientLight;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // places the quad on the far clip
                #if defined(UNITY_REVERSED_Z)
                    o.pos.z = 1.0e-9f;
                #else
                    o.pos.z = o.pos.w - 1.0e-6f;
                #endif

                o.uv = v.uv;
                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.rayDir = wpos - _WorldSpaceCameraPos;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the terrain texture
                float3 rayDir = normalize(i.rayDir);
                half4 color = half4(0,0,0,0);

                float3 wpos;
                float dither = frac(dot(float2(2.4084507, 3.2535211), i.pos.xy));
                float incr = 1.015;
                float t = 16 + dither;
                for (int k=0;k<RAY_STEPS;k++) {
                    wpos = _WorldSpaceCameraPos + rayDir * t;
                    wpos = floor(wpos) + 0.5;
                    float2 tpos = (wpos.xz - _SnapshotData.xy) / _SnapshotData.z;
                    float4 terrain = tex2Dlod(_TerrainFarChunksTex, float4(tpos, 0, 0));
                    float terrainAltitude = terrain.a * _TerrainMaxAltitude;
                    if (wpos.y < terrainAltitude) {
                        color = half4(terrain.rgb, 1.0);
                        break;
                    }
                    t ++;
                    t *= incr;
                }
                if (color.a == 0) return 0;

                // refine hit position using binary search                
                float t1 = t;
                float t0 = (t / incr) - 1;
                float3 hpos = wpos;
                half ao = 1.0;
                for (int i=0;i<BSEARCH_STEPS;i++) {
                    t = (t1 + t0) * 0.5;
                    wpos = _WorldSpaceCameraPos + rayDir * t;
                    hpos = wpos;
                    wpos = floor(wpos) + 0.5;
                    float2 tpos = (wpos.xz - _SnapshotData.xy) / _SnapshotData.z;
                    float4 terrain = tex2Dlod(_TerrainFarChunksTex, float4(tpos, 0, 0));
                    float terrainAltitude = terrain.a * _TerrainMaxAltitude;
                    if (wpos.y < terrainAltitude) {
                        t1 = t;
                        color = half4(terrain.rgb, 1.0);
                        ao = min(1.0, 0.25 + frac(hpos.y) * 0.75 + t / 512);
                    } else {
                        t0 = t;
                    }
                }

                // compute if pixel is under shadow by casting a ray from pixel to the Sun
                half atten = 1.0;
                #if _SHADOWS
                    float3 rpos = hpos;
                    float v = 2; // Shadow ray starting distance
                    for (int j = 0; j < RAY_SHADOW_STEPS; j++) {
                        rpos = hpos + _WorldSpaceLightPos0 * v;
                        if (rpos.y > _TerrainMaxAltitude) {
                            break; // Above terrain max altitude so in direct light
                        }
                        float2 tpos = (rpos.xz - _SnapshotData.xy) / _SnapshotData.z;
                        float4 terrain = tex2Dlod(_TerrainFarChunksTex, float4(tpos, 0, 0));
                        float terrainAltitude = terrain.a * _TerrainMaxAltitude;
                        if (rpos.y < terrainAltitude) {
                            atten = _ShadowIntensity;
                            break;
                        }
                        v ++;
                        v *= incr;
                    }
                #endif
              
                // compute normal
                float3 dc = abs(hpos - wpos);
                dc.y *= 1.05; // avoid artifacts at the edges
                float3 norm = float3(0, -rayDir.y, 0);
                if (dc.z > dc.x && dc.z > dc.y) norm = float3(0, 0, -sign(rayDir.z));
                if (dc.x > dc.z && dc.x > dc.y) norm = float3(-sign(rayDir.x), 0, 0);

                // add water
                if (hpos.y < _WaterLevel) {
                   color.rgb = lerp(color.rgb, _WaterColor.rgb, _WaterColor.a);
                   norm = float3(0, -rayDir.y, 0);
                }

                // apply shadow attenuation
                atten = saturate( saturate(atten + _WorldSpaceLightPos0.y * _VPDaylightShadowAtten) + _VPAmbientLight);
                color.rgb *= min((atten * ao), 1.2);

                // diffuse lambertian wrap
                norm = normalize(norm);
                half ndt = saturate(dot(norm, _WorldSpaceLightPos0) * 0.5 + 0.5);
                color.rgb *= lerp(ndt, 1.0, t / 512);

                // day/night cycle
                color.rgb *= saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);

                // add fog
                half fogFactor = 512/t;
                color.a *= saturate(fogFactor * fogFactor);

                return color;
                

            }
            ENDCG
        }
    }
}
