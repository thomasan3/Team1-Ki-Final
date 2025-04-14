Shader "UUIFX/UILiquidLightTextureBased"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _LightIntensity("Intensity", Float) = 1
        _WaveSpeed("Speed", Float) = 1
        _WaveLength("Length", Float) = 1
        _WaveOffset("Offset", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LightIntensity;
            float _WaveSpeed;
            float _WaveLength;
            float _WaveOffset;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 获取原始纹理颜色
                fixed4 col = tex2D(_MainTex, i.uv);

                // 计算流光效果
                float wave = (sin(i.uv.x * _WaveLength + _Time.y * _WaveSpeed + _WaveOffset) + 1) / 2 * _LightIntensity;

                // 仅在非透明区域应用流光效果
                col.rgb += col.a * wave * col.rgb;

                return col;
            }
            ENDCG
        }
    }
}
