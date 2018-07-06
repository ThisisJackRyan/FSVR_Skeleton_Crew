// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge Beta 0.34 
// Shader Forge (c) Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.34;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:1,uamb:True,mssp:True,lmpd:False,lprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,blpr:0,bsrc:0,bdst:0,culm:0,dpts:2,wrdp:True,ufog:True,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1,x:32719,y:32712|diff-5-OUT,spec-1170-OUT,normal-1173-OUT;n:type:ShaderForge.SFN_Tex2d,id:2,x:34187,y:32415,ptlb:top,ptin:_top,tex:dc5db1a88bf80944d9b748c7918bc1fc,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:4,x:34187,y:32230,ptlb:base,ptin:_base,tex:7845c6bc7beee704c840b1f172f2cba5,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Lerp,id:5,x:33301,y:32577|A-4-RGB,B-2-RGB,T-1139-OUT;n:type:ShaderForge.SFN_VertexColor,id:12,x:34572,y:32967;n:type:ShaderForge.SFN_Tex2d,id:22,x:34987,y:32577,ptlb:heightmap,ptin:_heightmap,tex:0c30a591363799f4b9b2f46db73dd7a9,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Posterize,id:625,x:34737,y:32622|IN-22-RGB,STPS-627-OUT;n:type:ShaderForge.SFN_Vector1,id:627,x:34998,y:32774,v1:2;n:type:ShaderForge.SFN_Multiply,id:881,x:34372,y:32608|A-625-OUT,B-882-OUT;n:type:ShaderForge.SFN_Vector1,id:882,x:34737,y:32771,v1:2;n:type:ShaderForge.SFN_Clamp,id:884,x:34187,y:32608|IN-881-OUT,MIN-885-OUT,MAX-886-OUT;n:type:ShaderForge.SFN_Vector1,id:885,x:34728,y:32831,v1:-1;n:type:ShaderForge.SFN_Vector1,id:886,x:34728,y:32897,v1:1.1;n:type:ShaderForge.SFN_Blend,id:965,x:34013,y:32720,blmd:10,clmp:True|SRC-884-OUT,DST-12-R;n:type:ShaderForge.SFN_Lerp,id:1014,x:33634,y:32754|A-1015-OUT,B-1016-OUT,T-965-OUT;n:type:ShaderForge.SFN_Vector1,id:1015,x:33890,y:32550,v1:0;n:type:ShaderForge.SFN_Vector1,id:1016,x:33890,y:32622,v1:1;n:type:ShaderForge.SFN_Clamp01,id:1139,x:33464,y:32754|IN-1014-OUT;n:type:ShaderForge.SFN_Lerp,id:1170,x:33242,y:32886|A-1193-OUT,B-1229-OUT,T-1139-OUT;n:type:ShaderForge.SFN_Lerp,id:1173,x:33385,y:33372|A-1176-RGB,B-1174-RGB,T-1139-OUT;n:type:ShaderForge.SFN_Tex2d,id:1174,x:33702,y:33336,ptlb:Top_nrm,ptin:_Top_nrm,tex:50f82b3bac8910549b62bda7a7f308a9,ntxv:3,isnm:True;n:type:ShaderForge.SFN_Tex2d,id:1175,x:33752,y:32915,ptlb:Top_spec,ptin:_Top_spec,tex:ecbcdcba20438b444a98fc4f359ea196,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:1176,x:33702,y:33514,ptlb:Bottom_nrm,ptin:_Bottom_nrm,tex:9a5f8e2d84dfae441845bd216e398439,ntxv:3,isnm:True;n:type:ShaderForge.SFN_Tex2d,id:1180,x:33752,y:33093,ptlb:Bottom_spec,ptin:_Bottom_spec,tex:36e3192bbbb7f034d87608edc25f5939,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Multiply,id:1193,x:33503,y:33060|A-1194-OUT,B-1180-RGB;n:type:ShaderForge.SFN_Vector1,id:1194,x:34007,y:33016,v1:0.1;n:type:ShaderForge.SFN_Multiply,id:1229,x:33534,y:32915|A-1175-RGB,B-1232-OUT;n:type:ShaderForge.SFN_Vector1,id:1232,x:34007,y:32943,v1:1.25;proporder:2-4-22-1174-1176-1175-1180;pass:END;sub:END;*/

