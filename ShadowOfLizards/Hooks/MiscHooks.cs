using System;
using UnityEngine;
using RWCustom;
using static ShadowOfLizards.ShadowOfLizards;
using Menu;
using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;

namespace ShadowOfLizards;

internal class MiscHooks
{
    public static void Apply()
    {
        On.LizardJumpModule.Update += GasLeak;

        On.SlugcatStats.NourishmentOfObjectEaten += LizardLegEaten;

        On.Spear.HitSomething += SpearHit;
        On.DartMaggot.Update += DartMaggotUpdate;

        On.FlareBomb.Update += FlareBombUpdate;
        On.Explosion.Update += ExplosionUpdate;

        On.LizardBubble.DrawSprites += BubbleDraw;

        On.Creature.Update += CreatureUpdate;

        On.BigEel.Swallow += BigEelSwallow;
        On.BigEel.JawsSnap += BigEelJawsSnap;

        On.SocialEventRecognizer.Killing += SocialEventRecognizerKilling;

        On.Spark.DrawSprites += SparkDrawSprites;
        On.StationaryEffect.DrawSprites += StationaryEffectDrawSprites;

        On.Creature.Grab += CreatureGrab;

        On.MoreSlugcats.SingularityBomb.Explode += SingularityBombExplode;
    }

    static void GasLeak(On.LizardJumpModule.orig_Update orig, LizardJumpModule self)
    {
        orig.Invoke(self);

        float roll = UnityEngine.Random.Range(0, 100);

        if (!ShadowOfOptions.jump_ability.Value || self.gasLeakSpear == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || roll >= ShadowOfOptions.jump_ability_chance.Value)
        {
            if (ShadowOfOptions.chance_logs.Value && ShadowOfOptions.jump_ability.Value && self.gasLeakSpear != null && lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData _))
                Debug.Log(all + self + " Failure! " + ShadowOfOptions.jump_ability_chance.Value + "/" + roll  + " for Removing Jump ABility due to Gas Leak");

            return;
        }

        try
        {
            if (ShadowOfOptions.chance_logs.Value)
                Debug.Log(all + self + " Success! " + ShadowOfOptions.jump_ability_chance.Value + "/" + roll + " for Removing Jump ABility due to Gas Leak");

            data.liz["CanJump"] = "False";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " lost the ability to Jump due to Gas Leak");

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= 5;
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static int LizardLegEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
    {
        if (eatenobject is LizCutLeg)
        {
            if (ModManager.MSC && slugcatIndex == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                return -1;
            }
            int num = 0;
            return (!(slugcatIndex == SlugcatStats.Name.Red) && (!ModManager.MSC || !(slugcatIndex == MoreSlugcatsEnums.SlugcatStatsName.Artificer))) ? (num + eatenobject.FoodPoints) : (num + 2 * eatenobject.FoodPoints);
        }
        return orig.Invoke(slugcatIndex, eatenobject);
    }

    static bool SpearHit(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if (ShadowOfOptions.cut_in_half.Value && result.chunk != null && result.chunk.owner != null && result.chunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(result.chunk.index))
        {
            return false;
        }
        else
        {
            return orig(self, result, eu);
        }
    }

    static void DartMaggotUpdate(On.DartMaggot.orig_Update orig, DartMaggot self, bool eu)
    {
        orig(self, eu);

        if (self.mode == DartMaggot.Mode.StuckInChunk && self.stuckInChunk != null && self.stuckInChunk.owner != null && self.stuckInChunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(self.stuckInChunk.index))
        {
            self.Unstuck();
        }
    }

    static void FlareBombUpdate(On.FlareBomb.orig_Update orig, FlareBomb self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.blind.Value || self.burning <= 0f)
        {
            return;
        }

        try
        {
            if (!singleuse.TryGetValue(self, out OneTimeUseData data2))
            {
                singleuse.Add(self, new OneTimeUseData());
                singleuse.TryGetValue(self, out OneTimeUseData dat);
                data2 = dat;
            }

            for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
            {
                if (self.room.abstractRoom.creatures[i].realizedCreature != null && (self.room.abstractRoom.creatures[i].rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.abstractRoom.creatures[i].rippleBothSides || self.abstractPhysicalObject.rippleBothSides)
                    && (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 600f) || (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 1600f)
                    && self.room.VisualContact(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos))))
                {
                    if (self.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && !data2.lizStorage.Contains(liz)
                        && (!ShadowOfOptions.distance_based_blind.Value || (int)Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f) > 300))
                    {
                        data2.lizStorage.Add(liz);

                        float multiplier = ShadowOfOptions.distance_based_blind.Value ? Custom.LerpMap(Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f), 20, 400, 0, 2) : 1;

