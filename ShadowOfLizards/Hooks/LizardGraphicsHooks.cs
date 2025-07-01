using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;
using Watcher;
using LizardCosmetics;
using static ShadowOfLizards.ShadowOfLizards;
using static RoomCamera;

namespace ShadowOfLizards;

internal class LizardGraphicsHooks
{
    public static void Apply()
    {
        On.LizardGraphics.ctor += NewLizardGraphics;
        On.LizardGraphics.DrawSprites += LizardGraphicsDraw;
        On.LizardGraphics.InitiateSprites += LizardGraphicsInitiateSprites;
        On.LizardGraphics.AddToContainer += LizardGraphicsAddToContainer;

        On.LizardCosmetics.Antennae.ctor += NewAntennae;

        //On.LizardCosmetics.SpineSpikes.DrawSprites += SpineSpikesDraw;
    }

    static void NewLizardGraphics(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (!graphicstorage.TryGetValue(self, out GraphicsData _))
        {
            graphicstorage.Add(self, new GraphicsData());
        }

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || (data.transformation != "Melted" && data.transformation != "MeltedTransformation") || !ModManager.DLCShared || self.lizard.lizardParams.template != DLCSharedEnums.CreatureTemplateType.SpitLizard)
        {
            return;
        }

        self.lizard.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
    }

    static void LizardGraphicsDraw(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
        try
        {
            if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                if (!lizardGoreStorage.TryGetValue(self.lizard.abstractCreature, out LizardGoreData _))
                {
                    return;
                }
                else
                {
                    if (!lizardGoreStorage.TryGetValue(self.lizard.abstractCreature, out LizardGoreData goreData))
                    {
                        return;
                    }

                    data = goreData.origLizardData;
                    goto Line1;
                }
            }

            data.sLeaser = sLeaser;
            data.rCam = rCam;

            if (data.Beheaded == true)
            {
                if (Futile.atlasManager.DoesContainElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut"))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 3].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut");
                    if (self.lizard.Template.type != WatcherEnums.CreatureTemplateType.IndigoLizard || self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard && bloodcolours == null)
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 3].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                    }
                    if (self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BlizzardLizard)
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 3].anchorY = 0.5f;
                    }
                }
                else
                {
                    sLeaser.sprites[self.SpriteHeadStart + 3].isVisible = false;
                }

                sLeaser.sprites[self.SpriteHeadStart].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 1].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 2].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 4].isVisible = false;
                self.lizard.bodyChunks[0].collideWithObjects = false;
                if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && graphicstorage.TryGetValue(self, out GraphicsData data2))
                {
                    sLeaser.sprites[data2.EyesSprites].isVisible = false;
                    sLeaser.sprites[data2.EyesSprites + 1].isVisible = false;
                }

                for (int j = 0; j < self.cosmetics.Count; j++)
                {
                    if (self.cosmetics[j] is Whiskers || self.cosmetics[j] is AxolotlGills || self.cosmetics[j] is Antennae)
                    {
                        for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                        {
                            sLeaser.sprites[k].isVisible = false;
                        }
                    }
                }
            }
            else
            {
                float num7 = Mathf.Lerp(self.lastHeadDepthRotation, self.headDepthRotation, timeStacker);
                Vector2 val3 = Vector2.Lerp(Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker), Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker), 0.2f);
                Vector2 val4 = Custom.PerpendicularVector(Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker) - Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker));
                Vector2 normalized = val4.normalized;
                float num8 = Mathf.Lerp(self.lizard.lastJawOpen, self.lizard.JawOpen, timeStacker);

                if (self.lizard.JawReadyForBite && self.lizard.Consious)
                {
                    num8 += UnityEngine.Random.value * 0.2f;
                }

                num8 = Mathf.Lerp(num8, Mathf.Lerp(self.lastVoiceVisualization, self.voiceVisualization, timeStacker) + 0.2f, Mathf.Lerp(self.lastVoiceVisualizationIntensity, self.voiceVisualizationIntensity, timeStacker) * 0.8f);
                num8 = Mathf.Clamp(num8, 0f, 1f);
                float num9 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker), Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker));
                int num10 = 3 - (int)(Mathf.Abs(num7) * 3.9f);

                if (num10 < 0 || num10 > 3)
                {
                    num10 = 0;
                }

                if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out string teeth) && teeth != "Incompatible")
                {
                    if (data.liz["UpperTeeth"] != "Normal")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 2].element = Futile.atlasManager.GetElementWithName("LizardUpperTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[2].ToString() + data.liz["UpperTeeth"]);
                    }

                    if (data.liz["LowerTeeth"] != "Normal")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 1].element = Futile.atlasManager.GetElementWithName("LizardLowerTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[1].ToString() + data.liz["LowerTeeth"]);
                    }
                }

                if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && graphicstorage.TryGetValue(self, out GraphicsData graphData))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 4].element = Futile.atlasManager.GetElementWithName("LizardEyes" + num10 + "." + self.lizard.lizardParams.headGraphics[4] + "Nose");

                    for (int l = graphData.EyesSprites; l < graphData.EyesSprites + 2; l++)
                    {
                        sLeaser.sprites[l].scaleX = Mathf.Sign(num7) * self.lizard.lizardParams.headSize * self.iVars.headSize;
                        sLeaser.sprites[l].scaleY = self.lizard.lizardParams.headSize * self.iVars.headSize;
                        sLeaser.sprites[l].x = val3.x + normalized.x * num8 * num7 * self.lizard.lizardParams.jawOpenMoveJawsApart * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) - camPos.x;
                        sLeaser.sprites[l].y = val3.y + normalized.y * num8 * num7 * self.lizard.lizardParams.jawOpenMoveJawsApart * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) - camPos.y;
                        sLeaser.sprites[l].rotation = num9 + self.lizard.lizardParams.jawOpenAngle * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) * num8 * num7;
                        sLeaser.sprites[l].SetAnchor(sLeaser.sprites[self.SpriteHeadStart + 4].anchorX, sLeaser.sprites[self.SpriteHeadStart + 4].anchorY);
                    }

                    string spriteName = "LizardEyes" + num10 + "." + self.lizard.lizardParams.headGraphics[4];

                    string spriteNameR = sLeaser.sprites[self.SpriteHeadStart + 4].scaleX > 0f ? "Right" + spriteName : "Left" + spriteName;
                    string spriteNameL = sLeaser.sprites[self.SpriteHeadStart + 4].scaleX > 0f ? "Left" + spriteName : "Right" + spriteName;

                    switch (data.liz["EyeRight"])
                    {
                        case "Blind":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.EyesSprites].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                    }

                    switch (data.liz["EyeLeft"])
                    {
                        case "Blind":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                    }

                }

                if (data.liz.TryGetValue("Tongue", out string tongue) && tongue == "Tube" && self.lizard.tongue != null && self.lizard.tongue.Out)
                {
                    FSprite obj = sLeaser.sprites[self.SpriteTongueStart];

                    if (((TriangleMesh)((obj is TriangleMesh) ? obj : null)).verticeColors != null)
                    {
                        int num11 = 0;
                        while (true)
                        {
                            int num12 = num11;
                            FSprite obj2 = sLeaser.sprites[self.SpriteTongueStart];
                            if (num12 >= ((TriangleMesh)((obj2 is TriangleMesh) ? obj2 : null)).verticeColors.Length)
                            {
                                break;
                            }
                            FSprite obj3 = sLeaser.sprites[self.SpriteTongueStart];
                            ((TriangleMesh)((obj3 is TriangleMesh) ? obj3 : null)).verticeColors[num11] = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                            num11++;
                        }
                        sLeaser.sprites[self.SpriteTongueStart + 1].color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                    }
                    else
                    {
                        FSprite obj4 = sLeaser.sprites[self.SpriteTongueStart];
                        ((obj4 is TriangleMesh) ? obj4 : null).color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                        sLeaser.sprites[self.SpriteTongueStart + 1].color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                    }
                }
            }

        Line1:

            if (ShadowOfOptions.cut_in_half.Value)
                GoreLimbSprites(data.availableBodychunks, sLeaser, self, null, timeStacker, camPos);

            if (ShadowOfOptions.dismemberment.Value)
            {
                int num = self.SpriteLimbsColorStart - self.SpriteLimbsStart;
                for (int i = self.SpriteLimbsStart; i < self.SpriteLimbsEnd; i++)
                {
                    int num2 = i - self.SpriteLimbsStart;

                    if (data.ArmState[num2] == "Normal" || data.ArmState[num2] == "Spider")
                    {
                        continue;
                    }

                    string element = sLeaser.sprites[i].element.name;

                    if (element == "LizardArm_28A")
                        element = "LizardArm_28";

                    string cutNum = data.ArmState[num2] == "Cut1" ? "" : "3";
                    string cutColourNum = data.ArmState[num2] == "Cut1" ? "" : "3";

                    if (!Futile.atlasManager.DoesContainElementWithName(sLeaser.sprites[i].element + "Cut" + cutNum))
                    {
                        sLeaser.sprites[i].isVisible = false;
                        sLeaser.sprites[i + num].isVisible = false;
                        continue;
                    }

                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name + "Cut" + cutNum);

                    sLeaser.sprites[i + num].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i + num].element.name + "Cut" + cutColourNum);
                    if (bloodcolours != null)
                    {
                        sLeaser.sprites[i + num].color = bloodcolours[self.lizard.Template.type.ToString()];
                    }
                }
            }

            if (ShadowOfOptions.electric_transformation.Value && data.liz.TryGetValue("ElectricColorTimer", out _) && graphicstorage.TryGetValue(self, out GraphicsData electricData) && (data.transformation == "ElectricTransformation" || data.transformation == "Electric"))
            {
                TransformationElectric.ElectricLizardGraphicsDraw(self, sLeaser, timeStacker, camPos, data, electricData);
                return;
            }

            if (ShadowOfOptions.spider_transformation.Value && data.transformation == "SpiderTransformation" && !self.lizard.dead && !self.lizard.Stunned)
            {
                TransformationSpider.SpiderLizardGraphicsDraw(self, sLeaser, data);
                return;
            }

            return;
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        static void GoreLimbSprites(List<int> availableBodychunks, SpriteLeaser sLeaser, LizardGraphics self, LizardGoreData lizardGoreData, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < self.lizard.bodyChunks.Length; i++)
            {
                if (!availableBodychunks.Contains(i))
                {
                    self.lizard.bodyChunks[i].collideWithObjects = false;
                    self.lizard.bodyChunks[i].mass = 0f;
                }
            }

            /*
            if (lizardGoreData != null && availableBodychunks.Contains(0) && !availableBodychunks.Contains(1))
            {
                for (int i = 0; i < self.SpriteHeadStart; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }

                for (int i = self.SpriteHeadEnd; i < self.startOfExtraSprites; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }

                for (int j = 0; j < self.cosmetics.Count; j++)
                {
                    if (self.cosmetics[j] is not Antennae && self.cosmetics[j] is not AxolotlGills && self.cosmetics[j] is not Whiskers)
                    {
                        for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                        {
                            sLeaser.sprites[k].isVisible = false;
                        }
                    }
                }
                return;
            }
            */

            if (!availableBodychunks.Contains(0))
            {
                for (int i = self.SpriteHeadStart; i < self.SpriteHeadEnd; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }

                for (int j = 0; j < self.cosmetics.Count; j++)
                {
                    if (self.cosmetics[j] is Whiskers || self.cosmetics[j] is AxolotlGills || self.cosmetics[j] is Antennae)
                    {
                        for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                        {
                            sLeaser.sprites[k].isVisible = false;
                        }
                    }
                }
            }

            if (!availableBodychunks.Contains(1))
            {
                if (self.limbs.Length == 4)
                {
                    sLeaser.sprites[self.SpriteLimbsStart].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 1].isVisible = false;

                    sLeaser.sprites[self.SpriteLimbsColorStart].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 1].isVisible = false;
                }
                else
                {
                    sLeaser.sprites[self.SpriteLimbsStart].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 1].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 2].isVisible = false;

                    sLeaser.sprites[self.SpriteLimbsColorStart].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 1].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 2].isVisible = false;
                }
            }
            if (!availableBodychunks.Contains(2))
            {
                sLeaser.sprites[self.SpriteTail].isVisible = false;
                if (self.limbs.Length == 4)
                {
                    sLeaser.sprites[self.SpriteLimbsStart + 2].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 3].isVisible = false;

                    sLeaser.sprites[self.SpriteLimbsColorStart + 2].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 3].isVisible = false;
                }
                else
                {
                    sLeaser.sprites[self.SpriteLimbsStart + 3].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 4].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsStart + 5].isVisible = false;

                    sLeaser.sprites[self.SpriteLimbsColorStart + 3].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 4].isVisible = false;
                    sLeaser.sprites[self.SpriteLimbsColorStart + 5].isVisible = false;
                }
            }

            float num3 = 5f;
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            vector = Vector2.Lerp(vector, Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker), 0.2f);
            float num4 = (Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f) + 1f) * 0.5f * Mathf.Pow(1f - self.lizard.AI.runSpeed, 2f);
            for (int j = 0; j < 4; j++)
            {
                int num5 = (j < 2) ? 1 : 2;
                Vector2 vector2 = self.BodyPosition(num5, timeStacker);
                if (self.lizard.animation == Lizard.Animation.ThreatSpotted || self.lizard.animation == Lizard.Animation.ThreatReSpotted)
                {
                    vector2 += Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * 2f;
                }
                float num6 = self.BodyChunkDisplayRad(num5);
                if (num5 % 2 == 0)
                {
                    num6 = (num6 + self.BodyChunkDisplayRad(num5 - 1)) / 2f;
                }
                num6 *= 1f + num4 * (float)(3 - num5) * 0.1f * ((num5 == 0) ? 0.5f : 1f);
                num6 *= self.iVars.fatness;
                Vector2 normalized = (vector - vector2).normalized;
                Vector2 a = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance(vector2, vector);

                if ((num5 == 1 || j == 2) && !availableBodychunks.Contains(1))
                {
                    sLeaser.sprites[j].isVisible = false;
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 2, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 3, vector2 + normalized * d - a * num3 - camPos);
                }
                else if (num5 == 2 && !availableBodychunks.Contains(2))
                {
                    sLeaser.sprites[j].isVisible = false;
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 2, vector2 + normalized * d - a * num3 - camPos);
                    (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 3, vector2 + normalized * d - a * num3 - camPos);
                }
            }
        }
    }

    static void LizardGraphicsInitiateSprites(On.LizardGraphics.orig_InitiateSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);

        try
        {
            if (self == null || self.lizard == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                return;
            }

            if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _) && data.liz["UpperTeeth"] != "Incompatible")
            {
                if (self.lizard.lizardParams.headGraphics[4] != 0 && self.lizard.lizardParams.headGraphics[4] != 1 && self.lizard.lizardParams.headGraphics[4] != 2 && self.lizard.lizardParams.headGraphics[4] != 3 && self.lizard.lizardParams.headGraphics[4] != 8 && self.lizard.lizardParams.headGraphics[4] != 9)
                {
                    data.liz["UpperTeeth"] = "Incompatible";
                    data.liz["LowerTeeth"] = "Incompatible";

                    Debug.Log(all + "Teeth sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    ShadowOfLizards.Logger.LogError(all + "Teeth sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
            }

            if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && data.liz["EyeRight"] != "Incompatible")
            {
                if (self.lizard.lizardParams.headGraphics[4] != 0 && self.lizard.lizardParams.headGraphics[4] != 1 && self.lizard.lizardParams.headGraphics[4] != 2 && self.lizard.lizardParams.headGraphics[4] != 3 && self.lizard.lizardParams.headGraphics[4] != 8 && self.lizard.lizardParams.headGraphics[4] != 9)
                {
                    data.liz["EyeRight"] = "Incompatible";
                    data.liz["EyeLeft"] = "Incompatible";

                    Debug.Log(all + "Eye sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    ShadowOfLizards.Logger.LogError(all + "Eye sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
                else
                {
                    if (self != null && !graphicstorage.TryGetValue(self, out GraphicsData _))
                    {
                        graphicstorage.Add(self, new GraphicsData());
                    }

                    if (graphicstorage.TryGetValue(self, out GraphicsData data2))
                    {
                        data2.EyesSprites = sLeaser.sprites.Length;
                        Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
                        sLeaser.sprites[data2.EyesSprites] = new FSprite("pixel", true);
                        sLeaser.sprites[data2.EyesSprites + 1] = new FSprite("pixel", true);
                        self.AddToContainer(sLeaser, rCam, null);
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardGraphicsAddToContainer(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig.Invoke(self, sLeaser, rCam, newContatiner);

        if (!ShadowOfOptions.blind.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("EyeRight", out _) || data.liz["EyeRight"] == "Incompatible"
            || !graphicstorage.TryGetValue(self, out GraphicsData data2) || !(sLeaser.sprites.Length > data2.EyesSprites) || data2.EyesSprites == 0)
        {
            return;
        }

        newContatiner ??= rCam.ReturnFContainer("Midground");
        newContatiner.AddChild(sLeaser.sprites[data2.EyesSprites]);
        newContatiner.AddChild(sLeaser.sprites[data2.EyesSprites + 1]);
        sLeaser.sprites[data2.EyesSprites].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
        sLeaser.sprites[data2.EyesSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
    }

    static void NewAntennae(On.LizardCosmetics.Antennae.orig_ctor orig, Antennae self, LizardGraphics lGraphics, int startSprite)
    {
        orig(self, lGraphics, startSprite);

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data) || (data.transformation != "Melted" && data.transformation != "MeltedTransformation"))
        {
            return;
        }

        self.redderTint = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
    }

    void SpineSpikesDraw(On.LizardCosmetics.SpineSpikes.orig_DrawSprites orig, SpineSpikes self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        try
        {
            List<int> availableBodychunks;
            if (!lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data))
            {
                if (!lizardGoreStorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardGoreData goreData))
                {
                    return;
                }
                else
                {
                    availableBodychunks = goreData.availableBodychunks;
                }
            }
            else
            {
                availableBodychunks = data.availableBodychunks;
            }

            for (int i = self.startSprite + self.bumps - 1; i >= self.startSprite; i--)
            {

                //LizardGraphics.LizardSpineData lizardSpineData = self.lGraphics.SpinePosition(Mathf.Lerp(0.05f, self.spineLength / self.lGraphics.BodyAndTailLength, num), timeStacker);

                float num = Mathf.InverseLerp(self.startSprite, self.startSprite + self.bumps - 1, i);
                float num2 = Mathf.Lerp(0.05f, self.lGraphics.BodyAndTailLength, num);

                float num3 = self.lGraphics.bodyLength / 2;

                sLeaser.sprites[i].color = Color.white;
                if (self.colored > 0)
                {
                    sLeaser.sprites[i + self.bumps].color = Color.white;
                }

                if (!availableBodychunks.Contains(1) && num2 <= num3)
                {
                    sLeaser.sprites[i].isVisible = false;
                    if (self.colored > 0)
                    {
                        sLeaser.sprites[i + self.bumps].isVisible = false;
                    }
                }
                if (!availableBodychunks.Contains(2) && num2 > num3)
                {
                    sLeaser.sprites[i].isVisible = false;
                    if (self.colored > 0)
                    {
                        sLeaser.sprites[i + self.bumps].isVisible = false;
                    }
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    /// <summary>
    /// 
    ///0 Front Most body circle
    ///
    ///4 ???
    ///
    ///5 Neck and whole body connection
    ///6 Tail
    ///7-10 Arms
    ///11 Jaw
    ///12 Lower Teeth
    ///13 Upper Teeth
    ///14 Head
    ///15 Eyes
    ///16-19 Arm Colour
    ///
    /// </summary>
    /// 
}

