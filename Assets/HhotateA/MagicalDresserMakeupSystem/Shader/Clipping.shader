Shader "HhotateA/MagicalDresserMakeup/Clipping"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            void vert (appdata v)
            {
                return;
            }

            void frag (v2f i)
            {
                return;
            }
            ENDCG
        }
    }
}
