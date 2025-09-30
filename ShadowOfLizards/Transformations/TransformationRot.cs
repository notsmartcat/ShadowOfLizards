using System;
using RWCustom;
using UnityEngine;
using Watcher;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class TransformationRot
{
    public static void Apply()
    {
        On.Watcher.LizardRotGraphics.DrawSprites += LizardRotGraphicsDrawSprites;

        On.Watcher.LizardRotModule.Update += LizardRotModuleUpdate;

        On.DaddyGraphics.DaddyDeadLeg.ctor += NewDaddyDeadLeg;

        On.DaddyTentacle.Update += DaddyTentacleUpdate;

        On.Watcher.LizardRotModule.Act += LizardRotModuleAct;
    }
    static void LizardRotModuleAct(On.Watcher.LizardRotModule.orig_Act orig, LizardRotModule self)
    {
        if (ShadowOfLizardRotModuleAct())
        {
            float num9 = 0f;
            float num10 = 0f;
            for (int num11 = 0; num11 < self.tentacles.Length; num11++)
            {
                float num12 = Mathf.Pow(self.tentacles[num11].chunksGripping, 0.5f);
                if (self.tentacles[num11].atGrabDest && self.tentacles[num11].grabDest != null)
                {
                    num10 += Mathf.Pow(Mathf.InverseLerp(Custom.LerpMap((float)self.stuckCounter, 0f, 100f, -0.1f, -1f), 0.85f, Vector2.Dot((self.tentacles[num11].floatGrabDest.Value - self.lizard.mainBodyChunk.pos).normalized, self.moveDirection)), 0.8f) / (float)self.tentacles.Length;
                    num12 = Mathf.Lerp(num12, 1f, 0.75f);
                }
                num9 += num12 / (float)self.tentacles.Length;
            }
            num10 = Mathf.Pow(num10 * num9, Custom.LerpMap((float)self.stuckCounter, 100f, 200f, 0.8f, 0.1f));
            num9 = Mathf.Pow(num9, 0.3f);
            num9 = Mathf.Max(num9, self.unconditionalSupport);
            num10 = Mathf.Max(num10, self.unconditionalSupport);
            float num13 = 0f;
            for (int num14 = 0; num14 < self.tentacles.Length; num14++)
            {
                if (self.tentacles[num14].neededForLocomotion)
                {
                    num13 += 1f / (float)self.tentacles.Length;
                }
            }
            if (num9 < 1f - num13)
            {
                float num15 = float.MinValue;
                int num16 = UnityEngine.Random.Range(0, self.tentacles.Length);
                for (int num17 = 0; num17 < self.tentacles.Length; num17++)
                {
                    if (!self.tentacles[num17].neededForLocomotion)
                    {
                        float num18 = 1000f / Mathf.Lerp(self.tentacles[num17].idealLength * (float)self.room.aimap.getTerrainProximity(self.tentacles[num17].Tip.pos), 200f, 0.8f);
                        if (self.tentacles[num17].task == DaddyTentacle.Task.Grabbing)
                        {
                            num18 *= 0.01f;
                        }
                        if (self.tentacles[num17].task == DaddyTentacle.Task.Hunt)
                        {
                            num18 *= 0.1f;
                        }
                        if (self.tentacles[num17].task == DaddyTentacle.Task.ExamineSound)
                        {
                            num18 *= 0.6f;
                        }
                        if (num18 > num15)
                        {
                            num15 = num18;
                            num16 = num17;
                        }
                    }
                }
                self.tentacles[num16].neededForLocomotion = true;
            }
            else if ((double)num9 > 0.85)
            {
                self.tentacles[UnityEngine.Random.Range(0, self.tentacles.Length)].neededForLocomotion = false;
            }
            return;
        }

        orig(self);

        bool ShadowOfLizardRotModuleAct()
        {
            if (self.lizard is Lizard liz && self.tentacles.Length > 0 && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.cutAppendage.Count > 0)
            {
                int innactive = 0;

                for (int i = 0; i < self.tentacles.Length; i++)
                {
                    if (!InnactiveTentacleCheck(data, i, CycleNum(liz.abstractCreature)))
                    {
                        continue;
                    }

                    innactive++;
                }

                if (innactive > Mathf.Min(self.tentacles.Length - 2, self.tentacles.Length * 0.6f))
                {
                    return true;
                }
            }

            return false;
        }
    }

    static void DaddyTentacleUpdate(On.DaddyTentacle.orig_Update orig, DaddyTentacle self)
    {
        if (self.daddy is Lizard liz && liz.rotModule.tentacles.Length > 0 && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.cutAppendage.Count > 0)
        {
            int appIndex = liz.rotModule.tentacles.IndexOf(self);

            if (!InnactiveTentacleCheck(data, appIndex, CycleNum(liz.abstractCreature)))
            {
                orig(self);
                return;
            }

            (liz.State as DaddyLongLegs.IHaveTentacleHealthState).TentacleHealth[appIndex] = 0;

            self.neededForLocomotion = true;
            self.grabChunk = null;
            self.limp = true;

            self.stun = 0;
        }

        orig(self);
    }

    static void LizardRotModuleUpdate(On.Watcher.LizardRotModule.orig_Update orig, LizardRotModule self)
    {
        if (self.lizard is Lizard liz && self.tentacles.Length > 0 && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.cutAppendage.Count > 0)
        {
            int innactive = 0;

            for (int i = 0; i < self.tentacles.Length; i++)
            {
                DaddyTentacle leg = self.tentacles[i];

                if (!InnactiveTentacleCheck(data, i, CycleNum(liz.abstractCreature)))
                {
                    continue;
                }

                innactive++;

                (liz.State as DaddyLongLegs.IHaveTentacleHealthState).TentacleHealth[i] = 0;

                leg.neededForLocomotion = true;
                leg.grabChunk = null;

                liz.appendages[i].canBeHit = false;
            }

            if (innactive > Mathf.Min(self.tentacles.Length - 2, self.tentacles.Length * 0.6f))
            {
                self.nearNormalClimbable = true;

                if (LizardRotModule.rotTemplates.Contains(self.lizard.abstractCreature.creatureTemplate))
                {
                    CreatureTemplate creatureTemplate = new(self.lizard.abstractCreature.creatureTemplate);

                    CreatureTemplate creatureTemplate2 = StaticWorld.GetCreatureTemplate(self.lizard.Template.type);

                    creatureTemplate.pathingPreferencesTiles = creatureTemplate2.pathingPreferencesTiles;
                    creatureTemplate.pathingPreferencesConnections = creatureTemplate2.pathingPreferencesConnections;

                    self.lizard.abstractCreature.creatureTemplate = creatureTemplate;
                    self.lizard.AI.pathFinder.Reset(self.room);

                    TemplatePathingUpdate(self.lizard, data);
                }
            }
        }

        orig(self);
    }

    static void NewDaddyDeadLeg(On.DaddyGraphics.DaddyDeadLeg.orig_ctor orig, DaddyGraphics.DaddyDeadLeg self, GraphicsModule owner, DaddyGraphics.IHaveRotGraphics rotOwner, int index, int parts, int firstSprite, BodyChunk connectedChunk, BodyChunk secondaryChunk)
    {
        orig(self, owner, rotOwner, index, parts, firstSprite, connectedChunk, secondaryChunk);

        if (owner is not LizardGraphics lizGraphics)
        {
            return;
        }

        if (!graphicstorage.TryGetValue(lizGraphics, out GraphicsData data))
        {
            graphicstorage.Add(lizGraphics, new GraphicsData());
            graphicstorage.TryGetValue(lizGraphics, out data);
        }

        data.deadLeg.Add(connectedChunk.index);
    }

    static void LizardRotGraphicsDrawSprites(On.Watcher.LizardRotGraphics.orig_DrawSprites orig, LizardRotGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lGraphics == null || self.lGraphics.lizard == null || !graphicstorage.TryGetValue(self.lGraphics, out GraphicsData data2) || !lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            if (ShadowOfOptions.cut_in_half.Value && (!data.availableBodychunks.Contains(1) || !data.availableBodychunks.Contains(2)))
            {
                float num3 = self.lGraphics.headConnectionRad * 0.5f + self.lGraphics.lizard.bodyChunkConnections[0].distance + self.lGraphics.lizard.bodyChunkConnections[1].distance / 2;

                for (int i = 0; i < self.deadLegs.Length; i++)
                {
                    if (!data.availableBodychunks.Contains(1) && data2.deadLeg[i] <= 1)
                    {
                        for (int j = self.deadLegs[i].firstSprite; j < self.deadLegs[i].firstSprite + self.deadLegs[i].sprites; j++)
                        {
                            sLeaser.sprites[j].isVisible = false;
                        }
                    }
                    if (!data.availableBodychunks.Contains(2) && data2.deadLeg[i] >= 2)
                    {
                        for (int j = self.deadLegs[i].firstSprite; j < self.deadLegs[i].firstSprite + self.deadLegs[i].sprites; j++)
                        {
                            sLeaser.sprites[j].isVisible = false;
                        }
                    }
                }
                for (int i = 0; i < self.eyes.Length; i++)
                {
                    float eyePos = self.eyePositions[i].x * self.lGraphics.BodyAndTailLength;

                    if (!data.availableBodychunks.Contains(1) && eyePos <= num3)
                    {
                        for (int j = self.eyes[i].firstSprite; j < self.eyes[i].firstSprite + self.eyes[i].sprites; j++)
                        {
                            sLeaser.sprites[j].isVisible = false;
                        }
                    }
                    if (!data.availableBodychunks.Contains(2) && eyePos > num3)
                    {
                        for (int j = self.eyes[i].firstSprite; j < self.eyes[i].firstSprite + self.eyes[i].sprites; j++)
                        {
                            sLeaser.sprites[j].isVisible = false;
                        }
                    }
                }
            }

            if (ShadowOfOptions.dismemberment.Value && data.cutAppendage.Count > 0)
            {
                for(int i = 0; i < self.legs.Length; i++)
                {
                    if (!InnactiveTentacleCheck(data, i, CycleNum(self.lGraphics.lizard.abstractCreature)))
                    {
                        continue;
                    }

                    for (int j = self.legs[i].firstSprite + 1; j < self.legs[i].firstSprite + self.legs[i].sprites; j++)
                    {
                        sLeaser.sprites[j].isVisible = false;
                    }

                    float percentage = (float)data.cutAppendage[i] / (float)self.lGraphics.lizard.appendages[i].segments.Length;

                    DaddyGraphics.DaddyLegGraphic leg = self.legs[i];

                    int start = Mathf.RoundToInt(leg.segments.Length * percentage) - 1;

                    Vector2 vector = Vector2.Lerp(leg.segments[start].lastPos, leg.segments[start].pos, timeStacker);

                    for (int j = start + 1; j < leg.segments.Length; j++)
                    {
                        (sLeaser.sprites[leg.firstSprite] as TriangleMesh).MoveVertice(j * 4, vector - camPos);
                        (sLeaser.sprites[leg.firstSprite] as TriangleMesh).MoveVertice(j * 4 + 1, vector - camPos);
                        (sLeaser.sprites[leg.firstSprite] as TriangleMesh).MoveVertice(j * 4 + 2, vector - camPos);
                        (sLeaser.sprites[leg.firstSprite] as TriangleMesh).MoveVertice(j * 4 + 3, vector - camPos);
                    }

                    for (int j = leg.firstSprite + 1; j < leg.firstSprite + leg.bumps.Length; j++)
                    {
                        sLeaser.sprites[j].isVisible = false;
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static bool InnactiveTentacleCheck(LizardData data, int tentacleNum, int cycleNumber)
    {
        return data.cutAppendage.ContainsKey(tentacleNum) && data.cutAppendageCycle.TryGetValue(tentacleNum, out int cycle) && cycle == cycleNumber;
    }
}