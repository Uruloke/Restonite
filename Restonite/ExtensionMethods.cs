using FrooxEngine;
using FrooxEngine.UIX;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using Elements.Assets;

namespace Restonite;

internal static class ExtensionMethods
{
    public static int CommonStartSubstring(this string a, string b)
    {
        var commonLength = 0;

        for (var i = 0; i < a.Length && i < b.Length; i++)
        {
            if (a[i] == b[i])
                commonLength++;
            else
                break;
        }

        return commonLength;
    }

    public static Slot? FindSlot(this List<Slot> slots, Predicate<Slot> predicate, string? name = null, string? tag = null)
    {
        foreach (var slot in slots)
        {
            if (((name is null && tag is null) || (tag is not null && slot.Tag == tag) || (name is not null && slot.Name == name)) && predicate(slot))
                return slot;
        }

        return null;
    }

    public static bool HasMaterialSet(this MeshRenderer? renderer, [NotNullWhen(true)] out MaterialSet? materialSet)
    {
        if (renderer?.Materials.IsDriven == true && renderer.Materials.IsLinked)
        {
            var element = renderer.Materials.ActiveLink as SyncElement;
            if (element?.Component is MaterialSet set)
            {
                materialSet = set;
                return true;
            }
        }

        materialSet = null;
        return false;
    }

    public static bool IsDrivenByKnownStatueDriver(this Sync<bool> field)
    {
        if (field.IsDriven && field.IsLinked && field.ActiveLink is SyncElement element)
        {
            if (element.Slot.Name is "Avatar/Statue.BodyStatue" or "Body Statue Active")
                return true;

            var dynVar = element.Slot.GetComponent<DynamicValueVariableDriver<bool>>(x => x.VariableName == "Avatar/Statue.BodyStatue");
            if (dynVar is not null)
                return true;

            if (element.Component is ValueMultiDriver<bool> multiDriver)
                return IsDrivenByKnownStatueDriver(multiDriver.Value);
        }

        return false;
    }

    public static bool IsSnappable(this Slot slot)
    {
        var snapper = slot.GetComponentInParents<Snapper>();
        var snapTarget = slot.GetComponentInParents<SnapTarget>();

        return snapper is not null && snapTarget is not null && snapper.Slot.Parent == snapTarget.Slot;
    }

    public static void CopySyncDrivers(this Component source, Component target)
    {
        for(var i = 0; i < source.SyncMemberCount; i++)
        {
            var sourceSyncMember = source.GetSyncMember(i);

            // Find matching member in destination
            for(var j = 0; j < target.SyncMemberCount; j++)
            {
                var targetSyncMember = target.GetSyncMember(j);

                if (sourceSyncMember.Name == targetSyncMember.Name && sourceSyncMember.GetType() == targetSyncMember.GetType()
                    && sourceSyncMember.IsDriven && sourceSyncMember.IsLinked && !targetSyncMember.IsDriven && !targetSyncMember.IsLinked)
                {
                    if (sourceSyncMember is IField sourceField && targetSyncMember is IField targetField)
                    {
                        targetField.DriveFrom(sourceField);
                    }
                    else if (sourceSyncMember is ISyncRef sourceRef && targetSyncMember is ISyncRef targetRef)
                    {
                        targetRef.DriveFromRef(sourceRef);
                    }
                }
            }
        }
    }

    public static void Setup(this EnumMemberEditor editor, IField target)
    {
        var ui = new UIBuilder(editor.Slot);
        RadiantUI_Constants.SetupEditorStyle(ui);
        editor.Setup(target, null!, ui);
    }

    public static string ToLongString(this Component? component)
    {
        if (component is null)
            return "<color=hero.orange>null</color>";
        else
            return $"<color=hero.purple>{component.GetType().Name}</color> <color=gray>[{component.ReferenceID}]</color> on <color=hero.yellow>{component.Slot.Name.StripRTFTags()}</color>";
    }

    public static string ToLongString(this IAssetProvider<Material>? material)
    {
        if (material is null)
            return "<color=hero.orange>null</color>";
        else
            return $"<color=hero.purple>{material.GetType().Name}</color> <color=gray>[{material.ReferenceID}]</color> on <color=hero.yellow>{material.Slot.Name.StripRTFTags()}</color>";
    }

    public static string StripRTFTags(this string text)
    {
        return new StringRenderTree(text).GetRawString();
    }

    public static string ToNormalLineEndings(this string text)
    {
        return text.Replace("<br>", "\r\n");
    }

    public static string ToShortString(this Component? component)
    {
        if (component is null)
            return "<color=hero.orange>null</color>";
        else
            return $"<color=hero.purple>{component.GetType().Name}</color> <color=gray>[{component.ReferenceID}]</color>";
    }

    public static string ToShortString(this IAssetProvider<Material>? material)
    {
        if (material is null)
            return "<color=hero.orange>null</color>";
        else
            return $"<color=hero.purple>{material.GetType().Name}</color> <color=gray>[{material.ReferenceID}]</color>";
    }

    public static string ToShortString(this Slot? slot)
    {
        if (slot is null)
            return "<color=hero.orange>null</color>";
        else
            return $"<color=hero.yellow>{slot.Name.StripRTFTags()}</color> <color=gray>[{slot.ReferenceID}]</color>";
    }

    public static string ToUixLineEndings(this string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>");
    }
}
