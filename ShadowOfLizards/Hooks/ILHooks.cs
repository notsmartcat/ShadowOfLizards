using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class ILHooks
{
    public static void Apply()
    {
        IL.Lizard.ctor += ILLizard;
        IL.Lizard.SpearStick += ILSpearStick;

        IL.LizardAI.Update += ILLizardAI;

        IL.WormGrass.WormGrassPatch.InteractWithCreature += ILInteractWithCreature1;

        IL.DaddyCorruption.EatenCreature.Update += ILDaddyCorruptionEatenCreature;

        IL.Lizard.SwimBehavior += ILLizardSwimBehavior;
    }

    static void ILLizard(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[4]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<Lizard>(x, "lizardParams"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardBreedParams>(x, "tongue"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.Emit<ILHooks>(OpCodes.Call, "ShadowOfLizardCtor");
                val.Emit(OpCodes.Brfalse_S, target);
                ShadowOfLizards.Logger.LogInfo(all + "lizard success");
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "lizard fail");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardCtor(AbstractCreature liz)
    {
        if (ShadowOfOptions.tongue_stuff.Value && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz.TryGetValue("Tongue", out string tongue) && (tongue == "Null" || tongue == "get"))
        {
            return false;
        }

        return true;
    }

    static void ILSpearStick(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(0, new Func<Instruction, bool>[5]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<Lizard>(x, "jumpModule"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardJumpModule>(x, "gasLeakPower"),
            (Instruction x) => ILPatternMatchingExt.MatchLdcR4(x, 0f),
            (Instruction x) => ILPatternMatchingExt.MatchBleUn(x, out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_0);
                val.Emit<Lizard>(OpCodes.Ldfld, "jumpModule");
                val.Emit(OpCodes.Brfalse, target);
                ShadowOfLizards.Logger.LogInfo(all + "spear stick success");
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "spear stick fail");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void ILLizardAI(ILContext il)
    {
        ILCursor val = new(il);
        ILLabel target = null;
        if (val.TryGotoNext(new Func<Instruction, bool>[4]
        {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI>(x, "redSpitAI"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI.LizardSpitTracker>(x, "spitting"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
        }))
        {
            int index = val.Index;
            val.Index = index + 1;
            val.Emit<LizardAI>(OpCodes.Ldfld, "redSpitAI");
            val.Emit(OpCodes.Brfalse_S, target);
            val.Emit(OpCodes.Ldarg_0);
            ShadowOfLizards.Logger.LogInfo(all + "Lizard Spit success");
        }
        else
        {
            ShadowOfLizards.Logger.LogInfo(all + "Lizard Spit fail");
        }
    }

    static void ILInteractWithCreature1(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(0, new Func<Instruction, bool>[4]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 1),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<WormGrass.WormGrassPatch.CreatureAndPull>(x, "creature"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<UpdatableAndDeletable>(x, "Destroy"),
            (Instruction x) => ILPatternMatchingExt.MatchRet(x)
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.EmitDelegate(ShadowOfInteractWithCreature);
                ShadowOfLizards.Logger.LogInfo(all + "worm grass success");
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "worm grass fail");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static void ShadowOfInteractWithCreature(WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull)
    {
        if (!ShadowOfOptions.grass_immune.Value || creatureAndPull.creature == null || creatureAndPull.creature is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || liz.Template.wormGrassImmune)
        {
            return;
        }

        if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.grass_immune_chance.Value)
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "WormGrass Immune granted to " + liz);

            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                data.cheatDeathChance += 5;

            data.liz["WormGrassImmune"] = "True";
            data.lastDamageType = null;
        }
        else if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "WormGrass Immune not granted to " + liz);
    }

    static void ILDaddyCorruptionEatenCreature(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[3]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<DaddyCorruption.EatenCreature>(x, "creature"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "Die")
            }))
            {
                val.Emit(OpCodes.Ldarg_0);
                val.EmitDelegate(ShadowOfDaddyCorruptionEatenCreature);
                ShadowOfLizards.Logger.LogInfo(all + "daddy corruption success");
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "daddy corruption fail");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static void ShadowOfDaddyCorruptionEatenCreature(DaddyCorruption.EatenCreature eatenCreature)
    {
        if (!ShadowOfOptions.tentacle_immune.Value || eatenCreature.creature == null || eatenCreature.creature is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || liz.abstractCreature.tentacleImmune)
        {
            return;
        }

        if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tentacle_immune_chance.Value)
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Rot Tentacle Immune granted to " + liz);

            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                data.cheatDeathChance += 5;

            data.liz["TentacleImmune"] = "True";
            data.lastDamageType = null;
        }
        else if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "Rot Tentacle Immune not granted to " + liz);
    }

    static void ILLizardSwimBehavior(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[6]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_Template"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
            (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "Salamander"),
            (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Inequality"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_0);
                val.EmitDelegate(ShadowOfLizardSwimBehavior);
                val.Emit(OpCodes.Brtrue, target);

                ShadowOfLizards.Logger.LogInfo(all + "swim behaviour 1 success");

                if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[6]
                {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "Salamander"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrtrue(x, out target)
                }))
                {
                    val.Emit(OpCodes.Ldarg_0);
                    val.EmitDelegate(ShadowOfLizardSwimBehavior);
                    val.Emit(OpCodes.Brtrue, target);

                    ShadowOfLizards.Logger.LogInfo(all + "swim behaviour 2 success");
                }
                else
                {
                    ShadowOfLizards.Logger.LogInfo(all + "swim behaviour 2 fail");
                }
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "swim behaviour 1 fail");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardSwimBehavior(Creature self)
    {
        if (self.Template.canSwim && (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious || self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly))
        {
            return true;
        }
        return false;
    }
}

