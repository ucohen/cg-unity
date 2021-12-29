Shader "CG/Bonus"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                { 
                    float4 vertex   : POSITION;
                };

                struct v2f
                {
                    float4 pos      : SV_POSITION;
                };

                v2f vert (appdata input)
                {
                    v2f output;
                    output.pos = UnityObjectToClipPos(input.vertex);
                    return output;
                }

                fixed4 frag (v2f input) : SV_Target
                {                    
                    return 1;
                }

            ENDCG
        }
    }
}
