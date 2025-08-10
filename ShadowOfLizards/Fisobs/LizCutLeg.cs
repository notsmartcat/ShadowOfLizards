using RWCustom;
using UnityEngine;
using static Creature;
using static RoomCamera;

namespace ShadowOfLizards;

internal sealed class LizCutLeg : PlayerCarryableItem, IDrawable, IPlayerEdible
{
    #region Public
    public AbstractConsumable AbstrConsumable
    {
        get
        {
            return (AbstractConsumable)abstractPhysicalObject;
        }
    }

    public override float ThrowPowerFactor => 1f;

    public int ElectricColorTimer = 0;

    public int bites = 3;
    public int BitesLeft => bites;
    public int FoodPoints => 1;

    public bool Edible => true;

    public bool AutomaticPickUp => false;
    #endregion

    #region Private
    private float lastDarkness = -1f;
    private float darkness;

    private Color LizBodyColour;
    private Color LizEffectColour;
    private Color BloodColour;

    private Vector2 rotation;
    private Vector2 lastRotation;
    private Vector2? setRotation;

    private int whiteFlicker = 0;
    private int flicker;
    private float flickerColor = 0;

    private const int SourceCodeLizardsFlickerThreshold = 10;
    private const int SourceCodeLizardsWhiteFlickerThreshold = 15;

    private RoomPalette palette;

    private Color whiteCamoColor = new(0f, 0f, 0f);
    private Color whitePickUpColor;
    private float whiteCamoColorAmount = -1f;

    private float baseBlink;
    private float baseLastBlink;

    private bool everySecondDraw;
    #endregion

    private LizCutLegAbstract Abstr { get; }

