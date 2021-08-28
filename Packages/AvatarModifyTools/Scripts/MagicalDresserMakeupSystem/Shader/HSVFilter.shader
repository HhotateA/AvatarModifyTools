Shader "HhotateA/MagicalDresserMakeup/HSVFilter"
{
    Properties
    {
    	_Mask ("_Mask",2D) = "white" {}
    	_ForceColor("_ForceColor",color) = (1.0,0.0,0.0,0.0)
        _H ("_H",range(0,1)) = 0.5
        _S ("_S",range(0,1)) = 0.5
        _V ("_V",range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+150" }
        GrabPass { "_GrabPassTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            	float2 uv :TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            	float2 uv :TEXCOORD0;
                half4 grabPos : TEXCOORD1;
            };

            sampler2D _Mask;
            float4 _Mask_ST;
            fixed4 _ForceColor;
            sampler2D _GrabPassTexture;
            float _H,_S,_V;
            
		    inline float3 rgb2hsv(float3 c) {
			    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
			    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

			    float d = q.x - min(q.w, q.y);
			    float e = 1.0e-10;
			    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		    }
		    
		    inline fixed3 hsv2rgb(float3 hsv){
			    fixed4 t = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			    fixed3 p = abs(frac(hsv.xxx + t.xyz) * 6.0 - t.www);
			    return hsv.z * lerp(t.xxx, clamp(p - t.xxx, 0.0, 1.0), hsv.y);
		    }

            float getGray(float4 rgb)
            {
                return (rgb.x+rgb.y+rgb.z)*rgb.w*0.3333333333333333;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
		    	o.uv = TRANSFORM_TEX(v.uv,_Mask);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            	float4 mask = tex2D(_Mask,i.uv);
            	clip(getGray(mask)-0.5);
                float4 col = tex2Dproj(_GrabPassTexture, i.grabPos);
            	if(_S>0.74)
            	{
            		if(_S>0.99)
            		{
            			col *= _ForceColor;
            		}
            		else
            		{
            			col += _ForceColor * (_S-0.74) * 4.0;
            		}
            		_S = 0.5;
            	}
            	float3 hsv = rgb2hsv(col.rgb);
                hsv = float3(
                    hsv.x + _H - 0.5,
                    _S < 0.5 ? lerp(0.0,hsv.y,_S*2.0) : lerp(hsv.y,1.0,_S*2.0-1.0),
                    _V < 0.5 ? lerp(0.0,hsv.z,_V*2.0) : lerp(hsv.z,1.0,_V*2.0-1.0));
            	float3 rgb = hsv2rgb(hsv);
                return float4(rgb, col.a);
            }
            ENDCG
        }
    }
}
