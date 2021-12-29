Shader "CG/Bricks"
{
    Properties
    {
        [NoScaleOffset] _AlbedoMap ("Albedo Map", 2D) = "defaulttexture" {}
        _Ambient ("Ambient", Range(0, 1)) = 0.15
        [NoScaleOffset] _SpecularMap ("Specular Map", 2D) = "defaulttexture" {}
        _Shininess ("Shininess", Range(0.1, 100)) = 100
        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "defaulttexture" {}
        _BumpScale ("Bump Scale", Range(-100, 100)) = 40
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "CGUtils.cginc"

                // Declare used properties
                uniform sampler2D _AlbedoMap;
                uniform float _Ambient;
                uniform sampler2D _SpecularMap;
                uniform float _Shininess;
                uniform sampler2D _HeightMap;
                uniform float4 _HeightMap_TexelSize;
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
                    float4 pos : SV_POSITION;
                    float2 uv  : TEXCOORD0;
                    float3 normal: NORMAL;
                    float4 tangent  : TANGENT;
                    float4 worldpos : TEXCOORD1;
                };

                v2f vert (appdata input)
                {
                    v2f output;
                    output.pos = UnityObjectToClipPos(input.vertex);  // pos to clip-space
                    output.worldpos = mul(unity_ObjectToWorld, input.vertex);
                    
                    output.uv = input.uv;   // pass uv to frag shader
                    output.normal = mul(unity_ObjectToWorld, input.normal);
                    output.tangent = mul(unity_ObjectToWorld, input.tangent);
                    return output;
                }

                fixed4 frag (v2f input) : SV_Target
                {
                    fixed4 albedo = tex2D(_AlbedoMap, input.uv);                        // sample albado map
                    fixed4 specularity = tex2D(_SpecularMap, input.uv);                 // sample specular map

                    float3 l = normalize(_WorldSpaceLightPos0.xyz);                     // light direction in world space
                    float3 v = normalize(_WorldSpaceCameraPos + input.worldpos.xyz);    // camera direction
                    // float3 v = normalize(_WorldSpaceCameraPos);                         // camera direction
                    float3 n = normalize(input.normal);                                 // normal in world space


                    // use bampmap to change normal
                    bumpMapData bm;
                    bm.normal   = n;
                    bm.tangent  = normalize(input.tangent);
                    bm.uv       = input.uv;
                    bm.heightMap= _HeightMap;
                    bm.du       = _HeightMap_TexelSize.x;
                    bm.dv       = _HeightMap_TexelSize.y;
                    bm.bumpScale= _BumpScale/10000;

                    float3 bumped_normal = getBumpMappedNormal(bm);
                    fixed4 color = fixed4(blinnPhong(bumped_normal, v, l, _Shininess, albedo, specularity, _Ambient), 1);
                    // fixed4 color = fixed4(blinnPhong(n, v, l, _Shininess, albedo, specularity, _Ambient), 1);
                    return color;
                }

            ENDCG
        }
    }
}