    public LizCutLeg(LizCutLegAbstract abstr) : base(abstr)
    {
        Abstr = abstr;

        float num = 0.2f;
        bodyChunks = new BodyChunk[1];
        bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, 5f, num);
        bodyChunkConnections = new BodyChunkConnection[0];
        airFriction = 0.999f;
        gravity = 0.9f;
        bounce = 0.3f;
        surfaceFriction = 0.45f;
        collisionLayer = 2;
        waterFriction = 0.92f;
        buoyancy = 0.75f;
        GoThroughFloors = false;
        lastRotation = rotation;
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
        rotation = Custom.RNV();
        lastRotation = rotation;
        canBeHitByWeapons = false;
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);
        if (firstContact && speed > 10f)
        {
            room.PlaySound(SoundID.Lizard_Light_Terrain_Impact, firstChunk.pos, 0.35f, 2f);

            if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value && ShadowOfOptions.blood_emitter_impact.Value)
                LizCutLegBloodEmitter();
        }
    }

    private void LizCutLegBloodEmitter()
    {
        room.AddObject(new BloodParticle(bodyChunks[0].pos, new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 10f)), BloodColour, Abstr.LizBreed, null, 2.3f));
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

    private Color BodyColor(float f)
    {
        if (Abstr.LizBreed == "SpitLizard" || Abstr.LizBreed == "ZoopLizard")
        {
            return LizBodyColour;
        }
        if (Abstr.LizBreed == "WhiteLizard")
        {
            return Abstr.canCamo ? DynamicBodyColor(f) : new Color(1f, 1f, 1f);
        }
        if (Abstr.LizBreed == "Salamander")
        {
            return SalamanderColor;
        }

        return palette.blackColor;
    }

    private Color DynamicBodyColor(float f)
    {
        if (Abstr.LizBreed == "WhiteLizard")
        {
            return Color.Lerp(new Color(1f, 1f, 1f), whiteCamoColor, whiteCamoColorAmount);
        }
        if (Abstr.LizBreed == "Salamander")
        {
            return SalamanderColor;
        }
        return palette.blackColor;
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
                return Abstr.canCamo ?  Color.Lerp(new Color(1f, 1f, 1f), whiteCamoColor, whiteCamoColorAmount) : new Color(1f, 1f, 1f);
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

    private void Flicker(int fl)
    {
        if (fl > flicker)
            flicker = fl;
    }

    private Color Camo(Color col)
    {
        return Color.Lerp(col, whiteCamoColor, whiteCamoColorAmount);
    }

    private Color ElectricColor(Color col)
    {
        return Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)ElectricColorTimer / 50f);
    }
    #endregion

    public void InitiateSprites(SpriteLeaser sLeaser, RoomCamera rCam)
    {
        LizBodyColour = new Color(Abstr.LizBodyColourR, Abstr.LizBodyColourG, Abstr.LizBodyColourB);
        LizEffectColour = new Color(Abstr.LizEffectColourR, Abstr.LizEffectColourG, Abstr.LizEffectColourB);
        BloodColour = (Abstr.LizBloodColourR != -1f) ? new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB) : LizEffectColour;

        sLeaser.sprites = new FSprite[3];
        sLeaser.sprites[0] = new FSprite(Abstr.LizSpriteName, true);
        sLeaser.sprites[1] = new FSprite(Abstr.LizColourSpriteName, true);
        sLeaser.sprites[2] = new FSprite(Abstr.LizColourSpriteName + "Blood", true);

        sLeaser.sprites[0].color = new Color(0.1f, 0.1f, 0.1f, 1f);
        sLeaser.sprites[1].color = effectColor;
        sLeaser.sprites[2].color = BloodColour;

        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (Abstr.canCamo || Abstr.LizBreed == "ZoopLizard")
        {
            sLeaser.sprites[0].color = BodyColor(0f);
            whitePickUpColor = rCam.PixelColorAtCoordinate(bodyChunks[0].pos);

            whiteCamoColor = whitePickUpColor;
        }
        else if (Abstr.LizBreed == "BasiliskLizard")
        {
            sLeaser.sprites[0].color = HeadColor(timeStacker);
        }

        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        Vector2 vector2 = Vector3.Slerp(lastRotation, rotation, timeStacker);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(vector) * (1f - rCam.room.LightSourceExposure(vector));

        if (darkness != lastDarkness)
        {
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        if (ElectricColorTimer > 0)
        {
            ElectricColorTimer--;
        }

        Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        float num = Mathf.InverseLerp(305f, 380f, timeStacker);
        pos.y -= 20f * Mathf.Pow(num, 3f);
        float num2 = Mathf.Pow(1f - num, 0.25f);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(pos);
        darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(pos);

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = pos.x - camPos.x;
            sLeaser.sprites[i].y = pos.y - camPos.y;
            sLeaser.sprites[i].rotation = Custom.VecToDeg(vector2);
            sLeaser.sprites[i].scaleY = num2 * Abstr.scaleY;
            sLeaser.sprites[i].scaleX = num2 * Abstr.scaleX;
        }

        if (Abstr.LizBreed == "WhiteLizard")
        {
            sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
        }
        else if (Abstr.LizBreed == "SpitLizard" || Abstr.LizBreed == "ZoopLizard")
        {
            sLeaser.sprites[0].color = LizBodyColour;
        }
        else if (Abstr.LizBreed == "Salamander")
        {
            sLeaser.sprites[0].color = SalamanderColor;
        }
        else
        {
            sLeaser.sprites[0].color = palette.blackColor;
        }

        if (Abstr.LizBreed == "WhiteLizard" || Abstr.LizBreed == "CyanLizard" || Abstr.LizBreed == "IndigoLizard")
        {
            sLeaser.sprites[1].alpha = Mathf.Sin(whiteCamoColorAmount * 3.1415927f) * 0.3f;
            sLeaser.sprites[1].color = palette.blackColor;
        }
        else if (Abstr.LizBreed == "Salamander")
        {
            sLeaser.sprites[1].alpha = 0.3f;
            sLeaser.sprites[1].color = Abstr.blackSalamander ? effectColor : palette.blackColor;
        }
        else
        {
            sLeaser.sprites[1].alpha = Mathf.Lerp(1f, 0.3f, Mathf.Abs(Mathf.Lerp(lastRotation.x, rotation.x, timeStacker)));
            sLeaser.sprites[1].color = effectColor;
        }


        if (Abstr.canCamo)
        {
            sLeaser.sprites[0].color = Camo(sLeaser.sprites[0].color);
            sLeaser.sprites[1].color = Camo(sLeaser.sprites[1].color);
        }

        if (ElectricColorTimer > 0)
        {
            sLeaser.sprites[1].color = ElectricColor(sLeaser.sprites[1].color);
        }

        if (Abstr.LizBloodColourR != -1f)
        {
            sLeaser.sprites[2].color = BloodColour;
        }
        else
        {
            sLeaser.sprites[2].color = ElectricColor(sLeaser.sprites[2].color);
        }

        if (flicker > SourceCodeLizardsFlickerThreshold)
        {
            flickerColor = Random.value;
        }

        if (blink > 0 && Random.value < 0.5f)
        {
            sLeaser.sprites[1].color = blinkColor;
        }

        if (slatedForDeletetion || room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }

        if (UnityEngine.Random.value > 0.025f)
        {
            everySecondDraw = !everySecondDraw;
        }
    }

    public void ApplyPalette(SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        this.palette = palette;

        if (Abstr.LizBreed == "BlizzardLizard")
        {
            palette.blackColor = new Color(0.8f, 0.81f, 0.84f);
            this.palette = palette;
        }
        else if (Abstr.LizBreed == "IndigoLizard")
        {
            Vector3 vector = Custom.RGB2HSL(effectColor);
            palette.blackColor = Color.Lerp(new HSLColor(vector.x, vector.y, 0.4f).rgb, palette.blackColor, 0.95f);
            this.palette = palette;
        }
        if (Abstr.LizBreed == "WhiteLizard")
        {
            sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
        }
        else if (Abstr.LizBreed == "SpitLizard" || Abstr.LizBreed == "ZoopLizard")
        {
            sLeaser.sprites[0].color = LizBodyColour;
        }
        else if (Abstr.LizBreed == "Salamander")
        {
            sLeaser.sprites[0].color = SalamanderColor;
        }
        else
        {
            sLeaser.sprites[0].color = palette.blackColor;
        }
    }

    public void AddToContainer(SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer ??= rCam.ReturnFContainer("Items");

        foreach (FSprite val in sLeaser.sprites)
        {
            val.RemoveFromContainer();
            newContainer.AddChild((FNode)(object)val);
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        lastRotation = rotation;

        if (room.game.devToolsActive && Input.GetKey("b"))
        {
            BodyChunk firstChunk = base.firstChunk;
            firstChunk.vel += Custom.DirVec(firstChunk.pos, Futile.mousePosition) * 3f;
        }

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
            rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
            rotation.y = Mathf.Abs(rotation.y);
        }

        if (setRotation.HasValue)
        {
            rotation = setRotation.Value;
            setRotation = null;
        }

        if (firstChunk.ContactPoint.y < 0)
        {
            rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
            firstChunk.vel.x *= 0.8f;
        }

        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteCamoColorAmount, 1, 0.1f * UnityEngine.Random.value), 0.15f, 1f);
        }
    }

    public void BitByPlayer(Grasp grasp, bool eu)
    {
        bites--;
        room.PlaySound((bites == 0) ? SoundID.Slugcat_Eat_Meat_A : SoundID.Slugcat_Eat_Meat_B, firstChunk.pos);
        firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

        whiteCamoColorAmount = 0f;
        Flicker(20);

        if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value)
            LizCutLegBloodEmitter();

        if (bites < 1)
        {
            if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                LizCutLegBloodEmitter();

            ((Player)grasp.grabber).ObjectEaten(this);

            grasp.Release();
            Destroy();
        }
    }

    public void ThrowByPlayer()
    {
        Flicker(20);
    }
}
