using System;
using UnityEngine;
using RWCustom;

namespace ShadowOfLizards;

public class BrokenTooth : CosmeticSprite
{
    private RoomPalette palette;

    private Color colour;
    private Color rootColour;

    public int electricColorTimer = 0;

    private readonly string spriteName;

    private readonly bool isCyan = false;

    private int flicker;
    private readonly float flickerColor = 0;

    private float baseBlink;
    private float baseLastBlink;

    private const int SourceCodeLizardsFlickerThreshold = 10;

    public BrokenTooth(Vector2 pos, Vector2 vel, string spriteName, Color colour, Color rootColour, float scaleX, float scaleY)
    {
        pos += vel;
        lastPos = pos;
        this.vel = vel;
        hue = 1f;
        saturation = 0.5f;
        this.scaleX = scaleX;
        this.scaleY = scaleY;
        rotation = UnityEngine.Random.value * 360f;
        lastRotation = rotation;
        rotVel = Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * Custom.LerpMap(vel.magnitude, 0f, 18f, 5f, 26f);
        zRotation = UnityEngine.Random.value * 360f;
        lastZRotation = rotation;
        zRotVel = Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * Custom.LerpMap(vel.magnitude, 0f, 18f, 2f, 16f);

        this.colour = colour;
        this.rootColour = rootColour;
        this.spriteName = spriteName;

        baseBlink = UnityEngine.Random.value;
        baseLastBlink = baseBlink;
    }

    public override void Update(bool eu)
    {
        if (ShadowOfOptions.cosmetic_sprite_despawn.Value)
            counter++;

        if (room.PointSubmerged(pos))
        {
            vel *= 0.92f;
            vel.y -= room.gravity * 0.1f;
            rotVel *= 0.965f;
            zRotVel *= 0.965f;
        }
        else
        {
            vel *= 0.999f;
            vel.y -= room.gravity * 0.9f;
        }

        if (dripCounter < 10 && UnityEngine.Random.value < 0.1f)
        {
            dripCounter++;
            room.AddObject(new WaterDrip(Vector2.Lerp(lastPos, pos, UnityEngine.Random.value), vel + Custom.RNV() * UnityEngine.Random.value * 2f, false));
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

        lastRotation = rotation;
        rotation += rotVel * Vector2.Distance(lastPos, pos);
        lastZRotation = zRotation;
        zRotation += zRotVel * Vector2.Distance(lastPos, pos);
        if (!Custom.DistLess(lastPos, pos, 3f) && room.GetTile(pos).Solid && !room.GetTile(lastPos).Solid)
        {
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, room.GetTilePosition(lastPos), room.GetTilePosition(pos));
            FloatRect floatRect = Custom.RectCollision(pos, lastPos, room.TileRect(intVector.Value).Grow(2f));
            pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
            bool flag = false;
            if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
            {
                vel.x = Mathf.Abs(vel.x) * 0.15f;
                flag = true;
            }
            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
            {
                vel.x = -Mathf.Abs(vel.x) * 0.15f;
                flag = true;
            }
            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
            {
                vel.y = Mathf.Abs(vel.y) * 0.15f;
                flag = true;
            }
            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
            {
                vel.y = -Mathf.Abs(vel.y) * 0.15f;
                flag = true;
            }
            if (flag)
            {
                if (!firstImpact)
                {
                    room.PlaySound(impactSound, pos);
                }
                rotVel *= 0.8f;
                zRotVel *= 0.8f;
                if (vel.magnitude > 3f)
                {
                    rotVel += Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * 4f * UnityEngine.Random.value * Mathf.Abs(rotVel / 15f);
                    zRotVel += Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * 4f * UnityEngine.Random.value * Mathf.Abs(rotVel / 15f);
                }
                firstImpact = true;
            }
        }
        SharedPhysics.TerrainCollisionData terrainCollisionData = scratchTerrainCollisionData.Set(pos, lastPos, vel, 3f, new IntVector2(0, 0), true);
        terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
        terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
        pos = terrainCollisionData.pos;
        vel = terrainCollisionData.vel;
        if (terrainCollisionData.contactPoint.x != 0)
        {
            vel.y *= 0.6f;
        }
        if (terrainCollisionData.contactPoint.y != 0)
        {
            vel.x *= 0.6f;
        }
        if (terrainCollisionData.contactPoint.y < 0)
        {
            rotVel *= 0.7f;
            zRotVel *= 0.7f;
            if (vel.magnitude < 1f && ShadowOfOptions.cosmetic_sprite_despawn.Value)
            {
                dissapearCounter++;
                if (dissapearCounter > 30)
                {
                    counter = Math.Max(counter, 300);
                }
            }
        }
        if (dissapearCounter > 390 || pos.x < -100f || pos.y < -100f)
        {
            Destroy();
        }
        base.Update(eu);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];
        sLeaser.sprites[0] = new FSprite(Futile.atlasManager.GetElementWithName(spriteName), true);
        sLeaser.sprites[1] = new FSprite(Futile.atlasManager.GetElementWithName(spriteName + "Root"), true);

        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
        float num = Mathf.InverseLerp(305f, 380f, (float)counter + timeStacker);
        vector.y -= 20f * Mathf.Pow(num, 3f);
        float num2 = Mathf.Pow(1f - num, 0.25f);
        lastDarkness = darkness;
        darkness = rCam.room.Darkness(vector);
        darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(vector);
        Vector2 vector2 = Custom.DegToVec(Mathf.Lerp(lastZRotation, zRotation, timeStacker));

