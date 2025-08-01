using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ShadowOfLizards;

sealed class LizCutHead : PlayerCarryableItem, IDrawable
{
    #region values
    public float lastDarkness = -1f;
    public float darkness;

    public Color LizBodyColour;
    public Color LizEffectColour;

    public Color LizBloodColour;

    public Color LizEyeRightColour;
    public Color LizEyeLeftColour;
    #endregion

    public int ElectricColorTimer = 0;

    #region Values Misc
    public override float ThrowPowerFactor => 1f;

    private int whiteFlicker = 0;
    private int flicker;
    private float flickerColor = 0;

    public const int SourceCodeLizardsFlickerThreshold = 10;
    public const int SourceCodeLizardsWhiteFlickerThreshold = 15;

    public RoomPalette palette;

    public const int TotalSprites = 3;
    public const int SpriteJawStart = 2;

    public List<string> HeadSprites;

    public Vector2 rotation;
    public Vector2 lastRotation;

    public float jawRotation;
    public float lastJawRotation;

    public const float MaxJawRotation = 100f;
    public const float JawOpenSensitivity = 20f;
    public const float JawVelocityOverOpenSensitivity = 2.5f;

    public List<int> headSpriteNum = new() { 9, 16, 16, 10, 10, 10, 15, 14 };

    public Color whiteCamoColor = new(0f, 0f, 0f);
    public Color whitePickUpColor;
    private float whiteCamoColorAmount = -1f;

    public float baseBlink;
    public float baseLastBlink;

    public bool everySecondDraw;
    #endregion

    public LizCutHeadAbstract Abstr { get; }

    public LizCutHead(LizCutHeadAbstract abstr) : base(abstr)
    {
        Abstr = abstr;

        var pos = abstractPhysicalObject.Room.realizedRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile);

        bodyChunks = new[] 
            {
                new BodyChunk(this, 0, pos, Abstr.rad, Abstr.mass),
            };

        bodyChunkConnections = new BodyChunkConnection[0];

        airFriction = 0.97f;
        gravity = 0.9f;
        bounce = 0.1f;
        surfaceFriction = 0.45f;
        collisionLayer = 1;
        waterFriction = 0.92f;
        buoyancy = 0.75f;

        rotation = Vector2.zero;
        lastRotation = rotation;

        lastJawRotation = 0f;
        jawRotation = 0f;

