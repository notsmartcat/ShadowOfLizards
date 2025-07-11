using System;
using UnityEngine;
using RWCustom;
using static ShadowOfLizards.ShadowOfLizards;

using MoreSlugcats;

namespace ShadowOfLizards;

internal class MiscHooks
{
    public static void Apply()
    {
        On.LizardJumpModule.Update += GasLeak;

        On.SlugcatStats.NourishmentOfObjectEaten += LizardLegEaten;

        On.Spear.HitSomething += SpearHit;

        On.FlareBomb.Update += FlareBombUpdate;
        On.Explosion.Update += ExplosionUpdate;

        On.LizardBubble.DrawSprites += BubbleDraw;
    }

    static void GasLeak(On.LizardJumpModule.orig_Update orig, LizardJumpModule self)
    {
        orig.Invoke(self);

        if (self.gasLeakSpear == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || UnityEngine.Random.Range(0, 100) >= ShadowOfOptions.jump_ability_chance.Value)
        {
            return;
        }

        try
        {
            data.liz["CanJump"] = "False";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " lost the ability to Jump due to Gas Leak");

            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
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

    static bool SpearHit(On.Spear.orig_HitSomething orig, global::Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if (result.chunk != null && result.chunk.owner != null && result.chunk.owner is Lizard liz && ((lizardGoreStorage.TryGetValue(liz.abstractCreature, out LizardGoreData goreData) && !goreData.availableBodychunks.Contains(result.chunk.index))
            || (lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(result.chunk.index))))
        {
            return false;
        }
        else
        {
            return orig(self, result, eu);
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
                        && (int)Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f) > 300)
                    {
                        data2.lizStorage.Add(liz);

                        bool flag = eye == "Blind" || eye == "BlindScar" || eye == "Cut";
                        bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";
                        if (!flag && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_chance.Value)
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
                        if (!flag2 && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_chance.Value)
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
                            && (int)Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f) > 90)
                        {
                            data2.lizStorage.Add(liz);

                            if (ear == "Normal" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.deafen_chance.Value)
                            {
                                data.liz["EarRight"] = "Deaf";

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " Right Ear was Deafened");
                            }
                            if (data.liz["EarLeft"] == "Normal" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.deafen_chance.Value)
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
}

