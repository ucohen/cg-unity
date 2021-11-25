Shader "CG/BlinnPhong"
{
    Properties
    {
        _DiffuseColor ("Diffuse Color", Color) = (0.14, 0.43, 0.84, 1)
        _SpecularColor ("Specular Color", Color) = (0.7, 0.7, 0.7, 1)
        _AmbientColor ("Ambient Color", Color) = (0.05, 0.13, 0.25, 1)
        _Shininess ("Shininess", Range(0.1, 50)) = 10
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

                // From UnityCG
                uniform fixed4 _LightColor0; 

                // Declare used properties
                uniform fixed4 _DiffuseColor;
                uniform fixed4 _SpecularColor;
                uniform fixed4 _AmbientColor;
                uniform float _Shininess;

                struct appdata
                { 
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    fixed4 color : COLOR0;
                };

                v2f vert (appdata input)
                {
                    v2f output;
                    output.pos = UnityObjectToClipPos(input.vertex);

                    float3 n = UnityObjectToWorldNormal(input.normal);      // vertex normal in world space
                    float3 l = normalize(_WorldSpaceLightPos0.xyz);         // light pos in world space
                    float3 v = normalize(_WorldSpaceCameraPos);             // viewpoint in world space
                    float3 h = normalize((l+v)/2);

                    fixed4 color_ambient = _AmbientColor * _LightColor0;
                    fixed4 color_diffuse = _DiffuseColor * _LightColor0 * max(0, dot(l,n));
                    fixed4 color_specular= _SpecularColor* _LightColor0 * pow(max(0, dot(n,h)), _Shininess);
                    
                    output.color = color_ambient + color_diffuse + color_specular;
                    return output;
                }

                fixed4 frag (v2f input) : SV_Target
                {
                    return input.color;
                }

            ENDCG
        }
    }
}
