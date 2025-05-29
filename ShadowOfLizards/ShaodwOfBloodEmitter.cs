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
                creatureColor = cut.LizBloodColour;
                splatterColor = cut.Abstr.LizBreed;

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
        counter++;
        velocity = Mathf.Lerp(maxVelocity * UnityEngine.Random.Range(0.5f, 1f), -1f, Mathf.Sin((float)counter / 5f));

        if (emitPos.y > room.RoomRect.top + 100f)
        {
            Destroy();
        }

        if (chunk == null)
        {
            Destroy();
        }

        if (chunk.owner is Creature crit && !crit.dead)
        {
            bleedTime -= 0.025f;
        }
        else
        {
            bleedTime -= 0.05f;
        }

        if (bleedTime <= 0f)
        {
            Destroy();
            return;
        }

        if (chunk.owner is LizCutHead cutHead)
        {
            emitPos = chunk.pos;
            emitAngle = Custom.RotateAroundOrigo(cutHead.rotation, 120);

            if (velocity >= UnityEngine.Random.Range(0.65f, 1.1f))
            {
                room.AddObject(new ShadowOfBloodParticle(emitPos, emitAngle, creatureColor, splatterColor, this, velocity));
            }
        }
        else if (chunk.owner is Creature crit2 && !crit2.inShortcut && chunk == chunk.owner.bodyChunks[0])
        {
            emitPos = chunk.pos;
            emitAngle = chunk.owner.bodyChunks[1].Rotation;

            if (velocity >= UnityEngine.Random.Range(0.65f, 1.1f))
            {
                room.AddObject(new BloodParticle(emitPos, emitAngle, creatureColor, splatterColor, this, velocity));
            }
        }
        else if (chunk.owner is Creature crit3 && !crit3.inShortcut && chunk == chunk.owner.bodyChunks[0])
        {
            emitPos = chunk.pos;
            emitAngle = chunk.Rotation;

            if (velocity >= UnityEngine.Random.Range(0.65f, 1.1f))
            {
                room.AddObject(new BloodParticle(emitPos, emitAngle, creatureColor, splatterColor, this, velocity));
            }
        }
    }
}

