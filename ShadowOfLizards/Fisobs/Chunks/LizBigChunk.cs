using RWCustom;
using System.Security.Policy;
using UnityEngine;
using static RoomCamera;

namespace ShadowOfLizards;

internal sealed class LizBigChunk : PlayerCarryableItem, IDrawable
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

    public int meatLeft = 4;
    public int eatMeat = 0;
    #endregion

    #region Private
    private float lastDarkness = -1f;
    private float darkness;

    private Color bodyColour;
    private Color effectColour;
    private Color bloodColour;

    private Vector2 rotation;
    private Vector2 lastRotation;
    private Vector2? setRotation;

    private int whiteFlicker = 0;
    private int flicker;
    private float flickerColor = 0;

    private const int sourceCodeLizardsFlickerThreshold = 10;
    private const int sourceCodeLizardsWhiteFlickerThreshold = 15;

    private RoomPalette palette;

    private Color whiteCamoColor = new(0f, 0f, 0f);
    private Color whitePickUpColor;
    private float whiteCamoColorAmount = -1f;

    private float baseBlink;
    private float baseLastBlink;

    private bool everySecondDraw;
    #endregion

    private LizBigChunkAbstract Abstr { get; }

    public LizBigChunk(LizBigChunkAbstract abstr) : base(abstr)
    {
        Abstr = abstr;

        bodyChunks = new BodyChunk[1];
        bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, Abstr.rad, Abstr.mass);
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
                BloodEmitter();
        }
    }

    private void BloodEmitter()
    {
        room.AddObject(new BloodParticle(bodyChunks[0].pos, new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 10f)), bloodColour, Abstr.breed, null, 2.3f));
    }

    #region Colours
    private Color EffectColor
    {
        get
        {
            if (Abstr.breed == "BlackLizard")
            {
                return palette.blackColor;
            }
            return effectColour;
        }
    }

    private Color BodyColor
    {
        get
        {
            if (Abstr.breed == "SpitLizard" || Abstr.breed == "ZoopLizard")
            {
                return Abstr.canCamo ? Camo(bodyColour) : bodyColour;
            }
            if (Abstr.breed == "WhiteLizard")
            {
                return Abstr.canCamo ? Camo(new Color(1f, 1f, 1f)) : new Color(1f, 1f, 1f);
            }
            if (Abstr.breed == "Salamander")
            {
                return Abstr.canCamo ? Camo(SalamanderColor) : SalamanderColor;
            }

            return Abstr.canCamo ? Camo(palette.blackColor) : palette.blackColor;
        }
    }

    private Color SalamanderColor
    {
        get
        {
            if (Abstr.blackSalamander)
            {
                return Color.Lerp(palette.blackColor, EffectColor, 0.1f);
            }
            return Color.Lerp(new Color(0.9f, 0.9f, 0.95f), EffectColor, 0.06f);
        }
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
    #endregion

    public void InitiateSprites(SpriteLeaser sLeaser, RoomCamera rCam)
    {
        bodyColour = new Color(Abstr.bodyColourR, Abstr.bodyColourG, Abstr.bodyColourB);
        effectColour = new Color(Abstr.effectColourR, Abstr.effectColourG, Abstr.effectColourB);
        bloodColour = (Abstr.bloodColourR != -1f) ? new Color(Abstr.bloodColourR, Abstr.bloodColourG, Abstr.bloodColourB) : effectColour;

        sLeaser.sprites = new FSprite[2];
        sLeaser.sprites[0] = new FSprite("BigChunkInside" + Abstr.insideVariant, true);
        sLeaser.sprites[1] = new FSprite("BigChunkOutside" + Abstr.outsideVariant, true);

        sLeaser.sprites[0].color = Abstr.bloodColourR != -1f ? bloodColour : EffectColor;
        sLeaser.sprites[1].color = bodyColour;

        sLeaser.sprites[0].rotation = Abstr.insideRotation;
        sLeaser.sprites[1].rotation = Abstr.outsideRotation;

        sLeaser.sprites[0].scaleX = bodyChunks[0].rad * 0.1f;
        sLeaser.sprites[0].scaleY = bodyChunks[0].rad * 0.1f;
        sLeaser.sprites[1].scaleX = bodyChunks[0].rad * 0.1f;
        sLeaser.sprites[1].scaleY = bodyChunks[0].rad * 0.1f;

        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (Abstr.canCamo || Abstr.breed == "ZoopLizard")
        {
            sLeaser.sprites[1].color = BodyColor;
            whitePickUpColor = rCam.PixelColorAtCoordinate(bodyChunks[0].pos);

            whiteCamoColor = whitePickUpColor;
        }

        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        Vector2 vector2 = Vector3.Slerp(lastRotation, rotation, timeStacker);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(vector) * (1f - rCam.room.LightSourceExposure(vector));

        if (darkness != lastDarkness)
        {
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        float num = Mathf.InverseLerp(305f, 380f, timeStacker);
        pos.y -= 20f * Mathf.Pow(num, 3f);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(pos);
        darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(pos);

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = pos.x - camPos.x;
            sLeaser.sprites[i].y = pos.y - camPos.y;
        }

        sLeaser.sprites[0].rotation = Custom.VecToDeg(vector2) + Abstr.insideRotation;
        sLeaser.sprites[1].rotation = Custom.VecToDeg(vector2) + Abstr.outsideRotation;

        sLeaser.sprites[0].color = Abstr.bloodColourR == -1 ? Abstr.breed == "IndigoLizard" ? Color.Lerp(effectColour, palette.blackColor, 0.5f) : EffectColor : bloodColour;
        sLeaser.sprites[1].color = BodyColor;

        if (flicker > sourceCodeLizardsFlickerThreshold)
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

        if (Random.value > 0.025f)
        {
            everySecondDraw = !everySecondDraw;
        }
    }

    public void ApplyPalette(SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        this.palette = palette;

        if (Abstr.breed == "BlizzardLizard")
        {
            palette.blackColor = new Color(0.8f, 0.81f, 0.84f);
            this.palette = palette;
        }
        else if (Abstr.breed == "IndigoLizard")
        {
            Vector3 vector = Custom.RGB2HSL(EffectColor);
            palette.blackColor = Color.Lerp(new HSLColor(vector.x, vector.y, 0.4f).rgb, palette.blackColor, 0.95f);
            this.palette = palette;
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
            baseBlink = Random.value;
            baseLastBlink = Random.value;
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
            if (grabbedBy[0].grabber is Player slug && CanEatMeat(slug) && slug.input[0].pckp)
            {
                int num11 = 0;
                if (ModManager.MMF && (slug.grasps[0] == null || !(slug.grasps[0].grabbed is LizBigChunk)) && slug.grasps[1] != null && slug.grasps[1].grabbed is LizBigChunk)
                {
                    num11 = 1;
                }

                eatMeat++;
                EatMeatUpdate(slug, num11, eu);
            }
            else
            {
                eatMeat = 0;
            }
        }

        if (firstChunk.ContactPoint.y < 0)
        {
            rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
            firstChunk.vel.x *= 0.8f;
        }

        if (Abstr.canCamo)
        {
            whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteCamoColorAmount, (0.25f * (float)meatLeft), 0.1f * Random.value), 0.15f, (0.25f * (float)meatLeft));
        }

        static bool CanEatMeat(Player slug)
        {
            if (ModManager.MSC && (slug.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint || slug.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear))
            {
                return false;
            }
            return (slug.slugcatStats.name == SlugcatStats.Name.Red || ModManager.MSC && (slug.slugcatStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer || slug.slugcatStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand || slug.slugcatStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)) && (!ModManager.MSC || slug.pyroJumpCooldown <= 60f);
        }
    }

    public void EatMeatUpdate(Player slug, int graspIndex, bool eu)
    {
        if (eatMeat > 20)
        {
            slug.standing = false;
            slug.Blink(5);
            if (eatMeat % 5 == 0)
            {
                Vector2 b = Custom.RNV() * 3f;
                slug.mainBodyChunk.pos += b;
                slug.mainBodyChunk.vel += b;
            }
            Vector2 vector = bodyChunks[0].pos * bodyChunks[0].mass;
            float num = bodyChunks[0].mass;

            vector /= num;
            slug.mainBodyChunk.vel += Custom.DirVec(slug.mainBodyChunk.pos, vector) * 0.5f;
            slug.bodyChunks[1].vel -= Custom.DirVec(slug.mainBodyChunk.pos, vector) * 0.6f;
            if (slug.graphicsModule != null && meatLeft > 0 && slug.FoodInStomach < slug.MaxFoodInStomach)
            {
                if (!Custom.DistLess(bodyChunks[0].pos, (slug.graphicsModule as PlayerGraphics).head.pos, bodyChunks[0].rad))
                {
                    (slug.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(bodyChunks[0].pos, (slug.graphicsModule as PlayerGraphics).head.pos) * (bodyChunks[0].rad - Vector2.Distance(bodyChunks[0].pos, (slug.graphicsModule as PlayerGraphics).head.pos));
                }
                else if (eatMeat % 5 == 3)
                {
                    (slug.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
                }
                if (eatMeat > 40 && eatMeat % 15 == 3)
                {
                    slug.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, Random.value)) * 4f;
                    bodyChunks[0].vel += Custom.DirVec(vector, slug.mainBodyChunk.pos) * 0.9f / bodyChunks[0].mass;
                    for (int k = Random.Range(0, 3); k >= 0; k--)
                    {
                        slug.room.AddObject(new WaterDrip(Vector2.Lerp(bodyChunks[0].pos, slug.mainBodyChunk.pos, Random.value) + bodyChunks[0].rad * Custom.RNV() * Random.value, Custom.RNV() * 6f * Random.value + Custom.DirVec(vector, (slug.mainBodyChunk.pos + (slug.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, Random.value)) * Random.value * slug.EffectiveRoomGravity * 7f, false));
                    }
                    meatLeft--;

                    slug.AddFood(1);

                    whiteCamoColorAmount = 0f;
                    Flicker(20);

                    if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                        BloodEmitter();

                    slug.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, slug.mainBodyChunk);
                    return;
                }
                if (eatMeat % 15 == 3)
                {
                    slug.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, slug.mainBodyChunk);
                }
            }
            else
            {
                eatMeat = 0;
                slug.wantToPickUp = 0;
                slug.TossObject(graspIndex, eu);
                slug.ReleaseGrasp(graspIndex);
                slug.standing = true;
            }
        }
    }

    public void ThrowByPlayer()
    {
    }
}
