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

            DynamicVariableHelper.CreateVariable<MaterialSet>(meshRendererSlot, "MeshRendererConfig/NormalMaterialSet", map.NormalMaterialSet!);
            DynamicVariableHelper.CreateVariable<MeshRenderer>(meshRendererSlot, "MeshRendererConfig/NormalMeshRenderer", map.NormalMeshRenderer!);
            DynamicVariableHelper.CreateVariable<Slot>(meshRendererSlot, "MeshRendererConfig/NormalSlot", map.NormalSlot);
            DynamicVariableHelper.CreateVariable<MaterialSet>(meshRendererSlot, "MeshRendererConfig/StatueMaterialSet", map.StatueMaterialSet!);
            DynamicVariableHelper.CreateVariable<MeshRenderer>(meshRendererSlot, "MeshRendererConfig/StatueMeshRenderer", map.StatueMeshRenderer!);
            DynamicVariableHelper.CreateVariable<Slot>(meshRendererSlot, "MeshRendererConfig/StatueSlot", map.StatueSlot!);

            for(var set = 0; set < map.MaterialSets.Count; set++)
            {
                var setSlot = meshRendererSlot.AddSlot($"<color=hero.green>Material Set {set}</color>");
                setSlot.Tag_Field.Value = null!;
                setSlot.OrderOffset_Field.Value = set;

                for(var index = 0; index < map.MaterialSets[set].Count; index++)
                {
                    var materialSlot = setSlot.AddSlot($"<color=hero.cyan>Material {index}</color>");
                    materialSlot.OrderOffset_Field.Value = index;

                    var materialDynVarSpace = materialSlot.AttachComponent<DynamicVariableSpace>();
                    materialDynVarSpace.SpaceName.Value = "MaterialSlotConfig";

                    DynamicVariableHelper.CreateVariable<bool>(materialSlot, "MaterialSlotConfig/Clothes", map.MaterialSets[set][index].Clothes);
                    DynamicVariableHelper.CreateVariable<IAssetProvider<Material>>(materialSlot, "MaterialSlotConfig/Normal", map.MaterialSets[set][index].Normal!);
                    DynamicVariableHelper.CreateVariable<IAssetProvider<Material>>(materialSlot, "MaterialSlotConfig/Statue", map.MaterialSets[set][index].Statue!);
                    DynamicVariableHelper.CreateVariable<int>(materialSlot, "MaterialSlotConfig/TransitionType", (int)map.MaterialSets[set][index].TransitionType);
                    DynamicVariableHelper.CreateVariable<bool>(materialSlot, "MaterialSlotConfig/UseAsIs", map.MaterialSets[set][index].UseAsIs);

                    if(map.MaterialSets[set][index].Normal is not null)
                    {
                        var normalAssetLoader = materialSlot.AttachComponent<AssetLoader<Material>>();
                        normalAssetLoader.Asset.Target = map.MaterialSets[set][index].Normal!;
                    }

                    if(map.MaterialSets[set][index].Statue is not null)
                    {
                        var statueAssetLoader = materialSlot.AttachComponent<AssetLoader<Material>>();
                        statueAssetLoader.Asset.Target = map.MaterialSets[set][index].Statue!;
                    }
                }
            }
        }
    }

    private void ParseLastConfiguration(Slot slot)
    {
        _lastConfiguration.Clear();

        foreach(var meshRendererSlot in slot.GetChildrenWithTag("StatueSystemMeshRendererConfiguration"))
        {
            Log.Debug($"Trying to parse configuration in {meshRendererSlot.ToShortString()}");

            var dynVarSpace = DynamicVariableHelper.FindSpace(meshRendererSlot, "MeshRendererConfig");
            if(dynVarSpace is null)
            {
                Log.Warn($"Couldn't find MeshRendererConfig dynvar space at {meshRendererSlot.ToShortString()}");
                continue;
            }

            var meshRendererMap = new MeshRendererMap();

            if(dynVarSpace.TryReadValue<MaterialSet>("NormalMaterialSet", out var normalMaterialSet))
                meshRendererMap.NormalMaterialSet = normalMaterialSet;
            else
                Log.Warn($"Couldn't read NormalMaterialSet variable at {meshRendererSlot.ToShortString()}");

            if(dynVarSpace.TryReadValue<MeshRenderer>("NormalMeshRenderer", out var normalMeshRenderer))
                meshRendererMap.NormalMeshRenderer = normalMeshRenderer;
            else
                Log.Warn($"Couldn't read NormalMeshRenderer variable at {meshRendererSlot.ToShortString()}");

            if(dynVarSpace.TryReadValue<Slot>("NormalSlot", out var normalSlot))
                meshRendererMap.NormalSlot = normalSlot;
            else
                Log.Warn($"Couldn't read NormalSlot variable at {meshRendererSlot.ToShortString()}");

            if(dynVarSpace.TryReadValue<MaterialSet>("StatueMaterialSet", out var statueMaterialSet))
                meshRendererMap.StatueMaterialSet = statueMaterialSet;
            else
                Log.Warn($"Couldn't read StatueMaterialSet variable at {meshRendererSlot.ToShortString()}");

            if(dynVarSpace.TryReadValue<MeshRenderer>("StatueMeshRenderer", out var statueMeshRenderer))
                meshRendererMap.StatueMeshRenderer = statueMeshRenderer;
            else
                Log.Warn($"Couldn't read StatueMeshRenderer variable at {meshRendererSlot.ToShortString()}");

            if(dynVarSpace.TryReadValue<Slot>("StatueSlot", out var statueSlot))
                meshRendererMap.StatueSlot = statueSlot;
            else
                Log.Warn($"Couldn't read StatueSlot variable at {meshRendererSlot.ToShortString()}");


            for(var set = 0; set < meshRendererSlot.ChildrenCount; set++)
            {
                var setSlot = meshRendererSlot[set];
                var materials = new List<MaterialMap>();

                Log.Debug($"Found material set configuration at {setSlot.ToShortString()}");

                for(var index = 0; index < setSlot.ChildrenCount; index++)
                {
                    var indexSlot = setSlot[index];
                    var material = new MaterialMap();
                    var materialDynVarSpace = DynamicVariableHelper.FindSpace(indexSlot, "MaterialSlotConfig");

                    Log.Debug($"Trying to parse material configuration in {indexSlot.ToShortString()}");

                    if(materialDynVarSpace is null)
                    {
                        Log.Warn($"Couldn't find MaterialSlotConfig dynvar space at {indexSlot.ToShortString()}");
                        continue;
                    }

                    if(materialDynVarSpace.TryReadValue<bool>("Clothes", out var clothes))
                        material.Clothes = clothes;
                    else
                        Log.Warn($"Couldn't read Clothes variable at {indexSlot.ToShortString()}");

                    if(materialDynVarSpace.TryReadValue<IAssetProvider<Material>>("Normal", out var normal))
                        material.Normal = normal;
                    else
                        Log.Warn($"Couldn't read Normal variable at {indexSlot.ToShortString()}");

                    if(materialDynVarSpace.TryReadValue<IAssetProvider<Material>>("Statue", out var statue))
                        material.Statue = statue;
                    else
                        Log.Warn($"Couldn't read Statue variable at {indexSlot.ToShortString()}");

                    if(materialDynVarSpace.TryReadValue<int>("TransitionType", out var transitionType))
                        material.TransitionType = (StatueType)transitionType;
                    else
                        Log.Warn($"Couldn't read TransitionType variable at {indexSlot.ToShortString()}");

                    if(materialDynVarSpace.TryReadValue<bool>("UseAsIs", out var useAsIs))
                        material.UseAsIs = useAsIs;
                    else
                        Log.Warn($"Couldn't read UseAsIs variable at {indexSlot.ToShortString()}");


                    materials.Add(material);
                }

                meshRendererMap.MaterialSets.Add(materials);
            }

            Log.Info($"Found mesh renderer with {meshRendererMap.MaterialSets.Count} material sets and {meshRendererMap.MaterialSets.SelectMany(x => x).Count()} materials");

            _lastConfiguration.Add(meshRendererMap);
        }

        Log.Info($"Successfully parsed configuration for {_lastConfiguration.Count} mesh renderers");
    }

    private MeshRendererMap? FindConfiguration(MeshRenderer? meshRenderer)
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
