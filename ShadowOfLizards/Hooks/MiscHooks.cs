using System;
using UnityEngine;
using RWCustom;

using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class MiscHooks
{
    public static void Apply()
    {
        On.BigEel.JawsSnap += BigEelJawsSnap;
        On.BigEel.Swallow += BigEelSwallow;

        On.Creature.Grab += CreatureGrab;

        On.DartMaggot.Update += DartMaggotUpdate;

        On.Explosion.Update += ExplosionUpdate;

        On.FlareBomb.Update += FlareBombUpdate;

        On.KingTusks.Tusk.HitThisChunk += KingTuskHitThisChunk;

        On.LizardBubble.DrawSprites += BubbleDraw;
        On.LizardJumpModule.Update += GasLeak;

        On.Player.HeavyCarry += PlayerHeavyCarry;

        On.SlugcatHand.EngageInMovement += SlugcatHandEngageInMovement;
        On.SlugcatHand.Update += SlugcatHandUpdate;

        On.SlugcatStats.NourishmentOfObjectEaten += LizardLegEaten;

        On.Spark.DrawSprites += SparkDrawSprites;

        On.Spear.HitSomething += SpearHit;

        On.SocialEventRecognizer.Killing += SocialEventRecognizerKilling;

        On.StationaryEffect.DrawSprites += StationaryEffectDrawSprites;

        On.ZapCoil.Update += ZapCoilUpdate;

        On.MoreSlugcats.SingularityBomb.Explode += SingularityBombExplode;
    }

    #region BigEel
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
    static void BigEelSwallow(On.BigEel.orig_Swallow orig, BigEel self)
    {
        for (int i = 0; i < self.clampedObjects.Count; i++)
        {
            if (self.clampedObjects[i].chunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
            {
                UnderwaterDen(data, liz.abstractCreature);
            }
        }

        orig(self);
    }
    #endregion

    #region Creature
    static bool CreatureGrab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self.grasps != null && obj is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(chunkGrabbed))
        {
            if (chunkGrabbed == 0 && data.availableBodychunks.Contains(1) || chunkGrabbed == 2 && data.availableBodychunks.Contains(1))
            {
                chunkGrabbed = 1;
            }
            else if (chunkGrabbed == 0 && data.availableBodychunks.Contains(2) || chunkGrabbed == 1 && data.availableBodychunks.Contains(2))
            {
                chunkGrabbed = 2;
            }
            else
            {
                return false;
            }
        }
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }
    #endregion

    #region DartMaggot
    static void DartMaggotUpdate(On.DartMaggot.orig_Update orig, DartMaggot self, bool eu)
    {
        orig(self, eu);

        if (self.mode == DartMaggot.Mode.StuckInChunk && self.stuckInChunk != null && self.stuckInChunk.owner != null && self.stuckInChunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(self.stuckInChunk.index))
        {
            self.Unstuck();
        }
    }
    #endregion

    #region Explosion
    static void ExplosionUpdate(On.Explosion.orig_Update orig, Explosion self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.deafen.Value)
        {
            return;
        }

        try
        {
            if (!singleUse.TryGetValue(self.sourceObject, out OneTimeUseData data2))
            {
                singleUse.Add(self.sourceObject, new());
                singleUse.TryGetValue(self.sourceObject, out data2);
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
                        if (self.deafen > 0f && self.room.physicalObjects[j][k] is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EarRight", out string ear) && !data2.lizStorage.Contains(liz) && (!ShadowOfOptions.distance_based_deafen.Value || (int)Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f) > 90))
                        {
                            data2.lizStorage.Add(liz);

                            float multiplier = ShadowOfOptions.distance_based_deafen.Value ? Custom.LerpMap(Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f), 20, 160, 0, 2) : 1;

                            if (ear == "Normal" && Chance(liz.abstractCreature, ShadowOfOptions.deafen_chance.Value * multiplier, "Explosion Defening Right Ear"))
                            {
                                data.liz["EarRight"] = "Deaf";

                                if (data.liz["EarLeft"] == "Deaf")
                                {
                                    for (int i = 0; i < liz.AI.modules.Count; i++)
                                    {
                                        if (liz.AI.modules[i] is SuperHearing superHearing)
                                        {
                                            superHearing.superHearingSkill = 0f;

                                            break;
                                        }
                                    }

                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + liz + " was fully Deafened");
                                }
                                else
                                {
                                    for (int i = 0; i < liz.AI.modules.Count; i++)
                                    {
                                        if (liz.AI.modules[i] is SuperHearing superHearing)
                                        {
                                            superHearing.superHearingSkill /= 2;

                                            break;
                                        }
                                    }

                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + liz + " Right Ear was Deafened");
                                }
                            }
                            if (data.liz["EarLeft"] == "Normal" && Chance(liz.abstractCreature, ShadowOfOptions.deafen_chance.Value * multiplier, "Explosion Defening Left Ear"))
                            {
                                data.liz["EarLeft"] = "Deaf";

                                if (data.liz["EarRight"] == "Deaf")
                                {
                                    for (int i = 0; i < liz.AI.modules.Count; i++)
                                    {
                                        if (liz.AI.modules[i] is SuperHearing superHearing)
                                        {
                                            superHearing.superHearingSkill = 0f;

                                            break;
                                        }
                                    }

                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + liz + " was fully Deafened");
                                }
                                else
                                {
                                    for (int i = 0; i < liz.AI.modules.Count; i++)
                                    {
                                        if (liz.AI.modules[i] is SuperHearing superHearing)
                                        {
                                            superHearing.superHearingSkill /= 2;

                                            break;
                                        }
                                    }

                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + liz + " Left Ear was Deafened");
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region FlareBomb
    static void FlareBombUpdate(On.FlareBomb.orig_Update orig, FlareBomb self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.blind.Value || self.burning <= 0f)
        {
            return;
        }

        try
        {
            if (!singleUse.TryGetValue(self, out OneTimeUseData data2))
            {
                singleUse.Add(self, new());
                singleUse.TryGetValue(self, out data2);
            }

            for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
            {
                if (self.room.abstractRoom.creatures[i].realizedCreature != null && (self.room.abstractRoom.creatures[i].rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.abstractRoom.creatures[i].rippleBothSides || self.abstractPhysicalObject.rippleBothSides) && (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 600f) || (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 1600f) && self.room.VisualContact(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos))))
                {
                    if (self.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && !data2.lizStorage.Contains(liz) && (!ShadowOfOptions.distance_based_blind.Value || (int)Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f) > 300))
                    {
                        data2.lizStorage.Add(liz);

                        float multiplier = ShadowOfOptions.distance_based_blind.Value ? Custom.LerpMap(Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f), 20, 400, 0, 2) : 1;

                        bool eyeRightBlind = eye == "Blind" || eye == "BlindScar" || eye == "BlindScar2" || eye == "Cut";
                        bool eyeLeftBlind = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "BlindScar2" || data.liz["EyeLeft"] == "Cut";
                        if (!eyeRightBlind && Chance(liz.abstractCreature, ShadowOfOptions.blind_chance.Value * multiplier, "FlareBomb Blinding Right Eye"))
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
                            else if (eye == "Scar2")
                            {
                                data.liz["EyeRight"] = "BlindScar2";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + " Right Eye was Blinded");
                        }
                        if (!eyeLeftBlind && Chance(liz.abstractCreature, ShadowOfOptions.blind_chance.Value * multiplier, "FlareBomb Blinding Left Eye"))
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
                            else if (data.liz["EyeLeft"] == "Scar2")
                            {
                                data.liz["EyeLeft"] = "BlindScar2";
                                liz.Template.visualRadius -= data.visualRadius * 0.5f;
                                liz.Template.waterVision -= data.visualRadius * 0.5f;
                                liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                            }
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + " Left Eye was Blinded");
                        }

                        eyeRightBlind = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "BlindScar2" || data.liz["EyeRight"] == "Cut";
                        eyeLeftBlind = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "BlindScar2" || data.liz["EyeLeft"] == "Cut";

                        if (ShadowOfOptions.deafen.Value && data.liz.ContainsKey("EarRight") && eyeRightBlind && eyeLeftBlind)
                        {
                            bool hasSuperHearing = false;

                            float hearingMultiplier = (data.liz["EarRight"] != "Deaf" ? 1 : 0) + (data.liz["EarLeft"] != "Deaf" ? 1 : 0);

                            for (int j = 0; j < liz.AI.modules.Count; j++)
                            {
                                if (liz.AI.modules[j] is SuperHearing superHearing)
                                {
                                    hasSuperHearing = true;

                                    superHearing.superHearingSkill = hearingMultiplier * 175f;

                                    break;
                                }
                            }

                            if (!hasSuperHearing)
                            {
                                SuperHearing superHearing = new(liz.AI, liz.AI.tracker, hearingMultiplier * 175f)
                                {
                                    room = self.room
                                };
                                liz.AI.modules.Add(superHearing);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region KingTusk
    static bool KingTuskHitThisChunk(On.KingTusks.Tusk.orig_HitThisChunk orig, KingTusks.Tusk self, BodyChunk chunk)
    {
        if (ShadowOfOptions.cut_in_half.Value && chunk != null && chunk.owner != null && chunk.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(chunk.index))
        {
            return false;
        }

        return orig(self, chunk);
    }
    #endregion

    #region Lizard
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
                    TransformationElectric.ElectricBubbleDraw(self, sLeaser, timeStacker, data2);
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
                TransformationElectric.ElectricBubbleDraw(self, sLeaser, timeStacker, data2);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    static void GasLeak(On.LizardJumpModule.orig_Update orig, LizardJumpModule self)
    {
        orig(self);

        if (!ShadowOfOptions.jump_ability.Value || self.lizard == null || self.gasLeakSpear == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !Chance(self.lizard.abstractCreature, ShadowOfOptions.jump_ability_chance.Value, "Removing Jump Ability due to Gas Leak"))
        {
            return;
        }

        try
        {
            data.liz["CanJump"] = "False";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + " lost the Jump Ability due to Gas Leak");

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= 5;
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region Player
    static bool PlayerHeavyCarry(On.Player.orig_HeavyCarry orig, Player self, PhysicalObject obj)
    {
        return (obj is not LizCutHead || self.privSneak <= 0.5f) && orig(self, obj);
    }
    #endregion

    #region Slugcat
    static bool SlugcatHandEngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand self)
    {
        Player scug = self.owner.owner as Player;

        if (scug.privSneak > 0.5f && scug.grasps[self.limbNumber] != null && scug.grasps[self.limbNumber].grabbed is LizCutHead)
        {
            self.huntSpeed = 12f;
            self.quickness = 0.7f;
            return true;
        }
        return orig(self);
    }
    static void SlugcatHandUpdate(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);

        Player scug = self.owner.owner as Player;

        if (scug.privSneak > 0.5f && scug.grasps[self.limbNumber] != null)
        {
            if (scug.grasps[self.limbNumber].grabbed is LizCutHead)
            {
                self.relativeHuntPos *= 1f - (scug.grasps[self.limbNumber].grabbed as LizCutHead).donned;
            }
        }
    }

    static int LizardLegEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
    {
        if (eatenobject is LizCutLeg)
        {
            if (ModManager.MSC && slugcatIndex == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                return -1;
            }
            int num = 0;
            return (!(slugcatIndex == SlugcatStats.Name.Red) && (!ModManager.MSC || !(slugcatIndex == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer))) ? (num + eatenobject.FoodPoints) : (num + 2 * eatenobject.FoodPoints);
        }
        return orig(slugcatIndex, eatenobject);
    }
    #endregion

    #region Spark
    static void SparkDrawSprites(On.Spark.orig_DrawSprites orig, Spark self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lizard != null && graphicstorage.TryGetValue(self.lizard, out GraphicsData data2))
        {
            sLeaser.sprites[0].color = CamoElectric(self.lizard, data2, self.lizard.HeadColor(timeStacker));
        }
    }
    #endregion

    #region Spear
    static bool SpearHit(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        return (!ShadowOfOptions.cut_in_half.Value || result.chunk == null || result.chunk.owner == null || result.chunk.owner is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || data.availableBodychunks.Contains(result.chunk.index)) && orig(self, result, eu);
    }
    #endregion

    #region SocialEvent
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

            var score = Menu.StoryGameStatisticsScreen.GetNonSandboxKillscore(iconData.critType);
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
            Menu.SandboxSettingsInterface.DefaultKillScores(ref killScores);
            killScores[(int)MultiplayerUnlocks.SandboxUnlockID.Slugcat] = 1;
        }
        return killScores;
    }
    #endregion

    #region StationarySprite
    static void StationaryEffectDrawSprites(On.StationaryEffect.orig_DrawSprites orig, StationaryEffect self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lizard != null && graphicstorage.TryGetValue(self.lizard, out GraphicsData data))
        {
            sLeaser.sprites[0].color = CamoElectric(self.lizard, data, self.lizard.HeadColor(timeStacker));
        }
    }
    #endregion

    #region ZapCoil
    static void ZapCoilUpdate(On.ZapCoil.orig_Update orig, ZapCoil self, bool eu)
    {
        if (self.turnedOn > 0.5f)
        {
            for (int i = 0; i < self.room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                {
                    if (!ModManager.Watcher || self.room.physicalObjects[i][j] is not Lizard)
                    {
                        for (int k = 0; k < self.room.physicalObjects[i][j].bodyChunks.Length; k++)
                        {
                            if ((self.horizontalAlignment && self.room.physicalObjects[i][j].bodyChunks[k].ContactPoint.y != 0) || (!self.horizontalAlignment && self.room.physicalObjects[i][j].bodyChunks[k].ContactPoint.x != 0))
                            {
                                Vector2 a = self.room.physicalObjects[i][j].bodyChunks[k].ContactPoint.ToVector2();
                                Vector2 v = self.room.physicalObjects[i][j].bodyChunks[k].pos + a * (self.room.physicalObjects[i][j].bodyChunks[k].rad + 30f);
                                if (self.GetFloatRect.Vector2Inside(v))
                                {
                                    if (self.room.physicalObjects[i][j] is Lizard crit && lizardstorage.TryGetValue(crit.abstractCreature, out LizardData data))
                                    {
                                        ViolenceCheck(self.room.physicalObjects[i][j] as Lizard, data, "Electric");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Singularity
    static void SingularityBombExplode(On.MoreSlugcats.SingularityBomb.orig_Explode orig, MoreSlugcats.SingularityBomb self)
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
    #endregion
}