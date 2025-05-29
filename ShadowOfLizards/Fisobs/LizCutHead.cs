using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowOfLizards;

sealed class LizCutHead : PlayerCarryableItem, IDrawable
{
    public float lastDarkness = -1f;
    public float darkness;

    Color blackColor;
    Color earthColor;

    public List<Color> ColourArray;

    public Color LizBaseColour;
    public Color LizColour;

    public Color LizBloodColour;

    public Color LizEyeRightColour;
    public Color LizEyeLeftColour;

    public Vector2 rotation;
    public Vector2 lastRotation;
    public Vector2? setRotation;

    public List<string> HeadSprites;

    readonly string all = "ShadowOf: ";

    public LizCutHeadAbstract Abstr { get; }

    public LizCutHead(LizCutHeadAbstract abstr) : base(abstr)
    {
        Abstr = abstr;

        bodyChunks = new BodyChunk[1];
        bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, Abstr.rad, 0.4f);
        bodyChunkConnections = new BodyChunkConnection[0];
        airFriction = 0.999f;
        gravity = 0.9f;
        bounce = 0.3f;
        surfaceFriction = 0.45f;
        collisionLayer = 1;
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
        lastRotation = rotation;;

        if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value)
        {
            PlaceInRoomBloodEmitter(firstChunk);
        }
    }

    private void PlaceInRoomBloodEmitter(BodyChunk firstChunk)
    {
        room.AddObject(new ShadowOfBloodEmitter(null, firstChunk, Random.Range(11f, 16f), Random.Range(3f, 6f)));
        room.AddObject(new ShadowOfBloodEmitter(null, firstChunk, Random.Range(6f, 9f), Random.Range(7f, 12f)));
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);
        if (firstContact && speed > 2f)
        {
            room.PlaySound(SoundID.Lizard_Light_Terrain_Impact, firstChunk.pos, 0.35f, 2f);

            if (ShadowOfLizards.bloodModCheck && ShadowOfOptions.blood_emitter.Value && ShadowOfOptions.blood_emitter_impact.Value)
                TerrainImpactBloodEmitter();
        }
    }

    private void TerrainImpactBloodEmitter()
    {
        room.AddObject(new BloodParticle(bodyChunks[0].pos, new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 10f)), new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB), Abstr.LizBreed, null, 2.3f));
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        Color black = Color.black;
        LizBaseColour = new Color(Abstr.LizBaseColourR, Abstr.LizBaseColourG, Abstr.LizBaseColourB);
        LizColour = new Color(Abstr.LizColourR, Abstr.LizColourG, Abstr.LizColourB);
        LizBloodColour = (Abstr.LizBloodColourR != -1f) ? new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB) : Color.black;
        bool flag = Abstr.HeadSprite5 != null;
        LizEyeRightColour = flag ? new Color(Abstr.EyeRightColourR, Abstr.EyeRightColourG, Abstr.EyeRightColourB) : Color.black;
        LizEyeLeftColour = flag ? new Color(Abstr.EyeLeftColourR, Abstr.EyeLeftColourG, Abstr.EyeLeftColourB) : Color.black;
        int length = Abstr.HeadSprite0.Length;
        char c = Abstr.HeadSprite0[length - 3];
        char c2 = Abstr.HeadSprite0[length - 1];

        if (!Abstr.HeadSprite0.StartsWith("LizardJaw"))
        {
            Destroy();
        }

        HeadSprites = (flag ? new List<string>
            {
                Abstr.HeadSprite0,
                Abstr.HeadSprite1,
                Abstr.HeadSprite2,
                Abstr.HeadSprite3,
                Abstr.HeadSprite4,
                "LizardHead" + c + "." + c2 + "Cut2",
                Abstr.HeadSprite5,
                Abstr.HeadSprite6
            } : new List<string>
            {
                Abstr.HeadSprite0,
                Abstr.HeadSprite1,
                Abstr.HeadSprite2,
                Abstr.HeadSprite3,
                Abstr.HeadSprite4,
                "LizardHead" + c + "." + c2 + "Cut2"
            });

        sLeaser.sprites = new FSprite[HeadSprites.Count];

        for (int i = 0; i < HeadSprites.Count; i++)
        {
            sLeaser.sprites[i] = new FSprite(HeadSprites[i], true);
        }

        if (Abstr.LizType == "CyanLizard")
        {
            ColourArray = new List<Color>
            {
                LizBaseColour,
                LizColour,
                LizColour,
                LizBaseColour,
                LizColour,
                (Abstr.LizBloodColourR != -1f) ? LizBloodColour : LizColour,
                LizEyeRightColour,
                LizEyeLeftColour
            };
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + Abstr.LizType + "'s Cut Head Cyan Lizard Colours");
        }
        else if (Abstr.LizType == "SpitLizard" || Abstr.LizType == "ZoopLizard")
        {
            ColourArray = new List<Color>
            {
                LizColour,
                black,
                black,
                LizColour,
                black,
                (Abstr.LizBloodColourR != -1f) ? LizBloodColour : LizBaseColour,
                LizEyeRightColour,
                LizEyeLeftColour
            };
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + Abstr.LizType + "'s Cut Head Spit Lizard Colours");
        }
        else if (Abstr.LizType == "Salamander" || Abstr.LizType == "CyanLizard")
        {
            ColourArray = Abstr.blackSalamander ? new List<Color>
            {
                LizBaseColour,
                LizColour,
                LizColour,
                LizBaseColour,
                LizColour,
                (Abstr.LizBloodColourR != -1f) ? LizBloodColour : LizColour,
                LizEyeRightColour,
                LizEyeLeftColour
            } : new List<Color>
            {
                LizBaseColour,
                black,
                black,
                LizBaseColour,
                black,
                (Abstr.LizBloodColourR != -1f) ? LizBloodColour : LizColour,
                LizEyeRightColour,
                LizEyeLeftColour
            };
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + Abstr.LizType + "'s Cut Head Salamander Lizard Colours");
        }
        else
        {
            ColourArray = new List<Color>
            {
                LizColour,
                LizBaseColour,
                LizBaseColour,
                LizColour,
                LizBaseColour,
                (Abstr.LizBloodColourR != -1f) ? LizBloodColour : LizColour,
                LizEyeRightColour,
                LizEyeLeftColour
            };
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + Abstr.LizType + "'s Cut Head Default Lizard Colours");
        }

        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
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

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            if (ColourArray[i] == Color.black)
            {
                ColourArray[i] = blackColor;
            }
            sLeaser.sprites[i].x = pos.x - camPos.x;
            sLeaser.sprites[i].y = pos.y - camPos.y;

            sLeaser.sprites[i].rotation = Custom.VecToDeg(vector2);

            if (Abstr.LizBreed == "WhiteLizard" && (i == 6 || i == 7 || Abstr.HeadSprite5 == null && i == 4))
            {
                sLeaser.sprites[i].x = pos.x - camPos.x - (7f * rotation.x);
                sLeaser.sprites[i].y = pos.y - camPos.y - (7f * rotation.y);
            }

            sLeaser.sprites[i].scaleY = num2 * Abstr.scaleY;
            sLeaser.sprites[i].scaleX = num2 * Abstr.scaleX;
            sLeaser.sprites[i].color = ColourArray[i];
        }

        if (blink > 0 && Random.value < 0.5f)
        {
            sLeaser.sprites[0].color = blinkColor;
            sLeaser.sprites[3].color = blinkColor;
            sLeaser.sprites[5].color = blinkColor;
        }
        else if (num > 0.3f)
        {
            for (int j = 0; j < sLeaser.sprites.Length; j++)
            {
                sLeaser.sprites[j].color = Color.Lerp(sLeaser.sprites[j].color, earthColor, Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, num), 1.6f));
            }
        }

        if (slatedForDeletetion || room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        blackColor = palette.blackColor;
        earthColor = Color.Lerp(palette.fogColor, palette.blackColor, 0.5f);
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

    public override void Update(bool eu)
    {
        base.Update(eu);
        lastRotation = rotation;

        if (room.game.devToolsActive && Input.GetKey("b"))
        {
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
            Vector2 val = rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x;
            rotation = val.normalized;
            firstChunk.vel.x *= 0.8f;
        }
    }

    public override void HitByWeapon(Weapon weapon)
    {
    }

    public void ThrowByPlayer()
    {
    }
}
