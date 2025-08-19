using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class WorldSpaceOverlayUI : MonoBehaviour
{
    private const string ShaderTestModeProp = "unity_GUIZTestMode";
    private UnityEngine.Rendering.CompareFunction desiredUIComparison = UnityEngine.Rendering.CompareFunction.Always;
    private readonly Dictionary<Material, Material> materialMappings = new();

    private void OnEnable() => ApplyToAll();

    private void ApplyToAll()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == null) continue;
            ApplyMaterial(graphic.materialForRendering, mat => graphic.material = mat);
        }
    }

    private void ApplyMaterial(Material original, System.Action<Material> assign)
    {
        if (original == null) return;

        if (!materialMappings.TryGetValue(original, out var copy))
        {
            copy = new Material(original);
            copy.SetInt(ShaderTestModeProp, (int)desiredUIComparison);
            materialMappings[original] = copy;
        }
        assign(copy);
    }
}
