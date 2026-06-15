using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Restonite;

internal partial class Avatar
{
    #region Public Methods

    public void GenerateNormalMaterials()
    {
        if (_normalMaterials is null)
            return;

        Log.Info("=== Generating normal materials");

        // Destroy all existing children
        _normalMaterials.DestroyChildren();

        // Create alpha material and swap normal material for it
        var oldMaterialToNewNormalMaterialMap = new Dictionary<string, ReferenceMultiDriver<IAssetProvider<Material>>>();
        for (int i = 0; i < MeshRenderers.Count; i++)
        {
            MeshRendererMap map = MeshRenderers[i];

            if (map.NormalMeshRenderer is null)
                continue;

            for (int set = 0; set < map.MaterialSets.Count; set++)
            {
                for (int slot = 0; slot < map.MaterialSets[set].Count; ++slot)
                {
                    // Skip material slots where either material is null
                    if (map.MaterialSets[set][slot].Normal is null || map.MaterialSets[set][slot].Statue is null)
                        continue;

                    var name = $"{map.NormalMeshRenderer.ToLongString()}, material set {set}, slot {slot}";

                    var oldMaterial = map.MaterialSets[set][slot].Normal;
                    var statueType = map.MaterialSets[set][slot].TransitionType;
                    var key = $"{oldMaterial!.ReferenceID}_{statueType}";

                    if (!oldMaterialToNewNormalMaterialMap.ContainsKey(key))
                    {
                        Log.Info($"Creating normal material {oldMaterialToNewNormalMaterialMap.Count} for {oldMaterial.ToLongString()} using {statueType}");

                        var newSlot = _normalMaterials.AddSlot($"{oldMaterialToNewNormalMaterialMap.Count}: <color=hero.yellow>{map.NormalSlot!.Name.StripRTFTags()}</color> <color=hero.green>[Set {set}]</color> <color=hero.cyan>[Material {slot}]</color>");

                        // Create material based on transition type
                        var newMaterial = MaterialHelpers.CreateAlphaMaterial(oldMaterial, statueType, newSlot);

                        // boolean ref driver drives this, which drives everything else
                        var multiDriver = newSlot.AttachComponent<ReferenceMultiDriver<IAssetProvider<Material>>>();

                        if (map.MaterialSets[set][slot].Clothes)
                        {
                            var dynVarDriver = newSlot.AttachComponent<DynamicValueVariableDriver<int>>();
                            dynVarDriver.VariableName.Value = "Avatar/Statue.Clothing.TransitionType";

                            var valueField = newSlot.AttachComponent<ValueField<int>>();
                            dynVarDriver.Target.ForceLink(valueField.Value);

                            var valueEqualityDriver = newSlot.AttachComponent<ValueEqualityDriver<int>>();
                            valueEqualityDriver.Reference.Value = 0;
                            valueEqualityDriver.Invert.Value = true;
                            valueEqualityDriver.TargetValue.Target = valueField.Value;

                            var booleanReferenceDriver = newSlot.AttachComponent<BooleanReferenceDriver<IAssetProvider<Material>>>();
                            booleanReferenceDriver.FalseTarget.Value = oldMaterial.ReferenceID;
                            booleanReferenceDriver.TrueTarget.Value = newMaterial.ReferenceID;
                            valueEqualityDriver.Target.ForceLink(booleanReferenceDriver.State);
                            booleanReferenceDriver.TargetReference.ForceLink(multiDriver.Reference);
                        }
                        else
                        {
                            multiDriver.Reference.Target = newMaterial;
                        }

                        // Value/ReferenceCopy any drives from the old material
                        // to the new that isn't being used by the statue system
                        ((Component)oldMaterial).CopySyncDrivers((Component)newMaterial);

                        oldMaterialToNewNormalMaterialMap.Add(key, multiDriver);
                    }

                    var drives = oldMaterialToNewNormalMaterialMap[key].Drives;

                    if (map.NormalMaterialSet is not null)
                    {
                        drives.Add().ForceLink(map.NormalMaterialSet.Sets[set].GetElement(slot));
                    }
                    else
                    {
                        var materialSlot = map.NormalMeshRenderer.Materials.GetElement(slot);
                        var element = materialSlot.ActiveLink as SyncElement;
                        if (element is not null && materialSlot.IsDriven && materialSlot.IsLinked)
                            Log.Warn($"{name} appears to already be driven by {element.Component.ToLongString()}, attempting to set anyway");

                        drives.Add().ForceLink(materialSlot);
                    }
                }
            }
        }
    }