Shader "Shader Forge/Two_material_blend" {
    Properties {
        _top ("top", 2D) = "white" {}
        _base ("base", 2D) = "white" {}
        _heightmap ("heightmap", 2D) = "white" {}
        _Top_nrm ("Top_nrm", 2D) = "bump" {}
        _Bottom_nrm ("Bottom_nrm", 2D) = "bump" {}
        _Top_spec ("Top_spec", 2D) = "white" {}
        _Bottom_spec ("Bottom_spec", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers xbox360 ps3 flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _top; uniform float4 _top_ST;
            uniform sampler2D _base; uniform float4 _base_ST;
            uniform sampler2D _heightmap; uniform float4 _heightmap_ST;
            uniform sampler2D _Top_nrm; uniform float4 _Top_nrm_ST;
            uniform sampler2D _Top_spec; uniform float4 _Top_spec_ST;
            uniform sampler2D _Bottom_nrm; uniform float4 _Bottom_nrm_ST;
            uniform sampler2D _Bottom_spec; uniform float4 _Bottom_spec_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(5,6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float2 node_1250 = i.uv0;
                float node_627 = 2.0;
                float node_1139 = saturate(lerp(0.0,1.0,saturate(( i.vertexColor.r > 0.5 ? (1.0-(1.0-2.0*(i.vertexColor.r-0.5))*(1.0-clamp((floor(tex2D(_heightmap,TRANSFORM_TEX(node_1250.rg, _heightmap)).rgb * node_627) / (node_627 - 1)*2.0),(-1.0),1.1))) : (2.0*i.vertexColor.r*clamp((floor(tex2D(_heightmap,TRANSFORM_TEX(node_1250.rg, _heightmap)).rgb * node_627) / (node_627 - 1)*2.0),(-1.0),1.1)) ))));
                float3 normalLocal = lerp(UnpackNormal(tex2D(_Bottom_nrm,TRANSFORM_TEX(node_1250.rg, _Bottom_nrm))).rgb,UnpackNormal(tex2D(_Top_nrm,TRANSFORM_TEX(node_1250.rg, _Top_nrm))).rgb,node_1139);
                float3 normalDirection =  normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 diffuse = max( 0.0, NdotL) * attenColor + UNITY_LIGHTMODEL_AMBIENT.rgb;
///////// Gloss:
                float gloss = 0.5;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float3 specularColor = lerp((0.1*tex2D(_Bottom_spec,TRANSFORM_TEX(node_1250.rg, _Bottom_spec)).rgb),(tex2D(_Top_spec,TRANSFORM_TEX(node_1250.rg, _Top_spec)).rgb*1.25),node_1139);
                float3 specular = (floor(attenuation) * _LightColor0.xyz) * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                finalColor += diffuseLight * lerp(tex2D(_base,TRANSFORM_TEX(node_1250.rg, _base)).rgb,tex2D(_top,TRANSFORM_TEX(node_1250.rg, _top)).rgb,node_1139);
                finalColor += specular;
/// Final Color:
                return fixed4(finalColor,1);
            }
            ENDCG
        }
        Pass {
            Name "ForwardAdd"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            
            
            Fog { Color (0,0,0,0) }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma exclude_renderers xbox360 ps3 flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _top; uniform float4 _top_ST;
            uniform sampler2D _base; uniform float4 _base_ST;
            uniform sampler2D _heightmap; uniform float4 _heightmap_ST;
            uniform sampler2D _Top_nrm; uniform float4 _Top_nrm_ST;
            uniform sampler2D _Top_spec; uniform float4 _Top_spec_ST;
            uniform sampler2D _Bottom_nrm; uniform float4 _Bottom_nrm_ST;
            uniform sampler2D _Bottom_spec; uniform float4 _Bottom_spec_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(5,6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float2 node_1251 = i.uv0;
                float node_627 = 2.0;
                float node_1139 = saturate(lerp(0.0,1.0,saturate(( i.vertexColor.r > 0.5 ? (1.0-(1.0-2.0*(i.vertexColor.r-0.5))*(1.0-clamp((floor(tex2D(_heightmap,TRANSFORM_TEX(node_1251.rg, _heightmap)).rgb * node_627) / (node_627 - 1)*2.0),(-1.0),1.1))) : (2.0*i.vertexColor.r*clamp((floor(tex2D(_heightmap,TRANSFORM_TEX(node_1251.rg, _heightmap)).rgb * node_627) / (node_627 - 1)*2.0),(-1.0),1.1)) ))));
                float3 normalLocal = lerp(UnpackNormal(tex2D(_Bottom_nrm,TRANSFORM_TEX(node_1251.rg, _Bottom_nrm))).rgb,UnpackNormal(tex2D(_Top_nrm,TRANSFORM_TEX(node_1251.rg, _Top_nrm))).rgb,node_1139);
                float3 normalDirection =  normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 diffuse = max( 0.0, NdotL) * attenColor;
///////// Gloss:
                float gloss = 0.5;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float3 specularColor = lerp((0.1*tex2D(_Bottom_spec,TRANSFORM_TEX(node_1251.rg, _Bottom_spec)).rgb),(tex2D(_Top_spec,TRANSFORM_TEX(node_1251.rg, _Top_spec)).rgb*1.25),node_1139);
                float3 specular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                finalColor += diffuseLight * lerp(tex2D(_base,TRANSFORM_TEX(node_1251.rg, _base)).rgb,tex2D(_top,TRANSFORM_TEX(node_1251.rg, _top)).rgb,node_1139);
                finalColor += specular;
/// Final Color:
                return fixed4(finalColor * 1,0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
