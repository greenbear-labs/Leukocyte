using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leukocyte.EditorTools
{
    /// <summary>Creates the greybox materials in a Resources folder so their shaders and
    /// emission variant ship in player builds (runtime-only materials get stripped).</summary>
    public static class LeukocyteAssets
    {
        const string Folder = "Assets/Resources/Leukocyte";
        const string LitPath = Folder + "/GreyboxLit.mat";
        const string EmissivePath = Folder + "/GreyboxEmissive.mat";

        public static void EnsureMaterials()
        {
            if (File.Exists(LitPath) && File.Exists(EmissivePath)) return;

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var shader = PipelineShader();
            if (!File.Exists(LitPath))
            {
                var lit = new Material(shader) { name = "GreyboxLit", color = Color.white };
                AssetDatabase.CreateAsset(lit, LitPath);
            }
            if (!File.Exists(EmissivePath))
            {
                var emissive = new Material(shader) { name = "GreyboxEmissive", color = Color.white };
                if (emissive.HasProperty("_EmissionColor"))
                {
                    emissive.EnableKeyword("_EMISSION");
                    emissive.SetColor("_EmissionColor", Color.white);
                    emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                AssetDatabase.CreateAsset(emissive, EmissivePath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Leukocyte] Created greybox materials with shader " + shader.name);
        }

        static Shader PipelineShader()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline && pipeline.defaultMaterial) return pipeline.defaultMaterial.shader;
            return Shader.Find("Standard");
        }

        /// <summary>The level turns on fog at runtime; a scene saved with fog makes the build keep fog shader variants.</summary>
        public static void ApplySceneRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.22f, 0.02f, 0.04f);
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.16f, 0.18f);
            RenderSettings.skybox = null;
        }
    }
}
