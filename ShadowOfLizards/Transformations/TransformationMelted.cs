using MoreSlugcats;
using RWCustom;
using Smoke;
using UnityEngine;
using static Explosion;
using System;
using System.Collections.Generic;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class TransformationMelted
{
    static public void Apply()
    {
        On.LizardAI.DetermineBehavior += NoBehavior;
        On.LizardAI.AggressiveBehavior += NoBite;
    }

    #region Spit
    public static void MeltedSpitDraw(LizardSpit self, RoomCamera.SpriteLeaser sLeaser, LizardData data)
    {
        Color color = new(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));

        sLeaser.sprites[self.DotSprite].color = color;
    }

    public static void MeltedSpitUpdate(LizardSpit self)
    {
        if (self.stickChunk.owner is not Creature owner)
        {
            return;
        }

        if (owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            PreViolenceCheck(liz, data);
        }

        LethatWaterDamage(owner, self.stickChunk);

        if (owner is Lizard liz2 && lizardstorage.TryGetValue(liz2.abstractCreature, out LizardData data2))
        {
            PostViolenceCheck(liz2, data2, "Melted", self.lizard);
        }
    }
    #endregion

    public static void NewMeltedLizard(Lizard self, World world, LizardData data)
    {
        AbstractCreature abstractCreature = self.abstractCreature;

        abstractCreature.ignoreCycle = true;

        #region Swimming
        if (ShadowOfOptions.swim_ability.Value && !self.Template.canSwim)
        {
            data.liz["CanSwim"] = "True";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + "'s Melted Transformation has overridden CanSwim to True");

            self.Template.canSwim = true;

            if (abstractCreature.creatureTemplate.waterRelationship != CreatureTemplate.WaterRelationship.Amphibious && abstractCreature.creatureTemplate.waterRelationship != CreatureTemplate.WaterRelationship.WaterOnly)
            {
                abstractCreature.creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
            }

            if (abstractCreature.creatureTemplate.waterPathingResistance > 1f)
                abstractCreature.creatureTemplate.waterPathingResistance = ((abstractCreature.creatureTemplate.waterPathingResistance - 1f) / 2) + 1f > 1f ? ((abstractCreature.creatureTemplate.waterPathingResistance - 1f) / 2) + 1f : 1f;

            PathCost dropToWater = abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater];
            if (dropToWater.legality == PathCost.Legality.Unallowed || dropToWater.legality == PathCost.Legality.Unwanted)
            {
                List<TileConnectionResistance> list2 = new()
                            {
                            new TileConnectionResistance(MovementConnection.MovementType.DropToWater, 20f, PathCost.Legality.Allowed)
                            };

                for (int n = 0; n < list2.Count; n++)
                {
                    abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)list2[n].movementType] = list2[n].cost;
                }
            }
        }
        #endregion

        #region Hypothermia
        if (ModManager.HypothermiaModule)
        {
            bool immune = abstractCreature.HypothermiaImmune;

            abstractCreature.HypothermiaImmune = true;
            self.Template.BlizzardAdapted = true;
            self.Template.BlizzardWanderer = true;

            if (ShadowOfOptions.hypothermia_immune.Value && !immune && (!data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune) || HypothermiaImmune != "True"))
            {
                data.liz["HypothermiaImmune"] = "True";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + "'s Melted Transformation has overridden HypothermiaImmune to True");
            }
        }
        #endregion

        #region Lava
        if (ShadowOfOptions.lava_immune.Value && !abstractCreature.lavaImmune && (!data.liz.TryGetValue("LavaImmune", out string LavaImmune) || LavaImmune != "True"))
        {
            abstractCreature.lavaImmune = true;
            data.liz["LavaImmune"] = "True";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + "'s Melted Transformation has overridden LavaImmune to True");
        }
        #endregion

        #region Colour
        if (!data.liz.ContainsKey("MeltedR"))
        {
            Color waterColour = (world != null && world.activeRooms[0] != null && world.activeRooms[0].waterObject != null && world.activeRooms[0].waterObject.WaterIsLethal && world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 != null) ? world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 : new Color(0.4078431f, 0.5843138f, 0.1843137f);
            data.liz["MeltedR"] = waterColour.r.ToString();
            data.liz["MeltedG"] = waterColour.g.ToString();
            data.liz["MeltedB"] = waterColour.b.ToString();
        }

        if (false && abstractCreature.creatureTemplate.type != CreatureTemplate.Type.WhiteLizard)
        {
            self.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
        }
        #endregion
    }

    public static void PreMeltedLizardBite(Lizard self, LizardData data, BodyChunk chunk)
    {
        if (chunk != null && chunk.owner != null && chunk.owner is Creature owner)
        {
            if (owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out _))
            {
                PreViolenceCheck(liz, data);
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

    public static void PostMeltedLizardBite(Lizard self, LizardData data, BodyChunk chunk)
    {
        if (chunk == null || chunk.owner == null || chunk.owner is not Creature owner || owner is not Lizard liz || !lizardstorage.TryGetValue(liz.abstractCreature, out _))
        {
            return;
        }
        PostViolenceCheck(liz, data, "Melted", self);
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
                if (crit is Lizard liz2 && lizardstorage.TryGetValue(liz2.abstractCreature, out LizardData data2))
                {
                    data2.lastDamageType = "Melted";
                    PreViolenceCheck(liz2, data2);
                    crit.Die();
                    PostViolenceCheck(liz2, data2, "Melted", self.owner as Lizard);
                }
                else
                {
                    crit.Die();
                }
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
        if (!Chance(self.abstractCreature, ShadowOfOptions.melted_transformation_chance.Value, "Melted Transformation due to swimming in Lethal Water"))
        {
            return;
        }

        try
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + " was made Melted due to swimming in Lethal Water");

            bool isStorySession = self.abstractCreature.world.game.IsStorySession;
            int cycleNumber = isStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : -1;

            data.transformation = "Melted";
            data.transformationTimer = cycleNumber;

            data.liz.Remove("PreMeltedCycle");

            data.liz["MeltedR"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.r.ToString() : "0.4078431";
            data.liz["MeltedG"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.g.ToString() : "0.5843138";
            data.liz["MeltedB"] = data.rCam != null ? data.rCam.currentPalette.waterColor1.b.ToString() : "0.1843137";
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    #region Behavior
    static LizardAI.Behavior NoBehavior(On.LizardAI.orig_DetermineBehavior orig, LizardAI self)
    {
        if (ShadowOfOptions.melted_transformation.Value && lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) && data.transformation == "Melted")
        {
            return LizardAI.Behavior.Injured;
        }
        return orig.Invoke(self);
    }

    static void NoBite(On.LizardAI.orig_AggressiveBehavior orig, LizardAI self, Tracker.CreatureRepresentation target, float tongueChance)
    {
        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || data.transformation != "Melted")
        {
            orig.Invoke(self, target, tongueChance);
        }    
    }
    #endregion
}