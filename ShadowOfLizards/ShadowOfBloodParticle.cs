using System;
using RWCustom;
using UnityEngine;

namespace ShadowOfLizards
{
    public class ShadowOfBloodParticle : BloodParticle
    {
        public ShadowOfBloodParticle(Vector2 pos, Vector2 angle, Color color, string splatterColor, BloodEmitter emitter, float vel) : base(pos, angle, color, splatterColor, emitter, vel)
        {
            this.splatterColor = splatterColor;
            lastPos = pos;
            lastLastPos = pos;
            lastLastLastPos = pos;
            this.pos = pos;
            this.color = color;
            this.emitter = emitter;
            if (this.emitter == null)
            {
                this.vel = angle;
                bleedTime = 1f;
                initialBleedTime = 1f;
                return;
            }
            bleedTime = emitter.bleedTime;
            initialBleedTime = emitter.bleedTime;
            if (emitter.chunk == null)
            {
                this.vel = Custom.RotateAroundVector(angle, new Vector2(UnityEngine.Random.Range(-1.7f, 1.7f), vel), Custom.VecToDeg(emitter.spear.stuckInAppendage.appendage.OnAppendageDirection(emitter.spear.stuckInAppendage)) + 230f);
                return;
            }
            this.vel = Custom.RotateAroundVector(angle, new Vector2(UnityEngine.Random.Range(-1.7f, 1.7f), vel), Custom.VecToDeg(angle) + 230f);
        }
        public override void Update(bool eu)
        {
            bleedTime -= 0.025f;
            if (!collision)
            {
                lastPos = pos;
                lastLastPos = lastPos;
                lastLastLastPos = lastLastPos;
                vel.y = vel.y - room.gravity;
                if (room.GetTile(pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                {
                    Destroy();
                }
                if (room.GetTile(pos).Terrain == Room.Tile.TerrainType.Solid)
                {
                    if (room.GetTile(pos + new Vector2(0f, 20f)).Terrain == Room.Tile.TerrainType.Air)
                    {
                        pos.y = room.MiddleOfTile(pos).y + 10f;
                    }
                    else if (room.GetTile(pos + new Vector2(0f, -20f)).Terrain == Room.Tile.TerrainType.Air)
                    {
                        pos.y = room.MiddleOfTile(pos).y - 10f;
                    }
                    else if (room.GetTile(pos + new Vector2(20f, 0f)).Terrain == Room.Tile.TerrainType.Air)
                    {
                        pos.x = room.MiddleOfTile(pos).x + 10f;
                    }
                    else if (room.GetTile(pos + new Vector2(-20f, 0f)).Terrain == Room.Tile.TerrainType.Air)
                    {
                        pos.x = room.MiddleOfTile(pos).x - 10f;
                    }
                    if (room.GetTile(pos + new Vector2(0f, 20f)).Terrain != Room.Tile.TerrainType.ShortcutEntrance || room.GetTile(pos + new Vector2(0f, 20f)).Terrain != Room.Tile.TerrainType.Solid)
                    {
                        if (emitter != null)
                        {
                            if (UnityEngine.Random.value < Mathf.Lerp(0f, 0.8f, Mathf.Lerp(0f, 100f, BloodMod.Options.splatterRate.Value)))
                            {
                                room.AddObject(new BloodSplatter(pos, splatterColor + "Tex", UnityEngine.Random.Range(10f, 50f)));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                room.AddObject(new BloodSplatter(pos, splatterColor + "Tex", UnityEngine.Random.Range(20f, 30f)));
                            }
                        }
                        slatedForDeletetion = true;
                    }
                    collision = true;
                }
            }
            else
            {
                slatedForDeletetion = true;
            }
            base.Update(eu);
        }
    }
}
