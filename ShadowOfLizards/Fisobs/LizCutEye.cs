using RWCustom;
using UnityEngine;
using static RoomCamera;
using static SharedPhysics;

namespace ShadowOfLizards;

sealed class LizCutEye : PlayerCarryableItem, IDrawable
{
    public float lastDarkness = -1f;
    public float darkness;

    Color blackColor;

    public Color LizColour;
    public Color EyeColour;

    bool bump;

    public Vector2[,,] cords;

    public bool lastGrabbed;

    public float rotation;
    public float lastRotation;

    public float rotSpeed;

    readonly TerrainCollisionData scratchTerrainCollisionData = new();

    public LizCutEyeAbstract Abstr { get; }

    public LizCutEye(LizCutEyeAbstract abstr) : base(abstr)
    {
        Abstr = abstr;

        bodyChunks = new BodyChunk[1];
        bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);
        bodyChunkConnections = new BodyChunkConnection[0];
        airFriction = 0.999f;
        gravity = 0.9f;
        bounce = 0.4f;
        surfaceFriction = 0.9f;
        collisionLayer = 2;
        waterFriction = 0.98f;
        buoyancy = 0.4f;
        GoThroughFloors = false;
        rotation = Random.value * 360f;
        lastRotation = rotation;
        cords = new Vector2[2, 5, 4];

