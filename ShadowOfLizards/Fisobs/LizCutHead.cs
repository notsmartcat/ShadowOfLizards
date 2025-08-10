using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ShadowOfLizards;

sealed class LizCutHead : PlayerCarryableItem, IDrawable
{
    #region Public
    public int ElectricColorTimer = 0;

    public float donned = 0;

    public Color LizBloodColour;
    #endregion

    public Vector2 rotation;
    private Vector2 lastRotation;

    #region Private
    public override float ThrowPowerFactor => 1f;

    private float lastDarkness = -1f;
    private float darkness;

    private Color LizBodyColour;
    private Color LizEffectColour;

    private Color LizEyeRightColour;
    private Color LizEyeLeftColour;

    private bool flipX = false;

    private int whiteFlicker = 0;
    private int flicker;
    private float flickerColor = 0;

    private const int SourceCodeLizardsFlickerThreshold = 10;
    private const int SourceCodeLizardsWhiteFlickerThreshold = 15;

    private RoomPalette palette;

    private const int SpriteJawStart = 2;

    private List<string> HeadSprites;

    private float jawRotation;
    private float lastJawRotation;

    private const float JawVelocityOverOpenSensitivity = 2.5f;

    private readonly List<int> headSpriteNum = new() { 9, 16, 16, 10, 10, 10, 15, 14 };

    private Color whiteCamoColor = new(0f, 0f, 0f);
    private Color whitePickUpColor;
    private float whiteCamoColorAmount = -1f;

    private float baseBlink;
    private float baseLastBlink;

    private bool everySecondDraw;

    private Vector2 rotVel;

    private bool facingRight;
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

        facingRight = Abstr.scaleX > 0f;
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

        if (weapon.firstChunk.pos.x > firstChunk.pos.x && facingRight || weapon.firstChunk.pos.x < firstChunk.pos.x && !facingRight)
        {
            rotation = Custom.rotateVectorDeg(rotation, 180);
        }

        HitEffect(weapon.firstChunk.vel);

        void HitEffect(Vector2 impactVelocity)
        {
            var num = UnityEngine.Random.Range(3, 8);
            for (int k = 0; k < num; k++)
            {
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

        lastRotation = rotation;

        rotation = Custom.DegToVec(Custom.VecToDeg(rotation) + rotVel.x);

        rotVel = Vector2.ClampMagnitude(rotVel, 50f);
        rotVel *= Custom.LerpMap(rotVel.magnitude, 5f, 50f, 1f, 0.8f);

        facingRight = Custom.VecToDeg(rotation) > 0;

        var chunk = firstChunk;

        donned = 0;

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

            if (grabber is Player scug && scug.privSneak > 0.5f)
            {
                Vector2 faceDir = Custom.DegToVec(Custom.AimFromOneVectorToAnother(scug.bodyChunks[1].pos, scug.bodyChunks[0].pos));

                rotation = faceDir;

                //to = Mathf.InverseLerp(15f, 10f, Vector2.Distance((scug.graphicsModule as PlayerGraphics).hands[grabbedBy[0].graspUsed].pos, scug.mainBodyChunk.pos));

                donned = 1;

                if (faceDir.x > 0 == Abstr.scaleX > 0)
                {
                    flipX = true;
                }
                else
                {
                    flipX = false;
                }
            }
            else
            {
                rotation = Abstr.scaleX < 0 ? Custom.RotateAroundOrigo(Custom.PerpendicularVector(Custom.DirVec(chunk.pos, grabber.mainBodyChunk.pos)), 180) : Custom.PerpendicularVector(Custom.DirVec(chunk.pos, grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);

                flipX = false;
            }
        }
        else if (firstChunk.ContactPoint.y < 0)
        {
            Vector2 b;

            b = Custom.DegToVec(90f * (facingRight ? 1 : -1));

            rotation = Vector2.Lerp(rotation, b, UnityEngine.Random.value);
            rotVel *= UnityEngine.Random.value;
        }
        else if (Vector2.Distance(firstChunk.lastPos, firstChunk.pos) > 5f && rotVel.magnitude < 7f)
        {
            rotVel += Custom.RNV() * (Mathf.Lerp(7f, 25f, UnityEngine.Random.value) + firstChunk.vel.magnitude * 2f);
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

        //donned = Custom.LerpAndTick(donned, to, 0.11f, 0.033333335f);

        lastRotation = rotation;
    }

    #region Colours
    private Color effectColor
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

    private Color SalamanderColor
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
            if (Abstr.LizBreed == "WhiteLizard")
            {
                return Abstr.canCamo ? Color.Lerp(new Color(1f, 1f, 1f), whiteCamoColor, whiteCamoColorAmount) : new Color(1f, 1f, 1f);
            }
            if (Abstr.LizBreed == "BlackLizard")
            {
                return palette.blackColor;
            }
            if (Abstr.LizBreed == "Salamander")
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
            if (Abstr.LizBreed == "WhiteLizard")
            {
                return Abstr.canCamo ? Color.Lerp(palette.blackColor, whiteCamoColor, whiteCamoColorAmount) : palette.blackColor;
            }
            if (Abstr.LizBreed == "BlackLizard")
            {
                return palette.blackColor;
            }
            if (Abstr.LizBreed == "Salamander")
            {
                return SalamanderColor;
            }

            return effectColor;
        }
    }

