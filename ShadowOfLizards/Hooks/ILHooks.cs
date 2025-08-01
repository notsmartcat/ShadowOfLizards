using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class ILHooks
{
    public static void Apply()
    {
        IL.Creature.Update += Creature_Update;

        IL.Lizard.ctor += ILLizard; //Tongue

        IL.Lizard.SpearStick += ILSpearStick; //Jump

        IL.LizardAI.Update += ILLizardAI; //Spit

        IL.WormGrass.WormGrassPatch.InteractWithCreature += ILInteractWithCreature; //WormGrass

        IL.DaddyCorruption.EatenCreature.Update += ILDaddyCorruptionEatenCreature; //Rot
        IL.DaddyLongLegs.Eat += ILLDaddyLongLegsEat; //Rot

        IL.Lizard.SwimBehavior += ILLizardSwimBehavior; //Swim

        IL.Lizard.Update += ILLizardUpdate; //Breathing

        On.LizardAI.LurkTracker.Utility += ShadowOfLizardAILurkTrackerUtility; //Both Swim/Breathe and Camo Related

        IL.LizardAI.LurkTracker.LurkPosScore += ILLurkTrackerLurkPosScore;

        IL.LizardGraphics.DrawSprites += ILLizardGraphicsDrawSprites; //camo
        IL.LizardGraphics.Update += ILLizardGraphicsUpdate; //camo

        new Hook( //Camo
            typeof(Lizard).GetProperty(nameof(Lizard.VisibilityBonus)).GetGetMethod(),
            typeof(ILHooks).GetMethod(nameof(ShadowOfLizardVisibilityBonus)));

        new Hook( //TotalMass
            typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.TotalMass)).GetGetMethod(),
            typeof(ILHooks).GetMethod(nameof(ShadowOfTotalMass)));
    }

    #region Creature_Update
    public static void Creature_Update(ILContext il)
    {
        ILCursor c = new(il);
        try
        {
            c.GotoNext(new Func<Instruction, bool>[6]
            {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdnull(x),
                (Instruction x) => ILPatternMatchingExt.MatchLdcR4(x, 0f),
                (Instruction x) => ILPatternMatchingExt.MatchLdcR4(x, 5f),
                (Instruction x) => ILPatternMatchingExt.MatchNewobj<Vector2>(x),
                (Instruction x) => ILPatternMatchingExt.MatchNewobj<Vector2?>(x)
            });
            try
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ILPreViolenceCheck);
            }
            catch (Exception)
            {
                ShadowOfLizards.Logger.LogError(all + "Failed to inject for Pre-Fluid Hazard Violence!");
            }
        }
        catch (Exception ex2)
        {
            ShadowOfLizards.Logger.LogError(all + "Could not find match for Pre-Fluid Hazard Violence!");
            ShadowOfLizards.Logger.LogError(ex2);
        }
        try
        {
            c.GotoNext(new Func<Instruction, bool>[1]
            {
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "Violence")
            });
            try
            {
                int index = c.Index;
                c.Index = index + 1;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Creature>>((Action<Creature>)delegate (Creature creature)
                {
                    ILPostViolenceCheck(creature, "Melted");
                });
            }
            catch (Exception)
            {
                ShadowOfLizards.Logger.LogError(all + "Failed to inject for Post-Fluid Hazard Violence!");
            }
        }
        catch (Exception ex4)
        {
            ShadowOfLizards.Logger.LogError(all + "Could not find match for Post-Fluid Hazard Violence!");
            ShadowOfLizards.Logger.LogError(ex4);
        }
        try
        {
            c.GotoNext(new Func<Instruction, bool>[10]
            {
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_dead"),
                (Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Brtrue_S),
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_State"),
                (Instruction x) => ILPatternMatchingExt.MatchIsinst<HealthState>(x),
                (Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Brfalse_S),
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_State"),
                (Instruction x) => ILPatternMatchingExt.MatchIsinst<HealthState>(x),
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<HealthState>(x, "get_health")
            });
            c.GotoNext(new Func<Instruction, bool>[1]
            {
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "Die")
            });
            try
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Creature>>((Action<Creature>)delegate (Creature creature)
                {
                    if (!creature.dead)
                    {
                        TryAddKillFeedEntry(creature, "Bleed");
                    }
                });
            }
            catch (Exception)
            {
                ShadowOfLizards.Logger.LogError(all + "Failed to inject for Bleed Out!");
            }
        }
        catch (Exception ex6)
        {
            ShadowOfLizards.Logger.LogError(all + "Could not find match for Bleed Out!");
            ShadowOfLizards.Logger.LogError(ex6);
        }
        try
        {
            c.GotoNext(new Func<Instruction, bool>[8]
            {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<PhysicalObject>(x, "get_bodyChunks"),
                (Instruction x) => ILPatternMatchingExt.MatchLdcI4(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdelemRef(x),
                (Instruction x) => ILPatternMatchingExt.MatchLdflda<BodyChunk>(x, "pos"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<Vector2>(x, "y"),
                (Instruction x) => ILPatternMatchingExt.MatchLdloc(x, 0),
                (Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Bge_Un)
            });
            c.GotoNext(new Func<Instruction, bool>[4]
            {
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<ModManager>(x, "CoopAvailable"),
                (Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Brfalse_S),
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_State")
            });
            int index = c.Index;
            c.Index = index + 1;
            try
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Creature>>((Action<Creature>)delegate (Creature creature)
                {
                    if (!creature.dead)
                    {
                        TryAddKillFeedEntry(creature, "Fell");
                    }
                });
            }
            catch (Exception)
            {
                ShadowOfLizards.Logger.LogError(all + "Failed to inject for Death Fall!");
            }
        }
        catch (Exception ex8)
        {
            ShadowOfLizards.Logger.LogError(all + "Could not find match for Death Fall!");
            ShadowOfLizards.Logger.LogError(ex8);
        }
    }

    public static void ILPreViolenceCheck(Creature receiver)
    {
        if (receiver != null && receiver is Lizard liz && liz.abstractCreature != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            PreViolenceCheck(liz, data);
        }
    }

    public static void ILPostViolenceCheck(Creature receiver, string killType)
    {
        if (receiver != null && receiver is Lizard liz && liz.abstractCreature != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            PostViolenceCheck(liz, data, killType, null);
        }
    }

    public static void TryAddKillFeedEntry(Creature receiver, string killType)
    {
        if (receiver != null && receiver is Lizard liz && liz.abstractCreature != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            ViolenceCheck(liz, data, killType, null);
        }
    }
    #endregion

    #region Tongue
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
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizard!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardCtor(AbstractCreature liz)
    {
        if (ShadowOfOptions.tongue_ability.Value && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz.TryGetValue("Tongue", out string tongue) && (tongue == "Null" || tongue == "get"))
        {
            return false;
        }

        return true;
    }
    #endregion

    #region Jump
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
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILSpearStick!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region Spit
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
        }
        else
        {
            ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardAI!");
        }
    }
    #endregion

    #region WormGrass
    static void ILInteractWithCreature(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
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
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILILInteractWithCreature!");
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

        if (Chance(liz, ShadowOfOptions.grass_immune_chance.Value, "WormGrass Immune"))
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "WormGrass Immune granted to " + liz);

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance += 5;

            data.liz["WormGrassImmune"] = "True";
            data.lastDamageType = null;
        }
        else if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "WormGrass Immune not granted to " + liz);
    }
    #endregion

    #region Rot
    static void ILDaddyCorruptionEatenCreature(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[3]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<DaddyCorruption.EatenCreature>(x, "creature"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "Die")
            }))
            {
                val.Emit(OpCodes.Ldarg_0);
                val.Emit<DaddyCorruption.EatenCreature>(OpCodes.Ldfld, "creature");
                val.EmitDelegate(ShadowOfDaddyCorruptionEatenCreature);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILDaddyCorruptionEatenCreature!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    static void ILLDaddyLongLegsEat(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[7]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<DaddyLongLegs>(x, "eatObjects"),
            (Instruction x) => ILPatternMatchingExt.MatchLdloc(x, 1),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt(x, typeof(List<DaddyLongLegs.EatObject>).GetMethod("get_Item")),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<DaddyLongLegs.EatObject>(x, "chunk"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<BodyChunk>(x, "get_owner"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<UpdatableAndDeletable>(x, "Destroy")
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<DaddyLongLegs>(OpCodes.Ldfld, "eatObjects");
                val.Emit(OpCodes.Ldloc_1);
                val.Emit(OpCodes.Callvirt, typeof(List<DaddyLongLegs.EatObject>).GetMethod("get_Item"));
                val.Emit<DaddyLongLegs.EatObject>(OpCodes.Ldfld, "chunk");
                val.Emit<BodyChunk>(OpCodes.Callvirt, "get_owner");
                val.Emit(OpCodes.Isinst, typeof(Creature));
                val.EmitDelegate(ShadowOfDaddyCorruptionEatenCreature);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLDaddyLongLegsEat!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static void ShadowOfDaddyCorruptionEatenCreature(Creature eatenCreature)
    {
        if (!ShadowOfOptions.tentacle_immune.Value || eatenCreature == null || eatenCreature is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || liz.abstractCreature.tentacleImmune)
        {
            return;
        }

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "Rot Tentacle Immune granted to " + liz);

        if (ShadowOfOptions.dynamic_cheat_death.Value)
            data.cheatDeathChance += 5;

        data.liz["TentacleImmune"] = "True";
        data.lastDamageType = null;

    }
    #endregion

    #region WaterRelated
    #region Swim
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
                val.Emit(OpCodes.Brfalse, target);

                val.Emit(OpCodes.Ldarg_0);
                val.EmitDelegate(ShadowOfLizardSwimBehavior);
                val.Emit(OpCodes.Brtrue, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardSwimBehavior flag!");
            }
            
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

                val.Emit(OpCodes.Ldarg_0);
                val.EmitDelegate(ShadowOfLizardSwimBehavior);
                val.Emit(OpCodes.Brfalse, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardSwimBehavior!");
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
    #endregion

    #region Breathing
    static void ILLizardUpdate(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;

            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[2]
            {
            (Instruction x) => ILPatternMatchingExt.MatchCall<ModManager>(x, "get_DLCShared"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
            }))
            {
                if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[6]
                {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchCall<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "Salamander"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrtrue(x, out target2)
                }))
                {
                    val.MoveAfterLabels();

                    val.Emit(OpCodes.Ldarg_0);
                    val.EmitDelegate(ShadowOfLizardUpdate);
                    val.Emit(OpCodes.Brtrue, target2);

                    val.Emit(OpCodes.Ldarg_0);
                    val.EmitDelegate(ShadowOfLizardSalamanderUpdate);
                    val.Emit(OpCodes.Brfalse, target);
                }
                else
                {
                    ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardUpdate!");
                }
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardUpdate Eel!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardUpdate(Creature self)
    {
        if (ShadowOfOptions.water_breather.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather == "True")
        {
            return true;
        }
        return false;
    }
    public static bool ShadowOfLizardSalamanderUpdate(Creature self)
    {
        if (ShadowOfOptions.water_breather.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather != "True")
        {
            return false;
        }
        return true;
    }
    #endregion
    #endregion

    static float ShadowOfLizardAILurkTrackerUtility(On.LizardAI.LurkTracker.orig_Utility orig, LizardAI.LurkTracker self) //Both Swim/Breathe and Camo Related
    {
        if (lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            if (ShadowOfOptions.swim_ability.Value && data.liz.TryGetValue("CanSwim", out _) || ShadowOfOptions.water_breather.Value && data.liz.TryGetValue("WaterBreather", out _))
            {
                bool canLurk = data.liz.TryGetValue("CanSwim", out string CanSwim) && CanSwim != "True" || data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather != "True";

                if (!canLurk)
                {
                    if (ShadowOfOptions.camo_ability.Value && data.liz.TryGetValue("CanCamo", out string CanCamo))
                    {
                        return CanCamo == "True" ? 0.5f : 0f;
                    }
                    return 0f;
                }
                if (self.LurkPosScore(self.lurkPosition) <= 0f)
                {
                    return 0f;
                }
                if (!self.lizard.room.GetTile(self.lurkPosition).AnyWater)
                {
                    return 0.2f;
                }
            }
            else if (ShadowOfOptions.camo_ability.Value && data.liz.TryGetValue("CanCamo", out string CanCamo))
            {
                return CanCamo == "True" ? 0.5f : 0f;
            }
        }
        return orig(self);
    }

    static void ILLurkTrackerLurkPosScore(ILContext il) //Both Swim/Breathe and Camo Related
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[7]
            {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI.LurkTracker>(x, "lizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "WhiteLizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardCamoLurkPosScore);
                val.Emit(OpCodes.Brtrue_S, target);

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardCamoLurkPosScore);
                val.Emit(OpCodes.Brfalse_S, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore White!");
            }

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[2]
            {
                (Instruction x) => ILPatternMatchingExt.MatchCall<ModManager>(x, "get_DLCShared"),
                (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target2)
            }))
            {}
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore Eel!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
            {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI.LurkTracker>(x, "lizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "Salamander"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrtrue(x, out target)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardSwimLurkPosScore);
                val.Emit(OpCodes.Brtrue_S, target);

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardSwimLurkPosScore);
                val.Emit(OpCodes.Brfalse, target2);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardSwimLurkPosScore(Creature self)
    {
        if (self.Template.canSwim && (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious || self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly))
        {
            if (ShadowOfOptions.water_breather.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("WaterBreather", out string WaterBreather))
            {
                return WaterBreather == "True";
            }
            else if (self.Template.type == CreatureTemplate.Type.Salamander || (ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.EelLizard))
            {
                return true;
            }
        }
        return false;
    }
    public static bool ShadowOfLizardCamoLurkPosScore(Creature self)
    {
        if (ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            if (!data.liz.TryGetValue("CanCamo", out string CanCamo) && self.Template.type == CreatureTemplate.Type.WhiteLizard || CanCamo == "True")
            {
                return true;
            }
        }
        return false;
    }

    #region Camo
    static void ILLizardGraphicsDrawSprites(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;
            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[2]
            {
            (Instruction x) => ILPatternMatchingExt.MatchCall<ModManager>(x, "get_DLCShared"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse (x, out target2)
            }))
            {
                if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
                {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardGraphics>(x, "lizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "WhiteLizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrtrue(x, out target)
                }))
                {
                    val.MoveAfterLabels();

                    val.Emit(OpCodes.Ldarg_0);
                    val.Emit<LizardGraphics>(OpCodes.Ldfld, "lizard");
                    val.EmitDelegate(ShadowOfLizardGraphicsDrawSprites);
                    val.Emit(OpCodes.Brtrue_S, target);

                    val.Emit(OpCodes.Ldarg_0);
                    val.Emit<LizardGraphics>(OpCodes.Ldfld, "lizard");
                    val.EmitDelegate(ShadowOfLizardGraphicsDrawSprites);
                    val.Emit(OpCodes.Brfalse_S, target2);
                }
                else
                {
                    ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardGraphicsDrawSprites!");
                }
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardGraphicsDrawSprites Zoop!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    static void ILLizardGraphicsUpdate(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;

            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[4]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardGraphics>(x, "lizard"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "get_dead"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target2)
            }))
            {
                if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
                {
                (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardGraphics>(x, "lizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCallvirt<Creature>(x, "get_Template"),
                (Instruction x) => ILPatternMatchingExt.MatchLdfld<CreatureTemplate>(x, "type"),
                (Instruction x) => ILPatternMatchingExt.MatchLdsfld<CreatureTemplate.Type>(x, "WhiteLizard"),
                (Instruction x) => ILPatternMatchingExt.MatchCall(x, "ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
                }))
                {
                    val.MoveAfterLabels();

                    val.Emit(OpCodes.Ldarg_0);
                    val.Emit<LizardGraphics>(OpCodes.Ldfld, "lizard");
                    val.EmitDelegate(ShadowOfLizardGraphicsDrawSprites);
                    val.Emit(OpCodes.Brtrue_S, target2);

                    val.Emit(OpCodes.Ldarg_0);
                    val.Emit<LizardGraphics>(OpCodes.Ldfld, "lizard");
                    val.EmitDelegate(ShadowOfLizardGraphicsDrawSprites);
                    val.Emit(OpCodes.Brfalse, target);
                }
                else
                {
                    ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardGraphicsUpdate!");
                }
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardGraphicsUpdate Zoop");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static bool ShadowOfLizardGraphicsDrawSprites(Creature self)
    {
        if (ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo == "True")
        {
            return true;
        }
        return false;
    }
    /* Unused Code
    public static bool ShadowOfLizardWhiteGraphicsDrawSprites(Creature self)
    {
        if (ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo != "True")
        {
            return false;
        }
        return true;
    }
    public static bool ShadowOfLizardZoopGraphicsDrawSprites(Creature self)
    {
        if (ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo != "True" && (!ModManager.DLCShared || self.Template.type != DLCSharedEnums.CreatureTemplateType.ZoopLizard))
        {
            return false;
        }
        return true;
    }
    */
    public static float ShadowOfLizardVisibilityBonus(Func<Lizard, float> orig, Lizard self)
    {
        try
        {
            if (ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("CanCamo", out string CanCamo))
            {
                return CanCamo == "True" ? -(self.graphicsModule as LizardGraphics).Camouflaged : 0f;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
        return orig(self);
    }
    #endregion

    public static float ShadowOfTotalMass(Func<PhysicalObject, float> orig, PhysicalObject self)
    {
        try
        {
            if (self is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.availableBodychunks.Count != self.bodyChunks.Length)
            {
                float num = 0f;
                for (int i = 0; i < data.availableBodychunks.Count; i++)
                {
                    num += self.bodyChunks[data.availableBodychunks[i]].mass;
                }
                return num;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
        return orig(self);
    }
}
/*
class ILHooks
{
	public static bool ShadowOfLizardUpdate(Creature self)
    {
        return true;
    }
	public static bool ShadowOfLizardSalamanderUpdate(Creature self)
    {
        return true;
    }
}

class ILHooks
{
	public static bool ShadowOfLizardGraphicsDrawSprites(Creature self)
    {
        return true;
    }
	public static bool ShadowOfLizardWhiteGraphicsDrawSprites(Creature self)
    {
        return true;
    }
}
*/