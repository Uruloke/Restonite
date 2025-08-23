using FrooxEngine;
using System.Collections.Generic;

namespace Restonite;

internal class MeshRendererMap
{
    public List<List<MaterialMap>> MaterialSets { get; set; } = [];
    public MaterialSet? NormalMaterialSet { get; set; }
    public MeshRenderer? NormalMeshRenderer { get; set; }
    public Slot? NormalSlot { get; set; }
    public MaterialSet? StatueMaterialSet { get; set; }
    public MeshRenderer? StatueMeshRenderer { get; set; }
    public Slot? StatueSlot { get; set; }

    public void UpdateMeshRenderer(Slot statueSlot, MeshRenderer statue)
    {
        Log.Debug($"Mapping {NormalMeshRenderer.ToLongString()} to {statue.ToLongString()}");

        StatueSlot = statueSlot;
        StatueMeshRenderer = statue;

        if (statue.HasMaterialSet(out var statueMaterialSet))
        {
            StatueMaterialSet = statueMaterialSet;
            Log.Debug($"    Statue MeshRenderer has {statueMaterialSet.ToShortString()} with {statueMaterialSet.Sets.Count} sets");
        }
    }
}
