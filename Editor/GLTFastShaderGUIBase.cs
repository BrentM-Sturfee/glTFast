﻿using System;
using GLTFast.Materials;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using static GLTFast.Materials.StandardShaderHelper;

namespace GLTFast.Editor
{
    public class GLTFastShaderGUIBase : ShaderGUI
    {
        protected const float TOLERANCE = 0.001f;

        public struct UvTransform
        {
            public float rotation;
            public float2 scale;
        }

        protected UvTransform? TextureRotationSlider(
            Material material,
            UvTransform? uvTransform,
            int scaleTransformPropertyId,
            int rotationPropertyId,
            bool freezeScale = false
            )
        {
            UvTransform oldUvTransform;
            UvTransform newUvTransform;

            if (uvTransform.HasValue) {
                oldUvTransform = uvTransform.Value;
                newUvTransform = uvTransform.Value;
            } else {
                GetUvTransform(material,scaleTransformPropertyId,rotationPropertyId,out oldUvTransform);
                newUvTransform = new UvTransform();
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Texture Rotation");
            var newUvRotation = EditorGUILayout.Slider(oldUvTransform.rotation,0,360);
            GUILayout.EndHorizontal();

            float2 newUvScale = new float2(1,1);
            if (!freezeScale)
            {
                GUILayout.BeginHorizontal();
                newUvScale = EditorGUILayout.Vector2Field("Scale",oldUvTransform.scale);
                GUILayout.EndHorizontal();                
            }

            if (!uvTransform.HasValue) {
                newUvTransform.rotation = newUvRotation;
                newUvTransform.scale = newUvScale;
            }

            bool update = false;

            if (Math.Abs(newUvRotation - oldUvTransform.rotation) > TOLERANCE) {
                newUvTransform.rotation = newUvRotation;
                update = true;
            }

            if (!freezeScale && !newUvScale.Equals(oldUvTransform.scale))
            {
                newUvTransform.scale = newUvScale;
                update = true;
            }
            
            if(update) {
                var cos = math.cos(newUvTransform.rotation*Mathf.Deg2Rad);
                var sin = math.sin(newUvTransform.rotation*Mathf.Deg2Rad);
                var currentScaleTransform = material.GetVector(scaleTransformPropertyId);
                float2x2 rotScale = math.mul(new float2x2(cos, sin, -sin, cos), new float2x2(newUvTransform.scale.x,0,0,newUvTransform.scale.y));
                material.SetVector(scaleTransformPropertyId,new Vector4(rotScale.c0.x,rotScale.c1.y,currentScaleTransform.z,currentScaleTransform.w));
                material.SetVector(rotationPropertyId,new Vector4(rotScale.c0.y,rotScale.c1.x,0,0));
                if (newUvTransform.rotation == 0) {
                    material.DisableKeyword(KW_UV_ROTATION);
                } else {
                    material.EnableKeyword(StandardShaderHelper.KW_UV_ROTATION);
                }
                return newUvTransform;
            }

            return uvTransform;
        }

        /// <summary>
        /// Extracts a material's texture rotation (degrees) from the 2 by 2 matrix
        /// </summary>
        /// <param name="material"></param>
        /// <returns>texture rotation in degrees</returns>
        static void GetUvTransform(Material material, int scaleTransformPropertyId, int rotationPropertyId, out UvTransform uvTransform) {
            float4 st = material.GetVector(scaleTransformPropertyId);
            float2 r = (Vector2)material.GetVector(rotationPropertyId);

            uvTransform.scale.x = Mathematics.normalize(new float2(st.x,r.x), out var r1);
            uvTransform.scale.y = Mathematics.normalize(new float2(st.y,r.y), out var r2);

            var acos = Mathf.Acos(r1.x);
            if (r2.x < 0) acos = Mathf.PI*2-acos;
            uvTransform.rotation = acos * Mathf.Rad2Deg;
        }
    }
}
