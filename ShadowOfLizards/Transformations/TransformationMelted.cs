using MoreSlugcats;
using RWCustom;
using Smoke;
using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;
using static Explosion;
using System;

namespace ShadowOfLizards;

internal class TransformationMelted
{
    static public void Apply()
    {
        On.LizardAI.DetermineBehavior += NoBehavior;
        On.LizardAI.AggressiveBehavior += NoBite;
    }

    public static void MeltedSpitDraw(LizardSpit self, RoomCamera.SpriteLeaser sLeaser, LizardData data)
    {
        Color color = new(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));

        sLeaser.sprites[self.DotSprite].color = color;
    }

    public static void MeltedSpitUpdate(LizardSpit self)
    {
        if (self.stickChunk.owner is Creature || self.stickChunk.owner is Player)
        {
            Creature owner = self.stickChunk.owner as Creature;

            if (owner is Lizard)
            {
                PreViolenceCheck(owner);
            }

            LethatWaterDamage(owner, self.stickChunk);

            if (owner is Lizard liz)
            {
                PostViolenceCheck(liz, "Melted", self.lizard);
            }
        }
    }

    public static void PreMeltedLizardBite(Lizard self, BodyChunk chunk)
    {
        if (chunk != null && chunk.owner != null && chunk.owner is Creature owner)
        {
            if (owner is Lizard && lizardstorage.TryGetValue(owner.abstractCreature, out ShadowOfLizards.LizardData _))
            {
                PreViolenceCheck(owner);
            }

            LethatWaterDamage(owner, self.mainBodyChunk);
        }
        else
        {
            self.room.AddObject(new Smolder(self.room, self.firstChunk.pos, self.firstChunk, null));
            self.room.AddObject(new ExplosionSmoke(self.firstChunk.pos, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
            self.room.PlaySound(SoundID.Firecracker_Burn, self.firstChunk.pos, 0.5f, 0.5f + UnityEngine.Random.value * 1.5f);
        }
    }

    public static void PostMeltedLizardBite(Lizard self, BodyChunk chunk)
    {
        if (chunk == null || chunk.owner == null || chunk.owner is not Creature owner)
        {
            return;
        }

        if (owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData _))
        {
            PostViolenceCheck(liz, "Melted", self);
        }
    }

    static void LethatWaterDamage(Creature crit, BodyChunk self)
    {
        try
        {
            if (crit is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && (data.transformation == "Melted" || data.transformation == "MeltedTransformation") || crit.abstractCreature.lavaImmune)
            {
                crit.room.AddObject(new Smolder(crit.room, crit.firstChunk.pos, crit.firstChunk, null));
                crit.room.AddObject(new ExplosionSmoke(crit.firstChunk.pos, Custom.RNV() * 5f * UnityEngine.Random.value, 1f));
                crit.room.PlaySound(SoundID.Firecracker_Burn, crit.firstChunk.pos, 0.5f, 0.5f + UnityEngine.Random.value * 1.5f);
                return;
            }

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
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void MeltedLizardUpdate(Lizard self, LizardData data)
    {
        /*
        if (data.transformation == "Melted")
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
        else if (data.transformation == "MeltedTransformation")
        {
            self.AI.redSpitAI ??= new LizardAI.LizardSpitTracker(self.AI);

            if (!self.dead && !self.Stunned)
            {
                LizardGraphics graphicsModule = self.graphicsModule as LizardGraphics;

                for (int i = 0; i < 6; i++)
                {
                    if (data.ArmState[i] != "Normal" && Random.value < 0.015f)
                    {
                        self.room.AddObject(new LizardSpit(graphicsModule.limbs[0].pos, new Vector2(0f, 0f), self));
                    }        
                }
            }
        }
        */

        try
        {
            bool isStorySession = self.abstractCreature.world.game.IsStorySession;
            int cycleNumber = isStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : -1;

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " in Lethal Water");

            data.liz["PreMeltedTime"] = isStorySession ? cycleNumber.ToString() : "-1";
            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_transformation_chance.Value)
            {
                return;
            }

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " was made Melted due to swimming in Lethal Water");

            data.transformation = "Melted";
            data.transformationTimer = isStorySession ? cycleNumber : -1;

            data.liz.Remove("PreMeltedTime");

            if (!data.liz.TryGetValue("MeltedR", out _))
            {
                data.liz.Add("MeltedR", data.rCam != null ? data.rCam.currentPalette.waterColor1.r.ToString() : "0.4078431");
                data.liz.Add("MeltedG", data.rCam != null ? data.rCam.currentPalette.waterColor1.g.ToString() : "0.5843138");
                data.liz.Add("MeltedB", data.rCam != null ? data.rCam.currentPalette.waterColor1.b.ToString() : "0.1843137");
            }
            else
            {
                data.liz["MeltedR"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.r.ToString() : "0.4078431";
                data.liz["MeltedG"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.g.ToString() : "0.5843138";
                data.liz["MeltedB"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.b.ToString() : "0.1843137";
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void MeltedEatRegrowth(Lizard self, Lizard liz, LizardData data, LizardData data2)
    {
        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + self.ToString() + " was made Melted due to eating " + self.grasps[0].grabbed);

        data.transformation = "Melted";
        data.transformationTimer = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

        bool data2Melted = data2.liz.TryGetValue("MeltedR", out string _);

        if (!data.liz.TryGetValue("MeltedR", out string _))
        {
            data.liz.Add("MeltedR", data2Melted ? data2.liz["MeltedR"] : "0.4078431");
            data.liz.Add("MeltedG", data2Melted ? data2.liz["MeltedG"] : "0.5843138");
            data.liz.Add("MeltedB", data2Melted ? data2.liz["MeltedB"] : "0.1843137");
        }
        else
        {
            data.liz["MeltedR"] = data2Melted ? data2.liz["MeltedR"] : "0.4078431";
            data.liz["MeltedG"] = data2Melted ? data2.liz["MeltedG"] : "0.5843138";
            data.liz["MeltedB"] = data2Melted ? data2.liz["MeltedB"] : "0.1843137";
        }
    }

    #region Behavior
    static LizardAI.Behavior NoBehavior(On.LizardAI.orig_DetermineBehavior orig, LizardAI self)
    {
        if (ShadowOfOptions.melted_transformation.Value && lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            if (data.transformation == "Melted")
            {
                return LizardAI.Behavior.Frustrated;
            }
            if (data.transformation == "MeltedTransformation")
            {
                return LizardAI.Behavior.Hunt;
            }
        }
        return orig.Invoke(self);
    }

    static void NoBite(On.LizardAI.orig_AggressiveBehavior orig, LizardAI self, Tracker.CreatureRepresentation target, float tongueChance)
    {
        if (ShadowOfOptions.melted_transformation.Value && lizardstorage.TryGetValue(self.lizard.abstractCreature, out ShadowOfLizards.LizardData data) && data.transformation == "Melted")
        {
            return;
        }

        orig.Invoke(self, target, tongueChance);
    }
    #endregion
}
