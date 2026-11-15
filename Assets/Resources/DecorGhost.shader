Shader "LunarRelay/DecorGhost" {
 Properties { _BaseColor("Tint",Color)=(0.2,0.8,0.8,0.4) }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _BaseColor;
 struct A {float4 p:POSITION;};struct V {float4 p:SV_POSITION;};V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);return o;}half4 frag(V v):SV_Target{return _BaseColor;}
 ENDHLSL
 }}}