        baseBlink = UnityEngine.Random.value;
        baseLastBlink = baseBlink;
    }

    private static float Rand => UnityEngine.Random.value;

    public override void PickedUp(Creature upPicker)
    {
        room.PlaySound(SoundID.Lizard_Light_Terrain_Impact, firstChunk);
        Flicker(20);
    }

    public override void HitByWeapon(Weapon weapon)
    {
        base.HitByWeapon(weapon);

        WhiteFlicker(20);
        Flicker(30);

        if (grabbedBy.Count > 0)
        {
            Creature grabber = grabbedBy[0].grabber;
            Vector2 push = firstChunk.vel * firstChunk.mass / grabber.firstChunk.mass;
            grabber.firstChunk.vel += push;
        }

        firstChunk.vel = Vector2.zero;

        HitEffect(weapon.firstChunk.vel);

        void HitEffect(Vector2 impactVelocity)
        {
            var num = UnityEngine.Random.Range(3, 8);
            for (int k = 0; k < num; k++)
            {
                //-- MR7: Figure out how to make sparks have the lizard graphics thing where they change color, without NEEDING lizard graphics.
                Vector2 pos = firstChunk.pos + Custom.DegToVec(Rand * 360f) * 5f * Rand;
                Vector2 vel = -impactVelocity * -0.1f + Custom.DegToVec(Rand * 360f) * Mathf.Lerp(0.2f, 0.4f, Rand) * impactVelocity.magnitude;
                room.AddObject(new Spark(pos, vel, Abstr.canCamo ? Camo(HeadColor(UnityEngine.Random.value)) : HeadColor(UnityEngine.Random.value), null, 10, 170));
            }

            room.AddObject(new StationaryEffect(firstChunk.pos, Abstr.canCamo ? Camo(HeadColor(UnityEngine.Random.value)) : HeadColor(UnityEngine.Random.value), null, StationaryEffect.EffectType.FlashingOrb));
        }
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
        rotation = Custom.RNV();
        lastRotation = rotation;;

        if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value)
        {
            PlaceInRoomBloodEmitter(firstChunk);
        }

        void PlaceInRoomBloodEmitter(BodyChunk firstChunk)
        {
            room.AddObject(new ShadowOfBloodEmitter(null, firstChunk, UnityEngine.Random.Range(11f, 16f), UnityEngine.Random.Range(3f, 6f)));
            room.AddObject(new ShadowOfBloodEmitter(null, firstChunk, UnityEngine.Random.Range(6f, 9f), UnityEngine.Random.Range(7f, 12f)));
        }
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);

        if (firstContact && speed > 10)
        {
            room.PlaySound(SoundID.Lizard_Light_Terrain_Impact, firstChunk.pos, 0.35f, 2f);

            if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value && ShadowOfOptions.blood_emitter_impact.Value)
                TerrainImpactBloodEmitter();
        }

        void TerrainImpactBloodEmitter()
        {
            room.AddObject(new BloodParticle(bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(5f, 10f)), new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB), Abstr.LizBreed, null, 2.3f));
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        var chunk = firstChunk;

        if (flicker > 0)
        {
            flicker--;
            baseBlink = UnityEngine.Random.value;
            baseLastBlink = UnityEngine.Random.value;
        }
        else
        {
            baseLastBlink = baseBlink;

            baseBlink = Mathf.Lerp(baseBlink - Mathf.Floor(baseBlink), 0.25f, 0.02f);

        }
        if (whiteFlicker > 0)
            whiteFlicker--;

        if (grabbedBy.Count > 0)
        {
            var grabber = grabbedBy[0].grabber;

            if (grabber is Player && (grabber as Player).Sneak > 0.01f)
            {
                var scug = grabber as Player;

                Vector2 scugAimDir = new Vector2(scug.ThrowDirection, 0);

                if (scug.Sneak < 0.5f)
                {
                    Vector2 faceDir = new Vector2(scug.ThrowDirection, 0);

                    bodyChunks[0].pos = Vector2.Lerp(bodyChunks[0].pos, bodyChunks[0].pos + faceDir * 15, 0.3f);
                }
                bodyChunks[0].vel = Vector2.Lerp(bodyChunks[0].vel, Vector2.zero, 0.3f);
                rotation = Vector2.Lerp(rotation, Custom.DirVec(grabber.mainBodyChunk.pos + scugAimDir * 2, chunk.pos), 0.3f);
            }
            else
            {
                rotation = Custom.PerpendicularVector(Custom.DirVec(chunk.pos, grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
            }
        }
        else
        {
            rotation += 0.9f * Custom.DirVec(chunk.lastPos, chunk.pos) * Custom.Dist(chunk.lastPos, chunk.pos);
        }

        if (!Custom.DistLess(chunk.lastPos, chunk.pos, 3f) && room.GetTile(chunk.pos).Solid && !room.GetTile(chunk.lastPos).Solid)
        {
            var firstSolid = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, room.GetTilePosition(chunk.lastPos), room.GetTilePosition(chunk.pos));
            if (firstSolid != null)
            {
                FloatRect floatRect = Custom.RectCollision(chunk.pos, chunk.lastPos, room.TileRect(firstSolid.Value).Grow(2f));
                chunk.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);

                if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                {
                    chunk.vel.x = Mathf.Abs(chunk.vel.x) * 0.15f;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                {
                    chunk.vel.x = -Mathf.Abs(chunk.vel.x) * 0.15f;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                {
                    chunk.vel.y = Mathf.Abs(chunk.vel.y) * 0.15f;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                {
                    chunk.vel.y = -Mathf.Abs(chunk.vel.y) * 0.15f;
                }
            }
        }

        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteCamoColorAmount, 1, 0.05f * UnityEngine.Random.value), 0f, 1f);
        }

        lastRotation = rotation;
    }

    #region Colours
    public Color effectColor
    {
        get
        {
            if (Abstr.LizBreed == "BlackLizard")
            {
                return palette.blackColor;
            }
            return LizEffectColour;
        }
    }

    public Color SalamanderColor
    {
        get
        {
            if (Abstr.blackSalamander)
            {
                return Color.Lerp(palette.blackColor, effectColor, 0.1f);
            }
            return Color.Lerp(new Color(0.9f, 0.9f, 0.95f), effectColor, 0.06f);
        }
    }

    private Color HeadColor1
    {
        get
        { 
            if (Abstr.LizType == "WhiteLizard")
            {
                return Abstr.canCamo ? Color.Lerp(new Color(1f, 1f, 1f), whiteCamoColor, whiteCamoColorAmount) : new Color(1f, 1f, 1f);
            }
            if (Abstr.LizType == "BlackLizard")
            {
                return palette.blackColor;
            }
            if (Abstr.LizType == "Salamander")
            {
                return SalamanderColor;
            }
            
            return palette.blackColor;
        }
    }

    private Color HeadColor2
    {
        get
        {
            if (Abstr.LizType == "WhiteLizard")
            {
                return Abstr.canCamo ? Color.Lerp(palette.blackColor, whiteCamoColor, whiteCamoColorAmount) : palette.blackColor;
            }
            if (Abstr.LizType == "BlackLizard")
            {
                return palette.blackColor;
            }
            if (Abstr.LizType == "Salamander")
            {
                return SalamanderColor;
            }

            return effectColor;
        }
    }

    public Color HeadColor(float timeStacker)
    {
        if (whiteFlicker > 0 && (whiteFlicker > SourceCodeLizardsWhiteFlickerThreshold || everySecondDraw))
        {
            return new Color(1f, 1f, 1f);
        }
        float num = 1f - Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(baseLastBlink, baseBlink, timeStacker) * 2f * 3.1415927f), 1.5f);
        if (flicker > SourceCodeLizardsFlickerThreshold)
        {
            num = flickerColor;
        }
        return Color.Lerp(HeadColor1, HeadColor2, num);
    }

    Color Camo(Color col)
    {
        return Abstr.canCamo ? Color.Lerp(col, whiteCamoColor, whiteCamoColorAmount) : col;
    }

    Color ElectricColor(Color col)
    {
        return ElectricColorTimer > 0 ? Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)ElectricColorTimer / 50f) : col;
    }

    public void Flicker(int fl)
    {
        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = 0f;
        }

        if (fl > flicker)
            flicker = fl;
    }

    public void WhiteFlicker(int fl)
    {
        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = 0f;
        }

        if (fl > whiteFlicker)
            whiteFlicker = fl;
    }
    #endregion

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        LizBodyColour = new Color(Abstr.LizBodyColourR, Abstr.LizBodyColourG, Abstr.LizBodyColourB);
        LizEffectColour = new Color(Abstr.LizEffectColourR, Abstr.LizEffectColourG, Abstr.LizEffectColourB);
        LizBloodColour = (Abstr.LizBloodColourR != -1f) ? new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB) : Color.black;
        bool flag = Abstr.HeadSprite5 != null;
        LizEyeRightColour = flag ? new Color(Abstr.EyeRightColourR, Abstr.EyeRightColourG, Abstr.EyeRightColourB) : Color.black;
        LizEyeLeftColour = flag ? new Color(Abstr.EyeLeftColourR, Abstr.EyeLeftColourG, Abstr.EyeLeftColourB) : Color.black;

        HeadSprites = flag ? new List<string>
            {
                Abstr.HeadSprite0,
                Abstr.HeadSprite1,
                Abstr.HeadSprite2,
                Abstr.HeadSprite3,
                Abstr.HeadSprite4,
                Abstr.HeadSprite3 + "Cut2",
                Abstr.HeadSprite5,
                Abstr.HeadSprite6
            } : new List<string>
            {
                Abstr.HeadSprite0,
                Abstr.HeadSprite1,
                Abstr.HeadSprite2,
                Abstr.HeadSprite3,
                Abstr.HeadSprite4,
                Abstr.HeadSprite3 + "Cut2"
            };

        if (flag)
        {
            bool right = HeadSprites[6].Contains("Right");

            headSpriteNum[6] = right ? 15 : 14;
            headSpriteNum[7] = right ? 14 : 15;
        }

        sLeaser.sprites = new FSprite[HeadSprites.Count];

        for (int i = 0; i < HeadSprites.Count; i++)
        {
            sLeaser.sprites[i] = new FSprite(HeadSprites[i], true);
        }

        if (flag)
        {
            sLeaser.sprites[6].color = LizEyeRightColour;
            sLeaser.sprites[7].color = LizEyeLeftColour;
        }

        if (Abstr.LizBreed == "BlackLizard")
        {
            sLeaser.sprites[4].isVisible = false;
        }

        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        Vector2 rot = Vector3.Slerp(lastRotation, rotation, timeStacker);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));

        if (darkness != lastDarkness)
        {
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        if (Abstr.canCamo || Abstr.LizType == "ZoopLizard")
        {
            whitePickUpColor = rCam.PixelColorAtCoordinate(bodyChunks[0].pos);

            whiteCamoColor = whitePickUpColor;
        }

        if(ElectricColorTimer > 0)
            ElectricColorTimer--;

        string headAngleNum = "0";
        if (Math.Abs(rotation.x) > 45f)
            headAngleNum = "1";
        else if (Math.Abs(rotation.x) > 90f)
            headAngleNum = "2";

        float headRotation = Custom.VecToDeg(rot);

        float totalVel = Math.Abs(bodyChunks[0].vel.x) + Math.Abs(bodyChunks[0].vel.y);

        float jawOpenRatio = Math.Abs(
            Mathf.Clamp(
                Vector2.Dot(rot, bodyChunks[0].vel)
                - 0.6f,
                -1 - totalVel * JawVelocityOverOpenSensitivity,
                0)
            );

        lastJawRotation = jawRotation;

        float desiredJawRotation = -Mathf.Clamp(jawOpenRatio * JawOpenSensitivity, 0, MaxJawRotation);
        jawRotation = Mathf.Lerp(lastJawRotation, headRotation + desiredJawRotation, 0.25f);

        if (jawRotation > headRotation + 10)
            jawRotation = headRotation + 10;

        headRotation %= 360f;
        jawRotation %= 360f;

        if (flicker > SourceCodeLizardsFlickerThreshold)
        {
            flickerColor = UnityEngine.Random.value;
        }

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(pos);
        darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(pos);

        for (int i = 0; i < HeadSprites.Count; i++)
        {
            string name = HeadSprites[i].Remove(headSpriteNum[i], 1);

            if (!Futile.atlasManager.DoesContainElementWithName(name.Insert(headSpriteNum[i], headAngleNum)))
            {
                Destroy();
                return;
            }

            sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(name.Insert(headSpriteNum[i], headAngleNum));
            sLeaser.sprites[i].x = pos.x - camPos.x;
            sLeaser.sprites[i].y = pos.y - camPos.y;
            sLeaser.sprites[i].rotation = i < SpriteJawStart ? jawRotation : headRotation;
        }

        if (Abstr.LizType == "CyanLizard")
        {
            sLeaser.sprites[4].color = effectColor;
            sLeaser.sprites[1].color = ElectricColor(HeadColor(timeStacker));
            sLeaser.sprites[2].color = ElectricColor(HeadColor(timeStacker));

            sLeaser.sprites[0].color = Camo(palette.blackColor);
            sLeaser.sprites[3].color = Camo(palette.blackColor);
        }
        else if (Abstr.LizType == "IndigoLizard")
        {
            Vector3 vector7 = Custom.RGB2HSL(LizEffectColour);
            sLeaser.sprites[4].color = new HSLColor(vector7.x, vector7.y, 0.7f).rgb;
            sLeaser.sprites[1].color = HeadColor(timeStacker);
            sLeaser.sprites[2].color = palette.blackColor;

            sLeaser.sprites[0].color = Camo(palette.blackColor);
            sLeaser.sprites[3].color = Camo(palette.blackColor);
        }
        else if (Abstr.LizType == "BasiliskLizard")
        {
            Color color4 = ElectricColor(Camo(Color.Lerp(HeadColor(timeStacker), effectColor, 0.7f)));
            if (whiteFlicker > 0 && (whiteFlicker > SourceCodeLizardsWhiteFlickerThreshold || everySecondDraw))
            {
                color4 = ElectricColor(Camo(new Color(1f, 1f, 1f)));
            }
            sLeaser.sprites[0].color = color4;
            sLeaser.sprites[3].color = color4;
        }
        else
        {
            sLeaser.sprites[0].color = ElectricColor(Camo(HeadColor(timeStacker)));
            sLeaser.sprites[3].color = ElectricColor(Camo(HeadColor(timeStacker)));
        }

        if (Abstr.LizType != "IndigoLizard" || (Abstr.LizType == "IndigoLizard" && Abstr.LizBloodColourR == -1f))
        {
            sLeaser.sprites[5].color = (Abstr.LizBloodColourR != -1f) ? LizBloodColour : ElectricColor(effectColor);
        }
        else
        {
            sLeaser.sprites[5].color = ElectricColor(palette.blackColor);
        }

        if (slatedForDeletetion || room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }

        if (blink > 0 && UnityEngine.Random.value < 0.5f)
        {
            sLeaser.sprites[0].color = blinkColor;
            sLeaser.sprites[3].color = blinkColor;
            if(Abstr.LizBloodColourR == -1f)
                sLeaser.sprites[5].color = blinkColor;
        }

        if (UnityEngine.Random.value > 0.025f)
        {
            everySecondDraw = !everySecondDraw;
        }
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        this.palette = palette;

        if (Abstr.LizType == "BlizzardLizard")
        {
            palette.blackColor = new Color(0.8f, 0.81f, 0.84f);
            this.palette = palette;
        }
        else if (Abstr.LizType == "IndigoLizard")
        {
            Color IndigoColour = new Color(Abstr.LizEffectColourR, Abstr.LizEffectColourG, Abstr.LizEffectColourB);

            Vector3 vector = Custom.RGB2HSL(IndigoColour);
            palette.blackColor = Color.Lerp(new HSLColor(vector.x, vector.y, 0.4f).rgb, palette.blackColor, 0.95f);
            this.palette = palette;
        }

        sLeaser.sprites[1].color = palette.blackColor;
        sLeaser.sprites[2].color = palette.blackColor;
        sLeaser.sprites[4].color = palette.blackColor;

        if (Abstr.LizType == "Salamander" && Abstr.blackSalamander)
        {
            sLeaser.sprites[4].color = effectColor;
        }
        else if (Abstr.LizType == "CyanLizard" || Abstr.LizType == "IndigoLizard")
        {
            sLeaser.sprites[0].color = palette.blackColor;
            sLeaser.sprites[3].color = palette.blackColor;
        }
        else if (Abstr.LizType == "BlizzardLizard")
        {
            Color color = new Color(0.99f, 1f, 0.98f);
            sLeaser.sprites[4].color = color;
            sLeaser.sprites[1].color = color;
            sLeaser.sprites[2].color = color;
        }
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer ??= rCam.ReturnFContainer("Items");

        foreach (FSprite fsprite in sLeaser.sprites)
        {
            fsprite.RemoveFromContainer();
            newContainer.AddChild(fsprite);
        }
    }

    public void ThrowByPlayer()
    {
        Flicker(20);
    }
}
