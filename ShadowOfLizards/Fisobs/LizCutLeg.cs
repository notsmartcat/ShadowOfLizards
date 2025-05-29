using RWCustom;
using UnityEngine;
using static Creature;
using static RoomCamera;

namespace ShadowOfLizards;

internal sealed class LizCutLeg : PlayerCarryableItem, IDrawable, IPlayerEdible
{
    public float lastDarkness = -1f;
    public float darkness;

    Color blackColor;
    Color earthColor;

    public Color LizBaseColour;
    public Color LizColour;
    public Color BloodColour;

    public Vector2 rotation;
    public Vector2 lastRotation;
    public Vector2? setRotation;

    public AbstractConsumable AbstrConsumable
    {
        get
        {
            return (AbstractConsumable)abstractPhysicalObject;
        }
    }

    public LizCutLegAbstract Abstr { get; }

    public int bites = 3;
    public int BitesLeft => bites;
    public int FoodPoints => 1;

    public bool Edible => true;

    public bool AutomaticPickUp => false;

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
        if (firstContact && speed > 7f)
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

    public void InitiateSprites(SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[3];
        sLeaser.sprites[0] = new FSprite(Abstr.LizSpriteName, true);
        sLeaser.sprites[1] = new FSprite(Abstr.LizColourSpriteName, true);
        sLeaser.sprites[2] = new FSprite(Abstr.LizColourSpriteName + "Blood", true);
        LizBaseColour = new Color(Abstr.LizBaseColourR, Abstr.LizBaseColourG, Abstr.LizBaseColourB);
        LizColour = new Color(Abstr.LizColourR, Abstr.LizColourG, Abstr.LizColourB);
        BloodColour = ((Abstr.LizBloodColourR != -1f) ? new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB) : new Color(Abstr.LizColourR, Abstr.LizColourG, Abstr.LizColourB));
        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
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
        float num2 = Mathf.Pow(1f - num, 0.25f);

        lastDarkness = darkness;
        darkness = rCam.room.Darkness(pos);
        darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(pos);

        if (LizBaseColour == Color.black)
        {
            LizBaseColour = blackColor;
        }

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = pos.x - camPos.x;
            sLeaser.sprites[i].y = pos.y - camPos.y;
            sLeaser.sprites[i].rotation = Custom.VecToDeg(vector2);
            sLeaser.sprites[i].scaleY = num2 * Abstr.scaleY;
            sLeaser.sprites[i].scaleX = num2 * Abstr.scaleX;
        }
        sLeaser.sprites[0].color = Abstr.LizBreed == "WhiteLizard" ? LizColour : LizBaseColour;
        sLeaser.sprites[1].color = LizColour;
        sLeaser.sprites[2].color = BloodColour;

        if (blink > 0 && Random.value < 0.5f)
        {
            sLeaser.sprites[1].color = blinkColor;
        }
        else if (num > 0.3f)
        {
            for (int j = 0; j < 2; j++)
            {
                sLeaser.sprites[j].color = Color.Lerp(sLeaser.sprites[j].color, earthColor, Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, num), 1.6f));
            }
        }

        if (slatedForDeletetion || room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }
    }

    public void ApplyPalette(SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        blackColor = palette.blackColor;
        earthColor = Color.Lerp(palette.fogColor, palette.blackColor, 0.5f);
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
    }

    public void BitByPlayer(Grasp grasp, bool eu)
    {
        bites--;
        room.PlaySound((bites == 0) ? SoundID.Slugcat_Eat_Meat_A : SoundID.Slugcat_Eat_Meat_B, firstChunk.pos);
        firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);

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
    }
}
