using Elements.Core;
using FrooxEngine;
using System.Linq;

namespace Restonite;

internal static partial class MaterialHelpers
{
    private static void SetupAlphaFadeMultiUvMaterial(PBS_MultiUV_Material oldMaterial, PBS_MultiUV_Material newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFadeAlt(destination, newMaterial.AlphaHandling, newMaterial.AlphaClip, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeDualsidedMaterial(PBS_DualSidedMaterial oldMaterial, PBS_DualSidedMaterial newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFadeAlt(destination, newMaterial.AlphaHandling, newMaterial.AlphaClip, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeVertexColorMaterial(PBS_VertexColor oldMaterial, PBS_VertexColor newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFadeAlt(destination, newMaterial.AlphaHandling, newMaterial.AlphaClip, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeDisplaceMaterial(PBS_DisplaceMaterial oldMaterial, PBS_DisplaceMaterial newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFadeAlt(destination, newMaterial.AlphaHandling, newMaterial.AlphaClip, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeLerpMaterial(PBSLerpMaterial oldMaterial, PBSLerpMaterial newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFade(destination, newMaterial.AlbedoColor0, newMaterial.AlbedoColor1, oldMaterial.BlendMode, newMaterial.BlendMode, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadePBSMaterial(IPBS_Material oldMaterial, PBS_Material newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFade(destination, newMaterial.AlbedoColor, null, oldMaterial.BlendMode, newMaterial.BlendMode, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeUnlitMaterial(UnlitMaterial oldMaterial, UnlitMaterial newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFade(destination, newMaterial.TintColor, null, oldMaterial.BlendMode, newMaterial.BlendMode, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFadeXiexeMaterial(XiexeToonMaterial oldMaterial, XiexeToonMaterial newMaterial, Slot destination)
    {
        FrooxEngine.MaterialHelper.CopyMaterialProperties(oldMaterial, newMaterial);
        SetupAlphaFade(destination, newMaterial.Color, null, oldMaterial.BlendMode, newMaterial.BlendMode, newMaterial.OffsetFactor);
    }

    private static void SetupAlphaFade(Slot destination, Sync<colorX> color0, Sync<colorX>? color1, BlendMode oldBlendMode, Sync<BlendMode> blendMode, Sync<float> offsetFactor)
    {
        var bodyNormalPersistMultiDriver = destination.AttachComponent<ValueMultiDriver<bool>>();
        var bodyNormalPersistDriver = destination.AttachComponent<DynamicValueVariableDriver<bool>>();
        bodyNormalPersistDriver.VariableName.Value = "Avatar/Statue.BodyNormal.Persist";
        bodyNormalPersistDriver.Target.ForceLink(bodyNormalPersistMultiDriver.Value);

        // Save original albedo
        var multiplierGradientDriver0 = destination.AttachComponent<ValueGradientDriver<colorX>>();
        multiplierGradientDriver0.Points.Add().Value.Value = color0.Value;
        multiplierGradientDriver0.Points.Add().Value.Value = color0.Value * new colorX(1.0f, 1.0f, 1.0f, 0.0f);
        multiplierGradientDriver0.Points.Last().Position.Value = 1.0f;

        // Drive gradient driver's progress
        var alphaMultiDriver = destination.AttachComponent<ValueMultiDriver<float>>();
        var alphaDriver = destination.AttachComponent<DynamicValueVariableDriver<float>>();
        alphaDriver.VariableName.Value = "Avatar/Statue.Material.Progress";
        alphaDriver.Target.ForceLink(alphaMultiDriver.Value);
        alphaMultiDriver.Drives.Add().ForceLink(multiplierGradientDriver0.Progress);

        // Gate gradiant driver through BodyNormal.Persist
        var bodyNormalPersistGate0 = destination.AttachComponent<BooleanValueDriver<colorX>>();
        bodyNormalPersistGate0.TargetField.ForceLink(color0);
        bodyNormalPersistGate0.TrueValue.Value = color0.Value;
        multiplierGradientDriver0.Target.ForceLink(bodyNormalPersistGate0.FalseValue);
        bodyNormalPersistMultiDriver.Drives.Add().ForceLink(bodyNormalPersistGate0.State);

        if(color1 is not null)
        {
            // Save original albedo
            var multiplierGradientDriver1 = destination.AttachComponent<ValueGradientDriver<colorX>>();
            multiplierGradientDriver1.Points.Add().Value.Value = color1.Value;
            multiplierGradientDriver1.Points.Add().Value.Value = color1.Value * new colorX(1.0f, 1.0f, 1.0f, 0.0f);
            multiplierGradientDriver1.Points.Last().Position.Value = 1.0f;

            // Drive gradient driver's progress
            alphaMultiDriver.Drives.Add().ForceLink(multiplierGradientDriver1.Progress);

            // Gate gradiant driver through BodyNormal.Persist
            var bodyNormalPersistGate1 = destination.AttachComponent<BooleanValueDriver<colorX>>();
            bodyNormalPersistGate1.TargetField.ForceLink(color1);
            bodyNormalPersistGate1.TrueValue.Value = color1.Value;
            multiplierGradientDriver1.Target.ForceLink(bodyNormalPersistGate1.FalseValue);
            bodyNormalPersistMultiDriver.Drives.Add().ForceLink(bodyNormalPersistGate1.State);
        }

        // Drive blendmode of material
        var blendModeDriver = destination.AttachComponent<DynamicValueVariableDriver<BlendMode>>();
        blendModeDriver.VariableName.Value = "Avatar/Statue.BlendMode";
        var blendModeActiveDriver = destination.AttachComponent<DynamicValueVariableDriver<bool>>();
        blendModeActiveDriver.VariableName.Value = "Avatar/Statue.BodyNormal.GreaterThan0";
        var blendModeBoolDriver = destination.AttachComponent<BooleanValueDriver<BlendMode>>();
        blendModeBoolDriver.TargetField.ForceLink(blendMode);

        // Save original blend mode
        blendModeBoolDriver.FalseValue.Value = oldBlendMode;
        blendModeDriver.Target.ForceLink(blendModeBoolDriver.TrueValue);

        // Gate blendmode through BodyNormal.Persist
        var bodyNormalPersist = destination.AttachComponent<ValueField<bool>>();
        bodyNormalPersistMultiDriver.Drives.Add().ForceLink(bodyNormalPersist.Value);
        var bodyNormalGreaterThan0 = destination.AttachComponent<ValueField<bool>>();
        blendModeActiveDriver.Target.ForceLink(bodyNormalGreaterThan0.Value);

        var bodyNormalConditionDriver = destination.AttachComponent<MultiBoolConditionDriver>();
        var condition1 = bodyNormalConditionDriver.Conditions.Add();
        condition1.Field.Value = bodyNormalPersist.Value.ReferenceID;
        condition1.Invert.Value = true;
        var condition2 = bodyNormalConditionDriver.Conditions.Add();
        condition2.Field.Value = bodyNormalGreaterThan0.Value.ReferenceID;
        bodyNormalConditionDriver.Target.ForceLink(blendModeBoolDriver.State);

        offsetFactor.Value = -0.1f;
    }

    private static void SetupAlphaFadeAlt(Slot destination, Sync<AlphaHandling> alphaHandling, Sync<float> alphaClip, Sync<float> offsetFactor)
    {
        var bodyNormalPersistDriver = destination.AttachComponent<DynamicValueVariableDriver<bool>>();
        bodyNormalPersistDriver.VariableName.Value = "Avatar/Statue.BodyNormal.Persist";

        var alphaDriver = destination.AttachComponent<DynamicValueVariableDriver<float>>();
        alphaDriver.VariableName.Value = "Avatar/Statue.Material.Progress";

        // Gate alpha driver through BodyNormal.Persist
        var bodyNormalPersistGate = destination.AttachComponent<BooleanValueDriver<float>>();
        bodyNormalPersistGate.TargetField.ForceLink(alphaClip);
        bodyNormalPersistGate.TrueValue.Value = 0.0f;

        alphaDriver.Target.ForceLink(bodyNormalPersistGate.FalseValue);
        bodyNormalPersistDriver.Target.ForceLink(bodyNormalPersistGate.State);

        alphaHandling.Value = AlphaHandling.AlphaBlend;
        offsetFactor.Value = -0.1f;
    }
}
