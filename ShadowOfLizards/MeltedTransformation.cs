using MoreSlugcats;
using RWCustom;
using Smoke;
using UnityEngine;
using static Explosion;

namespace ShadowOfLizards;

internal class MeltedTransformation
{
    static public void Apply()
    {
        On.LizardSpit.DrawSprites += MeltingSpitDraw;
        On.LizardSpit.Update += MeltingSpitSpit;
        On.Lizard.Bite += BiteMeltingCheck;
        On.Lizard.Update += MeltedLegUpdate;
        On.LizardAI.DetermineBehavior += NoBehavior;
        On.LizardAI.AggressiveBehavior += NoBite;

    }

    static void MeltingSpitDraw(On.LizardSpit.orig_DrawSprites orig, LizardSpit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_spit.Value && self != null && self.lizard != null && ShadowOfLizards.lizardstorage.TryGetValue(self.lizard, out ShadowOfLizards.LizardData value) && value.liz["Melted"] == "True")
        {
            Color color = new(float.Parse(value.liz["MeltedR"]), float.Parse(value.liz["MeltedG"]), float.Parse(value.liz["MeltedB"]));
            for (int i = 0; i < self.slime.GetLength(0); i++)
            {
                sLeaser.sprites[self.SlimeSprite(i)].color = color;
            }
        }
    }

    static void MeltingSpitSpit(On.LizardSpit.orig_Update orig, LizardSpit self, bool eu)
    {
        orig.Invoke(self, eu);
        if (!ShadowOfOptions.melted_transformation.Value || !ShadowOfOptions.melted_spit.Value || self == null || self.lizard == null || !ShadowOfLizards.lizardstorage.TryGetValue(self.lizard, out ShadowOfLizards.LizardData value) || !(value.liz["Melted"] == "True") 
            || !(Random.value < 0.1f) || self.stickChunk == null || self.stickChunk.owner == null || self.stickChunk.owner.room != self.room || !Custom.DistLess(self.stickChunk.pos, self.pos, self.stickChunk.rad + 40f) || self.fallOff <= 0)
        {
            return;
        }

        if (self.stickChunk.owner is Creature || self.stickChunk.owner is Player)
        {
            Creature owner = self.stickChunk.owner as Creature;

            if (owner is Lizard)
            {
                ShadowOfLizards.PreViolenceCheck(owner);
            }

            LethatWaterDamage(owner, self.stickChunk);

            if (owner is Lizard liz)
            {
                ShadowOfLizards.PostViolenceCheck(liz, "Melted", self.lizard);
            }
        }
    }

    static void BiteMeltingCheck(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfLizards.lizardstorage.TryGetValue(self, out ShadowOfLizards.LizardData value) && value.liz["Melted"] == "True" && 
            chunk != null && chunk.owner != null && chunk.owner is Creature owner && !owner.abstractCreature.lavaImmune)
        {
            if (owner is Lizard && ShadowOfLizards.lizardstorage.TryGetValue((Lizard)owner, out ShadowOfLizards.LizardData _))
            {
                ShadowOfLizards.PreViolenceCheck(owner);
            }

            LethatWaterDamage(owner, self.mainBodyChunk);

            orig.Invoke(self, chunk);

            if (owner is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz, out ShadowOfLizards.LizardData _))
            {
                ShadowOfLizards.PostViolenceCheck(liz, "Melted", self);
            }
        }
        else
        {
            orig.Invoke(self, chunk);
        }
    }

    static void LethatWaterDamage(Creature crit, BodyChunk self)
    {
        if (crit is Player && !crit.dead)
        {
            if (ModManager.MSC && ((crit as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) || (crit as Player).SlugCatClass != null && (crit as Player).SlugCatClass.value == "sproutcat")
            {
                (crit as Player).pyroJumpCounter = (crit as Player).pyroJumpCounter + 1;
                if ((crit as Player).pyroJumpCounter >= MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value)
                {
                    (crit as Player).PyroDeath();
                }
            }
            else
            {
                crit.Die();
            }
        }
        else if (crit.State is HealthState || (crit.State is HealthState && (crit.State as HealthState).health > 1f))
        {
            if (!crit.dead)
            {
                crit.Violence(null, new Vector2?(new Vector2(0f, 5f)), crit.firstChunk, null, Creature.DamageType.Explosion, 0.2f, 0.1f);
            }
        }
        else if (!crit.dead)
        {
            crit.Die();
        }

        if (crit.lavaContactCount == 0)
        {
            crit.mainBodyChunk.vel.x = 35f * self.vel.normalized.x;
            crit.mainBodyChunk.vel.y = 35f * self.vel.normalized.y;
            crit.room.AddObject(new Smolder(crit.room, crit.firstChunk.pos, crit.firstChunk, null));
        }
        else if (crit.lavaContactCount == 1)
        {
            crit.mainBodyChunk.vel.x = 20f * self.vel.normalized.x;
            crit.mainBodyChunk.vel.y = 20f * self.vel.normalized.y;
        }
        else if (crit.lavaContactCount == 2)
        {
            crit.mainBodyChunk.vel.x = 15f * self.vel.normalized.x;
            crit.mainBodyChunk.vel.y = 15f * self.vel.normalized.y;
        }
        else if (crit.lavaContactCount == 3)
        {
            crit.mainBodyChunk.vel.x = 5f * self.vel.normalized.x;
            crit.mainBodyChunk.vel.y = 5f * self.vel.normalized.y;
            crit.room.AddObject(new Smolder(crit.room, crit.firstChunk.pos, crit.firstChunk, null));
        }

        if (crit.lavaContactCount < ((crit is Player) ? 400 : 30))
        {
            crit.lavaContactCount++;
            crit.room.AddObject(new ExplosionSmoke(crit.firstChunk.pos, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
        }

        if (!crit.lavaContact)
        {
            if (crit.lavaContactCount <= 3)
            {
                for (int i = 0; i < 14 + (3 - crit.lavaContactCount) * 5; i++)
                {
                    Vector2 val = Custom.RNV();
                    crit.room.AddObject(new Spark(crit.firstChunk.pos + val * UnityEngine.Random.value * 40f, val * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 8, 24));
                }
            }
            crit.room.PlaySound(SoundID.Firecracker_Burn, crit.firstChunk.pos, 0.5f, 0.5f + UnityEngine.Random.value * 1.5f);
            crit.lavaContact = true;
            crit.lavaContactCount++;
        }
    }

    static void MeltedLegUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig.Invoke(self, eu);
        if (!ShadowOfLizards.lizardstorage.TryGetValue(self, out ShadowOfLizards.LizardData data) || !ShadowOfOptions.melted_transformation.Value)
        {
            return;
        }
        if (data.liz["Melted"] == "True" && data.liz["MeltedTransformation"] == "False")
        {
            self.AI.redSpitAI ??= new LizardAI.LizardSpitTracker(self.AI);

            int num = 0;
            LizardGraphics graphicsModule = self.graphicsModule as LizardGraphics;
            while (true)
            {   
                if (num >= graphicsModule.bodyParts.Length)
                {
                    break;
                }
                if (Random.value < ((!self.dead) ? 0.015f : 0.001f))
                {
                    Room val = self.room;
                    val.AddObject(new LizardSpit(graphicsModule.bodyParts[num].pos, new Vector2(Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1)), self));
                }
                num++;
            }
        }
        else if (data.liz["MeltedTransformation"] == "True")
        {
            self.AI.redSpitAI ??= new LizardAI.LizardSpitTracker(self.AI);

            if (!self.dead && !self.Stunned)
            {
                LizardGraphics graphicsModule = self.graphicsModule as LizardGraphics;

                for (int i = 0; i < 6; i++)
                {
                    if (data.liz["ArmState" + i] != "Normal" && Random.value < 0.015f)
                    {
                        self.room.AddObject(new LizardSpit(graphicsModule.limbs[0].pos, new Vector2(0f, 0f), self));
                    }        
                }
            }
        }

        if (!(data.liz["PreMeltedTime"] == "False") || !(self.Submersion > 0.1f) || self.room.waterObject == null || !self.room.waterObject.WaterIsLethal || !(data.liz["Electric"] != "True") || !(data.liz["Melted"] != "True") || !(data.liz["SpiderMother"] != "True") 
            || !self.dead)
        {
            return;
        }

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(self.ToString() + " in Lethal Water");

        data.liz["PreMeltedTime"] = "True";
        if (Random.Range(0, 100) < ShadowOfOptions.melted_transformation_chance.Value)
        {
            return;
        }

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(self.ToString() + " was made Melted due to swimming in Lethal Water");

        data.liz["MeltedTime"] = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1";
        data.liz["Electric"] = "False";
        data.liz["Melted"] = "True";
        data.liz["SpiderMother"] = "False";

        data.liz["MeltedR"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.r.ToString() : "0.4078431";
        data.liz["MeltedG"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.g.ToString() : "0.5843138";
        data.liz["MeltedB"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.b.ToString() : "0.1843137";

        if (ShadowOfOptions.melted_transformation_skip.Value)
        {
            data.liz["MeltedTime"] = "-4";
            data.liz["MeltedTransformation"] = "True";
        }
    }

    static LizardAI.Behavior NoBehavior(On.LizardAI.orig_DetermineBehavior orig, LizardAI self)
    {
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfLizards.lizardstorage.TryGetValue(self.lizard, out ShadowOfLizards.LizardData data))
        {
            if (data.liz["Melted"] == "True" && data.liz["MeltedTransformation"] == "False")
            {
                return LizardAI.Behavior.Frustrated;
            }
            if (data.liz["MeltedTransformation"] == "True")
            {
                return LizardAI.Behavior.Hunt;
            }
        }
        return orig.Invoke(self);
    }

    static void NoBite(On.LizardAI.orig_AggressiveBehavior orig, LizardAI self, Tracker.CreatureRepresentation target, float tongueChance)
    {
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfLizards.lizardstorage.TryGetValue(self.lizard, out ShadowOfLizards.LizardData data) && (data.liz["Melted"] == "True") && (data.liz["MeltedTransformation"] == "False"))
        {
            return;
        }
        orig.Invoke(self, target, tongueChance);
    }
}
