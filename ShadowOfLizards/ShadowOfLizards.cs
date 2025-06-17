using BepInEx;
using Fisobs.Core;
using LizardCosmetics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Watcher;
using static RoomCamera;

namespace ShadowOfLizards;

[BepInPlugin("notsmartcat.shadowoflizards", "Shadow of Lizards", "1.0.0")]
internal class ShadowOfLizards : BaseUnityPlugin
{
    #region Classes
    public class LizardData
    {
        public bool Beheaded = false;

        //Dictionaty stores most of the important values. they are first set inside the "NewLizard" Hook
        public Dictionary<string, string> liz;

        //Stored inside the LizardHraphics Hook and used outside if LizardGraphics
        public SpriteLeaser sLeaser;
        public RoomCamera rCam;

        //Used to check the last damage type that was dealt to the lizard for determining death couse in case a lizard bleeds out
        public string lastDamageType = "null";

        //Values used for the Spider Transformation Spider Legs
        public List<List<Creature>> legSpiders = new() { new(), new(), new(), new(), new(), new(), };
        public List<int> legSpidersFrame = new() { 1, 3, 6, 8, 3, 5 };
        public int spiderlegtimer = 0;
        public float directionmultiplier = 1f;

        //Used to store the default LizardTemplate values for accurate changes to the values when Eyes are damaged
        public float visualRadius;
        public float waterVision;
        public float throughSurfaceVision;

        //Used to make sure regrowth code only triggers once when a lizard brings something back to it's den
        public bool denCheck = false;

        //Used to determine which BodyChunks belong to the Lizard In case the Lizard is Cut in Half
        public List<int> availableBodychunks = new() { 0, 1, 2 };

        //Transformation Related Values

        //This Ranges from the first stage oka "Melted" to the last stage aka "MeltedTransformation"
        public string transformation = "Null";

        /// <summary>
        //Transformation timers
        //The Spider Transformation one sets the value to the cycle number when the Transformation was gained and the Transformation progresses when 3 cycles have passed, If the Lizard get's rid of all Spiders that are inside it without dying or it dies and lives the Transformation is lost
        //The Electric Transformation sets this value to 1, each time the Lizard gets hit by electric damage or by eating electric creatures the value will go up by 1, if the value is grater then 3 then the Transformation progresses. if it goes to less then 1 the Transformation is lost
        //The Melted Transformation sets the value to the cycle number when the Transformation was gained and the Transformation progresses when 3 cycles have passed. the Transformation cannot be lost
        /// </summary>
        public int transformationTimer = -1;

        public int lizardUpdatedCycle = -1;

        public List<string> ArmState = new();

        public int cheatDeathChance = 0;
    }
    public class LizardGoreData
    {
        public LizardData origLizardData;

        public List<int> availableBodychunks = new();
    }
    public class EliteLizardData
    {
        public string name;
        public string region;

        public int level;

        public string breedBonus;
        public string transformationBonus;
        public string miscBonus;

        public string trait1;

        public string trait2;

        public string trait3;

        public string gang;
    }
    public class GraphicsData
    {
        public int EyesSprites;

        public List<int> SpiderLeg = new() { 0, 0, 0, 0, 0, 0 };

        public float legLength = 1;

        public int ElectricColorTimer = 0;

        public bool once = false;

        public float lightFlash;

        public LightSource lightSource;
    }
    public class SpiderAsLeg
    {
        public Creature liz;
    }
    public class ElectricSpit
    {
        public bool Shocked = false;

        public Color origColor;

        public int ElectricColorTimer = 0;

        public bool once = false;

        public float lightFlash;

        public LightSource lightSource;
    }
    public class OneTimeUseData
    {
        public List<Lizard> lizStorage = new();
    }
    #endregion

    #region ConditionalWeakTable
    public static readonly ConditionalWeakTable<AbstractCreature, LizardData> lizardstorage = new();
    public static readonly ConditionalWeakTable<AbstractCreature, LizardGoreData> lizardGoreStorage = new();
    public static readonly ConditionalWeakTable<LizardGraphics, GraphicsData> graphicstorage = new();
    public static readonly ConditionalWeakTable<Spider, SpiderAsLeg> SpidLeg = new();
    public static readonly ConditionalWeakTable<LizardSpit, ElectricSpit> ShockSpit = new();
    public static readonly ConditionalWeakTable<PhysicalObject, OneTimeUseData> singleuse = new();
    #endregion

    #region Misc Values
    public static bool storedCreatureWasDead = false;

    public static Dictionary<string, Color> bloodcolours;

    public static string all = "ShadowOf: ";

    public bool init = false;

    public static bool bloodModCheck = false;

    public static List<AbstractCreature> goreLizardList;
    #endregion

