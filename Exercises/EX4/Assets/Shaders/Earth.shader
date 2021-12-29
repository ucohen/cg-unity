Shader "CG/Earth"
{
    Properties
    {
        [NoScaleOffset] _AlbedoMap ("Albedo Map", 2D) = "defaulttexture" {}
        _Ambient ("Ambient", Range(0, 1)) = 0.15
        [NoScaleOffset] _SpecularMap ("Specular Map", 2D) = "defaulttexture" {}
        _Shininess ("Shininess", Range(0.1, 100)) = 50
        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "defaulttexture" {}
        _BumpScale ("Bump Scale", Range(1, 100)) = 30
        [NoScaleOffset] _CloudMap ("Cloud Map", 2D) = "black" {}
        _AtmosphereColor ("Atmosphere Color", Color) = (0.8, 0.85, 1, 1)
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
                uniform sampler2D _CloudMap;
                uniform fixed4 _AtmosphereColor;

                struct appdata
                { 
                    float4 vertex : POSITION;
                };

                struct v2f
                {
                    float4 pos      : SV_POSITION;
                    float4 objpos   : TEXCOORD0;        // to pass original vertex
                    float4 worldpos : TEXCOORD1;        // to pass vertex in world-coordinates

                };

                v2f vert (appdata input)
                {
                    v2f output;
                    output.pos = UnityObjectToClipPos(input.vertex);
                    output.objpos = input.vertex;
                    output.worldpos = mul(unity_ObjectToWorld, input.vertex);
                    return output;
                }

                fixed4 frag (v2f input) : SV_Target
                {
                    float2 uv = getSphericalUV(input.objpos);           // use object-space coord to select uv point
                    fixed4 albedo      = tex2D(_AlbedoMap,   uv);       // sample albado map
                    fixed4 specularity = tex2D(_SpecularMap, uv);       // sample specular map
                    fixed4 clouds      = tex2D(_CloudMap,    uv);       // sample specular map

                    float3 normal = normalize(input.objpos);
                    float3 n = normalize(mul(unity_ObjectToWorld, normal));             // normal in world space
                    float3 l = normalize(_WorldSpaceLightPos0.xyz);                     // light direction in world space
                    float3 v = normalize(_WorldSpaceCameraPos - input.worldpos.xyz);    // camera direction

                    /* Bumpmap */
                    bumpMapData bm;
                    bm.normal   = n;                                                // worldspace
                    bm.tangent  = normalize(cross(normal, float3(0,1,0)));          // surface tangent using cross of normal and up
                    bm.uv       = uv;
                    bm.heightMap= _HeightMap;
                    bm.du       = _HeightMap_TexelSize.x;
                    bm.dv       = _HeightMap_TexelSize.y;
                    bm.bumpScale= _BumpScale/10000;                                 // scaled down

                    // use specularity map where land has value 0 to enable bumpmapping in that region 
                    float3 finalNormal = (1-specularity) * getBumpMappedNormal(bm) + specularity*n;

                    /* Atmosphere */
                    float3 Lambert = max(0, dot(n,l));
                    float Atmosphere = (1-max(0,dot(n,v))) * sqrt(Lambert) * _AtmosphereColor;      // use non-bumped normal
                    float Clouds= clouds * (sqrt(Lambert) + _Ambient);
                    fixed4 color = fixed4(blinnPhong(finalNormal, v, l, _Shininess, albedo, specularity, _Ambient) + Atmosphere + Clouds, 1);

                    return color;
                }

            ENDCG
        }
    }
}