    private Color HeadColor(float timeStacker)
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

    private Color Camo(Color col)
    {
        return Abstr.canCamo ? Color.Lerp(col, whiteCamoColor, whiteCamoColorAmount) : col;
    }

    private Color ElectricColor(Color col)
    {
        return ElectricColorTimer > 0 ? Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)ElectricColorTimer / 50f) : col;
    }

    private void Flicker(int fl)
    {
        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = 0f;
        }

        if (fl > flicker)
            flicker = fl;
    }

    private void WhiteFlicker(int fl)
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
            sLeaser.sprites[i].scaleX = Abstr.scaleX;
            sLeaser.sprites[i].scaleY = Abstr.scaleY;
        }

        int headLength3 = Abstr.HeadSprite3.Length;
        char head3 = Abstr.HeadSprite3[headLength3 - 3];

        if (head3 == 3)
        {
            sLeaser.sprites[4].anchorY = 0.55f;

            if (flag)
            {
                sLeaser.sprites[6].anchorY = 0.75f;
                sLeaser.sprites[7].anchorY = 0.75f;
            }
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

        if (Abstr.canCamo || Abstr.LizBreed == "ZoopLizard")
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

        bool flipJaw = Abstr.scaleX > 0 ^ flipX;

        float desiredJawRotation = Mathf.Clamp(jawOpenRatio * Abstr.jawOpenMoveJawsApart, 0, Abstr.jawOpenAngle) * (flipJaw ? -1 : 1);
        jawRotation = Mathf.Lerp(lastJawRotation, headRotation + desiredJawRotation, 0.25f);

        if (flipJaw && jawRotation > headRotation + 10)
        {
            jawRotation = headRotation + 10;
        }
        else if (!flipJaw && jawRotation < headRotation - 10)
        {
            jawRotation = headRotation - 10;
        }

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

            sLeaser.sprites[i].scaleX = flipX ? Abstr.scaleX * -1 : Abstr.scaleX;
            sLeaser.sprites[i].scaleY = Abstr.scaleY;
        }

        if (Abstr.LizBreed == "CyanLizard")
        {
            sLeaser.sprites[4].color = effectColor;
            sLeaser.sprites[1].color = ElectricColor(HeadColor(timeStacker));
            sLeaser.sprites[2].color = ElectricColor(HeadColor(timeStacker));

            sLeaser.sprites[0].color = Camo(palette.blackColor);
            sLeaser.sprites[3].color = Camo(palette.blackColor);
        }
        else if (Abstr.LizBreed == "IndigoLizard")
        {
            Vector3 vector7 = Custom.RGB2HSL(LizEffectColour);
            sLeaser.sprites[4].color = new HSLColor(vector7.x, vector7.y, 0.7f).rgb;
            sLeaser.sprites[1].color = HeadColor(timeStacker);
            sLeaser.sprites[2].color = palette.blackColor;

            sLeaser.sprites[0].color = Camo(palette.blackColor);
            sLeaser.sprites[3].color = Camo(palette.blackColor);
        }
        else if (Abstr.LizBreed == "BasiliskLizard")
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

        if (Abstr.LizBreed != "IndigoLizard" || (Abstr.LizBreed == "IndigoLizard" && Abstr.LizBloodColourR == -1f))
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

        if (Abstr.LizBreed == "BlizzardLizard")
        {
            palette.blackColor = new Color(0.8f, 0.81f, 0.84f);
            this.palette = palette;
        }
        else if (Abstr.LizBreed == "IndigoLizard")
        {
            Color IndigoColour = new Color(Abstr.LizEffectColourR, Abstr.LizEffectColourG, Abstr.LizEffectColourB);

            Vector3 vector = Custom.RGB2HSL(IndigoColour);
            palette.blackColor = Color.Lerp(new HSLColor(vector.x, vector.y, 0.4f).rgb, palette.blackColor, 0.95f);
            this.palette = palette;
        }

        sLeaser.sprites[1].color = palette.blackColor;
        sLeaser.sprites[2].color = palette.blackColor;
        sLeaser.sprites[4].color = palette.blackColor;

        if (Abstr.LizBreed == "Salamander" && Abstr.blackSalamander)
        {
            sLeaser.sprites[4].color = effectColor;
        }
        else if (Abstr.LizBreed == "CyanLizard" || Abstr.LizBreed == "IndigoLizard")
        {
            sLeaser.sprites[0].color = palette.blackColor;
            sLeaser.sprites[3].color = palette.blackColor;
        }
        else if (Abstr.LizBreed == "BlizzardLizard")
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
