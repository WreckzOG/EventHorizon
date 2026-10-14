using UnityEngine;
using UnityEngine.Rendering;
namespace EventHorizon { internal static class DomainVisual {
private static Material glowTemplate;
        private static bool EnsureMaterial()
        {
            if (glowTemplate) return true;
            // RoR2 patches Shader.Find and throws for uncatalogued built-ins. Read actual loaded objects.
            Shader[] loaded = Resources.FindObjectsOfTypeAll<Shader>();
            string[] names = { "Particles/Standard Unlit", "Legacy Shaders/Particles/Additive", "Particles/Additive", "Sprites/Default" };
            Shader chosen = null;
            foreach (string name in names)
            {
                foreach (Shader shader in loaded)
                    if (shader && shader.name == name && shader.isSupported) { chosen = shader; break; }
                if (chosen) break;
            }
            if (!chosen) return false;
            glowTemplate = new Material(chosen) { name = "Event Horizon Photon Glow", renderQueue = 3100 };
            if (glowTemplate.HasProperty("_MainTex")) glowTemplate.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (glowTemplate.HasProperty("_Color")) glowTemplate.SetColor("_Color", Color.white);
            if (glowTemplate.HasProperty("_TintColor")) glowTemplate.SetColor("_TintColor", Color.white);
            if (glowTemplate.HasProperty("_Mode")) glowTemplate.SetFloat("_Mode", 4f);
            if (glowTemplate.HasProperty("_ColorMode")) glowTemplate.SetFloat("_ColorMode", 0f);
            glowTemplate.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            glowTemplate.SetInt("_DstBlend", (int)BlendMode.One);
            glowTemplate.SetInt("_ZWrite", 0);
            glowTemplate.SetInt("_Cull", (int)CullMode.Off);
            glowTemplate.DisableKeyword("_ALPHATEST_ON");
            glowTemplate.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glowTemplate.DisableKeyword("_ALPHAMODULATE_ON");
            glowTemplate.DisableKeyword("SOFTPARTICLES_ON");
            glowTemplate.DisableKeyword("FADING_ON");
            glowTemplate.EnableKeyword("_ALPHABLEND_ON");
            Plugin.Log.LogInfo("Domain visual shader resolved: " + chosen.name + " supported=" + chosen.isSupported);
            return true;
        }

        internal static Material CreateGlowMaterial()
        {
            return EnsureMaterial() ? new Material(glowTemplate) : null;
        }

internal static void Clear(){if(glowTemplate)Object.Destroy(glowTemplate);glowTemplate=null;}
}}