    public void OnEnable()
    {
        try
        {
            Content.Register(new IContent[1] { new LizCutLegFisobs() });
            Content.Register(new IContent[1] { new LizCutHeadFisobs() });
            Content.Register(new IContent[1] { new LizCutEyeFisobs() });

            ViolenceTypeCheck.Apply();
            SpiderTransformation.Apply();
            ElectricTransformation.Apply();
            MeltedTransformation.Apply();

            On.SlugcatStats.NourishmentOfObjectEaten += LizardLegEaten;
            On.Player.Update += DebugKeys;
            On.Lizard.Violence += LimbDeath;
            On.Lizard.ctor += NewLizard;
            On.Creature.Die += LizardDie;
            On.Lizard.Update += LizardUpdate;
            On.LizardGraphics.DrawSprites += LimbSprites;
            On.RainWorld.OnModsInit += ModInit;
            On.Lizard.HitHeadShield += HitHeadShield;
            On.LizardTongue.Update += TongueCheck;
            On.LizardTongue.ctor += LizTongue;
            On.LizardJumpModule.Update += GasLeak;
            On.Centipede.Shock += ShockDeath;
            On.RainWorldGame.ctor += BloodModCheck;
            On.LizardGraphics.InitiateSprites += LizardEyesInitiateSprites;
            On.LizardGraphics.AddToContainer += LizardEyesAddToContainer;
            On.LizardAI.ctor += AISpitAbility;
            On.LizardAI.Update += AIUpdate;
            On.Lizard.Bite += LizardBite;
            On.LizardGraphics.ctor += NewLizardGraphics;
            On.LizardCosmetics.Antennae.ctor += Antennae_ctor;

            On.Spear.HitSomething += Spear_HitSomething;

            IL.Lizard.SpearStick += ILSpearStick;
            IL.LizardAI.Update += ILLizardAI;
            IL.WormGrass.WormGrassPatch.InteractWithCreature += ILInteractWithCreature1;
            IL.Lizard.cctor += ILLizard;

            //On.LizardGraphics.ctor += LizardGraphics_ctor;
            //On.LizardGraphics.AddCosmetic += LizardGraphics_AddCosmetic;

            //On.LizardCosmetics.SpineSpikes.DrawSprites += SpineSpikes_DrawSprites;

            On.FlareBomb.Update += FlareBomb_Update;
            On.Explosion.Update += Explosion_Update;

            On.AbstractCreature.ctor += NewAbstractLizard;
            On.AbstractCreature.Abstractize += SaveAbstractLizard;

            //On.RoomCamera.SpriteLeaser.RemoveAllSpritesFromContainer += SpriteLeaser_RemoveAllSpritesFromContainer;

            //On.CreatureTemplate.ctor_CreatureTemplate += CreatureTemplate_ctor_CreatureTemplate;
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    #region IL
    void ILLizard(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(MoveType.After, new Func<Instruction, bool>[4]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<Lizard>(x, "lizardParams"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardBreedParams>(x, "tongue"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.Emit<ShadowOfLizards>(OpCodes.Call, "ShadowOfLizard");
                Logger.LogInfo(all + "lizard success");
            }
            else
            {
                Logger.LogInfo(all + "lizard fail");
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }
    public static bool ShadowOfLizard(AbstractCreature liz)
    {
        if (ShadowOfOptions.tongue_stuff.Value && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz.TryGetValue("Tongue", out string tongue) && tongue != "True")
        {
            return false;
        }

        return true;
    }

    void ILSpearStick(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(0, new Func<Instruction, bool>[5]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<Lizard>(x, "jumpModule"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardJumpModule>(x, "gasLeakPower"),
            (Instruction x) => ILPatternMatchingExt.MatchLdcR4(x, 0f),
            (Instruction x) => ILPatternMatchingExt.MatchBleUn(x, out target)
            }))
            {
                val.Emit(OpCodes.Ldarg_0);
                val.Emit<Lizard>(OpCodes.Ldfld, "jumpModule");
                val.Emit(OpCodes.Brfalse, target);
                Logger.LogInfo(all + "spear stick success");
            }
            else
            {
                Logger.LogInfo(all + "spear stick fail");
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void ILLizardAI(ILContext il)
    {
        ILCursor val = new(il);
        ILLabel target = null;
        if (val.TryGotoNext(new Func<Instruction, bool>[4]
        {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 0),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI>(x, "redSpitAI"),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<LizardAI.LizardSpitTracker>(x, "spitting"),
            (Instruction x) => ILPatternMatchingExt.MatchBrfalse(x, out target)
        }))
        {
            int index = val.Index;
            val.Index = index + 1;
            val.Emit<LizardAI>(OpCodes.Ldfld, "redSpitAI");
            val.Emit(OpCodes.Brfalse_S, target);
            val.Emit(OpCodes.Ldarg_0);
            Logger.LogInfo(all + "Lizard Spit success");
        }
        else
        {
            Logger.LogInfo(all + "Lizard Spit fail");
        }
    }

    void ILInteractWithCreature1(ILContext il)
    {
        try
        {
            ILCursor val = new(il);
            ILLabel target = null;
            if (val.TryGotoNext(0, new Func<Instruction, bool>[4]
            {
            (Instruction x) => ILPatternMatchingExt.MatchLdarg(x, 1),
            (Instruction x) => ILPatternMatchingExt.MatchLdfld<WormGrass.WormGrassPatch.CreatureAndPull>(x, "creature"),
            (Instruction x) => ILPatternMatchingExt.MatchCallvirt<UpdatableAndDeletable>(x, "Destroy"),
            (Instruction x) => ILPatternMatchingExt.MatchRet(x)
            }))
            {
                val.Emit(OpCodes.Ldarg_1);
                val.Emit<ShadowOfLizards>(OpCodes.Call, "ShadowOfInteractWithCreature");
                Logger.LogInfo(all + "worm grass success");
            }
            else
            {
                Logger.LogInfo(all + "worm grass fail");
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }
    public static void ShadowOfInteractWithCreature(WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull)
    {
        if (!ShadowOfOptions.grass_immune.Value || creatureAndPull.creature is not Lizard || !lizardstorage.TryGetValue(creatureAndPull.creature.abstractCreature, out LizardData data) || !data.liz.TryGetValue("Grass", out string grass) || grass == "True"
            || !creatureAndPull.creature.room.game.IsStorySession || data.liz["GrassCheck"] == creatureAndPull.creature.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString())
        {
            return;
        }

        if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.grass_immune_chance.Value)
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "WormGrass Immune granted to " + creatureAndPull.creature);

            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                data.cheatDeathChance += 5;

            data.liz["Grass"] = "True";
            data.lastDamageType = null;
        }
        else if (ShadowOfOptions.debug_logs.Value)
        {
            Debug.Log(all + "WormGrass Immune not granted to " + creatureAndPull.creature);
        }
        data.liz["GrassCheck"] = creatureAndPull.creature.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString();
    }

    #endregion


    void CreatureTemplate_ctor_CreatureTemplate(On.CreatureTemplate.orig_ctor_CreatureTemplate orig, CreatureTemplate self, CreatureTemplate copy)
    {
        if (copy == null || copy.TopAncestor().type != CreatureTemplate.Type.LizardTemplate)
        {
            Debug.Log("Creature Template Fail " + self.name + " " + copy.name);
            orig(self, copy);
            return;
        }

        Debug.Log("Creature Template Success " + self.name + " " + copy.name);
    }

    void SpriteLeaser_RemoveAllSpritesFromContainer(On.RoomCamera.SpriteLeaser.orig_RemoveAllSpritesFromContainer orig, SpriteLeaser self)
    {
        if (self.sprites != null)
        {
            for (int i = 0; i < self.sprites.Length; i++)
            {
                Debug.Log("sprites " + i + " " + self.sprites[i]);

                if (self.sprites[i] != null)
                {
                    Debug.Log("sprites " + i + " " + self.sprites[i].element.name);
                }
            }
        }
        if (self.containers != null)
        {
            for (int j = 0; j < self.containers.Length; j++)
            {
                Debug.Log("containers " + j + " " + self.containers[j]);

                if (self.sprites[j] != null)
                {
                    Debug.Log("containers " + j + " " + self.sprites[j].element.name);
                }
            }
        }

        orig(self);
    }


    void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.deafen.Value)
        {
            return;
        }

        if (!singleuse.TryGetValue(self.sourceObject, out OneTimeUseData data2))
        {
            singleuse.Add(self.sourceObject, new OneTimeUseData());
            singleuse.TryGetValue(self.sourceObject, out OneTimeUseData dat);
            data2 = dat;
        }

        float num = self.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)self.lifeTime, (float)self.frame) * 3.1415927f));
        for (int j = 0; j < self.room.physicalObjects.Length; j++)
        {
            for (int k = 0; k < self.room.physicalObjects[j].Count; k++)
            {
                float num3 = float.MaxValue;
                for (int l = 0; l < self.room.physicalObjects[j][k].bodyChunks.Length; l++)
                {
                    float num5 = Vector2.Distance(self.pos, self.room.physicalObjects[j][k].bodyChunks[l].pos);
                    num3 = Mathf.Min(num3, num5);
                }
                if (self.sourceObject != self.room.physicalObjects[j][k] && (self.sourceObject == null || self.sourceObject.abstractPhysicalObject.rippleLayer == self.room.physicalObjects[j][k].abstractPhysicalObject.rippleLayer || self.sourceObject.abstractPhysicalObject.rippleBothSides || self.room.physicalObjects[j][k].abstractPhysicalObject.rippleBothSides) && !self.room.physicalObjects[j][k].slatedForDeletetion)
                {
                    //Debug.Log("pre deafened " + Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f));

                    if (self.deafen > 0f && self.room.physicalObjects[j][k] is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EarRight", out string ear) && !data2.lizStorage.Contains(liz)
                        && (int)Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f) > 90)
                    {
                        data2.lizStorage.Add(liz);

                        //Debug.Log("deafened " + Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f));

                        if (ear == "Normal" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.deafen_chance.Value)
                        {
                            data.liz["EarRight"] = "Deaf";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " Right Ear was Deafened");
                        }
                        if (data.liz["EarLeft"] == "Normal" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.deafen_chance.Value)
                        {
                            data.liz["EarLeft"] = "Deaf";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " Left Ear was Deafened");
                        }
                    }
                }
            }
        }
    }

    void FlareBomb_Update(On.FlareBomb.orig_Update orig, FlareBomb self, bool eu)
    {
        orig(self, eu);

        if (!ShadowOfOptions.blind.Value || self.burning <= 0f)
        {
            return;
        }

        if (!singleuse.TryGetValue(self, out OneTimeUseData data2))
        {
            singleuse.Add(self, new OneTimeUseData());
            singleuse.TryGetValue(self, out OneTimeUseData dat);
            data2 = dat;
        }

        for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            if (self.room.abstractRoom.creatures[i].realizedCreature != null && (self.room.abstractRoom.creatures[i].rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.abstractRoom.creatures[i].rippleBothSides || self.abstractPhysicalObject.rippleBothSides)
                && (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 600f) || (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 1600f)
                && self.room.VisualContact(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos))))
            {
                if (self.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && !data2.lizStorage.Contains(liz)
                    && (int)Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f) > 300)
                {
                    data2.lizStorage.Add(liz);

                    bool flag = eye == "Blind" || eye == "BlindScar" || eye == "Cut";
                    bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";
                    if (!flag && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_chance.Value)
                    {
                        if (eye == "Normal")
                        {
                            data.liz["EyeRight"] = "Blind";
                            liz.Template.visualRadius -= data.visualRadius * 0.5f;
                            liz.Template.waterVision -= data.visualRadius * 0.5f;
                            liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                        }
                        else if (eye == "Scar")
                        {
                            data.liz["EyeRight"] = "BlindScar";
                            liz.Template.visualRadius -= data.visualRadius * 0.5f;
                            liz.Template.waterVision -= data.visualRadius * 0.5f;
                            liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                        }
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " Right Eye was Blinded");
                    }
                    if (!flag2 && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_chance.Value)
                    {
                        if (data.liz["EyeLeft"] == "Normal")
                        {
                            data.liz["EyeLeft"] = "Blind";
                            liz.Template.visualRadius -= data.visualRadius * 0.5f;
                            liz.Template.waterVision -= data.visualRadius * 0.5f;
                            liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                        }
                        else if (data.liz["EyeLeft"] == "Scar")
                        {
                            data.liz["EyeLeft"] = "BlindScar";
                            liz.Template.visualRadius -= data.visualRadius * 0.5f;
                            liz.Template.waterVision -= data.visualRadius * 0.5f;
                            liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                        }
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " Left Eye was Blinded");
                    }
                }
            }
        }
    }

    int LizardGraphics_AddCosmetic(On.LizardGraphics.orig_AddCosmetic orig, LizardGraphics self, int spriteIndex, Template cosmetic)
    {
        if (cosmetic is SpineSpikes)
        {
            return orig(self, spriteIndex, cosmetic);
        }
        else
        {
            return spriteIndex;
        }
    }

    void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        int num7 = self.startOfExtraSprites;
        num7 = self.AddCosmetic(num7, new BodyStripes(self, num7));
    }

    bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if (result.chunk != null && result.chunk.owner != null && result.chunk.owner is Lizard liz && ((lizardGoreStorage.TryGetValue(liz.abstractCreature, out LizardGoreData goreData) && !goreData.availableBodychunks.Contains(result.chunk.index))
            || (lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !data.availableBodychunks.Contains(result.chunk.index))))
        {
            return false;
        }
        else
        {
            return orig(self, result, eu);
        }
    }

    void Antennae_ctor(On.LizardCosmetics.Antennae.orig_ctor orig, Antennae self, LizardGraphics lGraphics, int startSprite)
    {
        orig(self, lGraphics, startSprite);

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data) || (data.transformation != "Melted" && data.transformation != "MeltedTransformation"))
        {
            return;
        }

        self.redderTint = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
    }

    int LizardLegEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
    {
        if (eatenobject is LizCutLeg)
        {
            if (ModManager.MSC && slugcatIndex == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                return -1;
            }
            int num = 0;
            return (!(slugcatIndex == SlugcatStats.Name.Red) && (!ModManager.MSC || !(slugcatIndex == MoreSlugcatsEnums.SlugcatStatsName.Artificer))) ? (num + eatenobject.FoodPoints) : (num + 2 * eatenobject.FoodPoints);
        }
        return orig.Invoke(slugcatIndex, eatenobject);
    }

    #region Blood Mod

    void BloodModCheck(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig.Invoke(self, manager);
        try
        {
            if (ShadowOfOptions.blood.Value && ModManager.ActiveMods.Any(mod => mod.id == "blood"))
            {
                bloodModCheck = true;
                Blood();
            }
            else
            {
                bloodcolours = null;
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void Blood()
    {
        try
        {
            bloodcolours = BloodData.Load();
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log((all + "BloodData loaded"));
        }
        catch (Exception e) { Logger.LogError(e); }

    }

    #endregion

    void ModInit(On.RainWorld.orig_OnModsInit orig, RainWorld rainWorld)
    {
        orig.Invoke(rainWorld);
        try
        {
            if (!init)
            {
                init = true;
                Futile.atlasManager.LoadAtlas("atlases/ShadowOfAtlas");
            }
            MachineConnector.SetRegisteredOI("notsmartcat.shadowoflizards", ShadowOfOptions.instance);
        }
        catch (Exception e) { Logger.LogError(e); }

    }

    void DebugKeys(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig.Invoke(self, eu);
        try
        {
            if (ShadowOfOptions.debug_keys.Value)
            {
                if (self != null && self.room != null && self.room.game != null && self.room.game.devToolsActive && Input.GetKey("n"))
                {
                    List<AbstractCreature> list = new(self.abstractCreature.Room.creatures);
                    foreach (AbstractCreature creature in list)
                    {
                        if (creature.realizedCreature == null)
                        {
                            continue;
                        }

                        if (creature.realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.Beheaded == false)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + liz.ToString() + "'s Neck Hit by Debug");

                            data.Beheaded = true;
                            Decapitation(liz);
                            liz.Die();
                        }
                    }
                }

                if (self != null && self.room != null && self.room.game != null && self.room.game.devToolsActive && Input.GetKey("m"))
                {
                    List<AbstractCreature> list = new(self.abstractCreature.Room.creatures);
                    foreach (AbstractCreature creature in list)
                    {
                        if (creature.realizedCreature != null && creature.realizedCreature is Lizard liz)
                        {
                            if (lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.liz.TryGetValue("Tongue", out _) && liz.lizardParams.tongue)
                            {
                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + liz.ToString() + "'s Mouth Hit by Debug");

                                data.liz["Tongue"] = "False";
                                data.liz["NewTongue"] = "get";
                                liz.lizardParams.tongue = false;
                                liz.tongue.Retract();
                                //(liz.abstractCreature.creatureTemplate.breedParameters as LizardBreedParams).tongue = false;

                            }

                            if (data.liz.TryGetValue("CanSpit", out _) && liz.AI.redSpitAI != null)
                            {
                                liz.animation = Lizard.Animation.Standard;
                                liz.AI.behavior = LizardAI.Behavior.Frustrated;
                                liz.AI.modules.Remove(liz.AI.redSpitAI);
                                liz.AI.redSpitAI = null;
                                data.liz["CanSpit"] = "False";
                            }

                            if (data.liz.TryGetValue("EyeLeft", out _) && data.liz["EyeLeft"] == "Normal")
                            {
                                data.liz["EyeLeft"] = "Cut";
                                self.Blind(5);

                                EyeCut(liz, "EyeLeft");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void EliteLizard(Lizard self)
    {

    }

    void NewLizard(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);

        try
        {
            if (lizardGoreStorage.TryGetValue(abstractCreature, out LizardGoreData _))
            {
                self.Die();

                return;
            }

            if (self.abstractCreature == null || self.abstractCreature.state == null || self.abstractCreature.state.unrecognizedSaveStrings == null || abstractCreature.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate
                || !lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
            {
                return;
            }

            self.abstractCreature.creatureTemplate = new CreatureTemplate(self.abstractCreature.creatureTemplate);
            self.lizardParams = LizBread(self);

            data.visualRadius = self.Template.visualRadius;
            data.waterVision = self.Template.waterVision;
            data.throughSurfaceVision = self.Template.throughSurfaceVision;

            //Tongue
            if (ShadowOfOptions.tongue_stuff.Value && data.liz.TryGetValue("Tongue", out _))
            {
                self.lizardParams.tongue = data.liz["Tongue"] == "True";

                if (self.lizardParams.tongue)
                {
                    List<string> TongueList = new() { "WhiteLizard", "Salamander", "BlueLizard", "CyanLizard", "RedLizard", "ZoopLizard" };

                    if (data.liz["Tongue"] == "True" && data.liz["NewTongue"] != "Unknown" && data.liz["NewTongue"] != self.abstractCreature.creatureTemplate.type.ToString())
                    {
                        self.tongue = null;
                        if (data.liz["NewTongue"] == "get")
                        {
                            int num = UnityEngine.Random.Range(0, 6);
                            string tongue = TongueList[num].Contains(self.abstractCreature.creatureTemplate.type.ToString()) ? self.abstractCreature.creatureTemplate.type.ToString() : TongueList[num];

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " got new " + tongue + " Tongue");

                            data.liz["NewTongue"] = tongue;
                        }

                        BreedTongue(self);
                        self.tongue = new LizardTongue(self);

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " got it's Tongue replaced with a " + data.liz["NewTongue"] + " Tongue");
                    }
                }
                else
                {
                    self.tongue = null;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " does not have a Tongue");
                }
            }

            //Jump
            if (ShadowOfOptions.jump_stuff.Value)
            {
                if (!data.liz.TryGetValue("CanJump", out _))
                {
                    if (self.jumpModule == null)
                    {
                        data.liz.Add("CanJump", "False");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " cannot Jump");
                    }
                    else
                    {
                        data.liz.Add("CanJump", "True");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " can Jump");
                    }
                }
                else if ((self.jumpModule != null).ToString() != data.liz["CanJump"])
                {
                    if (data.liz["CanJump"] == "True")
                    {
                        self.jumpModule = new LizardJumpModule(self);

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has the ability to Jump");
                    }
                    else
                    {
                        self.jumpModule = null;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " does not have the ability to Jump");
                    }
                }
            }

            //WormGrass
            if (ShadowOfOptions.grass_immune.Value)
            {
                if (!data.liz.TryGetValue("Grass", out _))
                {
                    if (!self.Template.wormGrassImmune)
                    {
                        data.liz.Add("Grass", "False");
                        data.liz.Add("GrassCheck", "-1");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is not Immune to Worm Grass");
                    }
                    else
                    {
                        data.liz.Add("Grass", "True");
                        //data.liz.Add("GrassCheck", "-1");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is Immune to Worm Grass");
                    }
                }
                else if (self.Template.wormGrassImmune.ToString() != data.liz["Grass"])
                {
                    if (data.liz["Grass"] == "True")
                    {
                        self.Template.wormGrassImmune = true;

                        if (!data.liz.TryGetValue("GrassCheck", out _))
                        {
                            data.liz.Remove("GrassCheck");
                        }

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has immunity to Worm Grass");
                    }
                    else
                    {
                        self.Template.wormGrassImmune = false;

                        data.liz.Add("GrassCheck", "-1");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " does not have immunity to Worm Grass");
                    }
                }
            }

            //Eye Blind
            if (ShadowOfOptions.blind.Value && self.Template.visualRadius > 0f)
            {
                if (!data.liz.TryGetValue("EyeRight", out _))
                {
                    data.liz.Add("EyeLeft", "Normal");
                    data.liz.Add("EyeRight", "Normal");

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " is not Blind");
                }
                else if (data.liz["EyeRight"] == "Incompatible")
                {
                    Debug.Log(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    Logger.LogError(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
                else
                {
                    bool flag = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "Cut";
                    bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";

                    if (flag && flag2)
                    {
                        self.Template.visualRadius = 0f;
                        self.Template.waterVision = 0f;
                        self.Template.throughSurfaceVision = 0f;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is Blind");
                    }
                    else
                    {
                        if (ShadowOfOptions.debug_logs.Value && (flag ^ flag2))
                            Debug.Log(all + self.ToString() + " is Blind");

                        float visualRadius = data.visualRadius;
                        float waterVision = data.waterVision;
                        float throughSurfaceVision = data.throughSurfaceVision;
                        if (data.liz["EyeRight"] != "Normal")
                        {
                            if (data.liz["EyeRight"] == "Scar")
                            {
                                self.Template.visualRadius -= visualRadius * 0.25f;
                                self.Template.waterVision -= waterVision * 0.25f;
                                self.Template.throughSurfaceVision -= throughSurfaceVision * 0.25f;
                            }
                            else if (flag)
                            {
                                self.Template.visualRadius -= visualRadius * 0.5f;
                                self.Template.waterVision -= waterVision * 0.5f;
                                self.Template.throughSurfaceVision -= throughSurfaceVision * 0.5f;
                            }
                        }

                        if (!(data.liz["EyeLeft"] == "Normal"))
                        {
                            if (data.liz["EyeLeft"] == "Scar")
                            {
                                self.Template.visualRadius -= visualRadius * 0.25f;
                                self.Template.waterVision -= waterVision * 0.25f;
                                self.Template.throughSurfaceVision -= throughSurfaceVision * 0.25f;
                            }
                            else if (flag2)
                            {
                                self.Template.visualRadius -= visualRadius * 0.5f;
                                self.Template.waterVision -= waterVision * 0.5f;
                                self.Template.throughSurfaceVision -= throughSurfaceVision * 0.5f;
                            }
                        }
                    }
                }
            }

            //Ear Deaf
            if (ShadowOfOptions.deafen.Value)
            {
                if (!data.liz.TryGetValue("EarRight", out _))
                {
                    if (false)
                    {
                        data.liz.Add("EarLeft", "DefaultDeaf");
                        data.liz.Add("EarRight", "DefaultDeaf");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is Deaf");
                    }
                    else
                    {
                        data.liz.Add("EarLeft", "Normal");
                        data.liz.Add("EarRight", "Normal");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is not Deaf");
                    }
                }
                else
                {
                    bool flag = data.liz["EarRight"] == "Deaf";
                    bool flag2 = data.liz["EarLeft"] == "Deaf";

                    if (flag && flag2 && self.deaf < 120)
                    {
                        self.deaf = 120;
                    }
                    else if ((flag ^ flag2) && self.deaf < 4)
                    {
                        self.deaf = 4;
                    }
                }
            }

            //Teeth
            if (ShadowOfOptions.teeth.Value)
            {
                if (!data.liz.TryGetValue("UpperTeeth", out string _))
                {
                    data.liz.Add("UpperTeeth", "Normal");
                    data.liz.Add("LowerTeeth", "Normal");
                }
                else if (data.liz["UpperTeeth"] == "Incompatible")
                {
                    Debug.Log(all + "Teeth sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    Logger.LogError(all + "Teeth sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
                else
                {
                    bool flag = data.liz["UpperTeeth"] != "Normal" && data.liz["LowerTeeth"] != "Normal";

                    if (flag)
                    {
                        self.lizardParams.biteDamageChance *= 0.5f;
                        self.lizardParams.biteDominance *= 0.5f;
                        self.lizardParams.biteDamage *= 1.1f;
                        self.lizardParams.getFreeBiteChance *= 1.1f;
                    }
                    else if (data.liz["UpperTeeth"] != "Normal" || data.liz["LowerTeeth"] != "Normal")
                    {
                        self.lizardParams.biteDamageChance *= 0.5f;
                        self.lizardParams.biteDominance *= 0.5f;
                        self.lizardParams.biteDamage *= 1.1f;
                        self.lizardParams.getFreeBiteChance *= 1.1f;
                    }
                }
            }

            #region Transformations
            //Melted Transformation
            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                self.Template.canSwim = true;

                if (!data.liz.TryGetValue("MeltedR", out string _))
                {
                    Color waterColour = (world != null && world.activeRooms[0] != null && world.activeRooms[0].waterObject != null && world.activeRooms[0].waterObject.WaterIsLethal && world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 != null) ? world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 : new Color(0.4078431f, 0.5843138f, 0.1843137f);
                    data.liz.Add("MeltedR", waterColour.r.ToString());
                    data.liz.Add("MeltedG", waterColour.g.ToString());
                    data.liz.Add("MeltedB", waterColour.b.ToString());
                }

                if (abstractCreature.creatureTemplate.type != CreatureTemplate.Type.WhiteLizard)
                {
                    self.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
                }

                self.abstractCreature.lavaImmune = true;

                if (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious)
                {
                    self.abstractCreature.creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
                }
            }
            #endregion

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating " + self);

            LizardCustomRelationsSet.Apply(self.Template.type, self);
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void NewAbstractLizard(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);

        try
        {
            if (self == null || self.state == null || self.state.unrecognizedSaveStrings == null || creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate)
            {
                return;
            }

            if (!lizardstorage.TryGetValue(self, out LizardData data))
            {
                lizardstorage.Add(self, new LizardData());
                lizardstorage.TryGetValue(self, out LizardData dat);
                data = dat;
            }

            bool firstTime = false;

            //self.creatureTemplate = new CreatureTemplate(creatureTemplate);

            data.liz = new();

            bool isStorySession = self.world.game.IsStorySession;
            int cycleNumber = isStorySession ? self.world.game.GetStorySession.saveState.cycleNumber : -1;

            Dictionary<string, string> savedData = self.state.unrecognizedSaveStrings;

            //First Time Creating Check
            if (!savedData.TryGetValue("ShadowOfBeheaded", out string beheaded))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "First time creating Abstract " + self);

                firstTime = true;

                data.Beheaded = false;

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                {
                    data.cheatDeathChance = UnityEngine.Random.Range(0, 101) + ShadowOfOptions.cheat_death_chance.Value;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " got " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being on.");
                }
                else
                {
                    data.cheatDeathChance = ShadowOfOptions.cheat_death_chance.Value;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " got a flat " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being off.");
                }

                for (int i = 0; i < (ModManager.DLCShared && creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.SpitLizard ? 6 : 4); i++)
                {
                    data.ArmState.Add("Normal");
                }
            }
            else //Loads info from the Lizard
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Not the first time creating Abstract " + self);

                data.Beheaded = beheaded == "True";
                savedData.Remove("ShadowOfBeheaded");

                string lizKeyTemp = "";
                string lizTemp = "";
                for (int i = 0; i < savedData["ShadowOfLiz"].Length; i++)
                {
                    char letter = savedData["ShadowOfLiz"][i];

                    if (letter.ToString() == "=")
                    {
                        lizKeyTemp = lizTemp;
                        lizTemp = "";
                    }
                    else if (letter.ToString() == ";")
                    {
                        data.liz.Add(lizKeyTemp, lizTemp);
                        lizKeyTemp = "";
                        lizTemp = "";
                    }
                    else
                    {
                        lizTemp += letter;
                    }
                }
                savedData.Remove("ShadowOfLiz");

                string ArmStateTemp = "";
                for (int i = 0; i < savedData["ShadowOfArmState"].Length; i++)
                {
                    char letter = savedData["ShadowOfArmState"][i];

                    if (letter.ToString() == ";")
                    {
                        data.ArmState.Add(ArmStateTemp);
                        ArmStateTemp = "";
                    }
                    else
                    {
                        ArmStateTemp += letter;
                    }
                }
                savedData.Remove("ShadowOfArmState");

                data.transformation = savedData["ShadowOfTransformation"];
                data.transformationTimer = int.Parse(savedData["ShadowOfTransformationTimer"]);

                data.cheatDeathChance = int.Parse(savedData["ShadowOfCheatDeathChance"]);

                data.lizardUpdatedCycle = int.Parse(savedData["ShadowOfLizardUpdatedCycle"]);

                string chunkTemp = "";
                for (int i = 0; i < savedData["ShadowOfAvailableBodychunks"].Length; i++)
                {
                    char letter = savedData["ShadowOfAvailableBodychunks"][i];

                    if (letter.ToString() == ";")
                    {
                        data.ArmState.Add(chunkTemp);
                        chunkTemp = "";
                    }
                    else
                    {
                        chunkTemp += letter;
                    }
                }

                Debug.Log(all + "Loading values for Abstract " + self);

                Debug.Log(all + self + " beheaded = " + data.Beheaded);
                Debug.Log(all + self + " lizDictionary = " + data.liz);

                Debug.Log(all + self + " bodyChunks = " + data.availableBodychunks);

                Debug.Log(all + self + " transformation = " + data.transformation);
                Debug.Log(all + self + " transformationTimer = " + data.transformationTimer);

                Debug.Log(all + self + " armState = " + data.ArmState);

                Debug.Log(all + self + " updatedCycle = " + data.lizardUpdatedCycle);

                Debug.Log(all + self + " cheatDeathChance = " + data.cheatDeathChance);

            }

            //Add Head Back if next cycle
            if (!data.liz.TryGetValue("BeheadedCycle", out _))
            {
                data.liz.Add("BeheadedCycle", "-1");
            }
            else if (data.Beheaded == true && isStorySession && data.liz["BeheadedCycle"] != self.world.game.GetStorySession.saveState.cycleNumber.ToString())
            {
                data.Beheaded = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " gained back it's head");
            }

            #region Transformations

            //Spider Transformation
            if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Null" || data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
            {
                if (firstTime)
                {
                    if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_spider_transformation_chance.Value)
                    {
                        data.transformation = "SpiderTransformation";
                        data.transformationTimer = isStorySession ? cycleNumber : 1;

                        data.liz.Add("SpiderNumber", UnityEngine.Random.Range(30, 55).ToString());

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Spider Transformation due to Chance");

                        goto SkipTransformations;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                    {
                        Debug.Log(all + self.ToString() + " is not Spider Mother");
                    }
                }
                else
                {
                    if (isStorySession && data.transformation == "Spider")
                    {
                        if (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.spider_transformation_skip.Value)
                        {
                            data.transformation = "SpiderTransformation";

                            if (!data.liz.TryGetValue("SpiderNumber", out string _))
                            {
                                data.liz.Add("SpiderNumber", UnityEngine.Random.Range(30, 55).ToString());
                            }
                            else if (data.lizardUpdatedCycle != (isStorySession ? cycleNumber : -1))
                            {
                                data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();
                            }

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " has gained the Spider Transformation");
                        }
                        else if (ShadowOfOptions.debug_logs.Value)
                        {
                            int num3 = data.transformationTimer - cycleNumber;
                            string text = (num3 < 0) ? (num3 * -1).ToString() : num3.ToString();
                            Debug.Log(all + self.ToString() + " is Spider Mother for " + text + " cycle out of the required 3 cycles to gain the Spider Transformation");
                        }
                        goto SkipTransformations;
                    }
                    else if (data.transformation == "SpiderTransformation")
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (data.ArmState[i] == "Cut1" || data.ArmState[i] == "Cut2")
                            {
                                data.ArmState[i] = "Spider";
                            }
                        }

                        if (isStorySession && data.transformationTimer != cycleNumber)
                        {
                            if (!data.liz.TryGetValue("SpiderNumber", out string _))
                            {
                                data.liz.Add("SpiderNumber", UnityEngine.Random.Range(30, 55).ToString());
                            }
                            else if (data.lizardUpdatedCycle != (isStorySession ? cycleNumber : -1))
                            {
                                data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();
                            }

                            data.transformationTimer = cycleNumber;
                        }
                        goto SkipTransformations;
                    }
                }
            }
            else
            {
                if (data.liz.TryGetValue("SpiderNumber", out string _))
                {
                    data.liz.Remove("SpiderNumber");
                }
            }

            //Electric Transformation
            if (ShadowOfOptions.electric_transformation.Value && (data.transformation == "Null" || data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                if (firstTime)
                {
                    if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_electric_transformation_chance.Value)
                    {
                        data.transformation = "ElectricTransformation";

                        data.liz.Add("ElectricColorTimer", "0");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Electric Transformation due to Chance");

                        goto SkipTransformations;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                    {
                        Debug.Log(all + self.ToString() + " is not Electric");
                    }
                }
                else if (isStorySession && data.transformation == "Electric")
                {
                    if (data.liz.TryGetValue("SpiderNumber", out string _))
                    {
                        data.liz.Remove("SpiderNumber");
                    }

                    if (data.transformationTimer <= 0)
                    {
                        data.transformation = "Null";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is no longer Electric due to running out of Charge.");
                    }
                    else if (data.transformationTimer >= 3 || ShadowOfOptions.electric_transformation_skip.Value)
                    {
                        data.transformation = "ElectricTransformation";

                        if (data.liz.TryGetValue("SpiderNumber", out string _))
                        {
                            data.liz.Remove("SpiderNumber");
                        }
                        if (data.liz.TryGetValue("PreMeltedTime", out string _))
                        {
                            data.liz.Remove("PreMeltedTime");
                        }

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has gained the Electric Transformation");
                    }
                    goto SkipTransformations;
                }
                else if (data.transformation == "ElectricTransformation")
                {
                    if (data.liz.TryGetValue("SpiderNumber", out string _))
                    {
                        data.liz.Remove("SpiderNumber");
                    }
                    if (data.liz.TryGetValue("PreMeltedTime", out string _))
                    {
                        data.liz.Remove("PreMeltedTime");
                    }

                    goto SkipTransformations;
                }
            }

            //Melted Transformation
            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Null" || data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                if (firstTime)
                {
                    if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_melted_transformation_chance.Value)
                    {
                        data.transformation = "MeltedTransformation";
                        data.transformationTimer = isStorySession ? cycleNumber : 1;

                        //self.lavaImmune = true;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Melted Transformation due to Chance");

                        goto SkipTransformations;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                    {
                        data.liz.Add("PreMeltedTime", "-1");

                        Debug.Log(all + self.ToString() + " is not Melted");
                    }
                }
                else
                {
                    if (data.transformation == "Melted" && isStorySession && (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.melted_transformation_skip.Value))
                    {
                        if (data.liz.TryGetValue("SpiderNumber", out string _))
                        {
                            data.liz.Remove("SpiderNumber");
                        }
                        if (data.liz.TryGetValue("PreMeltedTime", out string _))
                        {
                            data.liz.Remove("PreMeltedTime");
                        }

                        data.transformation = "MeltedTransformation";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has gained the Melted Transformation");
                    }
                    else if (data.transformation == "MeltedTransformation")
                    {
                        if (data.liz.TryGetValue("SpiderNumber", out string _))
                        {
                            data.liz.Remove("SpiderNumber");
                        }
                        if (data.liz.TryGetValue("PreMeltedTime", out string _))
                        {
                            data.liz.Remove("PreMeltedTime");
                        }
                    }
                }
            }
        SkipTransformations:

            #endregion

            LizardBreedParams breedParameters = self.creatureTemplate.breedParameters as LizardBreedParams;

            if (ShadowOfOptions.tongue_stuff.Value && firstTime && !data.liz.TryGetValue("Tongue", out _) && breedParameters.tongue)
            {
                data.liz.Add("Tongue", "True");
                if (creatureTemplate.type == CreatureTemplate.Type.WhiteLizard)
                {
                    data.liz.Add("NewTongue", "WhiteLizard");
                }
                else if (creatureTemplate.type == CreatureTemplate.Type.Salamander)
                {
                    data.liz.Add("NewTongue", "Salamander");
                }
                else if (creatureTemplate.type == CreatureTemplate.Type.BlueLizard)
                {
                    data.liz.Add("NewTongue", "BlueLizard");
                }
                else if (creatureTemplate.type == CreatureTemplate.Type.CyanLizard)
                {
                    data.liz.Add("NewTongue", "CyanLizard");
                }
                else if (creatureTemplate.type == CreatureTemplate.Type.RedLizard)
                {
                    data.liz.Add("NewTongue", "RedLizard");
                }
                else if (ModManager.DLCShared && creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.ZoopLizard)
                {
                    data.liz.Add("NewTongue", "ZoopLizard");
                }
                else
                {
                    data.liz.Add("NewTongue", "Unknown");

                    Debug.Log(all + "Tongue of " + self + " is Unknown, if able please report to the mod author of Shadow Of Lizards");
                    Logger.LogError(all + "Tongue of " + self + " is Unknown, if able please report to the mod author of Shadow Of Lizards");
                }

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " has a " + data.liz["NewTongue"] + " Tongue");
            }
            else
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " does not have a Tongue");
            }

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating Abstract " + self);

            data.lizardUpdatedCycle = isStorySession ? cycleNumber : -1;
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void SaveAbstractLizard(On.AbstractCreature.orig_Abstractize orig, AbstractCreature self, WorldCoordinate coord)
    {
        if (self == null || self.state == null || self.state.unrecognizedSaveStrings == null || self.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate || !self.destroyOnAbstraction || !lizardstorage.TryGetValue(self, out LizardData data))
        {
            orig(self, coord);
            return;
        }

        Dictionary<string, string> savedData = self.state.unrecognizedSaveStrings;

        savedData.Add("ShadowOfBeheaded", data.Beheaded == true ? "true" : "false");

        string liz = "";
        for (int i = 0; i < data.liz.Count; i++)
        {
            liz += data.liz.ElementAt(i).Key + "=";
            liz += data.liz.ElementAt(i).Value + ";";
        }
        savedData.Add("ShadowOfLiz", liz);

        string chunk = "";
        for (int i = 0; i < data.availableBodychunks.Count; i++)
        {
            chunk += data.availableBodychunks[i] + ";";
        }
        savedData.Add("ShadowOfAvailableBodychunks", chunk);

        savedData.Add("ShadowOfTransformation", data.transformation);
        savedData.Add("ShadowOfTransformationTimer", data.transformationTimer.ToString());

        string ArmState = "";
        for (int i = 0; i < data.ArmState.Count; i++)
        {
            ArmState += data.ArmState[i] + ";";
        }
        savedData.Add("ShadowOfArmState", ArmState);

        savedData.Add("ShadowOfLizardUpdatedCycle", data.lizardUpdatedCycle.ToString());

        savedData.Add("ShadowOfCheatDeathChance", data.cheatDeathChance.ToString());



        Debug.Log(all + "Saving values for Abstract " + self);

        Debug.Log(all + self + " beheaded = " + savedData["ShadowOfBeheaded"]);
        Debug.Log(all + self + " lizDictionary = " + savedData["ShadowOfLiz"]);

        Debug.Log(all + self + " bodyChunks = " + savedData["ShadowOfAvailableBodychunks"]);

        Debug.Log(all + self + " transformation = " + savedData["ShadowOfTransformation"]);
        Debug.Log(all + self + " transformationTimer = " + savedData["ShadowOfTransformationTimer"]);

        Debug.Log(all + self + " armState = " + savedData["ShadowOfArmState"]);

        Debug.Log(all + self + " updatedCycle = " + savedData["ShadowOfLizardUpdatedCycle"]);

        Debug.Log(all + self + " cheatDeathChance = " + savedData["ShadowOfCheatDeathChance"]);

        orig(self, coord);
    }

    void NewLizardGraphics(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (!graphicstorage.TryGetValue(self, out GraphicsData data2))
        {
            graphicstorage.Add(self, new GraphicsData());
            graphicstorage.TryGetValue(self, out GraphicsData dat2);
            data2 = dat2;
        }

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || (data.transformation != "Melted" && data.transformation != "MeltedTransformation") || !ModManager.DLCShared || self.lizard.lizardParams.template != DLCSharedEnums.CreatureTemplateType.SpitLizard)
        {
            return;
        }

        self.lizard.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
    }

    LizardBreedParams LizBread(Lizard liz)
    {
        LizardBreedParams lizardParams = liz.lizardParams;
        return new LizardBreedParams(liz.abstractCreature.creatureTemplate.type)
        {
            template = lizardParams.template,
            toughness = lizardParams.toughness,
            stunToughness = lizardParams.stunToughness,
            biteDamage = lizardParams.biteDamage,
            biteDamageChance = lizardParams.biteDamageChance,
            aggressionCurveExponent = lizardParams.aggressionCurveExponent,
            danger = lizardParams.danger,
            biteDelay = lizardParams.biteDelay,
            baseSpeed = lizardParams.baseSpeed,
            biteInFront = lizardParams.biteInFront,
            biteRadBonus = lizardParams.biteRadBonus,
            biteHomingSpeed = lizardParams.biteHomingSpeed,
            biteChance = lizardParams.biteChance,
            attemptBiteRadius = lizardParams.attemptBiteRadius,
            getFreeBiteChance = lizardParams.getFreeBiteChance,
            baseSpeedMultiplier = lizardParams.baseSpeedMultiplier,
            standardColor = lizardParams.standardColor,
            regainFootingCounter = lizardParams.regainFootingCounter,
            bodyMass = lizardParams.bodyMass,
            bodySizeFac = lizardParams.bodySizeFac,
            bodyLengthFac = lizardParams.bodyLengthFac,
            bodyRadFac = lizardParams.bodyRadFac,
            pullDownFac = lizardParams.pullDownFac,
            floorLeverage = lizardParams.floorLeverage,
            terrainSpeeds = lizardParams.terrainSpeeds,
            wiggleSpeed = lizardParams.wiggleSpeed,
            wiggleDelay = lizardParams.wiggleDelay,
            bodyStiffnes = lizardParams.bodyStiffnes,
            swimSpeed = lizardParams.swimSpeed,
            idleCounterSubtractWhenCloseToIdlePos = lizardParams.idleCounterSubtractWhenCloseToIdlePos,
            headShieldAngle = lizardParams.headShieldAngle,
            canExitLounge = lizardParams.canExitLounge,
            canExitLoungeWarmUp = lizardParams.canExitLoungeWarmUp,
            findLoungeDirection = lizardParams.findLoungeDirection,
            loungeDistance = lizardParams.loungeDistance,
            preLoungeCrouch = lizardParams.preLoungeCrouch,
            preLoungeCrouchMovement = lizardParams.preLoungeCrouchMovement,
            loungeSpeed = lizardParams.loungeSpeed,
            loungePropulsionFrames = lizardParams.loungePropulsionFrames,
            loungeMaximumFrames = lizardParams.loungeMaximumFrames,
            loungeJumpyness = lizardParams.loungeJumpyness,
            loungeDelay = lizardParams.loungeDelay,
            riskOfDoubleLoungeDelay = lizardParams.riskOfDoubleLoungeDelay,
            postLoungeStun = lizardParams.postLoungeStun,
            loungeTendensy = lizardParams.loungeTendensy,
            perfectVisionAngle = lizardParams.perfectVisionAngle,
            periferalVisionAngle = lizardParams.periferalVisionAngle,
            shakePrey = lizardParams.shakePrey,
            biteDominance = lizardParams.biteDominance,
            limbSize = lizardParams.limbSize,
            limbThickness = lizardParams.limbThickness,
            stepLength = lizardParams.stepLength,
            liftFeet = lizardParams.liftFeet,
            feetDown = lizardParams.feetDown,
            noGripSpeed = lizardParams.noGripSpeed,
            limbSpeed = lizardParams.limbSpeed,
            limbQuickness = lizardParams.limbQuickness,
            limbGripDelay = lizardParams.limbGripDelay,
            smoothenLegMovement = lizardParams.smoothenLegMovement,
            legPairDisplacement = lizardParams.legPairDisplacement,
            walkBob = lizardParams.walkBob,
            tailSegments = lizardParams.tailSegments,
            tailStiffness = lizardParams.tailStiffness,
            tailStiffnessDecline = lizardParams.tailStiffnessDecline,
            tailLengthFactor = lizardParams.tailLengthFactor,
            tailColorationStart = lizardParams.tailColorationStart,
            tailColorationExponent = lizardParams.tailColorationExponent,
            headSize = lizardParams.headSize,
            neckStiffness = lizardParams.neckStiffness,
            jawOpenAngle = lizardParams.jawOpenAngle,
            jawOpenLowerJawFac = lizardParams.jawOpenLowerJawFac,
            jawOpenMoveJawsApart = lizardParams.jawOpenMoveJawsApart,
            headGraphics = lizardParams.headGraphics,
            framesBetweenLookFocusChange = lizardParams.framesBetweenLookFocusChange,
            tongue = lizardParams.tongue,
            tongueAttackRange = lizardParams.tongueAttackRange,
            tongueWarmUp = lizardParams.tongueWarmUp,
            tongueSegments = lizardParams.tongueSegments,
            tongueChance = lizardParams.tongueChance,
            tamingDifficulty = lizardParams.tamingDifficulty,
            maxMusclePower = lizardParams.maxMusclePower,
        };
    }

    public static bool IsLizardValid(CreatureTemplate self)
    {
        CreatureTemplate.Type c = self.type;

        if (c == CreatureTemplate.Type.BlackLizard || c == CreatureTemplate.Type.BlueLizard || c == CreatureTemplate.Type.GreenLizard || c == CreatureTemplate.Type.PinkLizard || c == CreatureTemplate.Type.RedLizard || c == CreatureTemplate.Type.Salamander
            || c == CreatureTemplate.Type.WhiteLizard || c == CreatureTemplate.Type.YellowLizard || c == CreatureTemplate.Type.CyanLizard || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.EelLizard 
            || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.SpitLizard || ModManager.MSC && c == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.ZoopLizard 
            || ModManager.Watcher && c == WatcherEnums.CreatureTemplateType.BasiliskLizard || ModManager.Watcher && c == WatcherEnums.CreatureTemplateType.BlizzardLizard || ModManager.Watcher && c == WatcherEnums.CreatureTemplateType.IndigoLizard)
        {
            return true;
        }
        return false;
    }
    
    public static bool HealthBasedChance(Lizard self, float chance)
    {
        if (UnityEngine.Random.value * 100 < chance * (ShadowOfOptions.health_based_chance.Value ? ((ShadowOfOptions.health_based_chance_dead.Value && self.dead) ? 1 : Mathf.Lerp(ShadowOfOptions.health_based_chance_min.Value / 100, ShadowOfOptions.health_based_chance_max.Value / 100, self.LizardState.health)) : 1))
        {
            return true;
        }

        return false;
    }

    void AISpitAbility(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
    {
        orig.Invoke(self, creature, world);

        if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("CanSpit", out string _))
        {
            return;
        }

        try
        {
            if (data.liz["CanSpit"] == "True" && self.redSpitAI == null)
            {
                self.redSpitAI = new LizardAI.LizardSpitTracker(self);
                self.AddModule(self.redSpitAI);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " added Spit ability");
            }
            else if (data.liz["CanSpit"] == "False" && self.redSpitAI != null)
            {
                self.modules.Remove(self.redSpitAI);
                self.redSpitAI = null;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " removed Spit ability");
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void GasLeak(On.LizardJumpModule.orig_Update orig, LizardJumpModule self)
    {
        orig.Invoke(self);

        if (self.gasLeakSpear == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("CanJump", out _) || !(UnityEngine.Random.Range(0, 100) < ShadowOfOptions.jump_stuff_chance.Value))
        {
            return;
        }

        try
        {
            data.liz["CanJump"] = "False";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " lost the ability to Jump due to Gas Leak");

            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                data.cheatDeathChance -= 10;
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void LizTongue(On.LizardTongue.orig_ctor orig, LizardTongue self, Lizard lizard)
    {
        if (!ShadowOfOptions.tongue_stuff.Value || !lizardstorage.TryGetValue(lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("Tongue", out _) || !data.liz.TryGetValue("NewTongue", out string Tongue))
        {
            orig.Invoke(self, lizard);
            return;
        }
        Debug.Log("Tongue Orig " + Tongue);
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
                default:
                    Debug.Log(all + "Failed Getting the " + Tongue + " Tongue for " + lizard);
                    Logger.LogError(all + "Failed Getting the " + Tongue + " Tongue for " + lizard);
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
        catch (Exception e) { Logger.LogError(e); }
    }

    void TongueCheck(On.LizardTongue.orig_Update orig, LizardTongue self)
    {
        if (!ShadowOfOptions.tongue_stuff.Value || self.lizard.lizardParams.tongue || self.state != LizardTongue.State.Hidden || self.Out)
        {
            orig.Invoke(self);
            return;
        }

        try
        {
            self.lizard.animation = Lizard.Animation.Standard;
            //self.lizard.tongue = null;

            //Debug.Log("Tongue Update");
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void BreedTongue(Lizard self)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || !data.liz.TryGetValue("Tongue", out string Tongue))
        {
            return;
        }

        switch (Tongue)
        {
            case "WhiteLizard":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 440f;
                self.lizardParams.tongueWarmUp = 80;
                self.lizardParams.tongueSegments = 10;
                self.lizardParams.tongueChance = 0.1f;
                break;
            case "Salamander":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 150f;
                self.lizardParams.tongueWarmUp = 8;
                self.lizardParams.tongueSegments = 7;
                self.lizardParams.tongueChance = 1f / 3f;
                break;
            case "BlueLizard":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 140f;
                self.lizardParams.tongueWarmUp = 10;
                self.lizardParams.tongueSegments = 5;
                self.lizardParams.tongueChance = 0.25f;
                break;
            case "CyanLizard":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 160f;
                self.lizardParams.tongueWarmUp = 8;
                self.lizardParams.tongueSegments = 7;
                self.lizardParams.tongueChance = 1f / 3f;
                break;
            case "RedLizard":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 350f;
                self.lizardParams.tongueWarmUp = 8;
                self.lizardParams.tongueSegments = 10;
                self.lizardParams.tongueChance = 0.1f;
                break;
            case "ZoopLizard":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 440f;
                self.lizardParams.tongueWarmUp = 140;
                self.lizardParams.tongueSegments = 10;
                self.lizardParams.tongueChance = 0.3f;
                break;
            case "Tube":
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 200f;
                self.lizardParams.tongueWarmUp = 0;
                self.lizardParams.tongueSegments = 20;
                self.lizardParams.tongueChance = 0.3f;
                break;
            default:
                Debug.Log(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                Logger.LogError(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                self.lizardParams.tongue = true;
                self.lizardParams.tongueAttackRange = 140f;
                self.lizardParams.tongueWarmUp = 10;
                self.lizardParams.tongueSegments = 5;
                self.lizardParams.tongueChance = 0.25f;
                break;
        }
    }

    bool HitHeadShield(On.Lizard.orig_HitHeadShield orig, Lizard self, Vector2 direction)
    {
        if (lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.Beheaded == true)
        {
            return false;
        }
        return orig.Invoke(self, direction);
    }

    void LizardBite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (ShadowOfOptions.teeth.Value || !lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || data.lastDamageType == null || !data.liz.TryGetValue("UpperTeeth", out string upperTeeth))
        {
            orig.Invoke(self, chunk);
            return;
        }

        try
        {
            string lowerTeeth = data.liz["LowerTeeth"];

            bool flag = upperTeeth != "Normal" && lowerTeeth != "Normal";

            if (flag)
            {
                if (UnityEngine.Random.value > 0.4f)
                {
                    self.biteControlReset = false;
                    self.JawOpen = 0f;
                    self.lastJawOpen = 0f;

                    self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                    return;
                }
            }
            else if (upperTeeth != "Normal" || lowerTeeth != "Normal")
            {
                if (UnityEngine.Random.value > 0.7f)
                {
                    if (ModManager.MSC && self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && self.room != null)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 a = Custom.RNV();
                            self.room.AddObject(new Spark(self.firstChunk.pos + a * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 8, 24));
                        }
                    }
                    self.biteControlReset = false;
                    self.JawOpen = 0f;
                    self.lastJawOpen = 0f;

                    self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                    return;
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }

        orig.Invoke(self, chunk);
    }

    void LimbDeath(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || data.lastDamageType == null)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            return;
        }

        try
        {
            bool sourceFlag = source != null && source.owner != null;

            bool sourcetypeFlag = (source == null && type.ToString() == "Explosion") || (source != null && (source.owner == null || (source.owner != null && source.owner is not JellyFish)));

            #region Electric
            if (sourceFlag && source.owner is ElectricSpear && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + source.owner.ToString() + "'s damage was halved due to Resistance on " + self);

                damage /= 2f;
            }
            #endregion

            #region Melted
            if (data.lastDamageType != "Melted" && type.ToString() != "Explosion")
                data.lastDamageType = type.ToString();
            #endregion

            PreViolenceCheck(self);
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);

            if (sourceFlag && source.owner is Creature crit)
            {
                Lizard receiver = self;
                string killType = type.ToString();
                PostViolenceCheck(receiver, killType, crit);
            }

            if (hitChunk == null || damage < 0.5f || !sourcetypeFlag || !directionAndMomentum.HasValue)
            {
                return;
            }

            //Cut in Half
            if (false && hitChunk.index != 0 && hitChunk.index != 1 && data.availableBodychunks.Contains(hitChunk.index))
            {
                CutInHalf(self, hitChunk);
            }

            if (hitChunk.index == 0)
            {
                if (data.Beheaded != true && hitChunk.index == 0)
                {
                    if (LizHitHeadShield(directionAndMomentum.Value, self))
                    {
                        if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_cut_chance.Value)
                        {
                            string eye = (UnityEngine.Random.Range(0, 2) == 0) ? "EyeRight" : "EyeLeft";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + "'s " + eye + " was hit");

                            if (type == Creature.DamageType.Stab || type == Creature.DamageType.Bite)
                            {
                                if ((data.liz[eye] == "Normal" || data.liz[eye] == "Blind") && UnityEngine.Random.Range(0, 100) >= 25)
                                {
                                    HitEye(eye, data.liz[eye], "Scar");
                                }
                                else if (data.liz[eye] == "Cut")
                                {
                                    self.Blind(5);
                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + self.ToString() + "'s " + eye + " is already Cut");
                                }
                                else
                                {
                                    HitEye(eye, data.liz[eye], "Cut");
                                }
                            }
                            else
                            {
                                if (data.liz[eye] == "Normal" || data.liz[eye] == "Blind")
                                {
                                    HitEye(eye, data.liz[eye], "Scar");
                                }
                                else
                                {
                                    self.Blind(5);
                                }
                            }

                        }
                        else if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _) && type != Creature.DamageType.Bite && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.teeth_chance.Value)
                        {
                            string teeth = UnityEngine.Random.Range(0, 2) == 0 ? "UpperTeeth" : "LowerTeeth";
                            int teethNum = UnityEngine.Random.Range(1, 5);

                            if (data.liz[teeth] == "Normal")
                            {
                                data.liz[teeth] = "Broken" + teethNum.ToString();
                                self.lizardParams.biteDamageChance *= 0.5f;
                                self.lizardParams.biteDominance *= 0.5f;
                                self.lizardParams.biteDamage *= 1.1f;
                                self.lizardParams.getFreeBiteChance *= 1.1f;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " " + (teeth == "UpperTeeth" ? "Upper" : "Lower") + " Teeth were broken");

                                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                    data.cheatDeathChance -= 5;

                                self.room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, self.firstChunk, false, Custom.LerpMap(source.vel.magnitude, 0f, 8f, 0.2f, 1f) + 10, 1f);
                            }
                        }

                        if (ShadowOfOptions.decapitation.Value && data.lastDamageType != "Melted" && type.ToString() == "Explosion")
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " had it's Head cut by an explosion");

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.decapitation_chance.Value * 0.5)
                            {
                                data.Beheaded = true;
                                Decapitation(self);
                                self.Die();
                            }
                        }
                    }
                    else if (LizHitInMouth(directionAndMomentum.Value, self))
                    {
                        if (ShadowOfOptions.tongue_stuff.Value && data.liz.TryGetValue("Tongue", out _) && self.tongue != null && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " was hit in it's Mouth");

                            self.tongue.Retract();

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_stuff_chance.Value)
                            {
                                data.liz["Tongue"] = "False";
                                self.lizardParams.tongue = false;

                                data.liz["NewTongue"] = "get";

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " lost it's Tongue due to being hit in Mouth");
                            }
                            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                data.cheatDeathChance -= 10;
                        }
                    }
                    else if (ShadowOfOptions.decapitation.Value && data.Beheaded == false && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " was hit it's Neck");

                        if (HealthBasedChance(self, ShadowOfOptions.decapitation_chance.Value))
                        {
                            data.Beheaded = true;
                            Decapitation(self);
                            self.Die();
                        }
                    }
                }
            }
            else if (ShadowOfOptions.dismemberment.Value && (hitChunk.index == 1 || hitChunk.index == 2) && HealthBasedChance(self, ShadowOfOptions.dismemberment_chance.Value))
            {
                float num5 = Custom.Angle(new Vector2(directionAndMomentum.Value.x, directionAndMomentum.Value.y), -hitChunk.Rotation) * (hitChunk.index == 2 ? -1f : 1f);
                int num8;

                if (ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.EelLizard)
                {
                    if (hitChunk.index != 1)
                    {
                        return;
                    }

                    num8 = (num5 < 0f) ? 0 : 1;
                    int num9 = (num5 < 0f) ? 2 : 3;

                    if (data.ArmState[num8] == "Normal")
                    {
                        EllLegCut(num8, num9, num8);
                    }

                    return;
                }
                else if ((ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard) || (self.graphicsModule as LizardGraphics).limbs.Length == 6)
                {
                    if (hitChunk.index == 1)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;
                        int num9 = (num5 < 0f) ? 4 : 5;

                        if (UnityEngine.Random.value > 0.25 && data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }
                        else if (data.ArmState[num9] == "Normal")
                        {
                            LegCut(num9, num9);
                        }

                        return;
                    }

                    num8 = (num5 < 0f) ? 0 : 1;
                    int num10 = (num5 < 0f) ? 5 : 4;

                    if (UnityEngine.Random.value > 0.25 && data.ArmState[num8] == "Normal")
                    {
                        LegCut(num8, num8);
                    }
                    else if (data.ArmState[num10] == "Normal")
                    {
                        LegCut(num10, num10);
                    }

                    return;
                }
                else
                {
                    if (hitChunk.index == 2)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;

                        if (data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }

                        return;
                    }

                    num8 = (num5 < 0f) ? 0 : 1;

                    if (data.ArmState[num8] == "Normal")
                    {
                        LegCut(num8, num8);
                    }
                }
            }

            //Voids (and bools)
            void EllLegCut(int FirstLeg, int SecondLeg, int int1)
            {
                bool a = UnityEngine.Random.Range(0, 2) == 0;

                data.ArmState[FirstLeg] = a ? "Cut1" : "Cut2";
                data.ArmState[SecondLeg] = a ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.ArmState[FirstLeg]);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " limb cut " + int1);
            }

            void LegCut(int Leg, int int1)
            {
                data.ArmState[Leg] = UnityEngine.Random.Range(0, 2) == 0 ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.ArmState[Leg]);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " limb cut " + int1);
            }

            static bool LizHitHeadShield(Vector2 direction, Lizard self)
            {
                float num19 = Vector2.Angle(direction, -self.bodyChunks[0].Rotation);
                if (LizHitInMouth(direction, self))
                {
                    return false;
                }
                if (num19 < self.lizardParams.headShieldAngle + 20f * self.JawOpen)
                {
                    Room room = self.room;
                    room?.PlaySound(SoundID.Lizard_Head_Shield_Deflect, self.mainBodyChunk);
                    return true;
                }
                return false;
            }
            static bool LizHitInMouth(Vector2 direction, Lizard self)
            {
                if (direction.y > 0f)
                {
                    return false;
                }
                direction = Vector3.Slerp(direction, new Vector2(0f, 1f), 0.1f);
                return Mathf.Abs(Vector2.Angle(direction, -self.bodyChunks[0].Rotation)) < Mathf.Lerp(-15f, 11f, self.JawOpen);
            }

            void HitEye(string eye, string oldEye, string newEye)
            {
                bool cut = newEye == "Cut";

                data.liz[eye] = (oldEye.StartsWith("Blind") ? "Blind" : "") + newEye + (newEye == "Scar" ? (UnityEngine.Random.Range(0, 2) == 0 ? "" : "2") : "");
                self.Blind(cut ? 40 : 5);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + "'s " + eye + " was " + oldEye + " now it's " + data.liz[eye]);

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                    data.cheatDeathChance -= cut ? 10 : 5;

                if (cut)
                    EyeCut(self, eye);
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void CutInHalf(Lizard self, BodyChunk hitChunk)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            return;
        }

        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[hitChunk.index].pos);
        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        AbstractCreature abstractLizard = new(self.room.world, self.Template, null, pos, self.abstractCreature.ID);

        lizardGoreStorage.Add(abstractLizard, new LizardGoreData());
        lizardGoreStorage.TryGetValue(abstractLizard, out LizardGoreData goreData);

        goreData.origLizardData = data;

        for (int i = hitChunk.index; i < self.bodyChunks.Count() && data.availableBodychunks.Contains(i); i++)
        {
            goreData.availableBodychunks.Add(i);
            data.availableBodychunks.Remove(i);
        }

        self.room.abstractRoom.AddEntity(abstractLizard);

        abstractLizard.RealizeInRoom();
    }

    public static void LimbCut(Lizard self, BodyChunk hitChunk, int num4, string spriteVariant)
    {
        if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
            LimbCutBloodEmitter(self, hitChunk);

        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            return;
        }

        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
            data.cheatDeathChance -= 5;

        SpriteLeaser sLeaser = data.sLeaser;

        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[hitChunk.index].pos);
        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        string text2 = self.Template.type.ToString();

        float r = self.effectColor.r;
        float g = self.effectColor.g;
        float b = self.effectColor.b;

        float lizBloodColourR = -1f;
        float lizBloodColourG = -1f;
        float lizBloodColourB = -1f;

        if (ShadowOfOptions.blood.Value && bloodcolours != null)
        {
            lizBloodColourR = bloodcolours[self.Template.type.ToString()].r;
            lizBloodColourG = bloodcolours[self.Template.type.ToString()].g;
            lizBloodColourB = bloodcolours[self.Template.type.ToString()].b;
        }

        float r2;
        float g2;
        float b2;

        LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

        if (text2 == "Salamander")
        {
            r2 = graphicsModule.SalamanderColor.r;
            g2 = graphicsModule.SalamanderColor.g;
            b2 = graphicsModule.SalamanderColor.b;
        }
        else
        {
            r2 = graphicsModule.ivarBodyColor.r;
            g2 = graphicsModule.ivarBodyColor.g;
            b2 = graphicsModule.ivarBodyColor.b;
        }

        int num17 = graphicsModule.SpriteLimbsStart + num4;
        int num18 = graphicsModule.SpriteLimbsColorStart + num4;

        self.LizardState.limbHealth[num4] = 0f;

        graphicsModule.limbs[num4].currentlyDisabled = true;

        string lizardArm = sLeaser.sprites[num17].element.name;
        string lizardArmColor = sLeaser.sprites[num18].element.name;

        string lizardArmCut;
        string lizardArmColorCut;

        if (lizardArm == "LizardArm_28A")
                lizardArm = "LizardArm_28";

        if (Futile.atlasManager.DoesContainElementWithName(lizardArm + (spriteVariant == "Cut1" ? "Cut2" : "Cut4")))
        {
            lizardArmCut = lizardArm + (spriteVariant == "Cut1" ? "Cut2" : "Cut4");
            lizardArmColorCut = lizardArmColor + (spriteVariant == "Cut1" ? "Cut2" : "Cut4");
        }
        else
        {
            lizardArmCut = "LizardArm_28Cut2";
            lizardArmColorCut = "LizardArmColor_28Cut2";

            Debug.Log("LizCutLeg object could not be properily created due to the: " + lizardArm + " Leg Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
        }


        LizCutLegAbstract lizardCutLegAbstract = new(self.room.world, pos, self.room.game.GetNewID())
        {
            hue = 1f,
            saturation = 0.5f,
            scaleX = sLeaser.sprites[num17].scaleX,
            scaleY = sLeaser.sprites[num17].scaleY,
            LizType = text2,
            LizBaseColourR = r2,
            LizBaseColourG = g2,
            LizBaseColourB = b2,
            LizBloodColourR = lizBloodColourR,
            LizBloodColourG = lizBloodColourG,
            LizBloodColourB = lizBloodColourB,
            LizColourR = r,
            LizColourG = g,
            LizColourB = b,
            LizSpriteName = lizardArmCut,
            LizColourSpriteName = lizardArmColorCut,
            LizBreed = self.Template.type.value
        };

        self.room.abstractRoom.AddEntity(lizardCutLegAbstract);
        lizardCutLegAbstract.RealizeInRoom();

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "LizCutLeg Created");
    }

    static void LimbCutBloodEmitter(Lizard self, BodyChunk hitChunk)
    {
        self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(11f, 15f), UnityEngine.Random.Range(7f, 14f)));
        self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(9f, 20f)));
    }

    public static void Decapitation(Lizard self)
    {
        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + self.ToString() + " was Decapitated");

        if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
            DecapitationBloodEmitter(self);

        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || data.sLeaser == null)
        {
            return;
        }

        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
            data.cheatDeathChance -= 50;

        SpriteLeaser sLeaser = data.sLeaser;

        data.liz["BeheadedCycle"] = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "-1";
        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[0].pos);
        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        string template = self.Template.type.ToString();

        float r = self.effectColor.r;
        float g = self.effectColor.g;
        float b = self.effectColor.b;

        float lizBloodColourR = -1f;
        float lizBloodColourG = -1f;
        float lizBloodColourB = -1f;

        if (bloodcolours != null && ShadowOfOptions.blood.Value)
        {
            lizBloodColourR = bloodcolours[self.Template.type.ToString()].r;
            lizBloodColourG = bloodcolours[self.Template.type.ToString()].g;
            lizBloodColourB = bloodcolours[self.Template.type.ToString()].b;
        }

        float r2;
        float g2;
        float b2;

        LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

        if (template == "Salamander")
        {
            r2 = graphicsModule.SalamanderColor.r;
            g2 = graphicsModule.SalamanderColor.g;
            b2 = graphicsModule.SalamanderColor.b;
        }
        else
        {
            r2 = graphicsModule.ivarBodyColor.r;
            g2 = graphicsModule.ivarBodyColor.g;
            b2 = graphicsModule.ivarBodyColor.b;
        }

        int spriteHeadStart = graphicsModule.SpriteHeadStart;

        FSprite[] sprites = data.sLeaser.sprites;

        bool validHead;

        if (Futile.atlasManager.DoesContainElementWithName(sprites[graphicsModule.SpriteHeadStart + 3].element.name + "Cut2"))
        {
            validHead = true;
        }
        else
        {
            validHead = false;

            Debug.Log("LizCutHead object could not be properily created due to the: " + sprites[graphicsModule.SpriteHeadStart + 3].element.name + " Head Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
        }

        string headSprite0 = sprites[graphicsModule.SpriteHeadStart].element.name;
        string headSprite1 = sprites[graphicsModule.SpriteHeadStart + 1].element.name;
        string headSprite2 = sprites[graphicsModule.SpriteHeadStart + 2].element.name;
        string headSprite3 = validHead ? sprites[graphicsModule.SpriteHeadStart + 3].element.name : "LizardHead0.1";
        string headSprite4 = sprites[graphicsModule.SpriteHeadStart + 4].element.name;
        string headSprite5 = null;
        string headSprite6 = null;

        float eyeRightColourR = 0f;
        float eyeRightColourG = 0f;
        float eyeRightColourB = 0f;

        float eyeLeftColourR = 0f;
        float eyeLeftColourG = 0f;
        float eyeLeftColourB = 0f;

        if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2))
        {
            headSprite5 = data.sLeaser.sprites[data2.EyesSprites].element.name;
            headSprite6 = data.sLeaser.sprites[data2.EyesSprites + 1].element.name;
            eyeRightColourR = data.sLeaser.sprites[data2.EyesSprites].color.r;
            eyeRightColourG = data.sLeaser.sprites[data2.EyesSprites].color.g;
            eyeRightColourB = data.sLeaser.sprites[data2.EyesSprites].color.b;
            eyeLeftColourR = data.sLeaser.sprites[data2.EyesSprites + 1].color.r;
            eyeLeftColourG = data.sLeaser.sprites[data2.EyesSprites + 1].color.g;
            eyeLeftColourB = data.sLeaser.sprites[data2.EyesSprites + 1].color.b;
        }

        
        LizCutHeadAbstract lizCutHeadAbstract = new(self.room.world, pos, self.room.game.GetNewID())
        {
            hue = 1f,
            saturation = 0.5f,

            scaleX = sLeaser.sprites[spriteHeadStart].scaleX,
            scaleY = sLeaser.sprites[spriteHeadStart].scaleY,

            LizType = template,

            LizBaseColourR = r2,
            LizBaseColourG = g2,
            LizBaseColourB = b2,

            LizColourR = r,
            LizColourG = g,
            LizColourB = b,

            LizBloodColourR = lizBloodColourR,
            LizBloodColourG = lizBloodColourG,
            LizBloodColourB = lizBloodColourB,

            EyeRightColourR = eyeRightColourR,
            EyeRightColourG = eyeRightColourG,
            EyeRightColourB = eyeRightColourB,

            EyeLeftColourR = eyeLeftColourR,
            EyeLeftColourG = eyeLeftColourG,
            EyeLeftColourB = eyeLeftColourB,

            HeadSprite0 = headSprite0,
            HeadSprite1 = headSprite1,
            HeadSprite2 = headSprite2,
            HeadSprite3 = headSprite3,
            HeadSprite4 = headSprite4,
            HeadSprite5 = headSprite5,
            HeadSprite6 = headSprite6,

            blackSalamander = ((LizardGraphics)self.graphicsModule).blackSalamander,

            rad = self.bodyChunks[0].rad,

            LizBreed = self.Template.type.value
        };

        self.room.abstractRoom.AddEntity(lizCutHeadAbstract);
        lizCutHeadAbstract.RealizeInRoom();

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + self.ToString() + "'s Cut Head Object was Created");
    }

    static void DecapitationBloodEmitter(Lizard self)
    {
        self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(25f, 20f), UnityEngine.Random.Range(3f, 6f)));
        self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(7f, 16f)));
        self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(11f, 26f)));
    }

    public static void EyeCut(Lizard self, string Eye)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || !graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2))
        {
            return;
        }

        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[0].pos);

        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        string text = self.Template.type.ToString();

        float r = data.sLeaser.sprites[data2.EyesSprites].color.r;
        float g = data.sLeaser.sprites[data2.EyesSprites].color.g;
        float b = data.sLeaser.sprites[data2.EyesSprites].color.b;

        float r2 = data.sLeaser.sprites[data2.EyesSprites + 1].color.r;
        float g2 = data.sLeaser.sprites[data2.EyesSprites + 1].color.g;
        float b2 = data.sLeaser.sprites[data2.EyesSprites + 1].color.b;

        float eyeColourR;
        float eyeColourG;
        float eyeColourB;

        if (Eye == "EyeRight")
        {
            eyeColourR = r;
            eyeColourG = g;
            eyeColourB = b;
        }
        else
        {
            eyeColourR = r2;
            eyeColourG = g2;
            eyeColourB = b2;
        }

        float lizBloodColourR = -1f;
        float lizBloodColourG = -1f;
        float lizBloodColourB = -1f;

        if (bloodcolours != null && ShadowOfOptions.blood.Value)
        {
            lizBloodColourR = bloodcolours[self.Template.type.ToString()].r;
            lizBloodColourG = bloodcolours[self.Template.type.ToString()].g;
            lizBloodColourB = bloodcolours[self.Template.type.ToString()].b;
        }

        if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
            EyeCutBloodEmitter(self, new Color(lizBloodColourR, lizBloodColourG, lizBloodColourB));

        float r3;
        float g3;
        float b3;

        LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

        if (text == "Salamander")
        {
            r3 = graphicsModule.SalamanderColor.r;
            g3 = graphicsModule.SalamanderColor.g;
            b3 = graphicsModule.SalamanderColor.b;
        }
        else
        {
            r3 = graphicsModule.effectColor.r;
            g3 = graphicsModule.effectColor.g;
            b3 = graphicsModule.effectColor.b;
        }

        LizCutEyeAbstract lizCutEyeAbstract = new(self.room.world, pos, self.room.game.GetNewID())
        {
            LizColourR = r3,
            LizColourG = g3,
            LizColourB = b3,

            LizBloodColourR = lizBloodColourR,
            LizBloodColourG = lizBloodColourG,
            LizBloodColourB = lizBloodColourB,

            EyeColourR = eyeColourR,
            EyeColourG = eyeColourG,
            EyeColourB = eyeColourB
        };

        self.room.abstractRoom.AddEntity(lizCutEyeAbstract);
        lizCutEyeAbstract.RealizeInRoom();

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + self.ToString() + "'s Cut Eye Object was Created");
    }

    static void EyeCutBloodEmitter(Lizard self, Color colour)
    {
        self.room.AddObject(new BloodParticle(self.bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(5f, 10f)), colour, self.Template.type.value, null, 2.3f));
    }

    void AIUpdate(On.LizardAI.orig_Update orig, LizardAI self)
    {
        orig.Invoke(self);

        if (self != null && self.redSpitAI != null && self.redSpitAI.spitting && self.lizard != null)
        {
            self.lizard.EnterAnimation(Lizard.Animation.Spit, false);
        }
    }

    void LizardDie(On.Creature.orig_Die orig, Creature self)
    {
        try
        {
            Lizard liz = (self is Lizard lizard) ? lizard : null;

            if (liz != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !liz.dead)
            {
                if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
                {
                    SpiderTransformation.BabyPuff(liz);
                }

                if (self.abstractCreature.world.game.IsStorySession && UnityEngine.Random.Range(0, 100) < data.cheatDeathChance)
                {
                    self.dead = true;
                    self.LoseAllGrasps();

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " Cheated Death");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value && data.Beheaded == true)
                    {
                        data.cheatDeathChance += 50;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " Cheated Death while Decapitated!!!");
                    }

                    if (self.killTag != null && self.killTag.realizedCreature != null)
                    {
                        Room room = self.room;
                        room ??= self.abstractCreature.Room.realizedRoom;

                        if (room != null && room.socialEventRecognizer != null)
                        {
                            room.socialEventRecognizer.Killing(self.killTag.realizedCreature, self);
                        }
                    }
                }
                else
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " failed to cheat death");

                    orig.Invoke(self);
                }
            }
            else
            {
                orig.Invoke(self);
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void LizardUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig.Invoke(self, eu);

        try
        {
            if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
            {
                return;
            }

            if (ShadowOfOptions.deafen.Value && data.liz.TryGetValue("EarRight", out _))
            {
                bool flag = data.liz["EarRight"] == "Deaf";
                bool flag2 = data.liz["EarLeft"] == "Deaf";

                if (flag && flag2 && self.deaf < 120)
                {
                    self.deaf = 120;
                }
                else if ((flag ^ flag2) && self.deaf < 4)
                {
                    self.deaf = 4;
                }
            }

            if (ShadowOfOptions.dismemberment.Value && self.LizardState != null && self.LizardState.limbHealth != null)
            {
                for (int i = 0; i < data.ArmState.Count; i++)
                {
                    if (data.ArmState[i] != "Normal" && data.ArmState[i] != "Spider")
                    {
                        self.LizardState.limbHealth[i] = 0f;
                    }
                }
            }

            //Worm Grass Immunity Loss
            /*
            if (ShadowOfOptions.grass_immune.Value && self.AI != null && self.AI.behavior != null && self.AI.behavior == LizardAI.Behavior.Flee && self.room != null && self.room.updateList != null && 
                self.room.updateList.Any((UpdatableAndDeletable x) => x is WormGrass))
            {
                self.Template.wormGrassImmune = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " lost immunity to Worm Grass due to being Scared near Worm Grass");
            }
            */

            if (ShadowOfOptions.eat_regrowth.Value && self.enteringShortCut.HasValue && self.room != null && self.room.shortcutData(self.enteringShortCut.Value).shortCutType != null &&
            self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.CreatureHole && self.grasps[0] != null)
            { 
                if (data.denCheck == false)
                {
                    data.denCheck = true;
                    EatRegrowth(self, data);
                }
            }
            else
            {
                data.denCheck = false;
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void EatRegrowth(Lizard self, LizardData data)
    {
        Lizard liz = self.grasps[0].grabbed is Lizard lizard ? lizard : null;
        LizardData data2 = (liz != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData dat)) ? dat : null;

        #region Tongue
        if (ShadowOfOptions.tongue_stuff.Value && ShadowOfOptions.tongue_regrowth.Value && data.liz.TryGetValue("Tongue", out _) && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_regrowth_chance.Value)
        {
            if (self.grasps[0].grabbed is TubeWorm)
            {
                if (data.liz["Tongue"] == "False")
                {
                    data.liz["NewTongue"] = "Tube";
                    data.liz["Tongue"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " grew a new Tongue due to eating a " + self.grasps[0].grabbed);

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.cheatDeathChance += 10;
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating a " + self.grasps[0].grabbed + " because it already has one");
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && data2.liz.TryGetValue("Tongue", out string tongue2) && tongue2 == "True")
            {
                if (data.liz["Tongue"] == "False")
                {
                    data.liz["NewTongue"] = tongue2 != "Unknown" ? "get" : data2.liz["NewTongue"];
                    data.liz["Tongue"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " grew a new Tongue due to eating " + self.grasps[0].grabbed + " who had a Tongue");

                    data2.liz["NewTongue"] = "get";
                    data2.liz["Tongue"] = "False";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + liz.ToString() + " lost it's Tongue due to being eaten by " + self.grasps[0].grabbed + " that took it's Tongue");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.cheatDeathChance += 10;
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating " + self.grasps[0].grabbed + " because it already has one");
            }
        }
        #endregion

        #region Jump
        if (ShadowOfOptions.jump_stuff.Value && ShadowOfOptions.jump_regrowth.Value && data.liz.TryGetValue("CanJump", out _) && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.jump_regrowth_chance.Value)
        {
            if (self.grasps[0].grabbed is Yeek || self.grasps[0].grabbed is Cicada || self.grasps[0].grabbed is JetFish || (self.grasps[0].grabbed is Centipede centi && centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing))
            {
                if (data.liz["CanJump"] == "False")
                {
                    data.liz["CanJump"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating " + self.grasps[0].grabbed + " that had the ability to Jump");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.cheatDeathChance += 10;
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating " + self.grasps[0].grabbed + " because it already has one");
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && data2.liz.TryGetValue("CanJump", out _) && data2.liz["CanJump"] == "True")
            {
                if (data.liz["CanJump"] == "False")
                {
                    data.liz["CanJump"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating " + self.grasps[0].grabbed + " who had the ability to Jump");

                    data2.liz["CanJump"] = "False";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + liz.ToString() + " lost it's ability to Jump due to being eaten by " + self + " that took it's ability to Jump");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.cheatDeathChance += 10;
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating " + self.grasps[0].grabbed + " because it already has one");
            }
        }
        #endregion

        #region Melted
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_regrowth.Value && ShadowOfOptions.eat_lizard.Value && liz != null && data.transformation != "SpiderTransformation" && data.transformation != "ElectricTransformation" &&
            ((data2.transformation == "MeltedTransformation" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_regrowth_chance.Value) || (data2.transformation == "Melted" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_regrowth_chance.Value * 0.5)))
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

            return;
        }
        #endregion

        #region Electric
        if (ShadowOfOptions.electric_transformation.Value && ShadowOfOptions.electric_regrowth.Value && (data.transformation == "Null" && data.transformation == "Electric" && data.transformation == "Spider"))
        {
            if (data.transformation == "Electric")
            {
                if (ElectricChance())
                {
                    data.transformationTimer++;
                    return;
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "ElectricTransformation" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.electric_regrowth_chance.Value) ||
                        (data2.transformation == "Electric" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.electric_regrowth_chance.Value * 0.5)))
                {
                    data.transformationTimer++;
                    return;
                }
            }
            else if (data.transformation == "Null" || data.transformation == "Spider")
            {
                if (ElectricChance())
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made Electric due to eating " + self.grasps[0].grabbed);

                    data.transformation = "Electric";
                    data.transformationTimer = 1;

                    return;
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "ElectricTransformation" && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value) ||
                        (data2.transformation == "Electric" && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.5)))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made Electric due to eating " + self.grasps[0].grabbed);

                    data.transformation = "Electric";
                    data.transformationTimer = 1;

                    return;
                } 
            }

            bool ElectricChance()
            {
                return self.grasps[0].grabbed is JellyFish && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.25
                    || self.grasps[0].grabbed is Centipede centi && (centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SmallCentipede && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.5
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centipede && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 1.5
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedCentipede && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 2
                    || (ModManager.DLCShared && centi.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.AquaCenti && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.electric_regrowth_chance.Value * 2));
            }
        }
        #endregion

        #region Spider
        if (ShadowOfOptions.spider_transformation.Value && ShadowOfOptions.spider_regrowth.Value && data.transformation == "Null")
        {
            if (self.grasps[0].grabbed is BigSpider spid && (spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BigSpider && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.spider_regrowth_chance.Value * 0.5
                || spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.spider_regrowth_chance.Value
                || (ModManager.DLCShared && spid.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.spider_regrowth_chance.Value * 2)))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating " + self.grasps[0].grabbed);

                data.transformation = "Spider";
                data.transformationTimer = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                return;
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && ((data2.transformation == "SpiderTransformation" && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.spider_regrowth_chance.Value)
                    || (data2.transformation == "Spider" && UnityEngine.Random.Range(0, 100) <= ShadowOfOptions.spider_regrowth_chance.Value * 0.5)))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating " + self.grasps[0].grabbed);

                data.transformation = "Spider";
                data.transformationTimer = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                return;
            }
        }
        #endregion
    }

    void LimbSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
        try
        {
            if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                if (!lizardGoreStorage.TryGetValue(self.lizard.abstractCreature, out LizardGoreData _))
                {
                    return;
                }
                else
                {
                    if (!lizardGoreStorage.TryGetValue(self.lizard.abstractCreature, out LizardGoreData goreData))
                    {
                        return;
                    }

                    data = goreData.origLizardData;
                    goto Line1;
                }
            }

            data.sLeaser = sLeaser;
            data.rCam = rCam;

            if (data.Beheaded == true)
            {
                if (Futile.atlasManager.DoesContainElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut"))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 3].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[self.SpriteHeadStart + 3].element.name + "Cut");
                    sLeaser.sprites[self.SpriteHeadStart + 3].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                }
                else
                {
                    sLeaser.sprites[self.SpriteHeadStart + 3].isVisible = false;
                }

                sLeaser.sprites[self.SpriteHeadStart].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 1].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 2].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 4].isVisible = false;
                self.lizard.bodyChunks[0].collideWithObjects = false;
                if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && graphicstorage.TryGetValue(self, out GraphicsData data2))
                {
                    sLeaser.sprites[data2.EyesSprites].isVisible = false;
                    sLeaser.sprites[data2.EyesSprites + 1].isVisible = false;
                }

                for (int j = 0; j < self.cosmetics.Count; j++)
                {
                    if (self.cosmetics[j] is Whiskers || self.cosmetics[j] is AxolotlGills || self.cosmetics[j] is Antennae)
                    {
                        for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                        {
                            sLeaser.sprites[k].isVisible = false;
                        }
                    }
                }
            }
            else
            {
                float num7 = Mathf.Lerp(self.lastHeadDepthRotation, self.headDepthRotation, timeStacker);
                Vector2 val3 = Vector2.Lerp(Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker), Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker), 0.2f);
                Vector2 val4 = Custom.PerpendicularVector(Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker) - Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker));
                Vector2 normalized = val4.normalized;
                float num8 = Mathf.Lerp(self.lizard.lastJawOpen, self.lizard.JawOpen, timeStacker);

                if (self.lizard.JawReadyForBite && self.lizard.Consious)
                {
                    num8 += UnityEngine.Random.value * 0.2f;
                }

                num8 = Mathf.Lerp(num8, Mathf.Lerp(self.lastVoiceVisualization, self.voiceVisualization, timeStacker) + 0.2f, Mathf.Lerp(self.lastVoiceVisualizationIntensity, self.voiceVisualizationIntensity, timeStacker) * 0.8f);
                num8 = Mathf.Clamp(num8, 0f, 1f);
                float num9 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker), Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker));
                int num10 = 3 - (int)(Mathf.Abs(num7) * 3.9f);

                if (num10 < 0 || num10 > 3)
                {
                    num10 = 0;
                }

                if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out string teeth) && teeth != "Incompatible")
                {
                    if (data.liz["UpperTeeth"] != "Normal")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 2].element = Futile.atlasManager.GetElementWithName("LizardUpperTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[2].ToString() + data.liz["UpperTeeth"]);
                    }

                    if (data.liz["LowerTeeth"] != "Normal")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 1].element = Futile.atlasManager.GetElementWithName("LizardLowerTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[1].ToString() + data.liz["LowerTeeth"]);
                    }
                }

                if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && graphicstorage.TryGetValue(self, out GraphicsData graphData))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 4].element = Futile.atlasManager.GetElementWithName("LizardEyes" + num10 + "." + self.lizard.lizardParams.headGraphics[4] + "Nose");

                    for (int l = graphData.EyesSprites; l < graphData.EyesSprites + 2; l++)
                    {
                        sLeaser.sprites[l].scaleX = Mathf.Sign(num7) * self.lizard.lizardParams.headSize * self.iVars.headSize;
                        sLeaser.sprites[l].scaleY = self.lizard.lizardParams.headSize * self.iVars.headSize;
                        sLeaser.sprites[l].x = val3.x + normalized.x * num8 * num7 * self.lizard.lizardParams.jawOpenMoveJawsApart * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) - camPos.x;
                        sLeaser.sprites[l].y = val3.y + normalized.y * num8 * num7 * self.lizard.lizardParams.jawOpenMoveJawsApart * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) - camPos.y;
                        sLeaser.sprites[l].rotation = num9 + self.lizard.lizardParams.jawOpenAngle * (1f - self.lizard.lizardParams.jawOpenLowerJawFac) * num8 * num7;
                        sLeaser.sprites[l].SetAnchor(sLeaser.sprites[self.SpriteHeadStart + 4].anchorX, sLeaser.sprites[self.SpriteHeadStart + 4].anchorY);
                    }

                    string spriteName = "LizardEyes" + num10 + "." + self.lizard.lizardParams.headGraphics[4];

                    string spriteNameR = sLeaser.sprites[self.SpriteHeadStart + 4].scaleX > 0f ? "Right" + spriteName : "Left" + spriteName;
                    string spriteNameL = sLeaser.sprites[self.SpriteHeadStart + 4].scaleX > 0f ? "Left" + spriteName : "Right" + spriteName;

                    switch (data.liz["EyeRight"])
                    {
                        case "Blind":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.EyesSprites].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.EyesSprites].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                    }

                    switch (data.liz["EyeLeft"])
                    {
                        case "Blind":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[graphData.EyesSprites + 1].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Cut");
                            break;
                        default:
                            sLeaser.sprites[graphData.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[graphData.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                    }

                }

                if (data.liz.TryGetValue("Tongue", out _) && data.liz["NewTongue"] == "Tube" && self.lizard.tongue != null && self.lizard.tongue.Out)
                {
                    FSprite obj = sLeaser.sprites[self.SpriteTongueStart];

                    if (((TriangleMesh)((obj is TriangleMesh) ? obj : null)).verticeColors != null)
                    {
                        int num11 = 0;
                        while (true)
                        {
                            int num12 = num11;
                            FSprite obj2 = sLeaser.sprites[self.SpriteTongueStart];
                            if (num12 >= ((TriangleMesh)((obj2 is TriangleMesh) ? obj2 : null)).verticeColors.Length)
                            {
                                break;
                            }
                            FSprite obj3 = sLeaser.sprites[self.SpriteTongueStart];
                            ((TriangleMesh)((obj3 is TriangleMesh) ? obj3 : null)).verticeColors[num11] = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                            num11++;
                        }
                        sLeaser.sprites[self.SpriteTongueStart + 1].color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                    }
                    else
                    {
                        FSprite obj4 = sLeaser.sprites[self.SpriteTongueStart];
                        ((obj4 is TriangleMesh) ? obj4 : null).color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                        sLeaser.sprites[self.SpriteTongueStart + 1].color = Custom.HSL2RGB(0.95f, 1f, 0.865f);
                    }
                }
            }

        Line1:

            if (ShadowOfOptions.cut_in_half.Value)
                GoreLimbSprites(data.availableBodychunks, sLeaser, self, null, timeStacker, camPos);

            if (ShadowOfOptions.dismemberment.Value)
            {
                int num = self.SpriteLimbsColorStart - self.SpriteLimbsStart;
                for (int i = self.SpriteLimbsStart; i < self.SpriteLimbsEnd; i++)
                {
                    int num2 = i - self.SpriteLimbsStart;

                    if (data.ArmState[num2] == "Normal" || data.ArmState[num2] == "Spider")
                    {
                        continue;
                    }

                    string element = sLeaser.sprites[i].element.name;

                    if (element == "LizardArm_28A")
                        element = "LizardArm_28";

                    string cutNum = data.ArmState[num2] == "Cut1" ? "" : "3";
                    string cutColourNum = data.ArmState[num2] == "Cut1" ? "" : "3";

                    if (!Futile.atlasManager.DoesContainElementWithName(sLeaser.sprites[i].element + "Cut" + cutNum))
                    {
                        sLeaser.sprites[i].isVisible = false;
                        sLeaser.sprites[i + num].isVisible = false;
                        continue;
                    }

                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name + "Cut" + cutNum);

                    sLeaser.sprites[i + num].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i + num].element.name + "Cut" + cutColourNum);
                    if (bloodcolours != null)
                    {
                        sLeaser.sprites[i + num].color = bloodcolours[self.lizard.Template.type.ToString()];
                    }
                }
            }

            return;
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void GoreLimbSprites(List<int> availableBodychunks, SpriteLeaser sLeaser, LizardGraphics self, LizardGoreData lizardGoreData, float timeStacker, Vector2 camPos)
    {
        for (int i = 0; i < self.lizard.bodyChunks.Length; i++)
        {
            if (!availableBodychunks.Contains(i))
            {
                self.lizard.bodyChunks[i].collideWithObjects = false;
                self.lizard.bodyChunks[i].mass = 0f;
            }
        }

        /*
        if (lizardGoreData != null && availableBodychunks.Contains(0) && !availableBodychunks.Contains(1))
        {
            for (int i = 0; i < self.SpriteHeadStart; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }

            for (int i = self.SpriteHeadEnd; i < self.startOfExtraSprites; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }

            for (int j = 0; j < self.cosmetics.Count; j++)
            {
                if (self.cosmetics[j] is not Antennae && self.cosmetics[j] is not AxolotlGills && self.cosmetics[j] is not Whiskers)
                {
                    for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                    {
                        sLeaser.sprites[k].isVisible = false;
                    }
                }
            }
            return;
        }
        */

        if (!availableBodychunks.Contains(0))
        {
            for (int i = self.SpriteHeadStart; i < self.SpriteHeadEnd; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }

            for (int j = 0; j < self.cosmetics.Count; j++)
            {
                if (self.cosmetics[j] is Whiskers || self.cosmetics[j] is AxolotlGills || self.cosmetics[j] is Antennae)
                {
                    for (int k = self.cosmetics[j].startSprite; k < self.cosmetics[j].startSprite + self.cosmetics[j].numberOfSprites; k++)
                    {
                        sLeaser.sprites[k].isVisible = false;
                    }
                }
            }
        }

        if (!availableBodychunks.Contains(1))
        {
            if (self.limbs.Length == 4)
            {
                sLeaser.sprites[self.SpriteLimbsStart].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 1].isVisible = false;

                sLeaser.sprites[self.SpriteLimbsColorStart].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 1].isVisible = false;
            }
            else
            {
                sLeaser.sprites[self.SpriteLimbsStart].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 1].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 2].isVisible = false;

                sLeaser.sprites[self.SpriteLimbsColorStart].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 1].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 2].isVisible = false;
            }
        }
        if (!availableBodychunks.Contains(2))
        {
            sLeaser.sprites[self.SpriteTail].isVisible = false;
            if (self.limbs.Length == 4)
            {
                sLeaser.sprites[self.SpriteLimbsStart + 2].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 3].isVisible = false;

                sLeaser.sprites[self.SpriteLimbsColorStart + 2].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 3].isVisible = false;
            }
            else
            {
                sLeaser.sprites[self.SpriteLimbsStart + 3].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 4].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsStart + 5].isVisible = false;

                sLeaser.sprites[self.SpriteLimbsColorStart + 3].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 4].isVisible = false;
                sLeaser.sprites[self.SpriteLimbsColorStart + 5].isVisible = false;
            }
        }

        float num3 = 5f;
        Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
        vector = Vector2.Lerp(vector, Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker), 0.2f);
        float num4 = (Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f) + 1f) * 0.5f * Mathf.Pow(1f - self.lizard.AI.runSpeed, 2f);
        for (int j = 0; j < 4; j++)
        {
            int num5 = (j < 2) ? 1 : 2;
            Vector2 vector2 = self.BodyPosition(num5, timeStacker);
            if (self.lizard.animation == Lizard.Animation.ThreatSpotted || self.lizard.animation == Lizard.Animation.ThreatReSpotted)
            {
                vector2 += Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * 2f;
            }
            float num6 = self.BodyChunkDisplayRad(num5);
            if (num5 % 2 == 0)
            {
                num6 = (num6 + self.BodyChunkDisplayRad(num5 - 1)) / 2f;
            }
            num6 *= 1f + num4 * (float)(3 - num5) * 0.1f * ((num5 == 0) ? 0.5f : 1f);
            num6 *= self.iVars.fatness;
            Vector2 normalized = (vector - vector2).normalized;
            Vector2 a = Custom.PerpendicularVector(normalized);
            float d = Vector2.Distance(vector2, vector);

            if ((num5 == 1 || j == 2) && !availableBodychunks.Contains(1))
            {
                sLeaser.sprites[j].isVisible = false;
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 2, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 3, vector2 + normalized * d - a * num3 - camPos);
            }
            else if (num5 == 2 && !availableBodychunks.Contains(2))
            {
                sLeaser.sprites[j].isVisible = false;
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 2, vector2 + normalized * d - a * num3 - camPos);
                (sLeaser.sprites[self.SpriteBodyMesh] as TriangleMesh).MoveVertice(j * 4 + 3, vector2 + normalized * d - a * num3 - camPos);
            }
        }
    }
    //0 Front Most body circle

    //4 ???

    //5 Neck and whole body connection
    //6 Tail
    //7-10 Arms
    //11 Jaw
    //12 Lower Teeth
    //13 Upper Teeth
    //14 Head
    //15 Eyes
    //16-19 Arm Colour

    void LizardEyesInitiateSprites(On.LizardGraphics.orig_InitiateSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);

        try
        {
            if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
            {
                return;
            }

            if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _) && data.liz["UpperTeeth"] != "Incompatible")
            {
                if (self.lizard.lizardParams.headGraphics[4] != 0 && self.lizard.lizardParams.headGraphics[4] != 1 && self.lizard.lizardParams.headGraphics[4] != 2 && self.lizard.lizardParams.headGraphics[4] != 3)
                {
                    data.liz["UpperTeeth"] = "Incompatible";
                    data.liz["LowerTeeth"] = "Incompatible";

                    Debug.Log(all + "Teeth sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    Logger.LogError(all + "Teeth sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
            }

            if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && data.liz["EyeRight"] != "Incompatible")
            {
                if (self.lizard.lizardParams.headGraphics[4] != 0 && self.lizard.lizardParams.headGraphics[4] != 1 && self.lizard.lizardParams.headGraphics[4] != 2 && self.lizard.lizardParams.headGraphics[4] != 3)
                {
                    data.liz["EyeRight"] = "Incompatible";
                    data.liz["EyeLeft"] = "Incompatible";

                    Debug.Log(all + "Eye sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    Logger.LogError(all + "Eye sprites of " + self.lizard + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
                else
                {
                    if (self != null && !graphicstorage.TryGetValue(self, out GraphicsData _))
                    {
                        graphicstorage.Add(self, new GraphicsData());
                    }

                    if (graphicstorage.TryGetValue(self, out GraphicsData data2))
                    {
                        data2.EyesSprites = sLeaser.sprites.Length;
                        Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
                        sLeaser.sprites[data2.EyesSprites] = new FSprite("pixel", true);
                        sLeaser.sprites[data2.EyesSprites + 1] = new FSprite("pixel", true);
                        self.AddToContainer(sLeaser, rCam, null);

                        //Debug.Log(all + "Initiated Eye Sprites for " + self.lizard);
                    }
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void SpineSpikes_DrawSprites(On.LizardCosmetics.SpineSpikes.orig_DrawSprites orig, SpineSpikes self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        try
        {
            List<int> availableBodychunks;
            if (!lizardstorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardData data))
            {
                if (!lizardGoreStorage.TryGetValue(self.lGraphics.lizard.abstractCreature, out LizardGoreData goreData))
                {
                    return;
                }
                else
                {
                    availableBodychunks = goreData.availableBodychunks;
                }
            }
            else
            {
                availableBodychunks = data.availableBodychunks;
            }

            for (int i = self.startSprite + self.bumps - 1; i >= self.startSprite; i--)
            {

                //LizardGraphics.LizardSpineData lizardSpineData = self.lGraphics.SpinePosition(Mathf.Lerp(0.05f, self.spineLength / self.lGraphics.BodyAndTailLength, num), timeStacker);

                float num = Mathf.InverseLerp(self.startSprite, self.startSprite + self.bumps - 1, i);
                float num2 = Mathf.Lerp(0.05f, self.lGraphics.BodyAndTailLength, num);

                float num3 = self.lGraphics.bodyLength / 2;

                sLeaser.sprites[i].color = Color.white;
                if (self.colored > 0)
                {
                    sLeaser.sprites[i + self.bumps].color = Color.white;
                }

                if (!availableBodychunks.Contains(1) && num2 <= num3)
                {
                    sLeaser.sprites[i].isVisible = false;
                    if (self.colored > 0)
                    {
                        sLeaser.sprites[i + self.bumps].isVisible = false;
                    }
                }
                if (!availableBodychunks.Contains(2) && num2 > num3)
                {
                    sLeaser.sprites[i].isVisible = false;
                    if (self.colored > 0)
                    {
                        sLeaser.sprites[i + self.bumps].isVisible = false;
                    }
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void LizardEyesAddToContainer(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig.Invoke(self, sLeaser, rCam, newContatiner);

        if (!ShadowOfOptions.blind.Value || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("EyeRight", out _) || data.liz["EyeRight"] == "Incompatible" 
            || !graphicstorage.TryGetValue(self, out GraphicsData data2) || !(sLeaser.sprites.Length > data2.EyesSprites) || data2.EyesSprites == 0)
        {
            return;
        }

        //Debug.Log(all + "Added Eye Sprites to container for " + self.lizard);

        newContatiner ??= rCam.ReturnFContainer("Midground");
        newContatiner.AddChild(sLeaser.sprites[data2.EyesSprites]);
        newContatiner.AddChild(sLeaser.sprites[data2.EyesSprites + 1]);
        sLeaser.sprites[data2.EyesSprites].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
        sLeaser.sprites[data2.EyesSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
    }

    public static void PostViolenceCheck(Lizard receiver, string killType, Creature sender = null)
    {
        if (receiver != null && storedCreatureWasDead && receiver.dead)
        {
            if (lizardstorage.TryGetValue(receiver.abstractCreature, out LizardData data) && data.lastDamageType != "Melted" && killType != "Explosion")
            {
                data.lastDamageType = killType;
            }
            TryAddKillFeedEntry(receiver, killType, sender);
        }
    }

    public static void TryAddKillFeedEntry(Lizard receiver, string killType, Creature sender = null)
    {
        if (receiver == null || killType == null)
        {
            return;
        }

        if (lizardstorage.TryGetValue(receiver.abstractCreature, out LizardData data))
        {
            if (killType == "Bleed")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " died by Bleed. Bleed death is Converted to last damage taken: '" + data.lastDamageType + "'");

                TryAddKillFeedEntry(receiver, data.lastDamageType, sender);
            }

            if (data.transformation == "Null" || data.transformation == "Spider" || data.transformation == "Electric")
            {
                if (ShadowOfOptions.spider_transformation.Value && sender != null &&(killType == "Stab" || killType == "Blunt" || killType == "Bite") && data.transformation == "Null")
                {
                    CreatureTemplate.Type type = sender.abstractCreature.creatureTemplate.type;

                    if (type == CreatureTemplate.Type.Spider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 0.25
                        || type == CreatureTemplate.Type.BigSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 0.5
                        || type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value
                        || ModManager.DLCShared && type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 2
                        || sender is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2) && (data2.transformation == "SpiderTransformation" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value
                        || data2.transformation == "Spider" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 0.5))
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + receiver.ToString() + " was made a Spider Mother due to being killed by " + sender);

                        data.transformation = "Spider";
                        data.transformationTimer = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                        return;
                    }
                }

                if (ShadowOfOptions.melted_transformation.Value && killType == "Melted" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_transformation_chance.Value)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " was made Melted due to dying to Acid");

                    data.transformation = "Melted";
                    data.transformationTimer = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                    if (!data.liz.TryGetValue("MeltedR", out string _))
                    {
                        if (sender != null && sender is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2))
                        {
                            data.liz.Add("MeltedR", data2.liz["MeltedR"]);
                            data.liz.Add("MeltedG", data2.liz["MeltedG"]);
                            data.liz.Add("MeltedB", data2.liz["MeltedB"]);
                        }
                        else if (receiver.room.waterObject != null && receiver.room.waterObject.WaterIsLethal)
                        {
                            data.liz.Add("MeltedR", data.rCam.currentPalette.waterColor1.r.ToString());
                            data.liz.Add("MeltedG", data.rCam.currentPalette.waterColor1.g.ToString());
                            data.liz.Add("MeltedB", data.rCam.currentPalette.waterColor1.b.ToString());
                        }
                        else
                        {
                            data.liz.Add("MeltedR", "0.4078431");
                            data.liz.Add("MeltedG", "0.5843138");
                            data.liz.Add("MeltedB", "0.1843137");
                        }
                    }
                    else
                    {
                        if (sender != null && sender is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2))
                        {
                            data.liz["MeltedR"] = data2.liz["MeltedR"];
                            data.liz["MeltedG"] = data2.liz["MeltedG"];
                            data.liz["MeltedB"] = data2.liz["MeltedB"];
                        }
                        else if (receiver.room.waterObject != null && receiver.room.waterObject.WaterIsLethal)
                        {
                            data.liz["MeltedR"] = data.rCam.currentPalette.waterColor1.r.ToString();
                            data.liz["MeltedG"] = data.rCam.currentPalette.waterColor1.g.ToString();
                            data.liz["MeltedB"] = data.rCam.currentPalette.waterColor1.b.ToString();
                        }
                        else
                        {
                            data.liz["MeltedR"] = "0.4078431";
                            data.liz["MeltedG"] = "0.5843138";
                            data.liz["MeltedB"] = "0.1843137";
                        }
                    }

                    return;
                }

                if (ShadowOfOptions.electric_transformation.Value && killType == "Electric" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.electric_transformation_chance.Value)
                {
                    if (data.transformation == "Electric")
                    {
                        data.transformationTimer++;
                    }
                    else
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + receiver.ToString() + " was made Electric due to dying to Electricity");

                        data.transformation = "Electric";
                        data.transformationTimer = 1;
                    }
                    return;
                }
            }

            if (!(killType == "Fell"))
            {
                return;
            }

            int num = UnityEngine.Random.Range(0, 2);
            if (ShadowOfOptions.tongue_stuff.Value && num == 0)
            {
                if (receiver.tongue == null)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " grew a new Tongue due to Falling out of map");

                    if (!data.liz.TryGetValue("Tongue", out _))
                    {
                        data.liz.Add("Tongue", "True");
                        data.liz.Add("NewTongue", "get");
                    }
                    else
                    {
                        data.liz["Tongue"] = "True";
                        data.liz["NewTongue"] = "get";
                    }
                }
                else
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " did not grow a new Tongue due to Falling out of map because it already has one");
                }
            }
            else if (ShadowOfOptions.jump_stuff.Value && data.liz.TryGetValue("CanJump", out _) && num == 1 && receiver.jumpModule == null)
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " has gained the Jump ability due to Falling out of map");

                data.liz["CanJump"] = "True";
            }
            else if (ShadowOfOptions.jump_stuff.Value && num == 1 && receiver.jumpModule != null && ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + receiver.ToString() + " did not gain the Jump ability due to Falling out of map because it already can Jump");
        }
    }

    public static void PreViolenceCheck(Creature receiver)
    {
        storedCreatureWasDead = receiver == null || receiver.dead;
    }

    void ShockDeath(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
    {
        if (shockObj != null && shockObj is Lizard receiver)
        {
            PreViolenceCheck(receiver);
            orig.Invoke(self, shockObj);
            PostViolenceCheck(receiver, Creature.DamageType.Electric.ToString(), self);
        }
        else
        {
            orig.Invoke(self, shockObj);
        }
    }
}