        for (int i = 0; i < cords.GetLength(0); i++)
        {
            for (int j = 0; j < cords.GetLength(1); j++)
            {
                cords[i, j, 3] = Custom.RNV() * Random.value;
            }
        }
    }

    public override void NewRoom(Room newRoom)
    {
        base.NewRoom(newRoom);
        Reset();
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < cords.GetLength(0); i++)
        {
            for (int j = 0; j < cords.GetLength(1); j++)
            {
                cords[i, j, 0] = firstChunk.pos + new Vector2(0f, 5f * i);
                cords[i, j, 1] = cords[i, j, 0];
                cords[i, j, 2] *= 0f;
            }
        }
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);

        if (firstContact && speed > 10f)
        {
            room.PlaySound(SoundID.Lizard_Light_Terrain_Impact, firstChunk.pos, 0.35f, 2f);
        }
    }

    public void InitiateSprites(SpriteLeaser sLeaser, RoomCamera rCam)
    {
        LizColour = Abstr.LizBloodColourR != -1f ? new Color(Abstr.LizBloodColourR, Abstr.LizBloodColourG, Abstr.LizBloodColourB) : new Color(Abstr.LizColourR, Abstr.LizColourG, Abstr.LizColourB);

        EyeColour = new Color(Abstr.EyeColourR, Abstr.EyeColourG, Abstr.EyeColourB);

        sLeaser.sprites = new FSprite[5];
        sLeaser.sprites[0] = new FSprite("mouseEyeA1", true)
        {
            scaleX = 1.15f,
            scaleY = 1.15f
        };
        sLeaser.sprites[1] = new FSprite("pixel", true)
        {
            scaleX = 2f,
            scaleY = 8f
        };

        for (int i = 0; i < cords.GetLength(0); i++)
        {
            sLeaser.sprites[2 + i] = TriangleMesh.MakeLongMesh(cords.GetLength(1), false, false);
        }

        sLeaser.sprites[4] = new FSprite("mouseEyeA1", true)
        {
            scaleX = 1f,
            scaleY = 1f
        };

        AddToContainer(sLeaser, rCam, null);
    }

    public void DrawSprites(SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 vector = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
        Vector2 vector2 = Custom.DegToVec(Mathf.Lerp(lastRotation, rotation, timeStacker));

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

        if (LizColour == Color.black)
        {
            LizColour = blackColor;
        }
        if (EyeColour == Color.black)
        {
            LizColour = blackColor;
        }

        sLeaser.sprites[0].x = vector.x - camPos.x;
        sLeaser.sprites[0].y = vector.y - camPos.y;
        sLeaser.sprites[1].x = vector.x - vector2.x * 4f - camPos.x;
        sLeaser.sprites[1].y = vector.y - vector2.y * 4f - camPos.y;
        sLeaser.sprites[4].x = vector.x + vector2.x * 1.15f - camPos.x;
        sLeaser.sprites[4].y = vector.y + vector2.y * 1.15f - camPos.y;
        sLeaser.sprites[0].rotation = Custom.VecToDeg(vector2);
        sLeaser.sprites[1].rotation = Custom.VecToDeg(vector2);
        sLeaser.sprites[4].rotation = Custom.VecToDeg(vector2);
        sLeaser.sprites[0].color = blackColor;
        sLeaser.sprites[1].color = LizColour;
        sLeaser.sprites[4].color = EyeColour;

        if (blink > 0 && Random.value < 0.5f)
        {
            sLeaser.sprites[0].color = blinkColor;
            sLeaser.sprites[1].color = blinkColor;
            sLeaser.sprites[4].color = blinkColor;
        }

        for (int i = 0; i < cords.GetLength(0); i++)
        {
            Vector2 vector4 = vector - vector2 * 4f;
            for (int j = 0; j < cords.GetLength(1); j++)
            {
                Vector2 vector5 = Vector2.Lerp(cords[i, j, 1], cords[i, j, 0], timeStacker);
                Vector2 normalized = (vector5 - vector4).normalized;
                Vector2 a = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance(vector5, vector4) / 5f;
                if (j == 0)
                {
                    ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4, vector4 - a * 0.5f - camPos);
                    ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4 + 1, vector4 + a * 0.5f - camPos);
                }
                else
                {
                    ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4, vector4 - a * 0.5f + normalized * d - camPos);
                    ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4 + 1, vector4 + a * 0.5f + normalized * d - camPos);
                }
                ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4 + 2, vector5 - a * 0.5f - normalized * d - camPos);
                ((TriangleMesh)sLeaser.sprites[2 + i]).MoveVertice(j * 4 + 3, vector5 + a * 0.5f - normalized * d - camPos);
                ((TriangleMesh)sLeaser.sprites[2 + i]).color = LizColour;
                vector4 = vector5;
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
    }

    public void AddToContainer(SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer ??= rCam.ReturnFContainer("Items");

        foreach (FSprite fsprite in sLeaser.sprites)
        {
            fsprite.RemoveFromContainer();
            newContainer.AddChild(fsprite);
        }
        sLeaser.sprites[1].MoveBehindOtherNode(sLeaser.sprites[0]);
        //sLeaser.sprites[3].MoveBehindOtherNode(sLeaser.sprites[1]);
        //sLeaser.sprites[4].MoveBehindOtherNode(sLeaser.sprites[1]);
        //sLeaser.sprites[0].MoveBehindOtherNode(sLeaser.sprites[1]);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        lastRotation = rotation;
        rotation += rotSpeed;

        if (room.game.devToolsActive && Input.GetKey("b"))
        {
            firstChunk.vel += Custom.DirVec(firstChunk.pos, Futile.mousePosition) * 3f;
        }

        if (firstChunk.ContactPoint.y < 0)
        {
            if (Mathf.Abs(firstChunk.pos.x - firstChunk.lastPos.x) > 1f)
            {
                rotSpeed = Mathf.Lerp(rotSpeed, (firstChunk.pos.x - firstChunk.lastPos.x) * 18f, 0.45f);
            }
            else
            {
                rotSpeed *= 0.8f;
            }
            if (Mathf.Abs(rotSpeed) > 6f)
            {
                BodyChunk firstChunk = base.firstChunk;
                firstChunk.vel.x *= 0.95f;
                BodyChunk firstChunk2 = base.firstChunk;
                firstChunk2.vel.x += rotSpeed / Custom.LerpMap(Mathf.Abs(base.firstChunk.vel.x), 0f, 14f, 30f, 5000f);
                if (Custom.DegToVec(rotation).y > 0.9f)
                {
                    if (bump)
                    {
                        BodyChunk firstChunk3 = base.firstChunk;
                        firstChunk3.pos.y += 3f;
                        BodyChunk firstChunk4 = base.firstChunk;
                        firstChunk4.vel.y += Mathf.Abs(firstChunk.vel.x) / 10f;
                        BodyChunk firstChunk5 = base.firstChunk;
                        firstChunk5.vel.x *= 0.8f;
                        rotSpeed /= 2f;
                        bump = false;
                    }
                }
                else
                {
                    bump = true;
                }
            }
            else
            {
                BodyChunk firstChunk6 = firstChunk;
                firstChunk6.vel.x *= 0.5f;
            }
        }

        if (grabbedBy.Count > 0)
        {
            rotation = Custom.AimFromOneVectorToAnother(firstChunk.pos, grabbedBy[0].grabber.firstChunk.pos) + 90f * ((grabbedBy[0].graspUsed == 0) ? -1f : 1f);
            rotSpeed = 0f;
            if (!lastGrabbed)
            {
                lastRotation = rotation;
            }
        }
        else if (lastGrabbed)
        {
            rotSpeed = Mathf.Lerp(-35f, 35f, Random.value);
        }

        lastGrabbed = grabbedBy.Count > 0;
        firstChunk.collideWithObjects = !lastGrabbed;
        firstChunk.collideWithTerrain = !lastGrabbed;
        firstChunk.goThroughFloors = lastGrabbed;
        Vector2 a = -Custom.DegToVec(rotation);

        for (int i = 0; i < cords.GetLength(0); i++)
        {
            for (int j = 0; j < cords.GetLength(1); j++)
            {
                float num = (float)j / (float)(cords.GetLength(1) - 1);
                cords[i, j, 1] = cords[i, j, 0];
                cords[i, j, 0] += cords[i, j, 2];
                cords[i, j, 2] *= Mathf.Lerp(1f, 0.85f, num);
                cords[i, j, 2] += a * (3f + Mathf.Abs(rotSpeed) / 5f) * Mathf.Pow(1f - num, 3f);
                if (j > 1 && room.GetTile(cords[i, j, 0]).Solid)
                {
                    TerrainCollisionData terrainCollisionData = scratchTerrainCollisionData.Set(cords[i, j, 0], cords[i, j, 1], cords[i, j, 2], 1f, new IntVector2(0, 0), lastGrabbed);
                    terrainCollisionData = VerticalCollision(room, terrainCollisionData);
                    terrainCollisionData = HorizontalCollision(room, terrainCollisionData);
                    cords[i, j, 0] = terrainCollisionData.pos;
                    cords[i, j, 2] = terrainCollisionData.vel;
                }
                if (room.PointSubmerged(cords[i, j, 0]))
                {
                    cords[i, j, 2] *= 0.8f;
                    cords[i, j, 2].y += 0.3f * num;
                }
                else
                {
                    cords[i, j, 2].y -= 0.9f * room.gravity * num;
                }
                cords[i, j, 2] += Custom.RotateAroundOrigo(cords[i, j, 3], rotation);
                ConnectSegment(i, j);
            }
            for (int k = cords.GetLength(0) - 1; k >= 0; k--)
            {
                ConnectSegment(i, k);
            }
        }
    }

    public void ConnectSegment(int c, int i)
    {
        if (i == 0)
        {
            Vector2 vector = firstChunk.pos - Custom.DegToVec(rotation) * 8f;
            Vector2 a = Custom.DirVec(cords[c, i, 0], vector);
            float num = Vector2.Distance(cords[c, i, 0], vector);
            cords[c, i, 0] -= a * (1.5f - num);
            cords[c, i, 2] -= a * (1.5f - num);
            return;
        }
        Vector2 a2 = Custom.DirVec(cords[c, i, 0], cords[c, i - 1, 0]);
        float num2 = Vector2.Distance(cords[c, i, 0], cords[c, i - 1, 0]);
        cords[c, i, 0] -= a2 * (1.5f - num2) * 0.5f;
        cords[c, i, 2] -= a2 * (1.5f - num2) * 0.5f;
        cords[c, i - 1, 0] += a2 * (1.5f - num2) * 0.5f;
        cords[c, i - 1, 2] += a2 * (1.5f - num2) * 0.5f;
    }

    public void ThrowByPlayer()
    {
    }
}
