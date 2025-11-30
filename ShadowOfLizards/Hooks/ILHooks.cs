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
        IL.Creature.Update += ILCreatureUpdate;

        IL.DaddyTentacle.Update += ILDaddyTentacleUpdate;

        IL.Watcher.LizardRotModule.ctor += ILNewLizardRotModule;

        IL.Lizard.ctor += ILNewLizard; //Tongue

        IL.Lizard.SpearStick += ILSpearStick; //Jump

        /*
        IL.LizardAI.ctor += NewLizardAI; //Spit

        IL.LizardAI.Update += ILLizardAIUpdate; //Spit
        */

        IL.WormGrass.WormGrassPatch.InteractWithCreature += ILInteractWithCreature; //WormGrass

        IL.DaddyCorruption.EatenCreature.Update += ILDaddyCorruptionEatenCreature; //Rot
        IL.DaddyLongLegs.Eat += ILLDaddyLongLegsEat; //Rot

        IL.Lizard.Update += ILLizardUpdate; //Breathing

        IL.Leech.Attached += ILLeechAttached;

        On.LizardAI.LurkTracker.Utility += ShadowOfLizardAILurkTrackerUtility; //Both Swim/Breathe and Camo Related

        IL.LizardAI.LurkTracker.LurkPosScore += ILLurkTrackerLurkPosScore;

        IL.LizardGraphics.DrawSprites += ILLizardGraphicsDrawSprites; //camo
        IL.LizardGraphics.Update += ILLizardGraphicsUpdate; //camo

        IL.AbstractCreature.IsEnteringDen += ILAbstractCreatureIsEnteringDen;

        new Hook( //Camo
            typeof(Lizard).GetProperty(nameof(Lizard.VisibilityBonus)).GetGetMethod(), ShadowOfLizardVisibilityBonus);

        new Hook( //TotalMass
            typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.TotalMass)).GetGetMethod(), ShadowOfTotalMass);

        new Hook( //Swimmer
            typeof(Lizard).GetProperty(nameof(Lizard.Swimmer)).GetGetMethod(), ShadowOfLizardSwimmer);

        new Hook(typeof(Lizard).GetProperty(nameof(Lizard.IsWallClimber)).GetGetMethod(), ShadowOfIsWallClimber);
    }

    private static void ILAbstractCreatureIsEnteringDen(ILContext il)
    {
        ILCursor val = new(il);
        if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[2]
        {
            x => x.MatchIsinst<AbstractCreature>(),
            x => x.MatchCallvirt<AbstractCreature>("Die")
        }))
        {
            val.MoveAfterLabels();

            val.Emit(OpCodes.Ldarg_0);
            val.Emit(OpCodes.Ldloc_0);
            val.EmitDelegate(EatRegrowth);
        }
        else
        {
            ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILAbstractCreatureIsEnteringDen!");
        }
    }

    public static void AbstractCreatureIsEnteringDen(AbstractCreature self, int i)
    {
        Debug.Log("self is = " + self + " other is = " + (self.stuckObjects[i].B as AbstractCreature));
    }


    static void ILNewLizardRotModule(ILContext il)
    {
        ILCursor val = new(il);
        if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[2]
        {
            x => x.MatchDiv(),
            x => x.MatchCallvirt(typeof(List<float>).GetMethod("Add")),
        }))
        {
            val.Emit(OpCodes.Ldarg_0);
            val.Emit(OpCodes.Ldloc_1);
            val.Emit(OpCodes.Ldloc_2);
            val.EmitDelegate(ShadowOfNewLizardRotModule);
        }
        else
        {
            ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILNewLizardRotModule!");
        }
    }
    public static void ShadowOfNewLizardRotModule(Watcher.LizardRotModule self, List<float> list, int num)
    {
        if (self.lizard != null && self.lizard.abstractCreature != null && lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            if (data.cutAppendage.ContainsKey(num) && data.cutAppendageCycle.ContainsKey(num) && data.cutAppendageCycle[num] != CycleNum(self.lizard.abstractCreature))
            {
                float listNum;

                if (ModManager.MMF)
                {
                    listNum = Math.Max(3, list[num] / 40f);
                }
                else
                {
                    listNum = list[num] / 40f;
                }

                float percentage = (float)data.cutAppendage[num] / (float)listNum;

                list[num] *= percentage;
            }
        }
    }

    static void ILDaddyTentacleUpdate(ILContext il)
    {
        ILCursor val = new(il);
        if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[2]
        {  
            x => x.MatchLdcI4(0),
            x => x.MatchStloc(6),
        }))
        {
            val.Emit(OpCodes.Ldarg_0);
            val.EmitDelegate(ShadowOfDaddyTentacleUpdate);
        }
        else
        {
            ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILDaddyTentacleUpdate!");
        }
    }
    public static void ShadowOfDaddyTentacleUpdate(DaddyTentacle self)
    {
        if (self.daddy is Lizard liz && liz.rotModule.tentacles.Length > 0 && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.cutAppendage.Count > 0 && TransformationRot.InnactiveTentacleCheck(data, liz.rotModule.tentacles.IndexOf(self), CycleNum(liz.abstractCreature)))
        {
            self.stun = 0;
            self.limp = true;
        } 
    }

    #region Creature_Update
    public static void ILCreatureUpdate(ILContext il)
    {
        ILCursor c = new(il);
        try
        {
            c.GotoNext(new Func<Instruction, bool>[6]
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdnull(),
                x => x.MatchLdcR4(0f),
                x => x.MatchLdcR4(5f),
                x => x.MatchNewobj<Vector2>(),
                x => x.MatchNewobj<Vector2?>()
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
                x => x.MatchCallvirt<Creature>("Violence")
            });
            try
            {
                int index = c.Index;
                c.Index = index + 1;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(delegate (Creature creature)
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
                x => x.MatchCall<Creature>("get_dead"),
                x => x.Match(OpCodes.Brtrue_S),
                x => x.MatchLdarg(0),
                x => x.MatchCall<Creature>("get_State"),
                x => x.MatchIsinst<HealthState>(),
                x => x.Match(OpCodes.Brfalse_S),
                x => x.MatchLdarg(0),
                x => x.MatchCall<Creature>("get_State"),
                x => x.MatchIsinst<HealthState>(),
                x => x.MatchCallvirt<HealthState>("get_health")
            });
            c.GotoNext(new Func<Instruction, bool>[1]
            {
                x => x.MatchCallvirt<Creature>("Die")
            });
            try
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(delegate (Creature creature)
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
                x => x.MatchLdarg(0),
                x => x.MatchCall<PhysicalObject>("get_bodyChunks"),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdflda<BodyChunk>("pos"),
                x => x.MatchLdfld<Vector2>("y"),
                x => x.MatchLdloc(0),
                x => x.Match(OpCodes.Bge_Un)
            });
            c.GotoNext(new Func<Instruction, bool>[4]
            {
                x => x.MatchLdsfld<ModManager>("CoopAvailable"),
                x => x.Match(OpCodes.Brfalse_S),
                x => x.MatchLdarg(0),
                x => x.MatchCall<Creature>("get_State")
            });
            int index = c.Index;
            c.Index = index + 1;
            try
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(delegate (Creature creature)
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
    static void ILNewLizard(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[4]
            {
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<Lizard>("lizardParams"),
            x => x.MatchLdfld<LizardBreedParams>("tongue"),
            x => x.MatchBrfalse(out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.EmitDelegate(ShadowOfLizardTongue);
                val.Emit(OpCodes.Brfalse_S, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizard Tongue!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardTongue(AbstractCreature liz)
    {
        if (ShadowOfOptions.tongue_ability.Value && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz.TryGetValue("Tongue", out string tongue) && (tongue == "Null" || tongue == "get"))
        {
            return false;
        }

        return true;
    }
    #endregion

    #region Jump Module
    static void ILSpearStick(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(0, new Func<Instruction, bool>[5]
            {
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<Lizard>("jumpModule"),
            x => x.MatchLdfld<LizardJumpModule>("gasLeakPower"),
            x => x.MatchLdcR4(0f),
            x => x.MatchBleUn(out target)
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

    /*
    #region Spit
    static void NewLizardAI(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;

            #region Lurk
            if (val.TryGotoNext(new Func<Instruction, bool>[6]
            {
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
            x => x.MatchLdfld<CreatureTemplate>("type"),
            x => x.MatchLdsfld<DLCSharedEnums.CreatureTemplateType>("PeachLizard"),
            x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
            x => x.MatchBrfalse(out target)
            }))
            {
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find target match for ILLizardAI!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[6]
            {
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
            x => x.MatchLdfld<CreatureTemplate>("type"),
            x => x.MatchLdsfld<DLCSharedEnums.CreatureTemplateType>("Salamander"),
            x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
            x => x.MatchBrtrue(out _)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI>(OpCodes.Call, "get_lizard");
                val.EmitDelegate(ShadowOfNewLizardAILurk);
                val.Emit(OpCodes.Brfalse, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardAI!");
            }
            #endregion

            #region Spit
            if (val.TryGotoNext(new Func<Instruction, bool>[2]
            {
            x => x.MatchCall<ModManager>("get_DLCShared"),
            x => x.MatchBrfalse(out target)
            }))
            {
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find target match for ILLizardAI!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[6]
            {
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<AbstractCreature>("creatureTemplate"),
            x => x.MatchLdfld<CreatureTemplate>("type"),
            x => x.MatchLdsfld<DLCSharedEnums.CreatureTemplateType>("RedLizard"),
            x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
            x => x.MatchBrtrue(out target2)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI>(OpCodes.Call, "get_lizard");
                val.EmitDelegate(ShadowOfNewLizardAISpit);
                val.Emit(OpCodes.Brfalse, target);

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI>(OpCodes.Call, "get_lizard");
                val.EmitDelegate(ShadowOfNewLizardAISpitValid);
                val.Emit(OpCodes.Brtrue, target2);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardAI!");
            }
            #endregion
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfNewLizardAILurk(Lizard self)
    {
        if ((ShadowOfOptions.swim_ability.Value || ShadowOfOptions.water_breather.Value) && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && (data.liz.TryGetValue("CanSwim", out string CanSwim) && CanSwim != "True" || data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather != "True"))
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " did not get it's Lurk ability because it cannot either swim or breathe underwater");

            return false;
        }
        return true;
    }
    public static bool ShadowOfNewLizardAISpit(Lizard self)
    {
        if (lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && (data.liz.TryGetValue("CanSpit", out string canSpit) && canSpit != "True" || data.isGoreHalf))
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " did not get it's Spit ability");

            return false;
        }
        return true;
    }
    public static bool ShadowOfNewLizardAISpitValid(Lizard self)
    {
        if (lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("CanSpit", out string canSpit) && canSpit == "True")
        {
            return true;
        }
        return false;
    }

    static void ILLizardAIUpdate(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            ILLabel target2 = null;

            if (val.TryGotoNext(new Func<Instruction, bool>[4]
            {
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<LizardAI>("redSpitAI"),
            x => x.MatchLdfld<LizardAI.LizardSpitTracker>("spitting"),
            x => x.MatchBrfalse(out target2)
            }))
            {
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find target match for ILLizardAI!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
            {
            x => x.MatchLdarg(0),
            x => x.MatchCall<LizardAI>("get_lizard"),
            x => x.MatchCallvirt<Creature>("get_Template"),
            x => x.MatchLdfld<CreatureTemplate>("type"),
            x => x.MatchLdsfld<CreatureTemplate.Type>("RedLizard"),
            x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
            x => x.MatchBrtrue(out target)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI>(OpCodes.Ldfld, "redSpitAI");
                val.Emit(OpCodes.Brtrue_S, target);

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI>(OpCodes.Ldfld, "redSpitAI");
                val.Emit(OpCodes.Brfalse_S, target2);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardAI!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion
    */

    #region WormGrass
    static void ILInteractWithCreature(ILContext il)
    {
        try
        {
            ILCursor val = new(il);

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[4]
            {
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<WormGrass.WormGrassPatch.CreatureAndPull>("creature"),
            x => x.MatchCallvirt<UpdatableAndDeletable>("Destroy"),
            x => x.MatchRet()
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.EmitDelegate(ShadowOfInteractWithCreature);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILInteractWithCreature!");
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

        if (Chance(liz.abstractCreature, ShadowOfOptions.grass_immune_chance.Value, "WormGrass Immune"))
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
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<DaddyCorruption.EatenCreature>("creature"),
            x => x.MatchCallvirt<Creature>("Die")
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
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<DaddyLongLegs>("eatObjects"),
            x => x.MatchLdloc(1),
            x => x.MatchCallvirt(typeof(List<DaddyLongLegs.EatObject>).GetMethod("get_Item")),
            x => x.MatchLdfld<DaddyLongLegs.EatObject>("chunk"),
            x => x.MatchCallvirt<BodyChunk>("get_owner"),
            x => x.MatchCallvirt<UpdatableAndDeletable>("Destroy")
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
    public static bool ShadowOfLizardSwimmer(Func<Lizard, bool> orig, Lizard self)
    {
        try
        {
            if (ShadowOfOptions.swim_ability.Value && self.Template.canSwim && (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious || self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly))
            {
                return true;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
        return orig(self);
    }
    #endregion

    #region Breathing
    static void ILLizardUpdate(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[3]
            {
            x => x.MatchLdarg(0),
            x => x.MatchCall<Lizard>("get_Burrower"),
            x => x.MatchBrfalse(out target)
            }))
            {            
            }       
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find target match for ILLizardUpdate!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[3]
            {
            x => x.MatchLdarg(0),
            x => x.MatchCall<Lizard>("get_Swimmer"),
            x => x.MatchBrtrue(out _)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.EmitDelegate(ShadowOfLizardUpdate);
                val.Emit(OpCodes.Brfalse_S, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLizardUpdate!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardUpdate(Lizard self)
    {
        if (ShadowOfOptions.water_breather.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("WaterBreather", out string WaterBreather))
        {
            if (self.Burrower && self.firstChunk.buried)
            {
                return true;
            }
            return WaterBreather == "True";         
        }
        return true;
    }

    static void ILLeechAttached(ILContext il)
    {
        try
        {
            ILCursor val = new(il);

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[4]
            {
                x => x.MatchLdloc(0),
                x => x.MatchCallvirt<BodyChunk>("get_owner"),
                x => x.MatchIsinst(typeof(Creature)),
                x => x.MatchCallvirt<Creature>("Die"),
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldloc_0);
                val.Emit<BodyChunk>(OpCodes.Callvirt, "get_owner");
                val.Emit(OpCodes.Isinst, typeof(Creature));
                val.EmitDelegate(ShadowOfLeechAttached);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLeechAttached!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void ShadowOfLeechAttached(Creature self)
    {
        if (ShadowOfOptions.water_breather.Value && self is Lizard && !self.dead && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            if (!data.liz.TryGetValue("WaterBreather", out string WaterBreather) && !defaultWaterBreather.Contains(self.Template.type.ToString()) || WaterBreather != "True")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " gained the Drowning Immunity due to dying to a Leech");

                data.liz["WaterBreather"] = "True";
            }
            else if (ShadowOfOptions.debug_logs.Value)
            {
                Debug.Log(all + self.ToString() + " did not gain the Drowning Immunity due to dying to a Leech because it already is Immune to Drowning");
            }
        }
    }
    #endregion
    #endregion

    #region Both Swim/Breathe and Camo Related
    static float ShadowOfLizardAILurkTrackerUtility(On.LizardAI.LurkTracker.orig_Utility orig, LizardAI.LurkTracker self) //Both Swim/Breathe and Camo Related
    {
        if (lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            if (ShadowOfOptions.swim_ability.Value && data.liz.ContainsKey("CanSwim") || ShadowOfOptions.water_breather.Value && data.liz.ContainsKey("WaterBreather"))
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

            if (val.TryGotoNext(MoveType.Before, new Func<Instruction, bool>[5]
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LizardAI.LurkTracker>("lizard"),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdfld<Room>("aimap"),
                x => x.MatchLdarg(1)

            }))
            {
                val.MoveAfterLabels();

                target2 = val.MarkLabel();
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore White target!");
            }

            if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LizardAI.LurkTracker>("lizard"),
                x => x.MatchCallvirt<Creature>("get_Template"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchLdsfld<CreatureTemplate.Type>("WhiteLizard"),
                x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                x => x.MatchBrfalse(out target)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardCamoLurkPosScore);
                val.Emit(OpCodes.Brtrue_S, target2);

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardCamoLurkPosScore);
                val.Emit(OpCodes.Brtrue_S, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore White!");
            }

            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[4]
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LizardAI.LurkTracker>("lizard"),
                x => x.MatchCallvirt<Lizard>("get_Swimmer"),
                x => x.MatchBrfalse(out target)
            }))
            {
                val.MoveAfterLabels();

                val.Emit(OpCodes.Ldarg_0);
                val.Emit<LizardAI.LurkTracker>(OpCodes.Ldfld, "lizard");
                val.EmitDelegate(ShadowOfLizardSwimLurkPosScore);
                val.Emit(OpCodes.Brfalse, target);
            }
            else
            {
                ShadowOfLizards.Logger.LogInfo(all + "Could not find match for ILLurkTrackerLurkPosScore!");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    public static bool ShadowOfLizardSwimLurkPosScore(Lizard self)
    {
        if (self.Template.canSwim && (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious || self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly))
        {
            if (ShadowOfOptions.water_breather.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.liz.TryGetValue("WaterBreather", out string WaterBreather))
            {
                return WaterBreather == "True";
            }
            else if (defaultWaterBreather.Contains(self.Template.type.ToString()))
            {
                return true;
            }
        }
        return false;
    }
    public static bool ShadowOfLizardCamoLurkPosScore(Creature self)
    {
        return ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && CanCamoCheck(data, self.Template.type.ToString());
    }
    #endregion

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
            x => x.MatchCall<ModManager>("get_DLCShared"),
            x => x.MatchBrfalse (out target2)
            }))
            {
                if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
                {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LizardGraphics>("lizard"),
                x => x.MatchCallvirt<Creature>("get_Template"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchLdsfld<CreatureTemplate.Type>("WhiteLizard"),
                x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                x => x.MatchBrtrue(out target)
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
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<LizardGraphics>("lizard"),
            x => x.MatchCallvirt<Creature>("get_dead"),
            x => x.MatchBrfalse(out target2)
            }))
            {
                if (val.TryGotoPrev(MoveType.Before, new Func<Instruction, bool>[7]
                {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LizardGraphics>("lizard"),
                x => x.MatchCallvirt<Creature>("get_Template"),
                x => x.MatchLdfld<CreatureTemplate>("type"),
                x => x.MatchLdsfld<CreatureTemplate.Type>("WhiteLizard"),
                x => x.MatchCall("ExtEnum`1<CreatureTemplate/Type>", "op_Equality"),
                x => x.MatchBrfalse(out target)
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
        return ShadowOfOptions.camo_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && CanCamoCheck(data, self.Template.type.ToString());
    }

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

    public static bool ShadowOfIsWallClimber(Func<Lizard, bool> orig, Lizard self)
    {
        try
        {
            if (ShadowOfOptions.climb_ability.Value && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
            {
                return ModManager.MMF && self.room != null && self.room.gravity <= Lizard.zeroGravityMovementThreshold || !data.liz.TryGetValue("CanClimbWall", out string CanClimbWall) && self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Wall].legality == PathCost.Legality.Allowed || CanClimbWall == "True";
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
        return orig(self);
    }
}
/*
class ILHooks
{
	public static bool ShadowOfLizardUpdate(Lizard self)
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