        if (electricColorTimer > 0)
        {
            electricColorTimer--;
        }

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = vector.x - camPos.x;
            sLeaser.sprites[i].y = vector.y - camPos.y;
            sLeaser.sprites[i].rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);
            sLeaser.sprites[i].scaleY = num2 * scaleY;
            if (Mathf.Abs(vector2.x) < 0.1f)
            {
                sLeaser.sprites[i].scaleX = 0.1f * Mathf.Sign(vector2.x) * num2 * scaleX;
            }
            else
            {
                sLeaser.sprites[i].scaleX = vector2.x * num2 * scaleX;
            }
        }
        sLeaser.sprites[0].x += Custom.DegToVec(Mathf.Lerp(lastRotation, rotation, timeStacker)).x * 1.5f * num2;
        sLeaser.sprites[0].y += Custom.DegToVec(Mathf.Lerp(lastRotation, rotation, timeStacker)).y * 1.5f * num2;

        sLeaser.sprites[0].color = Color.Lerp(Custom.HSL2RGB(hue, saturation, 0.5f), colour, 0.7f + 0.3f * darkness);

        sLeaser.sprites[1].color = Color.Lerp(Custom.HSL2RGB(hue, saturation, 0.5f), rootColour, 0.7f + 0.3f * darkness);

        if (isCyan)
        {
            sLeaser.sprites[0].color = electricColorTimer > 0 ? ElectricColor(HeadColor(timeStacker)) : HeadColor(timeStacker);
        }

        if (num > 0.3f)
        {
            for (int j = 0; j < sLeaser.sprites.Length; j++)
            {
                sLeaser.sprites[j].color = Color.Lerp(sLeaser.sprites[j].color, earthColor, Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, num), 1.6f));
            }
        }
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        this.palette = palette;
        earthColor = Color.Lerp(palette.fogColor, palette.blackColor, 0.5f);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }

    Color ElectricColor(Color col)
    {
        return Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)electricColorTimer / 50f);
    }

    public Color HeadColor(float timeStacker)
    {
        float num = 1f - Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(baseLastBlink, baseBlink, timeStacker) * 2f * 3.1415927f), 1.5f);
        if (flicker > SourceCodeLizardsFlickerThreshold)
        {
            num = flickerColor;
        }
        return Color.Lerp(palette.blackColor, colour, num);
    }

    private float rotation;
    private float lastRotation;

    private float rotVel;

    private float lastDarkness = -1f;
    private float darkness;

    private readonly float hue;
    private readonly float saturation;

    private readonly float scaleX;
    private readonly float scaleY;

    private float zRotation;
    private float lastZRotation;

    private float zRotVel;

    private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;

    private int counter;
    private int dripCounter;

    private int dissapearCounter;

    private readonly SoundID impactSound = SoundID.SS_AI_Marble_Hit_Floor;

    private bool firstImpact;

    private Color earthColor;
}