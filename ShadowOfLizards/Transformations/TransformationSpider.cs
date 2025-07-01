using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class TransformationSpider
{
    public static void Apply()
    {
        On.Spider.ConsiderPrey += SpiderConsiderPrey;

        On.Spider.Move_Vector2 += SpiderLegMove;
        On.Spider.FormCentipede += SpiderLegStopCentipede;
    }

    #region Spit
    public static void SpiderSpitDraw(RoomCamera.SpriteLeaser sLeaser)
    {
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].isVisible = false;
        }
    }

    public static void SpiderSpitUpdate(LizardSpit self, LizardData data)
    {
        Vector2 pos = self.pos;

        if (float.Parse(data.liz["SpiderNumber"]) > 0 && data.transformation == "Spider" ? UnityEngine.Random.Range(0, 100) < 10 : UnityEngine.Random.Range(0, 100) == 0)
        {
            AbstractCreature spid = new(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null, self.room.GetWorldCoordinate(pos), self.room.world.game.GetNewID());
            self.room.abstractRoom.AddEntity(spid);
            spid.RealizeInRoom();
            spid.realizedCreature.mainBodyChunk.vel = self.vel * 2f;
            ((Spider)spid.realizedCreature).bloodLust = 1f;
            data.liz["SpiderNumber"] = (float.Parse(data.liz["SpiderNumber"]) - 1f).ToString();

        }

        AbstractPhysicalObject spit = new(self.room.world, AbstractPhysicalObject.AbstractObjectType.DartMaggot, null, self.room.GetWorldCoordinate(pos), self.lizard.room.game.GetNewID());
        self.room.abstractRoom.AddEntity(spit);
        spit.RealizeInRoom();
        (spit.realizedObject as DartMaggot).Shoot(self.pos, self.vel.normalized, self.lizard);
        self.lizard.room.PlaySound(SoundID.Big_Spider_Spit, self.lizard.mainBodyChunk);

        self.Destroy();
    }
    #endregion

    public static void BabyPuff(Lizard self)
    {
        if (self.inShortcut || self.slatedForDeletetion || self.room == null || self.room.world == null || self.room.game.cameras[0].room != self.room || !lizardstorage.TryGetValue(self.abstractCreature, out ShadowOfLizards.LizardData data)
            || (data.transformation != "Spider" && data.transformation != "SpiderTransformation"))
        {
            return;
        }

        try
        {
            if (!data.liz.TryGetValue("SpiderNumber", out _))
            {
                Debug.Log(all + "SpiderNumber Value was not present on " + self + " If able please report to the mod author of Shadow Of Lizards");
                ShadowOfLizards.Logger.LogError(all + "SpiderNumber Value was not present on " + self + " If able please report to the mod author of Shadow Of Lizards");

                data.liz.Add("SpiderNumber", "0");
            }

            if (data.Beheaded == false && data.transformation != "SpiderTransformation")
            {
                data.Beheaded = true;
                Decapitation(self);
            }

            data.transformation = "Null";
            InsectCoordinator val = null;

            for (int i = 0; i < self.room.updateList.Count; i++)
            {
                if (self.room.updateList[i] is InsectCoordinator coordinator)
                {
                    val = coordinator;
                    break;
                }
            }

            for (int j = 0; j < 70; j++)
            {
                SporeCloud val2 = new(self.firstChunk.pos, Custom.RNV() * UnityEngine.Random.value * 10f, new Color(0.1f, 0.25f, 0.1f, 0.8f), 1f, null, j % 20, val)
                {
                    nonToxic = true
                };
                self.room.AddObject(val2);
            }

            SporePuffVisionObscurer val3 = new(self.firstChunk.pos)
            {
                doNotCallDeer = true
            };

            self.room.AddObject(val3);
            for (int k = 0; k < 7; k++)
            {
                self.room.AddObject(new PuffBallSkin(self.firstChunk.pos, Custom.RNV() * UnityEngine.Random.value * 16f, new Color(0.1f, 0.3f, 0.1f), new Color(0.1f, 0.1f, 0.3f)));
            }

            self.room.PlaySound(SoundID.Puffball_Eplode, self.firstChunk.pos);

            for (int l = 0; l < float.Parse(data.liz["SpiderNumber"]); l++)
            {
                Vector2 pos = self.mainBodyChunk.pos;
                AbstractCreature val4 = new(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null, self.room.GetWorldCoordinate(pos), self.room.world.game.GetNewID());
                self.room.abstractRoom.AddEntity(val4);
                val4.RealizeInRoom();
                ((Spider)val4.realizedCreature).bloodLust = 1f;
            }

            data.liz["SpiderNumber"] = "0";
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void SpiderEatRegrowth(Lizard self, Lizard liz, LizardData data, LizardData data2)
    {
        try
        {
            if (self.grasps[0].grabbed is BigSpider spid && (spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BigSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_regrowth_chance.Value * 0.5
                || spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_regrowth_chance.Value
                || (ModManager.DLCShared && spid.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_regrowth_chance.Value * 2)))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating " + self.grasps[0].grabbed);

                data.transformation = "Spider";
                data.transformationTimer = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                return;
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "SpiderTransformation" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_regrowth_chance.Value)
                    || (data2.transformation == "Spider" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_regrowth_chance.Value * 0.5)))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating " + self.grasps[0].grabbed);

                data.transformation = "Spider";
                data.transformationTimer = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                return;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    #region Small Spider
    static bool SpiderConsiderPrey(On.Spider.orig_ConsiderPrey orig, Spider self, Creature crit)
    {
        return (!ShadowOfOptions.spider_transformation.Value || crit == null || crit is not Lizard || !lizardstorage.TryGetValue(crit.abstractCreature, out ShadowOfLizards.LizardData data) || (data.transformation != "Spider" && data.transformation != "SpiderTransformation"))
            && orig.Invoke(self, crit);
    }

    static void SpiderLegMove(On.Spider.orig_Move_Vector2 orig, Spider self, Vector2 dest)
    {
        if (ShadowOfOptions.spider_transformation.Value && SpidLeg.TryGetValue(self, out ShadowOfLizards.SpiderAsLeg data) && data.liz != null && !data.liz.dead && self.room == data.liz.room)
        {
            self.moving = false;
        }
        else
        {
            orig.Invoke(self, dest);
        }
    }

    static void SpiderLegStopCentipede(On.Spider.orig_FormCentipede orig, Spider self, Spider otherSpider)
    {
        if (!ShadowOfOptions.spider_transformation.Value || !SpidLeg.TryGetValue(self, out ShadowOfLizards.SpiderAsLeg data) || data.liz == null || data.liz.dead)
        {
            orig.Invoke(self, otherSpider);
        }
    }

    public static void SpiderLizardGraphicsDraw(LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, ShadowOfLizards.LizardData data)
    {
        if (!data.liz.TryGetValue("SpiderNumber", out _))
        {
            Debug.Log(all + "SpiderNumber Value was not present on " + self + " If able please report to the mod author of Shadow Of Lizards");
            data.liz.Add("SpiderNumber", "0");
        }

        if (self.lizard.bodyChunks[1].vel.x < -1f || self.lizard.bodyChunks[1].vel.x > 1f)
        {
            data.spiderlegtimer++;
            if (data.spiderlegtimer >= 25)
            {
                data.spiderlegtimer = 0;
                for (int i = 0; i < data.legSpiders.Count; i++)
                {
                    data.legSpidersFrame[i]++;

                    if (i == 0 || i == 1)
                    {
                        if (data.legSpidersFrame[i] < 0)
                        {
                            data.legSpidersFrame[i] = 4;
                        }
                        else if (data.legSpidersFrame[i] > 4)
                        {
                            data.legSpidersFrame[i] = 0;
                        }
                    }
                    else if (i == 2 || i == 3)
                    {
                        if (data.legSpidersFrame[i] < 4)
                        {
                            data.legSpidersFrame[i] = 8;
                        }
                        else if (data.legSpidersFrame[i] > 8)
                        {
                            data.legSpidersFrame[i] = 4;
                        }
                    }
                    else if (i == 4 || i == 5)
                    {
                        if (data.legSpidersFrame[i] < 2)
                        {
                            data.legSpidersFrame[i] = 6;
                        }
                        else if (data.legSpidersFrame[i] > 6)
                        {
                            data.legSpidersFrame[i] = 2;
                        }
                    }
                }
            }
        }


        for (int i = self.SpriteLimbsStart; i < self.SpriteLimbsEnd; i++)
        {
            int armNo = i - self.SpriteLimbsStart;

            Vector2 bodyPos = self.lizard.bodyChunks[(armNo != 2 || armNo != 3) ? 1 : 2].pos;
            string ArmState = data.ArmState[armNo];

            if (ArmState != "Spider")
            {
                continue;
            }

            if (self.lizard.bodyChunks[1].vel.x > 3f)
            {
                data.directionmultiplier = 1f;
            }
            else if (self.lizard.bodyChunks[1].vel.x < -3f)
            {
                data.directionmultiplier = -1f;
            }

            if (int.Parse(data.liz["SpiderNumber"]) > 0)
            {
                for (int n = 0; n < data.legSpiders[armNo].Count; n++)
                {
                    if (data.legSpiders[armNo].Count > 0 && data.legSpiders[armNo][n] != null && data.legSpiders[armNo][n].dead)
                    {
                        if (data.legSpiders[armNo][n] != null && data.legSpiders[armNo][n].room != self.lizard.room && !data.legSpiders[armNo][n].dead)
                        {
                            data.liz["SpiderNumber"] = (float.Parse(data.liz["SpiderNumber"]) + 1f).ToString();
                        }
                        data.legSpiders[armNo].RemoveAt(n);
                        n--;
                    }
                }

                for (int n = data.legSpiders[armNo].Count; n < 5; n++)
                {
                    if (!(int.Parse(data.liz["SpiderNumber"]) > 0))
                    {
                        break;
                    }

                    AbstractCreature spid = new(self.lizard.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null, self.lizard.room.GetWorldCoordinate(bodyPos), self.lizard.room.world.game.GetNewID());
                    self.lizard.room.abstractRoom.AddEntity(spid);
                    spid.RealizeInRoom();
                    ((Spider)spid.realizedCreature).bloodLust = 0f;

                    SpidLeg.Add((Spider)spid.realizedCreature, new ShadowOfLizards.SpiderAsLeg());
                    SpidLeg.TryGetValue((Spider)spid.realizedCreature, out ShadowOfLizards.SpiderAsLeg spidData);

                    spidData.liz = self.lizard;
                    data.legSpiders[armNo].Add((Spider)spid.realizedCreature);

                    data.liz["SpiderNumber"] = (float.Parse(data.liz["SpiderNumber"]) - 1f).ToString();
                }
            }
            else
            {
                for (int n = 0; n < data.legSpiders[armNo].Count; n++)
                {
                    if (data.legSpiders[armNo].Count > 0 && (data.legSpiders[armNo][n] == null || data.legSpiders[armNo][n] != null && data.legSpiders[armNo][n].dead))
                    {
                        data.legSpiders[armNo].RemoveAt(n);
                        n--;
                    }
                    else if (data.legSpiders[armNo].Count != 0 && data.legSpiders[armNo].Count < 5 && data.legSpiders[armNo][n] != null)
                    {
                        SpidLeg.Remove((Spider)data.legSpiders[armNo][n]);

                        ((Spider)data.legSpiders[armNo][n].abstractCreature.realizedCreature).bloodLust = 1f;
                        data.legSpiders[armNo].RemoveAt(n);
                        n--;

                    }
                }
            }

            int num = self.SpriteLimbsColorStart - self.SpriteLimbsStart;

            sLeaser.sprites[i].isVisible = false;
            sLeaser.sprites[i + num].isVisible = false;

            if (data.legSpiders[armNo].Count == 0)
            {
                self.lizard.LizardState.limbHealth[armNo] = 0f;
                continue;
            }
            else
            {
                self.lizard.LizardState.limbHealth[armNo] = 1f;
            }

            List<Creature> list = data.legSpiders[armNo];

            List<int> spidBaseOffset = new() { 20, 20, -20, -20, -10, -10 };

            List<float> spidAnimFrameOffsetHorizontal = new() { 20f, 15f, 10f, 5f, 0f, -5f, -10f, -15f, -20f };
            List<float> spidAnimFrameOffsetVertical = new() { 0, 30, 50, 30, 0 };

            Vector2 vector = default;
            vector.Set(spidBaseOffset[armNo] * data.directionmultiplier, 0f);

            for (int SpidAnimPos = 0; SpidAnimPos < list.Count; SpidAnimPos++)
            {
                if (list[SpidAnimPos] == null || list[SpidAnimPos].room != self.lizard.room)
                {
                    continue;
                }

                list[SpidAnimPos].mainBodyChunk.HardSetPosition(bodyPos + new Vector2(spidAnimFrameOffsetHorizontal[data.legSpidersFrame[armNo]] * SpidAnimPos * data.directionmultiplier, spidAnimFrameOffsetVertical[SpidAnimPos]) + vector);
            }
        }
    }
    #endregion
}
