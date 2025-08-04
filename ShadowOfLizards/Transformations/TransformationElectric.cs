using System;
using RWCustom;
using UnityEngine;
using LizardCosmetics;
using Watcher;
using MoreSlugcats;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class TransformationElectric
{
    public static void Apply()
    {
        On.MoreSlugcats.ElectricSpear.Electrocute += SpearRecharge;

        On.Player.EatMeatUpdate += PlayerEatElectricLizard;

        On.LizardSpit.ctor += NewElectricSpit;

        On.LizardGraphics.Update += ELectricLizardGraphicsUpdate;
    }

    public static void ElectricBubbleDraw( LizardBubble self, RoomCamera.SpriteLeaser sLeaser, float timeStacker, GraphicsData data2, bool camo)
    {
        if (data2.ElectricColorTimer <= 0)
        {
            return;
        }

        try
        {

            Color color = Color.Lerp(self.lizardGraphics.effectColor, new Color(0.7f, 0.7f, 1f), (float)data2.ElectricColorTimer / 50f);

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
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    #region Misc
    static void SpearRecharge(On.MoreSlugcats.ElectricSpear.orig_Electrocute orig, ElectricSpear self, PhysicalObject otherObject)
    {
        orig.Invoke(self, otherObject);

        if (!ShadowOfOptions.electric_transformation.Value || otherObject == null || otherObject is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || (data.transformation != "Electric" && data.transformation != "ElectricTransformation"))
        {
            return;
        }

        try
        {
            if (self.abstractSpear.electricCharge == 0)
            {
                self.Recharge();

                if (data.transformation != "ElectricTransformation")
                {
                    data.transformationTimer--;
                    if (data.transformationTimer < 0f)
                    {
                        data.transformation = "Null";
                    }
                }
            }
            else if (data.transformation != "ElectricTransformation")
            {
                data.transformationTimer++;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void PlayerEatElectricLizard(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig.Invoke(self, graspIndex);

        if (!ShadowOfOptions.electric_transformation.Value || self.grasps[graspIndex] == null || self.grasps[graspIndex].grabbed is not Lizard liz || liz.graphicsModule == null || !graphicstorage.TryGetValue(liz.graphicsModule as LizardGraphics, out GraphicsData data2) 
            || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || data.transformation != "ElectricTransformation" || self.eatMeat <= 40 || self.eatMeat % 15 != 3)
        {
            return;
        }

        try
        {
            SoundID centipede_Shock = SoundID.Centipede_Shock;
            liz.room.PlaySound(centipede_Shock, liz.mainBodyChunk.pos);
            LizardSpark(liz, liz.mainBodyChunk, data2, 1, true);
            liz.mainBodyChunk.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
            liz.mainBodyChunk.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                BodyChunk obj = self.bodyChunks[i];
                obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }

            if (self != null)
            {
                self.Stun((int)Custom.LerpMap(self.TotalMass, 0f, liz.TotalMass * 2f, 300f, 30f));
                self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                self.LoseAllGrasps();

                data2.ElectricColorTimer = Mathf.Min(data2.ElectricColorTimer + 50, 250);;
            }

            if (self.Submersion > 0f)
            {
                Room room = self.room;
                room.AddObject(new UnderwaterShock(room, self, liz.mainBodyChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, 1400f), 2.1f, self, new Color(0.7f, 0.7f, 1f)));
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region Spit
    static void NewElectricSpit(On.LizardSpit.orig_ctor orig, LizardSpit self, Vector2 pos, Vector2 vel, Lizard lizard)
    {
        orig(self, pos, vel, lizard);

        if (!ShadowOfOptions.electric_transformation.Value || !ShadowOfOptions.electric_spit.Value || self.lizard == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data)
            || (data.transformation != "ElectricTransformation" && data.transformation != "Electric"))
        {
            return;
        }

        if (!ShockSpit.TryGetValue(self, out ElectricSpit data2))
        {
            ShockSpit.Add(self, new ElectricSpit());
            ShockSpit.TryGetValue(self, out ElectricSpit dat2);
            data2 = dat2;
        }

        data2.ElectricColorTimer = (lizard.graphicsModule != null && graphicstorage.TryGetValue(lizard.graphicsModule as LizardGraphics, out GraphicsData data3)) ? data3.ElectricColorTimer : 0;
    }

    public static void ElectricSpitUpdate(LizardSpit self, ElectricSpit data)
    {
        try
        {
            if (self.stickChunk != null && self.stickChunk.owner.room == self.room && Custom.DistLess(self.stickChunk.pos, self.pos, self.stickChunk.rad + 40f) && self.fallOff > 0 && !data.Shocked)
            {
                data.Shocked = true;

                PhysicalObject owner = self.stickChunk.owner;
                self.room.PlaySound(SoundID.Centipede_Shock, self.pos);
                SpitSpark(data, 1, true);
                self.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                self.pos += Custom.RNV() * 6f * UnityEngine.Random.value;

                data.lightFlash = 1f;

                for (int j = 0; j < owner.bodyChunks.Length; j++)
                {
                    BodyChunk obj = owner.bodyChunks[j];
                    obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                    obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
                }

                if (owner is Creature creature)
                {
                    creature.Stun((int)(Custom.LerpMap(owner.TotalMass, 0f, self.massLeft * 2f, 300f, 30f) * (owner is Centipede ? 0.4f : 1f)));
                    self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
                    creature.LoseAllGrasps();
                }

                if (owner.Submersion > 0f)
                {
                    self.room.AddObject(new UnderwaterShock(self.room, self.lizard, self.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, 1400f), 2.1f, self.lizard, new Color(0.7f, 0.7f, 1f)));
                }
            }
            if (!data.Shocked)
            {
                SpitSpark(data, UnityEngine.Random.Range(0, 11), false);
            }

            if (data.lightSource != null)
            {
                data.lightSource.stayAlive = true;
                data.lightSource.setPos = new Vector2?(self.pos);
                data.lightSource.setRad = new float?(300f * Mathf.Pow(data.lightFlash * UnityEngine.Random.value, 0.1f) * Mathf.Lerp(0.5f, 2f, self.massLeft / 2));
                data.lightSource.setAlpha = new float?(Mathf.Pow(data.lightFlash * UnityEngine.Random.value, 0.1f));
                float num5 = data.lightFlash * UnityEngine.Random.value;
                num5 = Mathf.Lerp(num5, 1f, 0.5f * (1f - self.lizard.room.Darkness(self.pos)));
                data.lightSource.color = new Color(num5, num5, 1f);
                if (data.lightFlash <= 0f)
                {
                    data.lightSource.Destroy();
                }
                if (data.lightSource.slatedForDeletetion)
                {
                    data.lightSource = null;
                }
            }
            else if (data.lightFlash > 0f)
            {
                data.lightSource = new LightSource(self.lizard.mainBodyChunk.pos, false, new Color(1f, 1f, 1f), self.lizard);
                data.lightSource.affectedByPaletteDarkness = 0f;
                data.lightSource.requireUpKeep = true;
                self.lizard.room.AddObject(data.lightSource);
            }
            if (data.lightFlash > 0f)
            {
                data.lightFlash = Mathf.Max(0f, data.lightFlash - 0.033333335f);
            }

            void SpitSpark(ElectricSpit data, int number, bool guarantee)
            {
                if (data == null)
                {
                    return;
                }

                if (guarantee)
                {
                    for (int i = 0; i < number; i++)
                    {
                        Spark();
                    }

                    return;
                }

                for (int i = 0; i < number; i++)
                {
                    if (UnityEngine.Random.value < (data.ElectricColorTimer == 0 ? 0.025f : 0.025f * (float)data.ElectricColorTimer))
                    {
                        Spark();
                    }
                }

                void Spark()
                {
                    self.room.AddObject(new Spark(self.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void ElectricSpitDraw(LizardSpit self, RoomCamera.SpriteLeaser sLeaser, ElectricSpit data)
    {
        try
        {
            if (data.origColor == null)
            {
                data.origColor = sLeaser.sprites[self.DotSprite].color;
            }

            if (data.ElectricColorTimer > 0)
            {
                data.once = true;

                Color color = Color.Lerp(data.origColor, new Color(0.7f, 0.7f, 1f), (float)data.ElectricColorTimer / 50f);

                data.ElectricColorTimer--;

                sLeaser.sprites[self.DotSprite].color = color;

            }
            else if (data.once)
            {
                data.once = false;

                sLeaser.sprites[self.DotSprite].color = data.origColor;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region Graphics
    static void ELectricLizardGraphicsUpdate(On.LizardGraphics.orig_Update orig, LizardGraphics self)
    {
        orig(self);

        if (!ShadowOfOptions.electric_transformation.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data)
            || !graphicstorage.TryGetValue(self, out GraphicsData data2) || (data.transformation != "ElectricTransformation" && data.transformation != "Electric"))
        {
            return;
        }

        try
        {
            if (data2.lightSource != null)
            {
                data2.lightSource.stayAlive = true;
                data2.lightSource.setPos = new Vector2?(self.lizard.mainBodyChunk.pos);
                data2.lightSource.setRad = new float?(300f * Mathf.Pow(data2.lightFlash * UnityEngine.Random.value, 0.1f) * Mathf.Lerp(0.5f, 2f, self.lizard.TotalMass / 2));
                data2.lightSource.setAlpha = new float?(Mathf.Pow(data2.lightFlash * UnityEngine.Random.value, 0.1f));
                float num5 = data2.lightFlash * UnityEngine.Random.value;
                num5 = Mathf.Lerp(num5, 1f, 0.5f * (1f - self.lizard.room.Darkness(self.lizard.mainBodyChunk.pos)));
                data2.lightSource.color = new Color(num5, num5, 1f);
                if (data2.lightFlash <= 0f)
                {
                    data2.lightSource.Destroy();
                }
                if (data2.lightSource.slatedForDeletetion)
                {
                    data2.lightSource = null;
                }
            }
            else if (data2.lightFlash > 0f)
            {
                data2.lightSource = new LightSource(self.lizard.mainBodyChunk.pos, false, new Color(1f, 1f, 1f), self.lizard);
                data2.lightSource.affectedByPaletteDarkness = 0f;
                data2.lightSource.requireUpKeep = true;
                self.lizard.room.AddObject(data2.lightSource);
            }
            if (data2.lightFlash > 0f)
            {
                data2.lightFlash = Mathf.Max(0f, data2.lightFlash - 0.033333335f);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void ElectricLizardGraphicsDraw(LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos, GraphicsData data2)
    {
        try
        {
            Color color = self.effectColor;
            Color headColor = self.effectColor;

            bool head = true;
            if (self.lizard.Template.type == CreatureTemplate.Type.BlackLizard || self.lizard.Template.type == CreatureTemplate.Type.Salamander || ModManager.Watcher && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BasiliskLizard
                || ModManager.Watcher && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard)
            {
                head = false;
            }

            if (data2.ElectricColorTimer > 0)
            {
                data2.once = true;

                color = Color.Lerp(self.effectColor, new Color(0.7f, 0.7f, 1f), (float)data2.ElectricColorTimer / 50f);

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
                    headColor = Color.Lerp(self.HeadColor1, color, num);
                }
                data2.ElectricColorTimer--;

                for (int s = self.SpriteLimbsColorStart; s < self.SpriteLimbsColorEnd; s++)
                {
                    sLeaser.sprites[s].color = color;
                }

                if (self.lizard.tongue != null && self.lizard.tongue.Out && self.lizard.Template.type == WatcherEnums.CreatureTemplateType.IndigoLizard)
                {
                    sLeaser.sprites[self.SpriteTongueStart + 1].color = color;
                }

                if (self.lizard.Template.type == WatcherEnums.CreatureTemplateType.BasiliskLizard)
                {
                    Color color4 = Color.Lerp(Color.Lerp(self.HeadColor(timeStacker), self.lizard.effectColor, 0.7f), new Color(0.7f, 0.7f, 1f), (float)data2.ElectricColorTimer / 50f);
                    if (self.whiteFlicker > 0 && (self.whiteFlicker > 15 || self.everySecondDraw))
                    {
                        color4 = Color.Lerp(new Color(1f, 1f, 1f), new Color(0.7f, 0.7f, 1f), (float)data2.ElectricColorTimer / 50f);
                    }
                    sLeaser.sprites[self.SpriteHeadStart].color = color4;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = color4;
                }
                else if (head && self.lizard.Template.type != CreatureTemplate.Type.CyanLizard)
                {
                    sLeaser.sprites[self.SpriteHeadStart].color = headColor;
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = headColor;
                }
                else if(self.lizard.Template.type == CreatureTemplate.Type.CyanLizard)
                {
                    sLeaser.sprites[self.SpriteHeadStart + 1].color = headColor;
                    sLeaser.sprites[self.SpriteHeadStart + 2].color = headColor;
                }

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
                            if (head)
                            {
                                sLeaser.sprites[antennae.startSprite + i].color = headColor;
                            }

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
                    }
                    else if (self.cosmetics[c] is AxolotlGills axolotlGills)
                    {
                        for (int i = axolotlGills.startSprite + axolotlGills.scalesPositions.Length - 1; i >= axolotlGills.startSprite; i--)
                        {
                            if (head)
                            {
                                sLeaser.sprites[i].color = headColor;
                            }

                            if (axolotlGills.colored)
                            {
                                sLeaser.sprites[i + axolotlGills.scalesPositions.Length].color = color;
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
                                    (sLeaser.sprites[i] as TriangleMesh).verticeColors[j] = color;
                                }
                                else
                                {
                                    (sLeaser.sprites[i] as TriangleMesh).verticeColors[j] = TailBodyColor(timeStacker);
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
                            sLeaser.sprites[i].x = lizardSpineData.outerPos.x - camPos.x;
                            sLeaser.sprites[i].y = lizardSpineData.outerPos.y - camPos.y;
                            if (bumpHawk.coloredHawk)
                            {
                                if (bumpHawk.coloredHawk)
                                {
                                    sLeaser.sprites[i].color = Color.Lerp(headColor, TailBodyColor(num2), num);
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is JumpRings jumpRings)
                    {
                        Color ringsColor = headColor;
                        if (self.lizard.animation == Lizard.Animation.PrepareToJump)
                        {
                            float num = 0.5f + 0.5f * Mathf.InverseLerp((float)self.lizard.timeToRemainInAnimation, 0f, (float)self.lizard.timeInAnimation);
                            ringsColor = Color.Lerp(headColor, Color.Lerp(Color.white, color, num), UnityEngine.Random.value);
                        }
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 0)].color = new Color(1f, 0f, 0f);
                                sLeaser.sprites[jumpRings.RingSprite(i, j, 0)].color = ringsColor;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongBodyScales longBodyScales)
                    {
                        for (int i = longBodyScales.startSprite + longBodyScales.scalesPositions.Length - 1; i >= longBodyScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = TailBodyColor(longBodyScales.scalesPositions[i - longBodyScales.startSprite].y);
                            if (longBodyScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = headColor;
                                }
                                else
                                {
                                    sLeaser.sprites[i + longBodyScales.scalesPositions.Length].color = color;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongHeadScales longHeadScales)
                    {
                        for (int i = longHeadScales.startSprite + longHeadScales.scalesPositions.Length - 1; i >= longHeadScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = headColor;
                            if (longHeadScales.colored)
                            {
                                sLeaser.sprites[i + longHeadScales.scalesPositions.Length].color = color;
                            }
                        }
                    }
                    else if (self.cosmetics[c] is LongShoulderScales longShoulderScales)
                    {
                        for (int i = longShoulderScales.startSprite + longShoulderScales.scalesPositions.Length - 1; i >= longShoulderScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = TailBodyColor(longShoulderScales.scalesPositions[i - longShoulderScales.startSprite].y);
                            if (longShoulderScales.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = headColor;
                                }
                                else
                                {
                                    sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].color = color;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is ShortBodyScales shortBodyScales)
                    {
                        for (int i = shortBodyScales.startSprite + shortBodyScales.scalesPositions.Length - 1; i >= shortBodyScales.startSprite; i--)
                        {
                            sLeaser.sprites[i].color = headColor;
                        }
                    }
                    else if (self.cosmetics[c] is SpineSpikes spineSpikes && spineSpikes.colored > 0)
                    {
                        for (int i = spineSpikes.startSprite; i < spineSpikes.startSprite + spineSpikes.bumps; i++)
                        {
                            float f = Mathf.Lerp(0.05f, spineSpikes.spineLength / self.BodyAndTailLength, Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i));
                            if (spineSpikes.colored == 1)
                            {
                                sLeaser.sprites[i + spineSpikes.bumps].color = color;
                            }
                            else if (spineSpikes.colored == 2)
                            {
                                float f2 = Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps - 1), (float)i);
                                sLeaser.sprites[i + spineSpikes.bumps].color = Color.Lerp(color, TailBodyColor(f), Mathf.Pow(f2, 0.5f));
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
                                sLeaser.sprites[j + num].color = TailBodyColor(f);
                                if (tailFin.colored)
                                {
                                    sLeaser.sprites[j + tailFin.bumps + num].color = color;
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
                                Color a = TailBodyColor(num2);
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
                                            sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Color.Lerp(a, color, Mathf.InverseLerp(0f, 0.5f, num4));
                                        }
                                        else
                                        {
                                            sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Color.Lerp(color, Color.white, Mathf.InverseLerp(0.5f, 1f, num4));
                                        }
                                    }
                                    else
                                    {
                                        sLeaser.sprites[tailGeckoScales.startSprite + i * tailGeckoScales.lines + j].color = Color.Lerp(a, color, Custom.LerpMap(num, 0f, 0.8f, 0.2f, Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.8f, 0.4f), 0.8f));
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
                            Color geckoColor = Color.Lerp(TailBodyColor(num5), color, 0.2f + 0.8f * Mathf.Pow(f, 0.5f));
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
                            sLeaser.sprites[i].color = TailBodyColor(tailTuft.scalesPositions[i - tailTuft.startSprite].y);
                            if (tailTuft.colored)
                            {
                                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = headColor;
                                }
                                else
                                {
                                    sLeaser.sprites[i + tailTuft.scalesPositions.Length].color = color;
                                }
                            }
                        }
                    }
                    else if (self.cosmetics[c] is Whiskers whiskers && head)
                    {
                        for (int i = 0; i < whiskers.amount; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                {
                                    for (int l = k * 4; l < k * 4 + ((k == 3) ? 3 : 4); l++)
                                    {
                                        (sLeaser.sprites[whiskers.startSprite + i * 2 + j] as TriangleMesh).verticeColors[l] = Color.Lerp(headColor, new Color(1f, 1f, 1f), (float)(k - 1) / 2f * Mathf.Lerp(whiskers.whiskerLightUp[i, j, 1], whiskers.whiskerLightUp[i, j, 0], timeStacker));
                                    }
                                }
                            }
                        }
                    }
                }

                if (self.iVars.tailColor > 0f)
                {
                    for (int j = 0; j < (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length; j++)
                    {
                        float t = (float)(j / 2) * 2f / (float)((sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length - 1);
                        (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors[j] = TailBodyColor(Mathf.Lerp(self.bodyLength / self.BodyAndTailLength, 1f, t));
                    }
                }
            }
            else if (data2.once)
            {
                data2.once = false;

                for (int s = self.SpriteLimbsColorStart; s < self.SpriteLimbsColorEnd; s++)
                {
                    sLeaser.sprites[s].color = color;
                }

                for (int c = 0; c < self.cosmetics.Count; c++)
                {
                    if (self.cosmetics[c] is LongBodyScales longBodyScales)
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
                    else if (self.cosmetics[c] is SpineSpikes spineSpikes && spineSpikes.colored > 0)
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
                    else if (self.cosmetics[c] is Whiskers whiskers && head)
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
                }

                if (self.iVars.tailColor > 0f)
                {
                    for (int j = 0; j < (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length; j++)
                    {
                        float t = (float)(j / 2) * 2f / (float)((sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors.Length - 1);
                        (sLeaser.sprites[self.SpriteTail] as TriangleMesh).verticeColors[j] = self.BodyColor(Mathf.Lerp(self.bodyLength / self.BodyAndTailLength, 1f, t));
                    }
                }
            }

            Color AntennaeEffectColor(int part, float tip, float flicker, Antennae antennae)
            {
                tip = Mathf.Pow(Mathf.InverseLerp(0f, 0.6f, tip), 0.5f);
                if (part == 0)
                {
                    return Color.Lerp(head ? headColor : color, Color.Lerp(color, self.palette.blackColor, flicker), tip);
                }
                return Color.Lerp(color, new Color(1f, 1f, 1f, antennae.alpha), flicker);
            }

            Color TailBodyColor(float f)
            {
                if (ModManager.DLCShared && (self.Caramel || self.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard) && (f < self.bodyLength / self.BodyAndTailLength || self.iVars.tailColor == 0f))
                {
                    return self.ivarBodyColor;
                }
                if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
                {
                    return self.DynamicBodyColor(f);
                }
                if (self.lizard.Template.type == CreatureTemplate.Type.Salamander)
                {
                    return self.SalamanderColor;
                }
                if (f < self.bodyLength / self.BodyAndTailLength || self.iVars.tailColor == 0f)
                {
                    return self.palette.blackColor;
                }
                float value = Mathf.InverseLerp(self.bodyLength / self.BodyAndTailLength, 1f, f);
                float num = Mathf.Clamp(Mathf.InverseLerp(self.lizard.lizardParams.tailColorationStart, 0.95f, value), 0f, 1f);
                num = Mathf.Pow(num, self.lizard.lizardParams.tailColorationExponent) * self.iVars.tailColor;
                if (ModManager.DLCShared && (self.Caramel || self.lizard.Template.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard))
                {
                    return Color.Lerp(self.ivarBodyColor, color, num);
                }
                return Color.Lerp(self.palette.blackColor, color, num);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    #endregion

    #region Lizard
    public static void ElectricLizardViolence(Lizard self, BodyChunk source, BodyChunk hitChunk, GraphicsData data)
    {
        try
        {
            PhysicalObject owner = source.owner;
            self.room.PlaySound(SoundID.Centipede_Shock, hitChunk.pos);
            LizardSpark(self, hitChunk, data, 1, true);
            hitChunk.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
            hitChunk.pos += Custom.RNV() * 6f * UnityEngine.Random.value;

            if (self.graphicsModule != null)
            {
                data.ElectricColorTimer = Mathf.Min(data.ElectricColorTimer + 50, 250);
                LizardSpark(self, self.mainBodyChunk, data, 1, true);
            }

            for (int i = 0; i < owner.bodyChunks.Length; i++)
            {
                BodyChunk obj = owner.bodyChunks[i];
                obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void ElectricLizardUpdate(Lizard self, LizardData data, GraphicsData data2)
    {
        if (self.grabbedBy.Count > 0 && !self.dead)
        {
            data2.shockCharge += 0.016666668f;
            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                self.bodyChunks[i].vel += Custom.RNV() * UnityEngine.Random.value * 2f;
            }
            if (data2.shockCharge >= 1f)
            {
                Creature grabber = self.grabbedBy[0].grabber;
                bool flag = ModManager.MSC && grabber is Player && (grabber as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint;

                Shock(self, data, data2, grabber);

                if (flag)
                {
                    (grabber as Player).SaintStagger(680);
                }
                self.Stun(14);
                for (int j = 0; j < self.bodyChunks.Length; j++)
                {
                    self.bodyChunks[j].vel += Custom.RNV() * UnityEngine.Random.value * 7f;
                }

                if (self.graphicsModule != null)
                {
                    data2.ElectricColorTimer = Mathf.Min(data2.ElectricColorTimer + 50, 250);
                    LizardSpark(self, self.mainBodyChunk, data2, 1, true);
                }
            }
        }

        LizardSpark(self, null, data2, UnityEngine.Random.Range(0, 11), false);

        data2.shockCharge = Mathf.Max(0f, data2.shockCharge - 0.008333334f);
    }

    public static void PreElectricLizardBite(Lizard self, BodyChunk chunk, GraphicsData graphicData, LizardData data)
    {
        try
        {
            if(chunk != null && chunk.owner != null)
                Shock(self, data, graphicData, chunk.owner);
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void PostElectricLizardBite(Lizard self, GraphicsData data, BodyChunk chunk)
    {
        if (self.graphicsModule != null)
        {
            data.lightFlash = 1f;
            for (int i = 0; i < (int)Mathf.Lerp(4f, 8f, self.TotalMass / 2); i++)
            {
                LizardSpark(self, self.mainBodyChunk, data, 1, true);
            }
        }

        if (chunk != null && chunk.owner != null && chunk.owner is Lizard liz && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data2))
        {
            if (!(liz.TotalMass < self.TotalMass / 2))
            {
                data2.lastDamageType = "Electric";
                PostViolenceCheck(liz, data2, "Electric", self);
            }
        }
    }
    #endregion

    static void LizardSpark(Lizard self, BodyChunk chunk, GraphicsData data, int number, bool guarantee)
    {
        if (self == null || data == null)
        {
            return;
        }

        try
        {
            if (guarantee)
            {
                for (int i = 0; i < number; i++)
                {
                    Spark();
                }

                return;
            }

            for (int i = 0; i < number; i++)
            {
                if (UnityEngine.Random.value < (data.ElectricColorTimer == 0 ? 0.025f : 0.025f * ((float)data.ElectricColorTimer / 1f)))
                {
                    Spark();
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        void Spark()
        {
            BodyChunk tempChunk = chunk == null ? self.bodyChunks[UnityEngine.Random.Range(0, self.bodyChunks.Length)] : chunk;
            Vector2 pos = tempChunk.pos + new Vector2(tempChunk.rad * UnityEngine.Random.Range(-1f, 1f), tempChunk.rad * UnityEngine.Random.Range(-1f, 1f));

            self.room.AddObject(new Spark(pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
        }
    }

    public static void ElectricEatRegrowth(Lizard self, Lizard liz, LizardData data, LizardData data2)
    {
        try
        {
            if (data.transformation == "Electric")
            {
                if (ElectricChance())
                {
                    data.transformationTimer++;
                    return;
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "ElectricTransformation" && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value, "Electric Regrowth by eating " + liz)) 
                    || (data2.transformation == "Electric" && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 0.5f, "Electric Regrowth by eating " + liz))))
                {
                    data.transformationTimer++;
                    return;
                }
            }
            else if (data.transformation == "Null" || data.transformation == "Spider")
            {
                if (ElectricChance())
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made Electric due to eating " + self.grasps[0].grabbed);

                    data.transformation = "Electric";
                    data.transformationTimer = 1;

                    return;
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "ElectricTransformation" && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value, "Electric Regrowth by eating " + liz)) ||
                        (data2.transformation == "Electric" && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 0.5f, "Electric Regrowth by eating " + liz))))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made Electric due to eating " + self.grasps[0].grabbed);

                    data.transformation = "Electric";
                    data.transformationTimer = 1;

                    return;
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        bool ElectricChance()
        {
            return self.grasps[0].grabbed is JellyFish && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 0.25f, "Electric Regrowth by eating YellyFish")
                || self.grasps[0].grabbed is Centipede centi && (centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SmallCentipede && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 0.5f, "Electric Regrowth by eating " + centi)
                || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centipede && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value, "Electric Regrowth by eating " + centi)
                || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 1.5f, "Electric Regrowth by eating " + centi)
                || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedCentipede && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 2f, "Electric Regrowth by eating " + centi)
                || (ModManager.DLCShared && centi.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.AquaCenti && Chance(self, ShadowOfOptions.electric_regrowth_chance.Value * 2f, "Electric Regrowth by eating " + centi)));
        }
    }

    static void Shock(Lizard self, LizardData data, GraphicsData graphicData, PhysicalObject shockObj)
    {
        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);

        if (self.graphicsModule != null)
        {
            graphicData.ElectricColorTimer = Mathf.Min(graphicData.ElectricColorTimer + 100, 250);
            LizardSpark(self, self.mainBodyChunk, graphicData, 1, true);
        }

        if (shockObj is Creature crit)
        {
            if (crit.TotalMass < self.TotalMass / 2)
            {
                if (ModManager.MSC && crit is Player player && (player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer || player.SlugCatClass != null && player.SlugCatClass.value == "sproutcat"))
                {
                    player.PyroDeath();
                }
                else
                {
                    if (crit is Lizard liz && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data2))
                    {
                        data2.lastDamageType = "Electric";
                        PreViolenceCheck(liz, data2);

                        crit.Die();

                        data2.lastDamageType = "Electric";
                        PreViolenceCheck(liz, data2);
                    }
                    else
                    {
                        crit.Die();
                    }

                    Room val = self.room;
                    val.AddObject(new CreatureSpasmer(crit, true, (int)Mathf.Lerp(70f, 120f, self.mainBodyChunk.rad)));
                }

                if (!crit.dead && data.transformation == "Electric" && crit is Centipede && UnityEngine.Random.value < 0.2)
                    data.transformationTimer++;
            }
            else
            {
                if (crit is Lizard liz && lizardstorage.TryGetValue(self.abstractCreature, out LizardData data2) && (data2.transformation == "Electric" || data2.transformation == "ElectricTransformation"))
                {
                    data2.lastDamageType = "Electric";

                    liz.Stun((int)(Custom.LerpMap(crit.TotalMass, 0f, self.TotalMass * 2f, 300f, 30f) * 0.2f));
                    self.room.AddObject(new CreatureSpasmer(liz, false, liz.stun));

                    if (!liz.dead && data.transformation == "Electric" && (data2.transformation == "Electric" || data2.transformation == "ElectricTransformation") && data2.transformationTimer > 0 && UnityEngine.Random.value < 0.2)
                    {
                        data.transformationTimer++;
                        if (data2.transformation == "Electric")
                            data2.transformationTimer--;
                    }
                }
                else
                {
                    crit.Stun((int)(Custom.LerpMap(crit.TotalMass, 0f, self.TotalMass * 2f, 300f, 30f) * (crit is Centipede ? 0.4f : 1f)));

                    Room val = self.room;
                    val.AddObject(new CreatureSpasmer(crit, false, crit.stun));
                    crit.LoseAllGrasps();
                    self.Stun(6);


                    if (!crit.dead && data.transformation == "Electric" && crit is Centipede && UnityEngine.Random.value < 0.2)
                        data.transformationTimer++;
                }
            }
        }

        if (shockObj.Submersion > 0f)
        {
            self.room.AddObject(new UnderwaterShock(self.room, self, self.mainBodyChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, self.mainBodyChunk.rad), 0.2f + 1.9f * self.mainBodyChunk.rad, self, new Color(0.7f, 0.7f, 1f)));
        }
    }
}
