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
        //On.LizardGraphics.Update += LizardGraphicsUpdate;
        On.LizardGraphics.DrawSprites += LizardGraphicsDraw;
        On.LizardGraphics.InitiateSprites += LizardGraphicsInitiateSprites;
        On.LizardGraphics.AddToContainer += LizardGraphicsAddToContainer;
        On.LizardGraphics.AddCosmetic += LizardGraphicsAddCosmetic;

        On.LizardCosmetics.Antennae.ctor += NewAntennae;
    }

    static void NewLizardGraphics(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (!graphicstorage.TryGetValue(self, out GraphicsData _))
        {
            graphicstorage.Add(self, new GraphicsData());
        }

        if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            if (data.isGoreHalf)
            {
                goto Line1;
            }

            if (ShadowOfOptions.water_breather.Value && data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather == "True")
            {
                bool addAxilotlGills = true;

                for (int c = 0; c < self.cosmetics.Count; c++)
                {
                    if (self.cosmetics[c] is AxolotlGills)
                    {
                        addAxilotlGills = false;
                        break;
                    }
                }

                if (addAxilotlGills)
                {
                    int num7 = self.TotalSprites;
                    self.AddCosmetic(num7, new AxolotlGills(self, num7));
                }
            }

        Line1:
            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation") && ModManager.DLCShared && self.lizard.lizardParams.template == DLCSharedEnums.CreatureTemplateType.SpitLizard)
            {
                self.lizard.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardGraphicsDraw(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (!graphicstorage.TryGetValue(self, out GraphicsData data2) || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            data.sLeaser = sLeaser;
            data.rCam = rCam;

            if (data.isGoreHalf)
            {
                goto Line1;
            }

            if (data.beheaded == true)
            {
                if (Futile.atlasManager.DoesContainElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut"))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 3].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut");
                    if (self.lizard.Template.type != WatcherEnums.CreatureTemplateType.IndigoLizard || self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard && bloodcolours == null)
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 3].color = BloodColoursCheck(self.lizard.Template.type.ToString()) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                    }
                    if (ModManager.Watcher && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BlizzardLizard || ModManager.DLCShared && self.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard)
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

                if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _))
                {
                    sLeaser.sprites[data2.eyeSprites].isVisible = false;
                    sLeaser.sprites[data2.eyeSprites + 1].isVisible = false;
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

                    for (int l = graphData.eyeSprites; l < graphData.eyeSprites + 2; l++)
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
                            sLeaser.sprites[graphData.eyeSprites].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.eyeSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.eyeSprites].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.eyeSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.eyeSprites].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.eyeSprites].color = BloodColoursCheck(self.lizard.Template.type.ToString()) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.eyeSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                    }

                    switch (data.liz["EyeLeft"])
                    {
                        case "Blind":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.eyeSprites + 1].color = BloodColoursCheck(self.lizard.Template.type.ToString()) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.eyeSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.eyeSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
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
            if(ShadowOfOptions.camo_ability.Value && CanCamoCheck(data, self.lizard.Template.type.ToString()) || ShadowOfOptions.electric_transformation.Value && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                CamoLizardGraphicsDraw(self, sLeaser, timeStacker, data, data2);
            }
         
            if (ShadowOfOptions.camo_ability.Value && data.liz.TryGetValue("CanCamo", out string CanCamo2))
            {
                if (CanCamo2 == "True" && self.lizard.Template.type != CreatureTemplate.Type.WhiteLizard)
                {
                    Color color = rCam.PixelColorAtCoordinate(self.lizard.mainBodyChunk.pos);
                    Color color2 = rCam.PixelColorAtCoordinate(self.lizard.bodyChunks[1].pos);
                    Color color3 = rCam.PixelColorAtCoordinate(self.lizard.bodyChunks[2].pos);

                    if (color == color2)
                    {
                        self.whitePickUpColor = color;
                    }
                    else if (color2 == color3)
                    {
                        self.whitePickUpColor = color2;
                    }
                    else if (color3 == color)
                    {
                        self.whitePickUpColor = color3;
                    }
                    else
                    {
                        self.whitePickUpColor = (color + color2 + color3) / 3f;
                    }

                    if (self.whiteCamoColorAmount == -1f)
                    {
                        self.whiteCamoColor = self.whitePickUpColor;
                        self.whiteCamoColorAmount = 1f;
                    }
                }
                else if (CanCamo2 == "False" && self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                {
                    self.whiteCamoColor = new Color(1f, 1f, 1f);
                    self.whiteCamoColorAmount = 0f;

                    self.ColorBody(sLeaser, new Color(1f, 1f, 1f));

                    sLeaser.sprites[self.SpriteHeadStart].color = WhiteNoCamoHeadColor(self, timeStacker);
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = WhiteNoCamoHeadColor(self, timeStacker);
                }
            } //Camo

            if (ShadowOfOptions.cut_in_half.Value)
                GoreLimbSprites(data.availableBodychunks);

            bool cosmeticValid = false;
            for (int i = 0; i < data.availableBodychunks.Count; i++)
            {
                if (!data.cosmeticBodychunks.Contains(data.availableBodychunks[i]))
                {
                    cosmeticValid = true;
                    break;
                }
            }

            if (cosmeticValid)
            {         
                CutInHalfGraphics(self, sLeaser, data.cosmeticBodychunks, camPos, timeStacker);
            }

            if (ShadowOfOptions.dismemberment.Value)
            {
                int num = self.SpriteLimbsColorStart - self.SpriteLimbsStart;
                for (int i = self.SpriteLimbsStart; i < self.SpriteLimbsEnd; i++)
                {
                    int num2 = i - self.SpriteLimbsStart;

                    if (data.armState[num2] == "Normal" || data.armState[num2] == "Spider")
                    {
                        continue;
                    }

                    string element = sLeaser.sprites[i].element.name;

                    if (element == "LizardArm_28A")
                        element = "LizardArm_28";

                    string cutNum = data.armState[num2] == "Cut1" ? "" : "3";
                    string cutColourNum = data.armState[num2] == "Cut1" ? "" : "3";

                    if (!Futile.atlasManager.DoesContainElementWithName(element + "Cut" + cutNum))
                    {
                        sLeaser.sprites[i].isVisible = false;
                        sLeaser.sprites[i + num].isVisible = false;
                        continue;
                    }

                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(element + "Cut" + cutNum);
                    sLeaser.sprites[i + num].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i + num].element.name + "Cut" + cutColourNum);

                    if (BloodColoursCheck(self.lizard.Template.type.ToString()))
                        sLeaser.sprites[i + num].color = bloodcolours[self.lizard.Template.type.ToString()];
                }
            }

            if (ShadowOfOptions.spider_transformation.Value && data.transformation == "SpiderTransformation" && !self.lizard.dead && !self.lizard.Stunned)
            {
                TransformationSpider.SpiderLizardGraphicsDraw(self, sLeaser, data);
                return;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        void GoreLimbSprites(List<int> availableBodychunks)
        {
            try
            {
                for (int i = 0; i < self.lizard.bodyChunks.Length; i++)
                {
                    if (!availableBodychunks.Contains(i))
                    {
                        self.lizard.bodyChunks[i].collideWithObjects = false;
                        //self.lizard.bodyChunks[i].mass = 0f;
                        self.lizard.bodyChunks[i].rad = 0f;
                    }
                }

                if (!availableBodychunks.Contains(0) && !data.beheaded)
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
                    else if (self.limbs.Length >= 6)
                    {
                        sLeaser.sprites[self.SpriteLimbsStart].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsStart + 1].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsStart + 2].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsStart + 3].isVisible = false;

                        sLeaser.sprites[self.SpriteLimbsColorStart].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsColorStart + 1].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsColorStart + 2].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsColorStart + 3].isVisible = false;
                    }

                    if (availableBodychunks.Contains(2))
                    {
                        Vector2 pos = ((Vector2.Lerp(self.lizard.bodyChunks[2].lastPos, self.lizard.bodyChunks[2].pos, timeStacker) + Vector2.Lerp(self.lizard.bodyChunks[1].lastPos, self.lizard.bodyChunks[1].pos, timeStacker)) / 2) - camPos;

                        for (int i = data2.cutHalfSprites; i < data2.cutHalfSprites + 2; i++)
                        {
                            sLeaser.sprites[i].isVisible = true;
                            sLeaser.sprites[i].x = pos.x;
                            sLeaser.sprites[i].y = pos.y;
                            sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(self.lizard.bodyChunks[2].pos, self.lizard.bodyChunks[1].pos);
                        }

                        sLeaser.sprites[data2.cutHalfSprites].element = Futile.atlasManager.GetElementWithName("LizardCutHalf1");
                        sLeaser.sprites[data2.cutHalfSprites + 1].element = Futile.atlasManager.GetElementWithName("LizardCutHalf12");

                        sLeaser.sprites[data2.cutHalfSprites].color = BloodColoursCheck(self.lizard.Template.type.ToString()) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                        sLeaser.sprites[data2.cutHalfSprites + 1].color = Color.white;

                        CutInHalfGraphics(self, sLeaser, availableBodychunks, camPos, timeStacker);
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
                    else if(self.limbs.Length >= 6)
                    {     
                        sLeaser.sprites[self.SpriteLimbsStart + 4].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsStart + 5].isVisible = false;                       
                        sLeaser.sprites[self.SpriteLimbsColorStart + 4].isVisible = false;
                        sLeaser.sprites[self.SpriteLimbsColorStart + 5].isVisible = false;
                    }

                    if (availableBodychunks.Contains(1))
                    {
                        Vector2 pos = ((Vector2.Lerp(self.lizard.bodyChunks[1].lastPos, self.lizard.bodyChunks[1].pos, timeStacker) + Vector2.Lerp(self.lizard.bodyChunks[2].lastPos, self.lizard.bodyChunks[2].pos, timeStacker)) / 2) - camPos;

                        for (int i = data2.cutHalfSprites; i < data2.cutHalfSprites + 2; i++)
                        {
                            sLeaser.sprites[i].isVisible = true;
                            sLeaser.sprites[i].x = pos.x;
                            sLeaser.sprites[i].y = pos.y;
                            sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(self.lizard.bodyChunks[1].pos, self.lizard.bodyChunks[2].pos);
                        }

                        sLeaser.sprites[data2.cutHalfSprites].element = Futile.atlasManager.GetElementWithName("LizardCutHalf1");
                        sLeaser.sprites[data2.cutHalfSprites + 1].element = Futile.atlasManager.GetElementWithName("LizardCutHalf12");

                        sLeaser.sprites[data2.cutHalfSprites].color = BloodColoursCheck(self.lizard.Template.type.ToString()) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                        sLeaser.sprites[data2.cutHalfSprites + 1].color = Color.white;

                        CutInHalfGraphics(self, sLeaser, availableBodychunks, camPos, timeStacker);
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
            catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
        }
    }

    public static void CamoLizardGraphicsDraw(LizardGraphics self, SpriteLeaser sLeaser, float timeStacker, LizardData data, GraphicsData data2)
    {
        try
        {
            Color effectColour = self.effectColor;
            Color headColour = self.effectColor;

            Color bodyColour = self.BodyColor(timeStacker);

            bool head = true;
            if (self.lizard.Template.type == CreatureTemplate.Type.BlackLizard || self.lizard.Template.type == CreatureTemplate.Type.Salamander || ModManager.Watcher && (self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BasiliskLizard || self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard))
            {
                head = false;
            }

            if (self.whiteCamoColorAmount > 0.25f || data2.electricColorTimer > 0)
            {
                data2.camoOnce = true;

                effectColour = CamoElectric(self, data2, self.effectColor);
                bodyColour = Camo(self, self.BodyColor(timeStacker));

                if(data2.electricColorTimer > 0)
                    data2.electricColorTimer--;

                if (head)
                {
                    float num = 1f - Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastBlink, self.blink, timeStacker) * 2f * 3.1415927f), 1.5f + self.lizard.AI.excitement * 1.5f);
                    if (self.headColorSetter != 0f)
                    {
                        num = Mathf.Lerp(num, (self.headColorSetter > 0f) ? 1f : 0f, Mathf.Abs(self.headColorSetter));
                    }
                    if (self.flicker > 10)
                    {
                        num = self.flickerColor;
                    }
                    num = Mathf.Lerp(num, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(self.lastVoiceVisualization, self.voiceVisualization, timeStacker)), 0.75f), Mathf.Lerp(self.lastVoiceVisualizationIntensity, self.voiceVisualizationIntensity, timeStacker));
                    headColour = Color.Lerp(CamoElectric(self, data2, self.HeadColor1), effectColour, num);
                }
                else if (self.lizard.Template.type == CreatureTemplate.Type.Salamander)
                {
                    headColour = Camo(self, self.SalamanderColor);
                }
                else
                {
                    headColour = bodyColour;
                }

                #region Body
                sLeaser.sprites[self.SpriteBodyMesh].color = bodyColour;
                sLeaser.sprites[self.SpriteTail].color = bodyColour;

                for (int i = self.SpriteBodyCirclesStart; i < self.SpriteBodyCirclesEnd; i++)
                {
                    sLeaser.sprites[i].color = bodyColour;
                }
                #endregion

                #region Limbs
                for (int j = self.SpriteLimbsStart; j < self.SpriteLimbsEnd; j++)
                {
                    sLeaser.sprites[j].color = bodyColour;
                }
                for (int s = self.SpriteLimbsColorStart; s < self.SpriteLimbsColorEnd; s++)
                {
                    if (!ShadowOfOptions.dismemberment.Value || data.armState[s - self.SpriteLimbsColorStart] != "Cut")
                    {
                        sLeaser.sprites[s].color = effectColour;
                    }
                }
                #endregion

                #region Tongue
                if (ModManager.Watcher && self.lizard.tongue != null && self.lizard.tongue.Out && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard)
                {
                    sLeaser.sprites[self.SpriteTongueStart + 1].color = effectColour;
                }
                #endregion

                #region Head
                if (self.lizard.Template.type == CreatureTemplate.Type.CyanLizard || ModManager.Watcher && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard)
                {
                    sLeaser.sprites[self.SpriteHeadStart].color = bodyColour;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = bodyColour;
                }
                else if (ModManager.Watcher && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BasiliskLizard)
                {
                    Color color4 = CamoElectric(self, data2, Color.Lerp(self.HeadColor(timeStacker), self.lizard.effectColor, 0.7f));
                    if (self.whiteFlicker > 0 && (self.whiteFlicker > 15 || self.everySecondDraw))
                    {
                        color4 = CamoElectric(self, data2, new Color(1f, 1f, 1f));
                    }
                    sLeaser.sprites[self.SpriteHeadStart].color = color4;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = color4;
                }
                else
                {
                    sLeaser.sprites[self.SpriteHeadStart].color = headColour;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = headColour;
                }
                #endregion

                #region Teeth
                if (self.lizard.Template.type == CreatureTemplate.Type.CyanLizard)
                {
                    sLeaser.sprites[self.SpriteHeadStart + 1].color = Electrify(self.HeadColor(timeStacker));
                    sLeaser.sprites[self.SpriteHeadStart + 2].color = Electrify(self.HeadColor(timeStacker));
                }
                #endregion

                #region Tail
                if (self.iVars.tailColor > 0f)
                {
                    for (int j = 0; j < (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length; j++)
                    {
                        float t = (float)(j / 2) * 2f / (float)((sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length - 1);
                        (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors[j] = bodyColour;
                    }
                }
                #endregion

                #region Cosmetics
                for (int c = 0; c < self.cosmetics.Count; c++)
                {
                    if (self.cosmetics[c] is Antennae antennae)
                    {
                        float flicker = Mathf.Pow(UnityEngine.Random.value, 1f - 0.5f * self.lizard.AI.yellowAI.commFlicker) * self.lizard.AI.yellowAI.commFlicker;
                        if (!self.lizard.Consious)
                        {
                            flicker = 0f;
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            sLeaser.sprites[antennae.startSprite + i].color = headColour;

                            float num2 = 0f;

                            for (int j = 0; j < antennae.segments; j++)
                            {
                                float num3 = (float)j / (float)(antennae.segments - 1);
                                for (int k = 0; k < 2; k++)
                                {
                                    (sLeaser.sprites[antennae.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4] = AntennaeEffectColor(k, (num3 + num2) / 2f, flicker, antennae);
                                    (sLeaser.sprites[antennae.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 1] = AntennaeEffectColor(k, (num3 + num2) / 2f, flicker, antennae);
                                    (sLeaser.sprites[antennae.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 2] = AntennaeEffectColor(k, num3, flicker, antennae);
                                    if (j < antennae.segments - 1)
                                    {
                                        (sLeaser.sprites[antennae.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 3] = AntennaeEffectColor(k, num3, flicker, antennae);
                                    }
                                }
                                num2 = num3;
                            }
                        }

                        Color AntennaeEffectColor(int part, float tip, float flicker, Antennae antennae)
                        {
                            tip = Mathf.Pow(Mathf.InverseLerp(0f, 0.6f, tip), 0.5f);
                            if (part == 0)
                            {
                                return Color.Lerp(head ? headColour : effectColour, Color.Lerp(effectColour, self.palette.blackColor, flicker), tip);
                            }
                            return Color.Lerp(effectColour, new Color(1f, 1f, 1f, antennae.alpha), flicker);
                        }
                    }
                    else if (self.cosmetics[c] is AxolotlGills axolotlGills)
                    {
                        for (int i = axolotlGills.startSprite + axolotlGills.scalesPositions.Length - 1; i >= axolotlGills.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = headColour;

                            if (axolotlGills.colored)
                            {
                                sLeaser.sprites[i + axolotlGills.scalesPositions.Length].color = effectColour;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is BodyStripes bodyStripes)
                    {
                        for (int i = bodyStripes.startSprite + bodyStripes.scalesPositions.Length - 1; i >= bodyStripes.startSprite; i--)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                if (j > 1)
                                {
                                    (sLeaser.sprites[i] as TriangleMesh).verticeColors[j] = effectColour;
                                }
                                else
                                {
                                    (sLeaser.sprites[i] as TriangleMesh).verticeColors[j] = bodyColour;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is BumpHawk bumpHawk)
                    {
                        for (int i = bumpHawk.startSprite + bumpHawk.numberOfSprites - 1; i >= bumpHawk.startSprite; i--)
                        {
                            float num = Mathf.InverseLerp((float)bumpHawk.startSprite, (float)(bumpHawk.startSprite + bumpHawk.numberOfSprites - 1), (float)i);
                            float num2 = Mathf.Lerp(0.05f, bumpHawk.spineLength / self.BodyAndTailLength, num);
                            LizardGraphics.LizardSpineData lizardSpineData = self.SpinePosition(num2, timeStacker);

                            if (bumpHawk.coloredHawk)
                            {
                                sLeaser.sprites[i].color = Color.Lerp(headColour, bodyColour, num);
                            }
                            else
                            {
                                sLeaser.sprites[i].color = bodyColour;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is JumpRings jumpRings)
                    {
                        Color ringsColor = headColour;
                        if (self.lizard.animation == Lizard.Animation.PrepareToJump)
                        {
                            float num = 0.5f + 0.5f * Mathf.InverseLerp((float)self.lizard.timeToRemainInAnimation, 0f, (float)self.lizard.timeInAnimation);
                            ringsColor = Color.Lerp(headColour, Color.Lerp(Color.white, effectColour, num), UnityEngine.Random.value);
                        }
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 0)].color = new Color(1f, 0f, 0f);
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 0)].color = ringsColor;
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 1)].color = bodyColour;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongBodyScales longBodyScales)
                    {
                        for (int i = longBodyScales.startSprite + longBodyScales.scalesPositions.Length - 1; i >= longBodyScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = Camo(self, self.BodyColor(longBodyScales.scalesPositions[i - longBodyScales.startSprite].y));
                            if (longBodyScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = CamoElectric(self, data2, self.HeadColor(1f));
                                }
                                else
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = effectColour;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongHeadScales longHeadScales)
                    {
                        for (int i = longHeadScales.startSprite + longHeadScales.scalesPositions.Length - 1; i >= longHeadScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = headColour;
                            if (longHeadScales.colored)
                            {
                                sLeaser.sprites[i + longHeadScales.scalesPositions.Length].color = effectColour;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongShoulderScales longShoulderScales)
                    {
                        for (int i = longShoulderScales.startSprite + longShoulderScales.scalesPositions.Length - 1; i >= longShoulderScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = Camo(self, self.BodyColor(longShoulderScales.scalesPositions[i - longShoulderScales.startSprite].y));
                            if (longShoulderScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = CamoElectric(self, data2, self.HeadColor(1f));
                                }
                                else
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = effectColour;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is ShortBodyScales shortBodyScales)
                    {
                        for (int i = shortBodyScales.startSprite + shortBodyScales.scalesPositions.Length - 1; i >= shortBodyScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = headColour;
                        }
                    }
                    else if (self.cosmetics[c] is SpineSpikes spineSpikes)
                    {
                        for (int i = spineSpikes.startSprite; i < spineSpikes.startSprite + spineSpikes.bumps; i++)
                        {
                            float f = Mathf.Lerp(0.05f, spineSpikes.spineLength / self.BodyAndTailLength, Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i));
                            sLeaser.sprites[i].color = Camo(self, self.BodyColor(f));
                            if (spineSpikes.colored == 1)
                            {
                                sLeaser.sprites[i + spineSpikes.bumps].color = effectColour;
                            }
                            else if (spineSpikes.colored == 2)
                            {
                                float f2 = Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i);
                                sLeaser.sprites[i + spineSpikes.bumps].color = CamoElectric(self, data2, Color.Lerp(effectColour, self.BodyColor(f), Mathf.Pow(f2, 0.5f)));
                            }
                        }
                    }
                    else if (self.cosmetics[c] is TailFin tailFin)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int num = i * (tailFin.colored ? (tailFin.bumps * 2) : tailFin.bumps);
                            for (int j = tailFin.startSprite; j < tailFin.startSprite + tailFin.bumps; j++)
                            {
                                float f = Mathf.Lerp(0.05f, tailFin.spineLength / self.BodyAndTailLength, Mathf.InverseLerp((float)tailFin.startSprite, (float)(tailFin.startSprite + tailFin.bumps - 1), (float)j));
                                sLeaser.sprites[j + num].color = Camo(self, self.BodyColor(f));
                                if (tailFin.colored)
                                {
                                    sLeaser.sprites[j + tailFin.bumps + num].color = effectColour;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is TailGeckoScales tailGeckoScales)
                    {
                        if (tailGeckoScales.bigScales)
                        {
                            LizardGraphics.LizardSpineData lizardSpineData = self.SpinePosition(0.4f, timeStacker);
                            for (int i = 0; i < tailGeckoScales.rows; i++)
                            {
                                float num = Mathf.InverseLerp(0f, (float)(tailGeckoScales.rows - 1), (float)i);
                                float num2 = Mathf.Lerp(0.5f, 0.99f, Mathf.Pow(num, 0.8f));
                                LizardGraphics.LizardSpineData lizardSpineData2 = self.SpinePosition(num2, timeStacker);
                                Color a = Camo(self, self.BodyColor(num2));
                                for (int j = 0; j < tailGeckoScales.lines; j++)
                                {
                                    float num3 = ((float)j + ((i % 2 == 0) ? 0.5f : 0f)) / (float)(tailGeckoScales.lines - 1);
                                    num3 = -1f + 2f * num3;
                                    num3 += Mathf.Lerp(self.lastDepthRotation, self.depthRotation, timeStacker);
                                    if (num3 < -1f)
                                    {
                                        num3 += 2f;
                                    }
                                    else if (num3 > 1f)
                                    {
                                        num3 -= 2f;
                                    }
                                    Vector2 vector = lizardSpineData.pos + lizardSpineData.perp * (lizardSpineData.rad + 0.5f) * num3;
                                    Vector2 vector2 = lizardSpineData2.pos + lizardSpineData2.perp * (lizardSpineData2.rad + 0.5f) * num3;
                                    if (self.iVars.tailColor > 0f)
                                    {
                                        float num4 = Mathf.InverseLerp(0.5f, 1f, Mathf.Abs(Vector2.Dot(Custom.DirVec(vector2, vector), Custom.DegToVec(-45f + 120f * num3))));
                                        num4 = Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.3f, 0f) + 0.7f * Mathf.Pow(num4 * Mathf.Pow(self.iVars.tailColor, 0.3f), Mathf.Lerp(2f, 0.5f, num));
                                        if (num < 0.5f)
                                        {
                                            num4 *= Custom.LerpMap(num, 0f, 0.5f, 0.2f, 1f);
                                        }
                                        num4 = Mathf.Pow(num4, Mathf.Lerp(2f, 0.5f, num));
                                        if (num4 < 0.5f)
                                        {
                                            sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Color.Lerp(a, effectColour, Mathf.InverseLerp(0f, 0.5f, num4));
                                        }
                                        else
                                        {
                                            sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Camo(self, Color.Lerp(effectColour, Color.white, Mathf.InverseLerp(0.5f, 1f, num4)));
                                        }
                                    }
                                    else
                                    {
                                        sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Color.Lerp(a, effectColour, Custom.LerpMap(num, 0f, 0.8f, 0.2f, Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.8f, 0.4f), 0.8f));
                                    }
                                }
                                lizardSpineData = lizardSpineData2;
                            }
                            return;
                        }
                        for (int k = 0; k < tailGeckoScales.rows; k++)
                        {
                            float f = Mathf.InverseLerp(0f, (float)(tailGeckoScales.rows - 1), (float)k);
                            float num5 = Mathf.Lerp(0.4f, 0.95f, Mathf.Pow(f, 0.8f));
                            Color geckoColor = Color.Lerp(Camo(self, self.BodyColor(num5)), effectColour, 0.2f + 0.8f * Mathf.Pow(f, 0.5f));
                            for (int l = 0; l < tailGeckoScales.lines; l++)
                            {
                                sLeaser.sprites[tailGeckoScales.startSprite + k * tailGeckoScales.lines + l].color = new Color(1f, 0f, 0f);
                                sLeaser.sprites[tailGeckoScales.startSprite + k * tailGeckoScales.lines + l].color = geckoColor;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is TailTuft tailTuft)
                    {
                        for (int i = tailTuft.startSprite + tailTuft.scalesPositions.Length - 1; i >= tailTuft.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = Camo(self, self.BodyColor(tailTuft.scalesPositions[i - tailTuft.startSprite].y));
                            if (tailTuft.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = CamoElectric(self, data2, self.HeadColor(1f));
                                }
                                else
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = effectColour;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is Whiskers whiskers)
                    {
                        for (int i = 0; i < whiskers.amount; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                {
                                    for (int l = k * 4; l < k * 4 + ((k == 3) ? 3 : 4); l++)
                                    {
                                        (sLeaser.sprites[whiskers.startSprite + i * 2 + j] as TriangleMesh).verticeColors[l] = Color.Lerp(headColour, new Color(1f, 1f, 1f), (float)(k - 1) / 2f * Mathf.Lerp(whiskers.whiskerLightUp[i, j, 1], whiskers.whiskerLightUp[i, j, 0], timeStacker));
                                    }
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is WingScales wingScales)
                    {
                        for (int i = 0; i < wingScales.numberOfSprites; i++)
                        {
                            if (ModManager.DLCShared && self.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard)
                            {
                                sLeaser.sprites[wingScales.startSprite + i].color = Camo(self, self.BodyColor(0f));
                            }
                            else
                            {
                                sLeaser.sprites[wingScales.startSprite + i].color = bodyColour;
                            }
                        }
                    }

                    if (!ModManager.Watcher)
                    {
                        continue;
                    }

                    else if (self.cosmetics[c] is SkinkStripes skinkStripes)
                    {
                        for (int i = 0; i < skinkStripes.segs; i++)
                        {
                            float s = Mathf.InverseLerp(0f, (float)skinkStripes.segs, (float)i);
                            LizardGraphics.LizardSpineData lizardSpineData = skinkStripes.lGraphics.SpinePosition(s, timeStacker);

                            Color colorStripes = CamoElectric(self, data2, skinkStripes.SetColorAlpha(skinkStripes.color, 0.5f + lizardSpineData.depthRotation * 0.5f));
                            (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).verticeColors[i * 2] = colorStripes;
                            (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).verticeColors[i * 2 + 1] = colorStripes;
                        }
                        if (self.iVars.tailColor > 0f)
                        {
                            for (int i = 0; i < (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors.Length; i++)
                            {
                                float f = (float)(i / 2) * 2f / (float)((sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors.Length - 1);
                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors[i] = Camo(self, self.BodyColor(f));
                            }
                        }
                        else
                        {
                            sLeaser.sprites[skinkStripes.startSprite].color = Camo(self, self.palette.blackColor);
                        }
                    }
                    else if (self.cosmetics[c] is SkinkSpeckles skinkSpeckles)
                    {
                        for (int i = 0; i < skinkSpeckles.spots; i++)
                        {
                            sLeaser.sprites[skinkSpeckles.startSprite + i].color = Camo(self, self.BodyColor(skinkSpeckles.spotInfo[i].x));
                        }
                    }
                }
                #endregion
            }
            else if (data2.camoOnce)
            {
                data2.camoOnce = false;

                if (ModManager.DLCShared && (self.Caramel || self.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard))
                {
                    self.ColorBody(sLeaser, self.ivarBodyColor);
                }
                else if (self.lizard.Template.type == CreatureTemplate.Type.Salamander)
                {
                    self.ColorBody(sLeaser, self.SalamanderColor);
                }
                else
                {
                    self.ColorBody(sLeaser, self.palette.blackColor);
                }

                if (self.lizard.Template.type == CreatureTemplate.Type.CyanLizard || self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard)
                {
                    sLeaser.sprites[self.SpriteHeadStart].color = self.palette.blackColor;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = self.palette.blackColor;
                }
                else if (self.lizard.Template.type == CreatureTemplate.Type.Salamander)
                {
                    sLeaser.sprites[self.SpriteHeadStart].color = self.SalamanderColor;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = self.SalamanderColor;
                }

                if (self.iVars.tailColor > 0f)
                {
                    for (int j = 0; j < (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length; j++)
                    {
                        float t = (float)(j / 2) * 2f / (float)((sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length - 1);
                        (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors[j] = self.BodyColor(Mathf.Lerp(self.bodyLength / self.BodyAndTailLength, 1f, t));
                    }
                }

                for (int c = 0; c < self.cosmetics.Count; c++)
                {
                    if (self.cosmetics[c] is JumpRings jumpRings)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 1)].color = self.BodyColor(timeStacker);
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongBodyScales longBodyScales)
                    {
                        for (int i = longBodyScales.startSprite + longBodyScales.scalesPositions.Length - 1; i >= longBodyScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = self.BodyColor(longBodyScales.scalesPositions[i - longBodyScales.startSprite].y);
                            if (longBodyScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = self.HeadColor(1f);
                                }
                                else
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = self.effectColor;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongShoulderScales longShoulderScales)
                    {
                        for (int i = longShoulderScales.startSprite + longShoulderScales.scalesPositions.Length - 1; i >= longShoulderScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = self.BodyColor(longShoulderScales.scalesPositions[i - longShoulderScales.startSprite].y);
                            if (longShoulderScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = self.HeadColor(1f);
                                }
                                else
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = self.effectColor;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is SpineSpikes spineSpikes)
                    {
                        for (int i = spineSpikes.startSprite; i < spineSpikes.startSprite + spineSpikes.bumps; i++)
                        {
                            float f = Mathf.Lerp(0.05f, spineSpikes.spineLength / self.BodyAndTailLength, Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i));
                            if (spineSpikes.colored == 1)
                            {
                                sLeaser.sprites[i + spineSpikes.bumps].color = self.effectColor;
                            }
                            else if (spineSpikes.colored == 2)
                            {
                                float f2 = Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i);
                                sLeaser.sprites[i + spineSpikes.bumps].color = Color.Lerp(self.effectColor, self.BodyColor(f), Mathf.Pow(f2, 0.5f));
                            }
                        }
                    }
                    else if (self.cosmetics[c] is TailFin tailFin)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int num = i * tailFin.bumps * 2;
                            for (int j = tailFin.startSprite; j < tailFin.startSprite + tailFin.bumps; j++)
                            {
                                float f = Mathf.Lerp(0.05f, tailFin.spineLength / self.BodyAndTailLength, Mathf.InverseLerp((float)tailFin.startSprite, (float)(tailFin.startSprite + tailFin.bumps - 1), (float)j));
                                sLeaser.sprites[j + num].color = self.BodyColor(f);
                                if (tailFin.colored)
                                {
                                    sLeaser.sprites[j + tailFin.bumps + num].color = self.effectColor;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is TailTuft tailTuft)
                    {
                        for (int i = tailTuft.startSprite + tailTuft.scalesPositions.Length - 1; i >= tailTuft.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = self.BodyColor(tailTuft.scalesPositions[i - tailTuft.startSprite].y);
                            if (tailTuft.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = self.HeadColor(1f);
                                }
                                else
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = self.effectColor;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is Whiskers whiskers)
                    {
                        for (int i = 0; i < whiskers.amount; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                {
                                    for (int l = k * 4; l < k * 4 + ((k == 3) ? 3 : 4); l++)
                                    {
                                        (sLeaser.sprites[whiskers.startSprite + i * 2 + j] as TriangleMesh).verticeColors[l] = Color.Lerp(self.HeadColor(timeStacker), new Color(1f, 1f, 1f), (float)(k - 1) / 2f * Mathf.Lerp(whiskers.whiskerLightUp[i, j, 1], whiskers.whiskerLightUp[i, j, 0], timeStacker));
                                    }
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is WingScales wingScales)
                    {
                        for (int i = 0; i < wingScales.numberOfSprites; i++)
                        {
                            if (ModManager.DLCShared && wingScales.lGraphics.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard)
                            {
                                sLeaser.sprites[wingScales.startSprite + i].color = wingScales.lGraphics.BodyColor(0f);
                            }
                            else
                            {
                                sLeaser.sprites[wingScales.startSprite + i].color = self.palette.blackColor;
                            }
                        }
                    }

                    if (!ModManager.Watcher)
                    {
                        continue;
                    }
                    else if (self.cosmetics[c] is SkinkStripes skinkStripes)
                    {
                        if (self.iVars.tailColor > 0f)
                        {
                            for (int i = 0; i < (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors.Length; i++)
                            {
                                float f = (float)(i / 2) * 2f / (float)((sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors.Length - 1);
                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).verticeColors[i] = self.BodyColor(f);
                            }
                        }
                        else
                        {
                            sLeaser.sprites[skinkStripes.startSprite].color = self.palette.blackColor;
                        }
                    }
                    else if (self.cosmetics[c] is SkinkSpeckles skinkSpeckles)
                    {
                        for (int i = 0; i < skinkSpeckles.spots; i++)
                        {
                            sLeaser.sprites[skinkSpeckles.startSprite + i].color = self.BodyColor(skinkSpeckles.spotInfo[i].x);
                        }
                    }
                }
            }

            Color Electrify(Color col)
            {
                return Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)(data2.electricColorTimer / 50f));
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static Color WhiteNoCamoHeadColor(LizardGraphics self, float timeStacker)
    {
        if (self.whiteFlicker > 0 && (self.whiteFlicker > 15 || self.everySecondDraw))
        {
            return new Color(1f, 1f, 1f);
        }
        float num = 1f - Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastBlink, self.blink, timeStacker) * 2f * 3.1415927f), 1.5f + self.lizard.AI.excitement * 1.5f);
        if (self.headColorSetter != 0f)
        {
            num = Mathf.Lerp(num, (self.headColorSetter > 0f) ? 1f : 0f, Mathf.Abs(self.headColorSetter));
        }
        if (self.flicker > 10)
        {
            num = self.flickerColor;
        }
        num = Mathf.Lerp(num, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(self.lastVoiceVisualization, self.voiceVisualization, timeStacker)), 0.75f), Mathf.Lerp(self.lastVoiceVisualizationIntensity, self.voiceVisualizationIntensity, timeStacker));
        return Color.Lerp(new Color(1f, 1f, 1f), self.palette.blackColor, num);
    }

    public static Color Camo(LizardGraphics self, Color col)
    {
        return Color.Lerp(col, self.whiteCamoColor, self.whiteCamoColorAmount);
    }

    /*
    static void LizardGraphicsUpdate(On.LizardGraphics.orig_Update orig, LizardGraphics self)
    {
        orig.Invoke(self);
        try
        {
            if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                return;
            }

            Debug.Log(self.whiteGlitchFit);
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    */

    static void LizardGraphicsInitiateSprites(On.LizardGraphics.orig_InitiateSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);

        try
        {
            if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                return;
            }
            else if (data.isGoreHalf)
            {
                goto Line1;
            }

            if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _) && data.liz["UpperTeeth"] != "Incompatible")
            {
                if (self.lizard.lizardParams.headGraphics[2] != 0 && self.lizard.lizardParams.headGraphics[2] != 1 && self.lizard.lizardParams.headGraphics[2] != 2 && self.lizard.lizardParams.headGraphics[2] != 3 && self.lizard.lizardParams.headGraphics[2] != 8 && self.lizard.lizardParams.headGraphics[2] != 9)
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
                    if (!graphicstorage.TryGetValue(self, out GraphicsData data2))
                    {
                        graphicstorage.Add(self, new GraphicsData());
                        graphicstorage.TryGetValue(self, out data2);
                    }

                    data2.eyeSprites = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
                    sLeaser.sprites[data2.eyeSprites] = new FSprite("pixel", true);
                    sLeaser.sprites[data2.eyeSprites + 1] = new FSprite("pixel", true);
                }
            }

        Line1:
            if (ShadowOfOptions.cut_in_half.Value)
            {
                if (!graphicstorage.TryGetValue(self, out GraphicsData data2))
                {
                    graphicstorage.Add(self, new GraphicsData());
                    graphicstorage.TryGetValue(self, out data2);
                }

                data2.cutHalfSprites = sLeaser.sprites.Length;
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
                sLeaser.sprites[data2.cutHalfSprites] = new FSprite("pixel", true);
                sLeaser.sprites[data2.cutHalfSprites + 1] = new FSprite("pixel", true);

                sLeaser.sprites[data2.cutHalfSprites].isVisible = false;
                sLeaser.sprites[data2.cutHalfSprites + 1].isVisible = false;
            }

            self.AddToContainer(sLeaser, rCam, null);
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardGraphicsAddToContainer(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig.Invoke(self, sLeaser, rCam, newContatiner);

        if (!graphicstorage.TryGetValue(self, out GraphicsData data2) || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return;
        }
        else if (data.isGoreHalf)
        {
            goto Line1;
        }

        if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && data.liz["EyeRight"] != "Incompatible" && sLeaser.sprites.Length > data2.eyeSprites && data2.eyeSprites != 0)
        {
            newContatiner ??= rCam.ReturnFContainer("Midground");
            newContatiner.AddChild(sLeaser.sprites[data2.eyeSprites]);
            newContatiner.AddChild(sLeaser.sprites[data2.eyeSprites + 1]);
            sLeaser.sprites[data2.eyeSprites].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
            sLeaser.sprites[data2.eyeSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
        }

    Line1:
        if (ShadowOfOptions.cut_in_half.Value && sLeaser.sprites.Length > data2.cutHalfSprites && data2.cutHalfSprites != 0)
        {
            newContatiner ??= rCam.ReturnFContainer("Midground");
            newContatiner.AddChild(sLeaser.sprites[data2.cutHalfSprites]);
            newContatiner.AddChild(sLeaser.sprites[data2.cutHalfSprites + 1]);
        }
    }

    static int LizardGraphicsAddCosmetic(On.LizardGraphics.orig_AddCosmetic orig, LizardGraphics self, int spriteIndex, Template cosmetic)
    {
        if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return orig.Invoke(self, spriteIndex, cosmetic);
        }
        else if (ShadowOfOptions.water_breather.Value && cosmetic is AxolotlGills && (!ModManager.Watcher || self.lizard.Template.type != WatcherEnums.CreatureTemplateType.BlizzardLizard) && data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather != "True" || !data.cosmeticBodychunks.Contains(0) && cosmetic is Whiskers && self.lizard.Template.type != CreatureTemplate.Type.BlackLizard || !data.cosmeticBodychunks.Contains(2) && (cosmetic is TailFin || cosmetic is TailGeckoScales || cosmetic is TailTuft))
        {
            return spriteIndex;
        }

        return orig.Invoke(self, spriteIndex, cosmetic);
    }

    static void NewAntennae(On.LizardCosmetics.Antennae.orig_ctor orig, Antennae self, LizardGraphics lGraphics, int startSprite)
    {
        orig(self, lGraphics, startSprite);

        if (ShadowOfOptions.melted_transformation.Value && lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data) && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
        {
            self.redderTint = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
        }
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