    public void GenerateStatueMaterials()
    {
        if (_statueMaterials is null)
            return;

        Log.Info("=== Generating statue materials");

        // Destroy all existing children
        _statueMaterials.DestroyChildren();
        IAssetProvider<Material>? transparentMaterial = null;
        if (MeshRenderers.SelectMany(x => x.MaterialSets.SelectMany(y => y)).Any(x => x.Clothes))
        {
            var slot = _statueMaterials.AddSlot("Transparent");
            var mat = slot.AttachComponent<PBS_Metallic>();
            mat.AlbedoColor.Value = new colorX(r: 0.0f, g: 0.0f, b: 0.0f, a: 0.0f);
            mat.BlendMode.Value = BlendMode.Alpha;
            transparentMaterial = mat;
        }

        // Create Material objects for each statue material
        var oldMaterialToStatueMaterialMap = new Dictionary<string, ReferenceMultiDriver<IAssetProvider<Material>>>();
        for (int i = 0; i < MeshRenderers.Count; i++)
        {
            MeshRendererMap map = MeshRenderers[i];
            var isBlinder = map.NormalMeshRenderer is null && map.StatueMeshRenderer is null;

            for (int set = 0; set < map.MaterialSets.Count; ++set)
            {
                for (int slot = 0; slot < map.MaterialSets[set].Count; ++slot)
                {
                    var name = map.StatueMeshRenderer is null ? "Blinder" : $"{map.StatueMeshRenderer.ToLongString()}, material set {set}, slot {slot}";

                    var normalMaterial = map.MaterialSets[set][slot].Normal;
                    var statueMaterial = map.MaterialSets[set][slot].Statue;
                    var defaultMaterialAsIs = isBlinder || map.MaterialSets[set][slot].UseAsIs;

                    if (statueMaterial is null)
                        continue;

                    if (!isBlinder && normalMaterial is null)
                    {
                        Log.Warn($"{map.NormalMeshRenderer.ToLongString()}, material {slot} is null, skipping statue material");
                        continue;
                    }

                    var key = defaultMaterialAsIs && !map.MaterialSets[set][slot].Clothes
                        ? $"{statueMaterial!.ReferenceID}"
                        : $"{normalMaterial!.ReferenceID}_{map.MaterialSets[set][slot].Clothes}";

                    if (!oldMaterialToStatueMaterialMap.ContainsKey(key))
                    {
                        Log.Info($"Creating statue material {oldMaterialToStatueMaterialMap.Count} as duplicate of {key}");
                        Log.Debug(defaultMaterialAsIs ? "Using material as-is" : "Merging with normal material maps");

                        // If assigned is null, use default

                        // Create a new statue material object (i.e. drives material slot on statue
                        // SMR, has default material with normal map)
                        var newMaterialHolder = _statueMaterials.AddSlot(map.StatueSlot is null
                            ? $"{oldMaterialToStatueMaterialMap.Count}: Default"
                            : $"{oldMaterialToStatueMaterialMap.Count}: <color=hero.yellow>{map.StatueSlot!.Name.StripRTFTags()}</color> <color=hero.green>[Set {set}]</color> <color=hero.cyan>[Material {slot}]</color>");

                        var newDefaultMaterialRefId = defaultMaterialAsIs
                            ? newMaterialHolder.CopyComponent((AssetProvider<Material>)statueMaterial!).ReferenceID
                            : MaterialHelpers.CreateStatueMaterial(normalMaterial!, statueMaterial!, newMaterialHolder).ReferenceID;

                        // Assigns Statue.Material.Assigned to equality
                        var assignedMaterialDriver = newMaterialHolder.AttachComponent<DynamicReferenceVariableDriver<IAssetProvider<Material>>>();
                        assignedMaterialDriver.VariableName.Value = "Avatar/Statue.Material.Assigned";

                        var assignedMaterialField = newMaterialHolder.AttachComponent<ReferenceField<IAssetProvider<Material>>>();
                        assignedMaterialDriver.Target.ForceLink(assignedMaterialField.Reference);

                        // Assigns Statue.Material.Assigned to boolean
                        var bassignedMaterialDriver = newMaterialHolder.AttachComponent<DynamicReferenceVariableDriver<IAssetProvider<Material>>>();
                        bassignedMaterialDriver.VariableName.Value = "Avatar/Statue.Material.Assigned";

                        // Decides whether we use default or assigned
                        var booleanReferenceDriver = newMaterialHolder.AttachComponent<BooleanReferenceDriver<IAssetProvider<Material>>>();
                        booleanReferenceDriver.TrueTarget.Value = newDefaultMaterialRefId;
                        bassignedMaterialDriver.Target.ForceLink(booleanReferenceDriver.FalseTarget);

                        // Checks if assigned material is null and writes that value to boolean ref driver
                        var equalityDriver = newMaterialHolder.AttachComponent<ReferenceEqualityDriver<IAssetProvider<Material>>>();
                        equalityDriver.TargetReference.Target = assignedMaterialField.Reference;
                        equalityDriver.Target.ForceLink(booleanReferenceDriver.State);

                        // boolean ref driver drives this, which drives everything else
                        var multiDriver = newMaterialHolder.AttachComponent<ReferenceMultiDriver<IAssetProvider<Material>>>();

                        if (map.MaterialSets[set][slot].Clothes)
                        {
                            var dynVarDriver = newMaterialHolder.AttachComponent<DynamicValueVariableDriver<int>>();
                            dynVarDriver.VariableName.Value = "Avatar/Statue.Clothing.TransitionType";

                            var multiplexer = newMaterialHolder.AttachComponent<ReferenceMultiplexer<IAssetProvider<Material>>>();
                            dynVarDriver.Target.ForceLink(multiplexer.Index);
                            multiplexer.References.Add().Target = normalMaterial!;
                            booleanReferenceDriver.TargetReference.ForceLink(multiplexer.References.Add());
                            multiplexer.References.Add().Target = transparentMaterial!;

                            multiplexer.Target.ForceLink(multiDriver.Reference);
                        }
                        else
                        {
                            booleanReferenceDriver.TargetReference.ForceLink(multiDriver.Reference);
                        }

                        if (_installType == InstallType.Avatar)
                        {
                            // Makes material accessible elsewhere
                            var dynMaterialVariable = newMaterialHolder.AttachComponent<DynamicReferenceVariable<IAssetProvider<Material>>>();
                            dynMaterialVariable.VariableName.Value = $"Avatar/Statue.Material{oldMaterialToStatueMaterialMap.Count}";

                            // Drive that dynvar
                            multiDriver.Drives.Add();
                            multiDriver.Drives[0].ForceLink(dynMaterialVariable.Reference);
                        }

                        oldMaterialToStatueMaterialMap.Add(key, multiDriver);
                    }

                    if (map.StatueMeshRenderer is not null && slot < map.StatueMeshRenderer.Materials.Count)
                    {
                        var drives = oldMaterialToStatueMaterialMap[key].Drives;

                        if (map.StatueMaterialSet is not null)
                        {
                            drives.Add().ForceLink(map.StatueMaterialSet.Sets[set].GetElement(slot));
                        }
                        else
                        {
                            var materialSlot = map.StatueMeshRenderer.Materials.GetElement(slot);
                            var element = materialSlot.ActiveLink as SyncElement;
                            if (element is not null && materialSlot.IsDriven && materialSlot.IsLinked)
                                Log.Warn($"{name} appears to already be driven by {element.Component.ToLongString()}, attempting to set anyway");

                            drives.Add().ForceLink(materialSlot);
                        }
                    }

                    // Thanks Dann :)
                }
            }
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void ChangeMaterialReferences(IAssetProvider<Material> material, IAssetProvider<Material> newMaterial)
    {
        foreach (var map in MeshRenderers.SelectMany(x => x.MaterialSets).SelectMany(x => x))
        {
            // Update material references for normal
            if (map.Normal == material)
                map.Normal = newMaterial;

            // Update material references for statue
            if (map.Statue == material)
                map.Statue = newMaterial;
        }
    }

    private IAssetProvider<Material>? GetDefaultMaterial(IAssetProvider<Material>? defaultMaterial)
    {
        var statue0Material = (IAssetProvider<Material>?)_generatedMaterials?
            .FindChild("Statue Materials")?
            .FindChild("Statue 0")?
            .GetComponent<AssetProvider<Material>>();
        statue0Material ??= (IAssetProvider<Material>?)_generatedMaterials?
            .FindChild("Statue Materials")?
            .FindChild("0: Default")?
            .GetComponent<AssetProvider<Material>>();

        if ((defaultMaterial is null || defaultMaterial.ReferenceID == RefID.Null) && statue0Material is not null)
        {
            defaultMaterial = statue0Material;
            Log.Debug($"Using existing default statue material, {statue0Material.ToShortString()}");
        }
        else if (defaultMaterial is not null && defaultMaterial.ReferenceID != RefID.Null)
        {
            Log.Info($"Using user supplied default statue material, {defaultMaterial.ToShortString()}");
        }
        else
        {
            Log.Warn("Couldn't find a material to use for default statue material");
        }

        return defaultMaterial;
    }

    #endregion Private Methods
}