                        bool flag = eye == "Blind" || eye == "BlindScar" || eye == "Cut";
                        bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";
                        if (!flag && Chance(liz, ShadowOfOptions.blind_chance.Value * multiplier, "FlareBomb Blinding Right Eye"))
                        {
                            if (eye == "Normal")
                            {
                                data.liz["EyeRight"] = "Blind";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            else if (eye == "Scar")
                            {
                                data.liz["EyeRight"] = "BlindScar";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " Right Eye was Blinded");
                        }
                        if (!flag2 && Chance(liz, ShadowOfOptions.blind_chance.Value * multiplier, "FlareBomb Blinding Left Eye"))
                        {
                            if (data.liz["EyeLeft"] == "Normal")
                            {
                                data.liz["EyeLeft"] = "Blind";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            else if (data.liz["EyeLeft"] == "Scar")
                            {
                                data.liz["EyeLeft"] = "BlindScar";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " Left Eye was Blinded");
                        }
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void ExplosionUpdate(On.Explosion.orig_Update orig, Explosion self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.deafen.Value)
        {
            return;
        }

        try
        {
            if (!singleuse.TryGetValue(self.sourceObject, out OneTimeUseData data2))
            {
                singleuse.Add(self.sourceObject, new OneTimeUseData());
                singleuse.TryGetValue(self.sourceObject, out OneTimeUseData dat);
                data2 = dat;
            }

            float num = self.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)self.lifeTime, (float)self.frame) * 3.1415927f));
            for (int j = 0; j < self.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < self.room.physicalObjects[j].Count; k++)
                {
                    float num3 = float.MaxValue;
                    for (int l = 0; l < self.room.physicalObjects[j][k].bodyChunks.Length; l++)
                    {
                        float num5 = Vector2.Distance(self.pos, self.room.physicalObjects[j][k].bodyChunks[l].pos);
                        num3 = Mathf.Min(num3, num5);
                    }
                    if (self.sourceObject != self.room.physicalObjects[j][k] && (self.sourceObject == null || self.sourceObject.abstractPhysicalObject.rippleLayer == self.room.physicalObjects[j][k].abstractPhysicalObject.rippleLayer || self.sourceObject.abstractPhysicalObject.rippleBothSides || self.room.physicalObjects[j][k].abstractPhysicalObject.rippleBothSides) && !self.room.physicalObjects[j][k].slatedForDeletetion)
                    {
                        if (self.deafen > 0f && self.room.physicalObjects[j][k] is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EarRight", out string ear) && !data2.lizStorage.Contains(liz)
                            && (!ShadowOfOptions.distance_based_deafen.Value || (int)Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f) > 90))
                        {
                            data2.lizStorage.Add(liz);

                            float multiplier = ShadowOfOptions.distance_based_deafen.Value ? Custom.LerpMap(Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f), 20, 160, 0, 2) : 1;

                            if (ear == "Normal" && Chance(liz, ShadowOfOptions.deafen_chance.Value * multiplier, "Explosion Defening Right Ear"))
                            {
                                data.liz["EarRight"] = "Deaf";

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " Right Ear was Deafened");
                            }
                            if (data.liz["EarLeft"] == "Normal" && Chance(liz, ShadowOfOptions.deafen_chance.Value * multiplier, "Explosion Defening Left Ear"))
                            {
                                data.liz["EarLeft"] = "Deaf";

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " Left Ear was Deafened");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void BubbleDraw(On.LizardBubble.orig_DrawSprites orig, LizardBubble self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lizardGraphics == null || !lizardstorage.TryGetValue(self.lizardGraphics.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            if (ShadowOfOptions.camo_ability.Value && data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo == "True" && self.lizardGraphics.whiteCamoColorAmount > 0.25f)
            {
                if (ShadowOfOptions.electric_transformation.Value && data.transformation == "ElectricTransformation" && graphicstorage.TryGetValue(self.lizardGraphics, out GraphicsData data2))
                {
                    TransformationElectric.ElectricBubbleDraw(self, sLeaser, timeStacker, data2, true);
                    return;
                }
                Color color = Color.Lerp(self.lizardGraphics.effectColor, self.lizardGraphics.whiteCamoColor, self.lizardGraphics.whiteCamoColorAmount);

                float num = 1f - Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lizardGraphics.lastBlink, self.lizardGraphics.blink, timeStacker) * 2f * 3.1415927f), 1.5f + self.lizardGraphics.lizard.AI.excitement * 1.5f);
                if (self.lizardGraphics.headColorSetter != 0f)
                {
                    num = Mathf.Lerp(num, (self.lizardGraphics.headColorSetter > 0f) ? 1f : 0f, Mathf.Abs(self.lizardGraphics.headColorSetter));
                }
                if (self.lizardGraphics.flicker > 10)
                {
                    num = self.lizardGraphics.flickerColor;
                }
                num = Mathf.Lerp(num, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(self.lizardGraphics.lastVoiceVisualization, self.lizardGraphics.voiceVisualization, timeStacker)), 0.75f), Mathf.Lerp(self.lizardGraphics.lastVoiceVisualizationIntensity, self.lizardGraphics.voiceVisualizationIntensity, timeStacker));

                sLeaser.sprites[0].color = Color.Lerp(Color.Lerp(Color.Lerp(self.lizardGraphics.HeadColor1, self.lizardGraphics.whiteCamoColor, self.lizardGraphics.whiteCamoColorAmount), color, num), self.lizardGraphics.palette.blackColor, 1f - Mathf.Clamp(Mathf.Lerp(self.lastLife, self.life, timeStacker) * 2f, 0f, 1f));
            }
            else if (ShadowOfOptions.electric_transformation.Value && data.transformation == "ElectricTransformation" && graphicstorage.TryGetValue(self.lizardGraphics, out GraphicsData data2))
            {
                TransformationElectric.ElectricBubbleDraw(self, sLeaser, timeStacker, data2, false);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void CreatureUpdate(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        if (self == null || self is Lizard || self.grasps == null || self.grasps[0] == null || self.grasps[0].grabbed == null || self.grasps[0].grabbed is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2))
        {
            orig(self, eu);
            return;
        }

        if(liz.dead)
            PreViolenceCheck(liz, data2);

        orig(self, eu);

        try
        {
            if (!denCheck.TryGetValue(self.abstractCreature, out CreatureDenCheck data))
            {
                denCheck.Add(self.abstractCreature, new CreatureDenCheck());
                denCheck.TryGetValue(self.abstractCreature, out CreatureDenCheck dat);
                data = dat;
            }

            if (self.enteringShortCut.HasValue && self.room != null && self.room.shortcutData(self.enteringShortCut.Value).shortCutType != null && self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.CreatureHole)
            {
                if (data.denCheck == false)
                {
                    data.denCheck = true;

                    if (self.Submersion > 0.1f || self is TentaclePlant)
                    {
                        UnderwaterDen(data2, liz);
                    }

                    if (liz.dead)
                        PostViolenceCheck(liz, data2, "Den", self);
                }
            }
            else
            {
                data.denCheck = false;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void BigEelSwallow(On.BigEel.orig_Swallow orig, BigEel self)
    {
        for (int i = 0; i < self.clampedObjects.Count; i++)
        {
            if (self.clampedObjects[i].chunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
            {
                UnderwaterDen(data, liz);
            }
        }

        orig(self);
    }

    static void BigEelJawsSnap(On.BigEel.orig_JawsSnap orig, BigEel self)
    {
        for (int j = 0; j < self.room.physicalObjects.Length; j++)
        {
            for (int k = self.room.physicalObjects[j].Count - 1; k >= 0; k--)
            {
                if (self.room.physicalObjects[j][k] is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && (self.room.physicalObjects[j][k].abstractPhysicalObject.rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.physicalObjects[j][k].abstractPhysicalObject.rippleBothSides || self.abstractPhysicalObject.rippleBothSides))
                {
                    for (int l = 0; l < self.room.physicalObjects[j][k].bodyChunks.Length; l++)
                    {
                        if (self.InBiteArea(self.room.physicalObjects[j][k].bodyChunks[l].pos, self.room.physicalObjects[j][k].bodyChunks[l].rad / 2f))
                        {
                            data.lastDamageType = "BigEel";
                        }
                    }
                }
            }
        }

        orig(self);
    }

    static void SocialEventRecognizerKilling(On.SocialEventRecognizer.orig_Killing orig, SocialEventRecognizer self, Creature killer, Creature victim)
    {
        orig(self, killer, victim);

        if(ShadowOfOptions.dynamic_cheat_death.Value && ShadowOfOptions.dynamic_cheat_death_kill.Value && killer != null && killer is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            IconSymbol.IconSymbolData icon = CreatureSymbol.SymbolDataFromCreature(victim.abstractCreature);

            int score = (victim is Player) ? KillScore(icon) * 10 : KillScore(icon);

            data.cheatDeathChance += score;

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + killer + " killed " + victim + " and gained it's score of " + score);
        }

        static int KillScore(IconSymbol.IconSymbolData iconData)
        {
            if (!CreatureSymbol.DoesCreatureEarnATrophy(iconData.critType))
            {
                return 0;
            }

            var score = StoryGameStatisticsScreen.GetNonSandboxKillscore(iconData.critType);
            if (score == 0 && MultiplayerUnlocks.SandboxUnlockForSymbolData(iconData) is MultiplayerUnlocks.SandboxUnlockID unlockID)
            {
                score = KillScores()[unlockID.Index];
            }

            return score;
        }
    }

    private static int[] killScores;
    private static int[] KillScores()
    {
        if (killScores == null || killScores.Length != ExtEnum<MultiplayerUnlocks.SandboxUnlockID>.values.Count)
        {
            killScores = new int[ExtEnum<MultiplayerUnlocks.SandboxUnlockID>.values.Count];
            for (int i = 0; i < killScores.Length; i++)
            {
                killScores[i] = 1;
            }
            SandboxSettingsInterface.DefaultKillScores(ref killScores);
            killScores[(int)MultiplayerUnlocks.SandboxUnlockID.Slugcat] = 1;
        }
        return killScores;
    }

    static void SparkDrawSprites(On.Spark.orig_DrawSprites orig, Spark self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!ShadowOfOptions.camo_ability.Value || self.lizard == null || !lizardstorage.TryGetValue(self.lizard.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        if (data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo == "True" && self.lizard.lizard.Template.type != CreatureTemplate.Type.WhiteLizard)
        {
            sLeaser.sprites[0].color = LizardGraphicsHooks.Camo(self.lizard ,self.lizard.HeadColor(timeStacker));
        }
        else if (self.lizard.lizard.Template.type == CreatureTemplate.Type.WhiteLizard && data.liz.TryGetValue("CanCamo", out string CanCamo2) && CanCamo2 == "False")
        {
            sLeaser.sprites[0].color = LizardGraphicsHooks.WhiteNoCamoHeadColor(self.lizard, timeStacker);
        }
    }

    static void StationaryEffectDrawSprites(On.StationaryEffect.orig_DrawSprites orig, StationaryEffect self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!ShadowOfOptions.camo_ability.Value || self.lizard == null || !lizardstorage.TryGetValue(self.lizard.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        if (data.liz.TryGetValue("CanCamo", out string CanCamo) && CanCamo == "True" && self.lizard.lizard.Template.type != CreatureTemplate.Type.WhiteLizard)
        {
            sLeaser.sprites[0].color = LizardGraphicsHooks.Camo(self.lizard, self.lizard.HeadColor(timeStacker));
        }
        else if (self.lizard.lizard.Template.type == CreatureTemplate.Type.WhiteLizard && data.liz.TryGetValue("CanCamo", out string CanCamo2) && CanCamo2 == "False")
        {
            sLeaser.sprites[0].color = LizardGraphicsHooks.WhiteNoCamoHeadColor(self.lizard, timeStacker);
        }
    }

    static bool CreatureGrab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self.grasps != null && obj is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(chunkGrabbed))
        {
            return false;
        }
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }

    static void SingularityBombExplode(On.MoreSlugcats.SingularityBomb.orig_Explode orig, SingularityBomb self)
    {
        if (ShadowOfOptions.dynamic_cheat_death.Value)
        {
            for (int m = 0; m < self.room.physicalObjects.Length; m++)
            {
                for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                {
                    if (self.room.physicalObjects[m][n].abstractPhysicalObject.rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.physicalObjects[m][n].abstractPhysicalObject.rippleBothSides || self.abstractPhysicalObject.rippleBothSides)
                    {
                        if (self.room.physicalObjects[m][n] is Lizard liz && Custom.Dist(self.room.physicalObjects[m][n].firstChunk.pos, self.firstChunk.pos) < 350f && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
                        {
                            data.cheatDeathChance -= 100;
                        }
                    }
                }
            }
        }

        orig(self);

        for (int m = 0; m < self.room.physicalObjects.Length; m++)
        {
            for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
            {
                if (self.room.physicalObjects[m][n].abstractPhysicalObject.rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.physicalObjects[m][n].abstractPhysicalObject.rippleBothSides || self.abstractPhysicalObject.rippleBothSides)
                {
                    if (self.room.physicalObjects[m][n] is Lizard && Custom.Dist(self.room.physicalObjects[m][n].firstChunk.pos, self.firstChunk.pos) < 350f)
                    {
                        Eviscerate(self.room.physicalObjects[m][n] as Lizard);
                    }
                }
            }
        }
    }
}

