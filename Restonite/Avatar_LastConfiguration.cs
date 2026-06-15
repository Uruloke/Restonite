using Elements.Core;
using FrooxEngine;
using System.Collections.Generic;
using System.Linq;

namespace Restonite;

internal partial class Avatar
{
    public void SaveLastConfiguration()
    {
        var slot = StatueRoot!.FindChildOrAdd("Installation Parameters");
        slot.Tag_Field.Value = "StatueSystemLastConfiguration";

        slot.DestroyChildren(true);

        foreach(var map in MeshRenderers)
        {
            if(map.NormalSlot is null)
                continue;

            var meshRendererSlot = slot.AddSlot($"<color=hero.yellow>{map.NormalSlot!.Name}</color>");
            meshRendererSlot.Tag_Field.Value = "StatueSystemMeshRendererConfiguration";

            var dynVarSpace = meshRendererSlot.AttachComponent<DynamicVariableSpace>();
            dynVarSpace.SpaceName.Value = "MeshRendererConfig";

            DynamicVariableHelper.CreateVariable<MaterialSet>(meshRendererSlot, "MeshRendererConfig/NormalMaterialSet", map.NormalMaterialSet);
            DynamicVariableHelper.CreateVariable<MeshRenderer>(meshRendererSlot, "MeshRendererConfig/NormalMeshRenderer", map.NormalMeshRenderer);
            DynamicVariableHelper.CreateVariable<Slot>(meshRendererSlot, "MeshRendererConfig/NormalSlot", map.NormalSlot);
            DynamicVariableHelper.CreateVariable<MaterialSet>(meshRendererSlot, "MeshRendererConfig/StatueMaterialSet", map.StatueMaterialSet);
            DynamicVariableHelper.CreateVariable<MeshRenderer>(meshRendererSlot, "MeshRendererConfig/StatueMeshRenderer", map.StatueMeshRenderer);
            DynamicVariableHelper.CreateVariable<Slot>(meshRendererSlot, "MeshRendererConfig/StatueSlot", map.StatueSlot);

            var materialSets = meshRendererSlot.AddSlot("Material Sets");
            materialSets.Tag_Field.Value = null;

            for(var set = 0; set < map.MaterialSets.Count; set++)
            {
                var setSlot = materialSets.AddSlot($"<color=hero.green>Set {set}</color>");

                for(var index = 0; index < map.MaterialSets[set].Count; index++)
                {
                    var materialSlot = setSlot.AddSlot($"<color=hero.cyan>Material {index}</color>");

                    var materialDynVarSpace = materialSlot.AttachComponent<DynamicVariableSpace>();
                    materialDynVarSpace.SpaceName.Value = "MaterialSlotConfig";

                    DynamicVariableHelper.CreateVariable<bool>(materialSlot, "MaterialSlotConfig/Clothes", map.MaterialSets[set][index].Clothes);
                    DynamicVariableHelper.CreateVariable<IAssetProvider<Material>>(materialSlot, "MaterialSlotConfig/Normal", map.MaterialSets[set][index].Normal);
                    DynamicVariableHelper.CreateVariable<IAssetProvider<Material>>(materialSlot, "MaterialSlotConfig/Statue", map.MaterialSets[set][index].Statue);
                    DynamicVariableHelper.CreateVariable<int>(materialSlot, "MaterialSlotConfig/TransitionType", (int)map.MaterialSets[set][index].TransitionType);
                    DynamicVariableHelper.CreateVariable<bool>(materialSlot, "MaterialSlotConfig/UseAsIs", map.MaterialSets[set][index].UseAsIs);
                }
            }
        }
    }

    private void ParseLastConfiguration(Slot slot)
    {
        _lastConfiguration.Clear();

        foreach(var meshRendererSlot in slot.GetChildrenWithTag("StatueSystemMeshRendererConfiguration"))
        {
            var dynVarSpace = DynamicVariableHelper.FindSpace(meshRendererSlot, "MeshRendererConfig");
            if(dynVarSpace is null)
                continue;

            var meshRendererMap = new MeshRendererMap();

            if(dynVarSpace.TryReadValue<MaterialSet>("NormalMaterialSet", out var normalMaterialSet))
                meshRendererMap.NormalMaterialSet = normalMaterialSet;
            if(dynVarSpace.TryReadValue<MeshRenderer>("NormalMeshRenderer", out var normalMeshRenderer))
                meshRendererMap.NormalMeshRenderer = normalMeshRenderer;
            if(dynVarSpace.TryReadValue<Slot>("NormalSlot", out var normalSlot))
                meshRendererMap.NormalSlot = normalSlot;
            if(dynVarSpace.TryReadValue<MaterialSet>("StatueMaterialSet", out var statueMaterialSet))
                meshRendererMap.StatueMaterialSet = statueMaterialSet;
            if(dynVarSpace.TryReadValue<MeshRenderer>("StatueMeshRenderer", out var statueMeshRenderer))
                meshRendererMap.StatueMeshRenderer = statueMeshRenderer;
            if(dynVarSpace.TryReadValue<Slot>("StatueSlot", out var statueSlot))
                meshRendererMap.StatueSlot = statueSlot;

            var materialSets = meshRendererSlot.FindChild("Material Sets");
            if(materialSets is not null)
            {
                for(var set = 0; set < materialSets.ChildrenCount; set++)
                {
                    var setSlot = materialSets[set];
                    var materials = new List<MaterialMap>();

                    for(var index = 0; index < setSlot.ChildrenCount; index++)
                    {
                        var indexSlot = materialSets[set][index];
                        var material = new MaterialMap();
                        var materialDynVarSpace = DynamicVariableHelper.FindSpace(indexSlot, "MaterialSlotConfig");

                        if(materialDynVarSpace.TryReadValue<bool>("Clothes", out var clothes))
                            material.Clothes = clothes;
                        if(materialDynVarSpace.TryReadValue<IAssetProvider<Material>>("Normal", out var normal))
                            material.Normal = normal;
                        if(materialDynVarSpace.TryReadValue<IAssetProvider<Material>>("Statue", out var statue))
                            material.Statue = statue;
                        if(materialDynVarSpace.TryReadValue<int>("TransitionType", out var transitionType))
                            material.TransitionType = (StatueType)transitionType;
                        if(materialDynVarSpace.TryReadValue<bool>("UseAsIs", out var useAsIs))
                            material.UseAsIs = useAsIs;

                        materials.Add(material);
                    }

                    meshRendererMap.MaterialSets.Add(materials);
                }
            }

            _lastConfiguration.Add(meshRendererMap);
        }
    }

    private MeshRendererMap? FindConfiguration(MeshRenderer meshRenderer)
    {
        foreach(var map in _lastConfiguration)
        {
            if(map.NormalMeshRenderer == meshRenderer)
                return map;
        }

        return null;
    }

    private readonly List<MeshRendererMap> _lastConfiguration = [];
}
