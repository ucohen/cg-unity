Shader "CG/Water"
{
    Properties
    {
        _CubeMap("Reflection Cube Map", Cube) = "" {}
        _NoiseScale("Texture Scale", Range(1, 100)) = 10 
        _TimeScale("Time Scale", Range(0.1, 5)) = 3 
        _BumpScale("Bump Scale", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "CGUtils.cginc"
                #include "CGRandom.cginc"

                #define DELTA 0.01

                // Declare used properties
                uniform samplerCUBE _CubeMap;
                uniform float _NoiseScale;
                uniform float _TimeScale;
                uniform float _BumpScale;

                struct appdata
                { 
                    float4 vertex   : POSITION;
                    float3 normal   : NORMAL;
                    float4 tangent  : TANGENT;
                    float2 uv       : TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos      : SV_POSITION;
                    float3 normal   : NORMAL;
                    float4 tangent  : TANGENT;
                    float4 worldpos : TEXCOORD1;
                    float2 uv       : TEXCOORD0;
                };

                // Returns the value of a noise function simulating water, at coordinates uv and time t
                float waterNoise(float2 uv, float t)
                {
                    // static perlin noise
                    // float r = perlin2d(uv);     // sample perlin noise [-1,1]
                    
                    // dynamic perlin noise
                    float r = perlin3d(float3(0.5*uv,0.5*t)) + 0.5*perlin3d(float3(uv,t)) + 0.2*perlin3d(float3(2*uv,3*t));
                    return r;  // [-1,1]
                }

                // Returns the world-space bump-mapped normal for the given bumpMapData and time t
                float3 getWaterBumpMappedNormal(bumpMapData i, float t)
                {
                    // n_tangentspace = normalize(float3(-s*F_u, -s*F_v, 1))
                    float F_prime_u = (waterNoise(i.uv + float2(i.du, 0), t) - waterNoise(i.uv, t)) / i.du;
                    float F_prime_v = (waterNoise(i.uv + float2(0, i.dv), t) - waterNoise(i.uv, t)) / i.dv;

                    float3 n_tangentspace = normalize(float3(-i.bumpScale * F_prime_u, -i.bumpScale * F_prime_v, 1));
                    float3 binormal = normalize(cross(i.tangent, i.normal));

                    // n_worldspace   = t*n_tangentspace.x + n*n_tangentspace.y + b*n_tangentspace.z
                    float3 n_worldspace = i.tangent * n_tangentspace.x +
                                        i.normal  * n_tangentspace.z +
                                        binormal  * n_tangentspace.y;
                    return normalize(n_worldspace);
                }

                v2f vert (appdata input)
                {
                    v2f output;

                    // displacement of vertices using perlin noise
                    float disp_point = waterNoise(input.uv * _NoiseScale, _Time.y*_TimeScale) * 0.5 + 0.5;           // [0,1]
                    // input.vertex += disp_point * _BumpScale;  //float4(0,disp_point * _BumpScale,0,0);

                    output.pos = UnityObjectToClipPos(input.vertex + float4(0,disp_point * _BumpScale,0,0)); //disp_point * _BumpScale);
                    output.uv = input.uv;
                    output.normal = input.normal;
                    output.tangent = input.tangent;
                    output.worldpos = mul(unity_ObjectToWorld, input.vertex);
                    return output;
                }

                fixed4 frag (v2f input) : SV_Target
                {
                    // float c = waterNoise(input.uv * _NoiseScale, _Time.y);  // (t/20,t,2t,3t)

                    float3 v = normalize(_WorldSpaceCameraPos - input.worldpos.xyz);    // camera direction
                    float3 n = normalize(mul(unity_ObjectToWorld, input.normal));       // normal in world space

                    bumpMapData i;
                    i.normal   = n;                         // worldspace
                    i.tangent  = normalize(input.tangent);  // surface tangent
                    i.uv       = input.uv * _NoiseScale;    //
                    i.du       = DELTA;                     //
                    i.dv       = DELTA;                     //
                    i.bumpScale= _BumpScale;                // scaled
                    
                    float3 bumpedNormal = getWaterBumpMappedNormal(i,  _Time.y * _TimeScale);

                    float3 r = 2 * dot(v, bumpedNormal) * bumpedNormal - v;             // reflection direction
                    // float3 l = normalize(_WorldSpaceLightPos0.xyz);                  // light direction in world space
                    fixed4 ReflectedColor = texCUBE(_CubeMap, r);
                    fixed4 Color = (1-max(0, dot(bumpedNormal,v))+0.2) * ReflectedColor;
                    return Color;
                }

            ENDCG
        }
    }
}
