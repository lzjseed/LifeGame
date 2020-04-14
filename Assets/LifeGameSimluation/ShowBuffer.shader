Shader "Hidden/ShowBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			
			float2 _ResolutionSize;
			float4 _MainTex_TexelSize;
			float4 _Mouse;
			float  _ZoomRange;
			float  _ZoomScale;
            sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				float2 pixelPos = i.uv * _ResolutionSize;

				float2 ndcPos = pixelPos / _ResolutionSize.x;
				
				float inCircle = 0.0f;

				if (_Mouse.z > 0.0)
				{
					float2 offsetMouse = pixelPos - _Mouse;

					if (length(offsetMouse) < _ZoomRange)
					{
						ndcPos = (_Mouse + (offsetMouse) * 1.0 / _ZoomScale) / _ResolutionSize.x;
					}

					inCircle = 1.0 - smoothstep(0, 1, abs(length(offsetMouse) - _ZoomRange));
				}

                float3 col = tex2D(_MainTex, ndcPos).rgb;
                
				col = lerp(col, float3(1.0, 1.0, 0.0), inCircle);

                return float4(col,1.0f);
            }
            ENDCG
        }
    }
}
