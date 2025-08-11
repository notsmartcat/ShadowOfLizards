using System;
using RWCustom;
using UnityEngine;

namespace ShadowOfLizards;

sealed class ShadowOfBloodEmitter : BloodEmitter
{
    Vector2 emitAngle;

    public ShadowOfBloodEmitter(Spear spear, BodyChunk chunk, float velocity, float bleedTime) : base(spear, chunk, velocity, bleedTime)
    {
        try
        {
            currentTime = Time.time;
            this.chunk = chunk;
            this.bleedTime = bleedTime;
            initialBleedTime = bleedTime;
            maxVelocity = velocity;
            creatureColor = new Color(0.5f, 0f, 0f);
            splatterColor = "GreenLizard";

            if (this.chunk.owner is Creature && BloodMod.creatureColors.ContainsKey((this.chunk.owner as Creature).Template.type.value))
            {
                creatureColor = BloodMod.creatureColors[(this.chunk.owner as Creature).Template.type.value];
                splatterColor = (this.chunk.owner as Creature).Template.type.value;
            }
            else if (this.chunk.owner is LizCutHead cut)
            {
                creatureColor = cut.bloodColour;
                splatterColor = cut.Abstr.breed;

                emitPos = chunk.pos;
                emitAngle = cut.rotation;
            }
        }
        catch (Exception e)
        {
            Destroy();
            Debug.LogException(e);
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        if (chunk.owner is LizCutHead cutHead)
        {
            emitPos = chunk.pos;
            emitAngle = Custom.DegToVec(Custom.VecToDeg(cutHead.rotation));

            if (velocity >= UnityEngine.Random.Range(0.65f, 1.1f))
            {
                room.AddObject(new ShadowOfBloodParticle(emitPos, emitAngle, creatureColor, splatterColor, this, velocity));
            }
        }
        else if (chunk.owner is Creature crit2 && !crit2.inShortcut)
        {
            emitPos = chunk.pos;
            emitAngle = chunk == chunk.owner.bodyChunks[0] ? chunk.owner.bodyChunks[1].Rotation : chunk.Rotation;

            if (velocity >= UnityEngine.Random.Range(0.65f, 1.1f))
            {
                room.AddObject(new BloodParticle(emitPos, emitAngle, creatureColor, splatterColor, this, velocity));
            }
        }
    }
}

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
            this.vel = Custom.RotateAroundVector(angle, new Vector2(UnityEngine.Random.Range(-1.7f, 1.7f), vel), Custom.VecToDeg(emitter.spear.stuckInAppendage.appendage.OnAppendageDirection(emitter.spear.stuckInAppendage)));
            return;
        }
        this.vel = Custom.RotateAroundVector(angle, new Vector2(UnityEngine.Random.Range(-1.7f, 1.7f), vel), Custom.VecToDeg(angle));
    }
}