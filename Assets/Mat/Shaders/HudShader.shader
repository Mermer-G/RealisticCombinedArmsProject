Shader "Custom/HUDShader"
{
    Properties
    {
        [Header(Glass Properties)]
        _Color ("Tint Color", Color) = (1,1,1,0.1) // Cam rengi ve saydamlýðý
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // Ýsteðe baðlý doku

        [Header(Stencil Settings)]
        [IntRange] _StencilRef ("Stencil Reference Value", Range(0, 255)) = 1 // Stencil deðeri (Script ile senkronize olmalý)

        // Diðer isteðe baðlý özellikler eklenebilir (örn. parlaklýk, metaliklik vs.)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // Stencil Buffer Ayarlarý
        Stencil
        {
            Ref [_StencilRef]   // Bu shader'ýn yazacaðý referans deðeri (Properties'den gelir)
            Comp Always         // Her zaman stencil testini geç (yani her zaman çiz)
            Pass Replace        // Stencil testi geçilirse (ki her zaman geçecek), buffer'daki deðeri Ref deðeri ile deðiþtir
            // Fail Keep        // Stencil testi baþarýsýz olursa buffer'ý deðiþtirme (Comp Always olduðu için bu satýr etkisiz)
            // ZFail Keep       // Derinlik testi baþarýsýz olursa buffer'ý deðiþtirme
        }

        Pass
        {
            ZWrite Off // Saydam objeler genellikle derinlik buffer'ýna yazmaz
            Blend SrcAlpha OneMinusSrcAlpha // Standart saydamlýk harmanlamasý

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit" // Eski donanýmlar için fallback
}