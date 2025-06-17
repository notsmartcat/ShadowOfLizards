using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowOfLizards;

public static class SpiderTransformation
{
    public static void Apply()
    {
        On.LizardSpit.DrawSprites += SpitSetInvisible;
        On.LizardSpit.Update += SpitChangeCheck;
        On.Spider.ConsiderPrey += SpiderConsiderPrey;

        On.Spider.Move_Vector2 += SpiderLegMove;
        On.Spider.FormCentipede += SpiderLegStopCentipede;
        On.LizardGraphics.DrawSprites += SpiderLimbSprites;
    }

    static void SpitSetInvisible(On.LizardSpit.orig_DrawSprites orig, LizardSpit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (!ShadowOfOptions.spider_transformation.Value || !ShadowOfOptions.spider_spit.Value || !ShadowOfLizards.lizardstorage.TryGetValue(self.lizard.abstractCreature, out ShadowOfLizards.LizardData data) || data.transformation != "SpiderTransformation")
        {
            return;
        }

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].isVisible = false;
        }
    }

    static void SpitChangeCheck(On.LizardSpit.orig_Update orig, LizardSpit self, bool eu)
    {
        if (!ShadowOfOptions.spider_transformation.Value || !ShadowOfOptions.spider_spit.Value || self.lizard.AI.redSpitAI == null || !ShadowOfLizards.lizardstorage.TryGetValue(self.lizard.abstractCreature, out ShadowOfLizards.LizardData data) 
            || data.transformation != "SpiderTransformation" || !data.liz.TryGetValue("SpiderNumber", out _))
        {
            orig.Invoke(self, eu);
            return;
        }

        Vector2 pos = self.pos;

        if (float.Parse(data.liz["SpiderNumber"]) > 0 && Random.Range(0, 100) == 0)
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

    public static void BabyPuff(Lizard self)
    {
        if (self.inShortcut || self.slatedForDeletetion || self.room == null || self.room.world == null || self.room.game.cameras[0].room != self.room || !ShadowOfLizards.lizardstorage.TryGetValue(self.abstractCreature, out ShadowOfLizards.LizardData data)
            || (data.transformation != "Spider" && data.transformation != "SpiderTransformation"))
        {
            return;
        }

        if (!data.liz.TryGetValue("SpiderNumber", out _))
        {
            Debug.Log(ShadowOfLizards.all + "SpiderNumber Value was not present on " + self + " If able please report to the mod author of Shadow Of Lizards");

            data.liz.Add("SpiderNumber", "0");
        }

        if (data.Beheaded == false && data.transformation != "SpiderTransformation")
        {
            data.Beheaded = true;
            ShadowOfLizards.Decapitation(self);
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
            SporeCloud val2 = new(self.firstChunk.pos, Custom.RNV() * Random.value * 10f, new Color(0.1f, 0.25f, 0.1f, 0.8f), 1f, null, j % 20, val)
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
            self.room.AddObject(new PuffBallSkin(self.firstChunk.pos, Custom.RNV() * Random.value * 16f, new Color(0.1f, 0.3f, 0.1f), new Color(0.1f, 0.1f, 0.3f)));
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

    static bool SpiderConsiderPrey(On.Spider.orig_ConsiderPrey orig, Spider self, Creature crit)
    {
        return (!ShadowOfOptions.spider_transformation.Value || crit == null || crit is not Lizard || !ShadowOfLizards.lizardstorage.TryGetValue(crit.abstractCreature, out ShadowOfLizards.LizardData data) || (data.transformation != "Spider" && data.transformation != "SpiderTransformation"))
            && orig.Invoke(self, crit);
    }


    static void SpiderLegMove(On.Spider.orig_Move_Vector2 orig, Spider self, Vector2 dest)
    {
        if (ShadowOfOptions.spider_transformation.Value && ShadowOfLizards.SpidLeg.TryGetValue(self, out ShadowOfLizards.SpiderAsLeg data) && data.liz != null && !data.liz.dead && self.room == data.liz.room)
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
        if (!ShadowOfOptions.spider_transformation.Value || !ShadowOfLizards.SpidLeg.TryGetValue(self, out ShadowOfLizards.SpiderAsLeg data) || data.liz == null || data.liz.dead)
        {
            orig.Invoke(self, otherSpider);
        }
    }

    static void SpiderLimbSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (!ShadowOfOptions.spider_transformation.Value || !ShadowOfLizards.lizardstorage.TryGetValue(self.lizard.abstractCreature, out ShadowOfLizards.LizardData data) || data.transformation != "SpiderTransformation" || self.lizard.dead || self.lizard.Stunned)
        {
            return;
        }

        if (!data.liz.TryGetValue("SpiderNumber", out _))
        {
            Debug.Log(ShadowOfLizards.all + "SpiderNumber Value was not present on " + self + " If able please report to the mod author of Shadow Of Lizards");
            data.liz.Add("SpiderNumber", "0");
        }

        data.sLeaser = sLeaser;

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
                        //Debug.Log("Spider Lizard Leg " + armNo + " not more then 0 SpiderNumber");
                        break;
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(ShadowOfLizards.all + "New LegSpider Created for " + self.lizard);

                    AbstractCreature spid = new(self.lizard.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null, self.lizard.room.GetWorldCoordinate(bodyPos), self.lizard.room.world.game.GetNewID());
                    self.lizard.room.abstractRoom.AddEntity(spid);
                    spid.RealizeInRoom();
                    ((Spider)spid.realizedCreature).bloodLust = 0f;

                    ShadowOfLizards.SpidLeg.Add((Spider)spid.realizedCreature, new ShadowOfLizards.SpiderAsLeg());
                    ShadowOfLizards.SpidLeg.TryGetValue((Spider)spid.realizedCreature, out ShadowOfLizards.SpiderAsLeg spidData);

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
                        ShadowOfLizards.SpidLeg.Remove((Spider)data.legSpiders[armNo][n]);

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
}
