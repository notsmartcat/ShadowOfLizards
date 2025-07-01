using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;
using System;
using MoreSlugcats;

namespace ShadowOfLizards;

internal class LizardTongueHooks
{
    public static void Apply()
    {
        On.LizardTongue.ctor += NewLizardTongue;
        On.LizardTongue.Update += LizardTongueUpdate;    
    }

    static void NewLizardTongue(On.LizardTongue.orig_ctor orig, LizardTongue self, Lizard lizard)
    {
        if (!ShadowOfOptions.tongue_stuff.Value || !lizardstorage.TryGetValue(lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("Tongue", out string Tongue))
        {
            orig.Invoke(self, lizard);
            return;
        }

        try
        {
            self.lizard = lizard;
            self.state = LizardTongue.State.Hidden;
            self.attachTerrainChance = 0.5f;
            self.pullAtChunkRatio = 1f;
            self.detatchMinDistanceTerrain = 20f;
            self.detatchMinDistanceCreature = 20f;
            self.totRExtraLimit = 40f;

            switch (Tongue)
            {
                case "WhiteLizard":
                    self.range = 540f;
                    self.elasticRange = 0.1f;
                    self.lashOutSpeed = 30f;
                    self.reelInSpeed = 0.0033333334f;
                    self.chunkDrag = 0f;
                    self.terrainDrag = 0f;
                    self.dragElasticity = 0.05f;
                    self.emptyElasticity = 0.01f;
                    self.involuntaryReleaseChance = 0.000125f;
                    self.voluntaryReleaseChance = 0.05f;
                    break;
                case "Salamander":
                    self.range = 140f;
                    self.elasticRange = 0.55f;
                    self.lashOutSpeed = 16f;
                    self.reelInSpeed = 0.000625f;
                    self.chunkDrag = 0.01f;
                    self.terrainDrag = 0.01f;
                    self.dragElasticity = 0.1f;
                    self.emptyElasticity = 0.8f;
                    self.involuntaryReleaseChance = 0.0025f;
                    self.voluntaryReleaseChance = 0.0125f;
                    break;
                case "BlueLizard":
                    self.range = 190f;
                    self.elasticRange = 0f;
                    self.lashOutSpeed = 26f;
                    self.reelInSpeed = 0f;
                    self.chunkDrag = 0.04f;
                    self.terrainDrag = 0.04f;
                    self.dragElasticity = 0f;
                    self.emptyElasticity = 0.07f;
                    self.involuntaryReleaseChance = 0.0033333334f;
                    self.voluntaryReleaseChance = 1f;
                    break;
                case "CyanLizard":
                    self.range = 140f;
                    self.elasticRange = 0.55f;
                    self.lashOutSpeed = 16f;
                    self.reelInSpeed = 0.002f;
                    self.chunkDrag = 0.01f;
                    self.terrainDrag = 0.01f;
                    self.dragElasticity = 0.1f;
                    self.emptyElasticity = 0.8f;
                    self.involuntaryReleaseChance = 0.005f;
                    self.voluntaryReleaseChance = 0.02f;
                    break;
                case "RedLizard":
                    self.range = 340f;
                    self.elasticRange = 0.1f;
                    self.lashOutSpeed = 37f;
                    self.reelInSpeed = 0.0043333336f;
                    self.chunkDrag = 0f;
                    self.terrainDrag = 0f;
                    self.dragElasticity = 0.05f;
                    self.emptyElasticity = 0.01f;
                    self.involuntaryReleaseChance = 1f;
                    self.voluntaryReleaseChance = 1f;
                    break;
                case "ZoopLizard":
                    self.range = 280f;
                    self.elasticRange = 0.1f;
                    self.lashOutSpeed = 30f;
                    self.reelInSpeed = 0.002f;
                    self.chunkDrag = 0.1f;
                    self.terrainDrag = 0.05f;
                    self.dragElasticity = 0.02f;
                    self.emptyElasticity = 0.003f;
                    self.involuntaryReleaseChance = 0.0025f;
                    self.voluntaryReleaseChance = 0.005f;
                    self.attachesBackgroundWalls = true;
                    self.attachTerrainChance = 1f;
                    self.pullAtChunkRatio = 0.05f;
                    self.detatchMinDistanceTerrain = 60f;
                    self.totRExtraLimit = 80f;
                    break;
                case "Tube":
                    self.range = 1200f;
                    self.elasticRange = 0f;
                    self.lashOutSpeed = 30f;
                    self.reelInSpeed = 0.0033333334f;
                    self.chunkDrag = 0f;
                    self.terrainDrag = 0f;
                    self.dragElasticity = 0.05f;
                    self.emptyElasticity = 0.01f;
                    self.involuntaryReleaseChance = 0.000125f;
                    self.voluntaryReleaseChance = 0.00025f;
                    break;
                case "IndigoLizard":
                    self.range = 200f;
                    self.elasticRange = 0.55f;
                    self.lashOutSpeed = 20f;
                    self.reelInSpeed = 0.002f;
                    self.chunkDrag = 0.01f;
                    self.terrainDrag = 0.01f;
                    self.dragElasticity = 0.1f;
                    self.emptyElasticity = 0.8f;
                    self.involuntaryReleaseChance = 0.005f;
                    self.voluntaryReleaseChance = 0.02f;
                    break;
                default:
                    Debug.Log(all + "Failed Getting the " + Tongue + " Tongue for " + lizard);
                    ShadowOfLizards.Logger.LogError(all + "Failed Getting the " + Tongue + " Tongue for " + lizard);
                    self.range = 190f;
                    self.elasticRange = 0f;
                    self.lashOutSpeed = 26f;
                    self.reelInSpeed = 0f;
                    self.chunkDrag = 0.04f;
                    self.terrainDrag = 0.04f;
                    self.dragElasticity = 0f;
                    self.emptyElasticity = 0.07f;
                    self.involuntaryReleaseChance = 0.0033333334f;
                    self.voluntaryReleaseChance = 1f;
                    break;
            }

            self.totR = self.range * 1.1f;
            self.graphPos = new Vector2[2];
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardTongueUpdate(On.LizardTongue.orig_Update orig, LizardTongue self)
    {
        if (!ShadowOfOptions.tongue_stuff.Value || self.lizard.lizardParams.tongue || self.state != LizardTongue.State.Hidden || self.Out)
        {
            orig.Invoke(self);
            return;
        }

        self.delay = 100;
    }
}

