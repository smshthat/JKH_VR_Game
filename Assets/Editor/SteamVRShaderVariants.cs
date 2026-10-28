using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Strip only disabled features of the Windows SteamVR profile. Stereo, lightmaps,
// material features, main-light shadows and the scene's fog mode are preserved.
public class SteamVRShaderVariants : IPreprocessShaders, IPreprocessBuildWithReport
{
    static HashSet<ulong> materialMasks;
    static readonly string[] materialKeywords = {
        "_NORMALMAP", "_PARALLAXMAP", "_RECEIVE_SHADOWS_OFF", "_DETAIL_MULX2", "_DETAIL_SCALED",
        "_SURFACE_TYPE_TRANSPARENT", "_ALPHATEST_ON", "_ALPHAPREMULTIPLY_ON", "_ALPHAMODULATE_ON",
        "_EMISSION", "_METALLICSPECGLOSSMAP", "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", "_OCCLUSIONMAP",
        "_SPECULARHIGHLIGHTS_OFF", "_ENVIRONMENTREFLECTIONS_OFF", "_SPECULAR_SETUP"
    };
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report) { materialMasks = null; }

    static HashSet<ulong> MaterialMasks(Shader shader)
    {
        if (materialMasks != null) return materialMasks;
        var materials = new HashSet<Material>(Resources.FindObjectsOfTypeAll<Material>());
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            foreach (var mat in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Material>()) materials.Add(mat);
        materialMasks = new HashSet<ulong> { 0 };
        foreach (var mat in materials)
        {
            if (mat == null || mat.shader != shader) continue;
            ulong mask = 0;
            var enabled = new HashSet<string>(mat.shaderKeywords);
            for (int i = 0; i < materialKeywords.Length; i++) if (enabled.Contains(materialKeywords[i])) mask |= 1UL << i;
            materialMasks.Add(mask);
            materialMasks.Add(mask ^ (1UL << 9)); // allow runtime emission changes
        }
        materialMasks.Add(1UL << 5); // runtime transparent base material
        materialMasks.Add((1UL << 5) | (1UL << 7));
        materialMasks.Add(1UL << 6);
        return materialMasks;
    }
    public int callbackOrder => 10000;
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64) return;
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) return;
        var remove = new List<string> { "DEBUG_DISPLAY", "STEREO_MULTIVIEW_ON", "DOTS_INSTANCING_ON" };
        if (!pipeline.supportsAdditionalLightShadows) remove.Add("_ADDITIONAL_LIGHT_SHADOWS");
        if (!pipeline.supportsLightCookies) remove.Add("_LIGHT_COOKIES");
        if (!pipeline.useRenderingLayers) remove.Add("_LIGHT_LAYERS");
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if (renderer != null && !renderer.rendererFeatures.Any(f => f != null && f.isActive && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")))
            remove.Add("_SCREEN_SPACE_OCCLUSION");
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>("Assets/Art/Upgrades/Lighting.asset");
        if (lighting != null && !lighting.realtimeGI) remove.Add("DYNAMICLIGHTMAP_ON");
        var serialized = new SerializedObject(pipeline);
        var quality = serialized.FindProperty("m_SoftShadowQuality");
        if (quality != null && quality.intValue < 3) remove.Add("_SHADOWS_SOFT_HIGH");
        foreach (var pair in new[] { ("FOG_LINEAR", FogMode.Linear), ("FOG_EXP", FogMode.Exponential), ("FOG_EXP2", FogMode.ExponentialSquared) })
            if (!RenderSettings.fog || RenderSettings.fogMode != pair.Item2) remove.Add(pair.Item1);
        var keywords = remove.Select(name => new ShaderKeyword(name)).ToArray();
        bool forwardPass = snippet.passName == "ForwardLit" || snippet.passName == "Universal Forward";
        bool requireForwardPlus = renderer != null && new SerializedObject(renderer).FindProperty("m_RenderingMode").intValue == 2
            && forwardPass && shader.keywordSpace.FindKeyword("_FORWARD_PLUS").isValid;
        var forwardPlus = new ShaderKeyword("_FORWARD_PLUS");
        bool filterMaterials = shader.name == "Universal Render Pipeline/Lit" && snippet.passName == "ForwardLit" && snippet.shaderType.ToString() == "Fragment";
        var masks = filterMaterials ? MaterialMasks(shader) : null;
        var locals = filterMaterials ? materialKeywords.Select(name => new ShaderKeyword(shader, name)).ToArray() : null;
        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (keywords.Any(keyword => data[i].shaderKeywordSet.IsEnabled(keyword)) ||
                (requireForwardPlus && !data[i].shaderKeywordSet.IsEnabled(forwardPlus))) { data.RemoveAt(i); continue; }
            if (!filterMaterials) continue;
            ulong mask = 0;
            for (int k = 0; k < locals.Length; k++) if (data[i].shaderKeywordSet.IsEnabled(locals[k])) mask |= 1UL << k;
            if (!masks.Contains(mask)) data.RemoveAt(i);
        }
    }
}
