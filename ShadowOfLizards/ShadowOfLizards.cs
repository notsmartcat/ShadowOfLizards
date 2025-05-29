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
using System.Text;
using UnityEngine;
using static RoomCamera;

namespace ShadowOfLizards;

[BepInPlugin("notsmartcat.shadowoflizards", "Shadow of Lizards", "1.0.0")]
internal class ShadowOfLizards : BaseUnityPlugin
{
    public class LizardData
    {
        public Dictionary<string, string> liz;

        public SpriteLeaser sLeaser;
        public RoomCamera rCam;

        public string lastDamageType = "null";

        public List<List<Creature>> legSpiders = new() {new(), new(), new(), new(), new(), new()};

        public List<int> legSpidersFrame = new() {1, 3, 6, 8, 3, 5};

        public int spiderlegtimer = 0;

        public int Woundnum = 0;

        public float visualRadius;
        public float waterVision;
        public float throughSurfaceVision;

        public bool WasBlinded = false;

        public float directionmultiplier = 1f;

        public bool DenCheck = false;

        public bool GrassCheck = false;

        public List<int> availableBodychunks = new();
    }

    public class LizardGoreData
    {
        public LizardData origLizardData;

        public List<int> availableBodychunks = new();
    }

    public static readonly ConditionalWeakTable<AbstractCreature, LizardGoreData> lizardGoreStorage = new();

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
    }

    public class SpiderAsLeg
    {
        public Creature liz;
    }

    public class ElectricSpit
    {
        public bool Shocked = false;
    }

    public class OneTimeUseData
    {
        public List<Lizard> lizStorage = new();
    }


    public static readonly ConditionalWeakTable<Lizard, LizardData> lizardstorage = new();

    public static readonly ConditionalWeakTable<LizardGraphics, GraphicsData> graphicstorage = new();

    public static readonly ConditionalWeakTable<Spider, SpiderAsLeg> SpidLeg = new();

    public static readonly ConditionalWeakTable<LizardSpit, ElectricSpit> ShockSpit = new();

    public static readonly ConditionalWeakTable<PhysicalObject, OneTimeUseData> singleuse = new();


    public static bool storedCreatureWasDead = false;

    public static Dictionary<string, Color> bloodcolours;

    public static string all = "ShadowOf: ";

    public bool init = false;

    public static bool bloodModCheck = false;

    public static List<AbstractCreature> goreLizardList;
    private int startSprite;

    public void OnEnable()
    {
        Content.Register(new IContent[1] {new LizCutLegFisobs()});
        Content.Register(new IContent[1] {new LizCutHeadFisobs()});
        Content.Register(new IContent[1] {new LizCutEyeFisobs()});

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
        On.WormGrass.WormGrassPatch.Update += WormGrassKill;
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

        //On.LizardGraphics.ctor += LizardGraphics_ctor;
        On.LizardGraphics.AddCosmetic += LizardGraphics_AddCosmetic;

        On.LizardCosmetics.SpineSpikes.DrawSprites += SpineSpikes_DrawSprites;

        On.FlareBomb.Update += FlareBomb_Update;
        On.Explosion.Update += Explosion_Update;
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
                    if (self.deafen > 0f && self.room.physicalObjects[j][k] is Lizard liz)
                    {
                        //data2.lizStorage.Add(liz);

                        //liz.Deafen((int)Custom.LerpMap(num3, num * 1.5f * self.deafen, num * Mathf.Lerp(1f, 4f, self.deafen), 650f * self.deafen, 0f));
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
            if (self.room.abstractRoom.creatures[i].realizedCreature != null && (self.room.abstractRoom.creatures[i].rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.abstractRoom.creatures[i].rippleBothSides || self.abstractPhysicalObject.rippleBothSides) && (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 600f) || (Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.LightIntensity * 1600f) && self.room.VisualContact(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos))))
            {
                if (self.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz["EyeRight"] != "DefaultBlind" && !data2.lizStorage.Contains(liz)
                    && (int)Custom.LerpMap(Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.VisionPoint), 60f, 600f, 400f, 20f) > 300)
                {
                    data2.lizStorage.Add(liz);

                    bool flag = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "Cut";
                    bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";
                    if (!flag && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_chance.Value)
                    {
                        if (data.liz["EyeRight"] == "Normal")
                        {
                            data.liz["EyeRight"] = "Blind";
                            liz.Template.visualRadius -= data.visualRadius * 0.5f;
                            liz.Template.waterVision -= data.visualRadius * 0.5f;
                            liz.Template.throughSurfaceVision -= data.visualRadius * 0.5f;
                        }
                        else if (data.liz["EyeRight"] == "Scar")
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

    bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if (result.chunk != null && result.chunk.owner != null && result.chunk.owner is Lizard liz && ((lizardGoreStorage.TryGetValue(liz.abstractCreature, out LizardGoreData goreData) && !goreData.availableBodychunks.Contains(result.chunk.index))
            || (lizardstorage.TryGetValue(liz, out LizardData data) && !data.availableBodychunks.Contains(result.chunk.index))))
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

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lGraphics.lizard, out LizardData data) || data.liz["MeltedTransformation"] != "True")
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
            return (!(slugcatIndex == SlugcatStats.Name.Red) && (!ModManager.MSC || !(slugcatIndex == MoreSlugcatsEnums.SlugcatStatsName.Artificer))) ? (num + eatenobject.FoodPoints) : (num + 4 * eatenobject.FoodPoints);
        }
        return orig.Invoke(slugcatIndex, eatenobject);
    }

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

                        if (creature.realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz, out LizardData data) && data.liz["Beheaded"] == "False")
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + liz.ToString() + "'s Neck Hit by Debug");

                            data.liz["Beheaded"] = "True";
                            Decapitation(liz);
                            liz.Die();
                        }
                    }
                }

                if (self != null && self.room != null && self.room.game != null && self.room.game.devToolsActive && Input.GetKey("m"))
                {
                    List<AbstractCreature> list = new (self.abstractCreature.Room.creatures);
                    foreach (AbstractCreature creature in list)
                    {
                        if (creature.realizedCreature != null && creature.realizedCreature is Lizard liz)
                        {
                            if (lizardstorage.TryGetValue(liz, out LizardData data) && liz.lizardParams.tongue)
                            {
                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + liz.ToString() + "'s Mouth Hit by Debug");

                                data.liz["Tongue"] = "False";
                                liz.lizardParams.tongue = false;
                                liz.tongue.Retract();
                            }
                            if (liz.AI.redSpitAI != null)
                            {
                                liz.animation = Lizard.Animation.Standard;
                                liz.AI.behavior = LizardAI.Behavior.Frustrated;
                                liz.AI.modules.Remove(liz.AI.redSpitAI);
                                liz.AI.redSpitAI = null;
                                data.liz["CanSpit"] = "False";
                            }

                            if (data.liz["EyeLeft"] == "Normal")
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

        if (lizardGoreStorage.TryGetValue(abstractCreature, out LizardGoreData goreData))
        {
            self.Die();

            return;
        }

        if (self == null || self.abstractCreature == null || self.abstractCreature.state == null || self.abstractCreature.state.unrecognizedSaveStrings == null || (ShadowOfOptions.valid_lizards.Value && !IsLizardValid(self)))
        {
            return;
        }

        
        if (!lizardstorage.TryGetValue(self, out LizardData data))
        {
            lizardstorage.Add(self, new LizardData());
            lizardstorage.TryGetValue(self, out LizardData dat);
            data = dat;
        }

        self.abstractCreature.creatureTemplate = new CreatureTemplate(self.abstractCreature.creatureTemplate);
        self.lizardParams = LizBread(self);

        data.liz = self.abstractCreature.state.unrecognizedSaveStrings;

        data.visualRadius = self.Template.visualRadius;
        data.waterVision = self.Template.waterVision;
        data.throughSurfaceVision = self.Template.throughSurfaceVision;

        bool isStorySession = self.abstractCreature.world.game.IsStorySession;
        int cycleNumber = isStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

        //First Time Creating Check
        if (!data.liz.TryGetValue("Beheaded", out _))
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "First time creating " + self);

            data.liz.Add("ArmState0", "Normal");
            data.liz.Add("ArmState1", "Normal");
            data.liz.Add("ArmState2", "Normal");
            data.liz.Add("ArmState3", "Normal");
            data.liz.Add("ArmState4", "Normal");
            data.liz.Add("ArmState5", "Normal");
            data.liz.Add("Beheaded", "False");

            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                data.availableBodychunks.Add(i);
            }
        }
        else
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Not the first time creating " + self);
        }

        //Add Head Back if next cycle
        if (!data.liz.TryGetValue("BeheadedTime", out _))
        {
            data.liz.Add("BeheadedTime", "-1");
        }
        else if (data.liz["Beheaded"] == "True" && isStorySession && data.liz["BeheadedTime"] != self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString())
        {
            data.liz["Beheaded"] = "False";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " gained back it's head");
        }

        //Tongue
        if (!data.liz.TryGetValue("Tongue", out _))
        {
            if (self.lizardParams.tongue)
            {
                data.liz.Add("Tongue", "True");
                if (self.lizardParams.template == CreatureTemplate.Type.WhiteLizard)
                {
                    data.liz.Add("NewTongue", "0");
                }
                else if (self.lizardParams.template == CreatureTemplate.Type.Salamander)
                {
                    data.liz.Add("NewTongue", "1");
                }
                else if (self.lizardParams.template == CreatureTemplate.Type.BlueLizard)
                {
                    data.liz.Add("NewTongue", "2");
                }
                else if (self.lizardParams.template == CreatureTemplate.Type.CyanLizard)
                {
                    data.liz.Add("NewTongue", "3");
                }
                else if (self.lizardParams.template == CreatureTemplate.Type.RedLizard)
                {
                    data.liz.Add("NewTongue", "4");
                }
                else if (ModManager.DLCShared && self.lizardParams.template == DLCSharedEnums.CreatureTemplateType.ZoopLizard)
                {
                    data.liz.Add("NewTongue", "5");
                }
                else
                {
                    data.liz.Add("NewTongue", "get");
                }

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " has a Tongue");
            }
            else
            {
                data.liz.Add("Tongue", "False");
                data.liz.Add("NewTongue", "Get");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " does not have a Tongue");
            }
        }
        else if (ShadowOfOptions.tongue_stuff.Value)
        {
            if (self.lizardParams.tongue.ToString() != data.liz["Tongue"])
            {
                if (self.lizardParams.tongue)
                {
                    self.lizardParams.tongue = false;
                    self.tongue = null;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " does not have a Tongue");
                }
                else
                {
                    if (data.liz["NewTongue"] == "get")
                    {
                        int num = UnityEngine.Random.Range(0, 6);

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " got a new Tongue of preset of No. " + num);

                        data.liz["NewTongue"] = num.ToString();
                    }

                    BreedTongue(self);
                    self.tongue = new LizardTongue(self);

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " already has a Tongue of No. " + data.liz["NewTongue"]);
                }
            }
            else if (self.lizardParams.tongue.ToString() == data.liz["Tongue"] && data.liz["Tongue"] == "True")
            {
                self.tongue = null;
                if (data.liz["NewTongue"] == "get")
                {
                    int num2 = UnityEngine.Random.Range(0, 6);

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " got new Tongue of preset no. " + num2);

                    data.liz["NewTongue"] = num2.ToString();
                }

                BreedTongue(self);
                self.tongue = new LizardTongue(self);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " got it's Tongue replaced with a Tongue of No. " + data.liz["NewTongue"]);
            }
        }

        //Jump
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
        else if (ShadowOfOptions.jump_stuff.Value && (self.jumpModule != null).ToString() != data.liz["CanJump"])
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

        //WormGrass
        if (!data.liz.TryGetValue("Grass", out _))
        {
            if (!self.Template.wormGrassImmune)
            {
                data.liz.Add("Grass", "False");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is not Immune to Worm Grass");
            }
            else
            {
                data.liz.Add("Grass", "True");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is Immune to Worm Grass");
            }
        }
        else if (ShadowOfOptions.grass_immune.Value && self.Template.wormGrassImmune.ToString() != data.liz["Grass"])
        {
            if (data.liz["Grass"] == "True")
            {
                self.Template.wormGrassImmune = true;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " has immunity to Worm Grass");
            }
            else
            {
                self.Template.wormGrassImmune = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " does not have immunity to Worm Grass");
            }
        }

        //Eye Blind
        if (!data.liz.TryGetValue("EyeRight", out _))
        {
            if (self.Template.visualRadius == 0f)
            {
                data.liz.Add("EyeLeft", "DefaultBlind");
                data.liz.Add("EyeRight", "DefaultBlind");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is Blind");
            }
            else
            {
                data.liz.Add("EyeLeft", "Normal");
                data.liz.Add("EyeRight", "Normal");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is not Blind");
            }
        }
        else if (ShadowOfOptions.blind.Value)
        {
            bool flag = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "Cut";
            bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "Cut";

            if (flag && flag2)
            {
                self.Template.visualRadius = 0f;
                self.Template.waterVision = 0f;
                self.Template.throughSurfaceVision = 0f;
            }
            else
            {
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

        //Eye Blind
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
        else if (ShadowOfOptions.deafen.Value)
        {
            bool flag = data.liz["EarRight"] == "Deaf";
            bool flag2 = data.liz["EarLeft"] == "Deaf";

            if (flag && flag2)
            {
                self.Template.visualRadius = 0f;
                self.Template.waterVision = 0f;
                self.Template.throughSurfaceVision = 0f;
            }
            else
            {
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

        //Teeth
        if (!data.liz.TryGetValue("UpperTeeth", out string _))
        {
            data.liz.Add("UpperTeeth", "0");
            data.liz.Add("LowerTeeth", "0");
        }
        else if (ShadowOfOptions.teeth.Value)
        {
            bool flag = data.liz["UpperTeeth"] != "0" && data.liz["LowerTeeth"] != "0";

            if (flag)
            {
                self.lizardParams.biteDamageChance *= 0.5f;
                self.lizardParams.biteDominance *= 0.5f;
                self.lizardParams.biteDamage *= 1.1f;
                self.lizardParams.getFreeBiteChance *= 1.1f;
            }
            else if (data.liz["UpperTeeth"] != "0" || data.liz["LowerTeeth"] != "0")
            {
                self.lizardParams.biteDamageChance *= 0.5f;
                self.lizardParams.biteDominance *= 0.5f;
                self.lizardParams.biteDamage *= 1.1f;
                self.lizardParams.getFreeBiteChance *= 1.1f;
            }
        }

        //Spider Transformation
        if (!data.liz.TryGetValue("SpiderMother", out string _))
        {
            if (ShadowOfOptions.spider_transformation.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_spider_transformation_chance.Value)
            {
                data.liz.Add("SpiderMother", "True");
                data.liz.Add("SpiderMotherTime", isStorySession ? cycleNumber.ToString() : "1");
                data.liz.Add("SpiderTransformation", "True");
                data.liz.Add("SpiderNumber", UnityEngine.Random.Range(30, 55).ToString());

                data.liz["ArmState0"] = "Spider";
                data.liz["ArmState1"] = "Spider";
                data.liz["ArmState2"] = "Spider";
                data.liz["ArmState3"] = "Spider";
                data.liz["ArmState4"] = "Spider";
                data.liz["ArmState5"] = "Spider";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " gained the Spider Transformation due to Chance");
            }
            else
            {
                data.liz.Add("SpiderMother", "False");
                data.liz.Add("SpiderMotherTime", "0");
                data.liz.Add("SpiderTransformation", "False");
                data.liz.Add("SpiderNumber", "0");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is not Spider Mother");
            }
        }
        else if(ShadowOfOptions.spider_transformation.Value)
        {
            if (isStorySession && data.liz["SpiderMother"] == "True" && data.liz["SpiderTransformation"] == "False")
            {
                if (int.Parse(data.liz["SpiderMotherTime"]) <= cycleNumber - 3 || int.Parse(data.liz["SpiderMotherTime"]) >= cycleNumber + 3)
                {
                    data.liz["SpiderTransformation"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " has gained the Spider Transformation");
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    int num3 = int.Parse(data.liz["SpiderMotherTime"]) - cycleNumber;
                    string text = (num3 < 0) ? (num3 * -1).ToString() : num3.ToString();
                    Debug.Log(all + self.ToString() + " is Spider Mother for " + text + " cycle out of the required 3 cycles to gain the Spider Transformation");
                }
            }
            else if (data.liz["SpiderTransformation"] == "True")
            {
                for (int i = 0; i < 6; i++)
                {
                    if (data.liz["ArmState" + i] == "Cut1" || data.liz["ArmState" + i] == "Cut2")
                    {
                        data.liz["ArmState" + i] = "Spider";
                    }
                }

                data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();
            }
        }

        //Electric Transformation
        if (!data.liz.TryGetValue("Electric", out string _))
        {
            if (ShadowOfOptions.electric_transformation.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_electric_transformation_chance.Value && data.liz["SpiderMother"] == "False")
            {
                data.liz.Add("Electric", "True");
                data.liz.Add("ElectricCharge", "0");
                data.liz.Add("ElectricTransformation", "True");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " gained the Electric Transformation due to Chance");
            }
            else
            {
                data.liz.Add("Electric", "False");
                data.liz.Add("ElectricCharge", "0");
                data.liz.Add("ElectricTransformation", "False");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is not Electric");
            }
        }
        else if (ShadowOfOptions.electric_transformation.Value && data.liz["Electric"] == "True" && data.liz["ElectricTransformation"] == "False")
        {
            if (int.Parse(data.liz["ElectricCharge"]) <= 0)
            {
                data.liz["Electric"] = "False";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is no longer Electric due to running out of Charge.");
            }
            else if (int.Parse(data.liz["ElectricCharge"]) >= 3)
            {
                data.liz["ElectricTransformation"] = "True";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " has gained the Electric Transformation");
            }
        }

        //Melted Transformation
        if (!data.liz.TryGetValue("Melted", out string _))
        {
            if (ShadowOfOptions.melted_transformation.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_melted_transformation_chance.Value && data.liz["SpiderMother"] == "False" && data.liz["Electric"] == "False")
            {
                Color waterColour = (world != null && world.activeRooms[0] != null && world.activeRooms[0].waterObject != null && world.activeRooms[0].waterObject.WaterIsLethal && world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 != null) ? world.activeRooms[0].game.cameras[0].currentPalette.waterColor1 : new Color(0.4078431f, 0.5843138f, 0.1843137f);

                data.liz.Add("Melted", "True");
                data.liz.Add("PreMeltedTime", "False");
                data.liz.Add("MeltedTime", isStorySession ? cycleNumber.ToString() : "1");
                data.liz.Add("MeltedTransformation", "True");
                data.liz.Add("MeltedR", waterColour.r.ToString());
                data.liz.Add("MeltedG", waterColour.g.ToString());
                data.liz.Add("MeltedB", waterColour.b.ToString());

                self.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));

                self.abstractCreature.lavaImmune = true;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " gained the Melted Transformation due to Chance");
            }
            else
            {
                data.liz.Add("Melted", "False");
                data.liz.Add("PreMeltedTime", "False");
                data.liz.Add("MeltedTime", "0");
                data.liz.Add("MeltedTransformation", "False");
                data.liz.Add("MeltedR", "0.4078431");
                data.liz.Add("MeltedG", "0.5843138");
                data.liz.Add("MeltedB", "0.1843137");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " is not Melted");
            }
        }
        else if (ShadowOfOptions.melted_transformation.Value)
        {
            if (data.liz["Melted"] == "True" && data.liz["MeltedTransformation"] == "False" && isStorySession && (int.Parse(data.liz["MeltedTime"]) <= cycleNumber - 3 || int.Parse(data.liz["MeltedTime"]) >= cycleNumber + 3 || ShadowOfOptions.melted_transformation_skip.Value))
            {
                data.liz["MeltedTransformation"] = "True";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " has gained the Melted Transformation");
            }
            else if (data.liz["MeltedTransformation"] == "True")
            {
                self.abstractCreature.lavaImmune = true;
                self.Template.canSwim = true;

                if (self.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.WhiteLizard)
                {
                    self.effectColor = new Color(float.Parse(data.liz["MeltedR"]), float.Parse(data.liz["MeltedG"]), float.Parse(data.liz["MeltedB"]));
                }

                if (self.abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.AirOnly)
                {
                    self.abstractCreature.creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;
                }
            }
        }

        //Cheat Death Chance
        if (!data.liz.TryGetValue("LifeChance", out string _))
        {
            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
            {
                data.liz.Add("LifeChance", (UnityEngine.Random.Range(0, 101) + ShadowOfOptions.cheat_death_chance.Value).ToString());

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " got " + data.liz["LifeChance"] + " Chance to Cheat Death due to Dynamic Death Chance being on.");
            }
            else
            {
                data.liz.Add("LifeChance", ShadowOfOptions.cheat_death_chance.Value.ToString());

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " got a flat " + data.liz["LifeChance"] + " Chance to Cheat Death due to Dynamic Death Chance being off.");
            }
        }

        if (ShadowOfOptions.debug_logs.Value)
            Debug.Log(all + "Finished creating " + self);

        LizardCustomRelationsSet.Apply(self.Template.type, self);
    }

    void NewLizardGraphics(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (!ShadowOfOptions.melted_transformation.Value || !lizardstorage.TryGetValue(self.lizard, out LizardData data) || data.liz["MeltedTransformation"] != "True" || !ModManager.DLCShared || self.lizard.lizardParams.template != DLCSharedEnums.CreatureTemplateType.SpitLizard)
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
            maxMusclePower = lizardParams.maxMusclePower
        };
    }

    public static bool IsLizardValid(Lizard self)
    {
        CreatureTemplate.Type c = self.Template.type;

        if (c == CreatureTemplate.Type.BlackLizard || c == CreatureTemplate.Type.BlueLizard || c == CreatureTemplate.Type.GreenLizard || c == CreatureTemplate.Type.PinkLizard || c == CreatureTemplate.Type.RedLizard || c == CreatureTemplate.Type.Salamander
            || c == CreatureTemplate.Type.WhiteLizard || c == CreatureTemplate.Type.YellowLizard || c == CreatureTemplate.Type.CyanLizard || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.EelLizard 
            || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.SpitLizard || ModManager.MSC && c == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard || ModManager.DLCShared && c == DLCSharedEnums.CreatureTemplateType.ZoopLizard)
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
        try
        {
            if (!lizardstorage.TryGetValue(self.lizard, out LizardData value))
            {
                return;
            }

            if (value.liz.TryGetValue("CanSpit", out string _))
            {
                if (value.liz["CanSpit"] == "True" && self.redSpitAI == null)
                {
                    self.redSpitAI = new LizardAI.LizardSpitTracker(self);
                    self.AddModule(self.redSpitAI);

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " added Spit ability");
                }
                else if (value.liz["CanSpit"] == "False" && self.redSpitAI != null)
                {
                    self.modules.Remove(self.redSpitAI);
                    self.redSpitAI = null;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " removed Spit ability");
                }
            }
            else if (self.redSpitAI == null)
            {
                value.liz.Add("CanSpit", "False");
            }
            else if (self.redSpitAI != null)
            {
                value.liz.Add("CanSpit", "True");
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void GasLeak(On.LizardJumpModule.orig_Update orig, LizardJumpModule self)
    {
        orig.Invoke(self);
        try
        {
            if (lizardstorage.TryGetValue(self.lizard, out LizardData value) && self.gasLeakSpear != null && UnityEngine.Random.value * 100f < ShadowOfOptions.jump_stuff_chance.Value && value.liz["CanJump"] == "True")
            {
                value.liz["CanJump"] = "False";

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " lost the ability to Jump due to Gas Leak");

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                    value.liz["LifeChance"] = (float.Parse(value.liz["LifeChance"]) - 10f).ToString();
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void LizTongue(On.LizardTongue.orig_ctor orig, LizardTongue self, Lizard lizard)
    {
        if (ShadowOfOptions.tongue_stuff.Value && lizardstorage.TryGetValue(lizard, out LizardData value))
        {
            self.lizard = lizard;
            self.state = LizardTongue.State.Hidden;
            self.attachTerrainChance = 0.5f;
            self.pullAtChunkRatio = 1f;
            self.detatchMinDistanceTerrain = 20f;
            self.detatchMinDistanceCreature = 20f;
            self.totRExtraLimit = 40f;
            if (int.TryParse(value.liz["NewTongue"], out int _))
            {
                switch (int.Parse(value.liz["NewTongue"]))
                {
                    case 0:
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
                    case 1:
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
                    case 2:
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
                    case 3:
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
                    case 4:
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
                    case 5:
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
                }
            }
            else if (value.liz["NewTongue"] == "Tube")
            {
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
            }
            self.totR = self.range * 1.1f;
            self.graphPos = new Vector2[2];
        }
        else
        {
            orig.Invoke(self, lizard);
        }
    }

    void TongueCheck(On.LizardTongue.orig_Update orig, LizardTongue self)
    {
        if (ShadowOfOptions.tongue_stuff.Value && !self.lizard.lizardParams.tongue && self.state == LizardTongue.State.Hidden && !self.Out)
        {
            self.lizard.animation = Lizard.Animation.Standard;
            self.lizard.tongue = null;
        }
        else
        {
            orig.Invoke(self);
        }
    }

    void BreedTongue(Lizard self)
    {
        if (!lizardstorage.TryGetValue(self, out LizardData value))
        {
            return;
        }
        if (int.TryParse(value.liz["NewTongue"], out int _))
        {
            switch (int.Parse(value.liz["NewTongue"]))
            {
                case 0:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 440f;
                    self.lizardParams.tongueWarmUp = 80;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.1f;
                    break;
                case 1:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 150f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 1f / 3f;
                    break;
                case 2:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 140f;
                    self.lizardParams.tongueWarmUp = 10;
                    self.lizardParams.tongueSegments = 5;
                    self.lizardParams.tongueChance = 0.25f;
                    break;
                case 3:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 160f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 1f / 3f;
                    break;
                case 4:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 350f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.1f;
                    break;
                case 5:
                    self.lizardParams.tongue = true;
                    self.lizardParams.tongueAttackRange = 440f;
                    self.lizardParams.tongueWarmUp = 140;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.3f;
                    break;
            }
        }
        else if (value.liz["NewTongue"] == "Tube")
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Tube Tongue created");

            self.lizardParams.tongue = true;
            self.lizardParams.tongueAttackRange = 200f;
            self.lizardParams.tongueWarmUp = 0;
            self.lizardParams.tongueSegments = 20;
            self.lizardParams.tongueChance = 0.3f;
        }
    }

    bool HitHeadShield(On.Lizard.orig_HitHeadShield orig, Lizard self, Vector2 direction)
    {
        if (lizardstorage.TryGetValue(self, out LizardData value))
        {
            if (value.liz["Beheaded"] == "True")
            {
                return false;
            }
        }
        return orig.Invoke(self, direction);
    }

    void LizardBite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (ShadowOfOptions.teeth.Value || !lizardstorage.TryGetValue(self, out LizardData data) || data.lastDamageType == null)
        {
            orig.Invoke(self, chunk);
            return;
        }

        if (!data.liz.TryGetValue("UpperTeeth", out string upperTeeth))
        {
            data.liz.Add("UpperTeeth", "0");
            upperTeeth = "0";
        }
        if (!data.liz.TryGetValue("LowerTeeth", out string lowerTeeth))
        {
            data.liz.Add("LowerTeeth", "0");
            lowerTeeth = "0";
        }

        bool flag = upperTeeth != "0" && lowerTeeth != "0";

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
        else if (upperTeeth != "0" || lowerTeeth != "0")
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

        orig.Invoke(self, chunk);
    }

    void LimbDeath(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        try
        {
            if (!lizardstorage.TryGetValue(self, out LizardData data) || data.lastDamageType == null)
            {
                orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
                return;
            }

            bool sourceFlag = source != null && source.owner != null;

            bool sourcetypeFlag = (source == null && type.ToString() == "Explosion") || (source != null && (source.owner == null || (source.owner != null && source.owner is not JellyFish)));

            #region Electric
            if (sourceFlag && source.owner is ElectricSpear && data.liz["Electric"] == "True")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + source.owner.ToString() + "'s damage was halved due to Resistance on " + self);

                damage /= 2f;
            }
            else if (type == Creature.DamageType.Electric && data.liz["Electric"] == "True")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Electric damage was halved due to Resistance on " + self);

                damage /= 2f;

                if (data.liz["ElectricTransformation"] == "False")
                {
                    data.liz["ElectricCharge"] = (float.Parse(data.liz["ElectricCharge"]) + 1f).ToString();
                }
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
                if (data.liz["Beheaded"] != "True" && hitChunk.index == 0)
                {
                    if (LizHitHeadShield(directionAndMomentum.Value, self))
                    {
                        string eye = (UnityEngine.Random.Range(0, 2) == 0) ? "EyeRight" : "EyeLeft";
                        if (ShadowOfOptions.blind.Value && data.liz[eye] != "DefaultBlind" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_cut_chance.Value)
                        {
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
                        else if (ShadowOfOptions.teeth.Value && type != Creature.DamageType.Bite && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.teeth_chance.Value)
                        {
                            string teeth = UnityEngine.Random.Range(0, 2) == 0 ? "UpperTeeth" : "LowerTeeth";
                            int teethNum = UnityEngine.Random.Range(1, 5);

                            if (data.liz[teeth] == "0")
                            {
                                data.liz[teeth] = teethNum.ToString();
                                self.lizardParams.biteDamageChance *= 0.5f;
                                self.lizardParams.biteDominance *= 0.5f;
                                self.lizardParams.biteDamage *= 1.1f;
                                self.lizardParams.getFreeBiteChance *= 1.1f;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " " + (teeth == "UpperTeeth" ? "Upper " : "Lower ") + "Teeth were broken");

                                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                    data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) - 5f).ToString();

                                self.room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, self.firstChunk, false, Custom.LerpMap(source.vel.magnitude, 0f, 8f, 0.2f, 1f) + 10, 1f);
                            }
                        }

                        if (ShadowOfOptions.decapitation.Value && data.lastDamageType != "Melted" && type.ToString() == "Explosion")
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " had it's Head cut by an explosion");

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.decapitation_chance.Value * 0.5)
                            {
                                data.liz["Beheaded"] = "True";
                                Decapitation(self);
                                self.Die();
                            }
                        }
                    }
                    else if (LizHitInMouth(directionAndMomentum.Value, self))
                    {
                        if (ShadowOfOptions.tongue_stuff.Value && self.tongue != null && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " was hit in it's Mouth");

                            self.tongue.Retract();

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_stuff_chance.Value)
                            {
                                data.liz["Tongue"] = "False";
                                self.lizardParams.tongue = false;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " lost it's Tongue due to being hit in Mouth");
                            }
                            if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) - 10f).ToString();
                        }
                    }
                    else if (ShadowOfOptions.decapitation.Value && data.liz["Beheaded"] == "False" && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " was hit it's Neck");

                        if (HealthBasedChance(self, ShadowOfOptions.decapitation_chance.Value))
                        {
                            data.liz["Beheaded"] = "True";
                            Decapitation(self);
                            self.Die();
                        }
                    }
                }
            }
            else if (ShadowOfOptions.dismemberment.Value && (hitChunk.index != 1 && hitChunk.index != 2) && HealthBasedChance(self, ShadowOfOptions.dismemberment_chance.Value))
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

                    if (data.liz["ArmState" + num8] == "Normal")
                    {
                        EllLegCut("ArmState" + num8, "ArmState" + num9, num8);
                    }

                    return;
                }
                else if ((ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard) || (self.graphicsModule as LizardGraphics).limbs.Length == 6)
                {
                    if (hitChunk.index == 1)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;
                        int num9 = (num5 < 0f) ? 4 : 5;

                        if (UnityEngine.Random.value > 0.25 && data.liz["ArmState" + num8] == "Normal")
                        {
                            LegCut("ArmState" + num8, num8);
                        }
                        else if (data.liz["ArmState" + num9] == "Normal")
                        {
                            LegCut("ArmState" + num9, num9);
                        }

                        return;
                    }

                    num8 = (num5 < 0f) ? 0 : 1;
                    int num10 = (num5 < 0f) ? 5 : 4;

                    if (UnityEngine.Random.value > 0.25 && data.liz["ArmState" + num8] == "Normal")
                    {
                        LegCut("ArmState" + num8, num8);
                    }
                    else if (data.liz["ArmState" + num10] == "Normal")
                    {
                        LegCut("ArmState" + num10, num10);
                    }

                    return;
                }
                else
                {
                    if (hitChunk.index == 2)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;

                        if (data.liz["ArmState" + num8] == "Normal")
                        {
                            LegCut("ArmState" + num8, num8);
                        }

                        return;
                    }

                    num8 = (num5 < 0f) ? 0 : 1;

                    if (data.liz["ArmState" + num8] == "Normal")
                    {
                        LegCut("ArmState" + num8, num8);
                    }
                }
            }

            //Voids (and bools)
            void EllLegCut(string FirstLeg, string SecondLeg, int int1)
            {
                bool a = UnityEngine.Random.Range(0, 2) == 0;

                data.liz[FirstLeg] = a ? "Cut1" : "Cut2";
                data.liz[SecondLeg] = a ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.liz[FirstLeg]);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " limb cut " + int1);
            }

            void LegCut(string Leg, int int1)
            {
                data.liz[Leg] = UnityEngine.Random.Range(0, 2) == 0 ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.liz[Leg]);

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
                    data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) - (cut ? 10f : 5f)).ToString();

                if (cut)
                    EyeCut(self, eye);
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void CutInHalf(Lizard self, BodyChunk hitChunk)
    {
        if (!lizardstorage.TryGetValue(self, out LizardData data))
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

        if (!lizardstorage.TryGetValue(self, out LizardData lizData))
        {
            return;
        }

        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
            lizData.liz["LifeChance"] = (float.Parse(lizData.liz["LifeChance"]) - 5f).ToString();

        SpriteLeaser sLeaser = lizData.sLeaser;

        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[hitChunk.index].pos);
        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        string text2 = self.Template.type.ToString();

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

        string name = sLeaser.sprites[num17].element.name;
        string name2 = sLeaser.sprites[num18].element.name;
        string name3 = "LizardArm_";
        string name4 = "LizardArmColor_";
        int result = -1;

        if (name.StartsWith(name3) && (!int.TryParse(name.Substring(name3.Length), out result) || result < 0))
        {
            Debug.Log("Failed to get LizardArm_ number");
        }
        else
        {
            int result2 = -1;

            string spriteName = ((result < 10) ? ("LizardArm_0" + result) : ("LizardArm_" + result)) + "Cut";
            string spriteColourName = ((result < 10) ? ("LizardArmColor_0" + result) : ("LizardArmColor_" + result)) + "Cut";

            if (name2.StartsWith(name4) && (!int.TryParse(name2.Substring(name4.Length), out result2) || result2 < 0))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Failed to get LizardArm_ number");
            }
            else
            {
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
                    LizSpriteName = spriteName + (spriteVariant == "-1" ? "2" : "4"),
                    LizColourSpriteName = spriteColourName + (spriteVariant == "-1" ? "2" : "4"),
                    LizBreed = self.Template.type.value
                };

                self.room.abstractRoom.AddEntity(lizardCutLegAbstract);
                lizardCutLegAbstract.RealizeInRoom();

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "LizCutLeg Created");
            }
        }
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

        if (!lizardstorage.TryGetValue(self, out LizardData value) || value.sLeaser == null)
        {
            return;
        }

        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
            value.liz["LifeChance"] = (float.Parse(value.liz["LifeChance"]) - 50f).ToString();

        SpriteLeaser sLeaser = value.sLeaser;

        value.liz["BeheadedTime"] = (self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "-1");
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

        FSprite[] sprites = value.sLeaser.sprites;

        string headSprite0 = sprites[graphicsModule.SpriteHeadStart].element.name;
        string headSprite1 = sprites[graphicsModule.SpriteHeadStart + 1].element.name;
        string headSprite2 = sprites[graphicsModule.SpriteHeadStart + 2].element.name;
        string headSprite3 = sprites[graphicsModule.SpriteHeadStart + 3].element.name;
        string headSprite4 = sprites[graphicsModule.SpriteHeadStart + 4].element.name;
        string headSprite5 = null;
        string headSprite6 = null;

        float eyeRightColourR = 0f;
        float eyeRightColourG = 0f;
        float eyeRightColourB = 0f;

        float eyeLeftColourR = 0f;
        float eyeLeftColourG = 0f;
        float eyeLeftColourB = 0f;

        if (ShadowOfOptions.blind.Value && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData value2))
        {
            headSprite5 = value.sLeaser.sprites[value2.EyesSprites].element.name;
            headSprite6 = value.sLeaser.sprites[value2.EyesSprites + 1].element.name;
            eyeRightColourR = value.sLeaser.sprites[value2.EyesSprites].color.r;
            eyeRightColourG = value.sLeaser.sprites[value2.EyesSprites].color.g;
            eyeRightColourB = value.sLeaser.sprites[value2.EyesSprites].color.b;
            eyeLeftColourR = value.sLeaser.sprites[value2.EyesSprites + 1].color.r;
            eyeLeftColourG = value.sLeaser.sprites[value2.EyesSprites + 1].color.g;
            eyeLeftColourB = value.sLeaser.sprites[value2.EyesSprites + 1].color.b;
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
        if (!lizardstorage.TryGetValue(self, out LizardData value) || !graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData value2))
        {
            return;
        }

        IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[0].pos);

        WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

        string text = self.Template.type.ToString();

        float r = value.sLeaser.sprites[value2.EyesSprites].color.r;
        float g = value.sLeaser.sprites[value2.EyesSprites].color.g;
        float b = value.sLeaser.sprites[value2.EyesSprites].color.b;

        float r2 = value.sLeaser.sprites[value2.EyesSprites + 1].color.r;
        float g2 = value.sLeaser.sprites[value2.EyesSprites + 1].color.g;
        float b2 = value.sLeaser.sprites[value2.EyesSprites + 1].color.b;

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
        Lizard liz = (self is Lizard lizard) ? lizard : null;

        if (liz != null && lizardstorage.TryGetValue(liz, out LizardData value) && !liz.dead)
        {
            if ((ShadowOfOptions.spider_transformation.Value && value.liz["SpiderMother"] == "True") || value.liz["SpiderTransformation"] == "True")
            {
                SpiderTransformation.BabyPuff(liz);
            }

            if (self.abstractCreature.world.game.IsStorySession && UnityEngine.Random.Range(0, 100) < (float.TryParse(value.liz["LifeChance"], out float result) ? result : 0f))
            {
                self.dead = true;
                self.LoseAllGrasps();

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " Cheated Death");

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value && value.liz["Beheaded"] == "True")
                {
                    value.liz["LifeChance"] = (float.Parse(value.liz["LifeChance"]) + 50f).ToString();

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

    void LizardUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig.Invoke(self, eu);

        try
        {
            if (!lizardstorage.TryGetValue(self, out LizardData data))
            {
                return;
            }

            if (false && ShadowOfOptions.deafen.Value)
            {
                self.deaf = 0;
            }

            if (ShadowOfOptions.dismemberment.Value && self.LizardState != null && self.LizardState.limbHealth != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (data.liz["ArmState" + i] != "Normal" && data.liz["ArmState" + i] != "Spider")
                    {
                        self.LizardState.limbHealth[i] = 0f;
                    }
                }
            }
            
            //Worm Grass Immunity Loss
            if (ShadowOfOptions.grass_immune.Value && self.AI != null && self.AI.behavior != null && self.AI.behavior == LizardAI.Behavior.Flee && self.room != null && self.room.updateList != null && 
                self.room.updateList.Any((UpdatableAndDeletable x) => x is WormGrass))
            {
                self.Template.wormGrassImmune = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " lost immunity to Worm Grass due to being Scared near Worm Grass");
            }

            if (ShadowOfOptions.eat_regrowth.Value && self.enteringShortCut.HasValue && self.room != null && self.room.shortcutData(self.enteringShortCut.Value).shortCutType != null &&
            self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.CreatureHole && self.grasps[0] != null)
            { 
                if (data.DenCheck == false)
                {
                    data.DenCheck = true;
                    EatRegrowth(self, data);
                }
            }
            else
            {
                data.DenCheck = false;
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void EatRegrowth(Lizard self, LizardData data)
    {
        Lizard liz = self.grasps[0].grabbed is Lizard lizard ? lizard : null;
        LizardData data2 = (liz != null && lizardstorage.TryGetValue(liz, out LizardData dat)) ? dat : null;

        #region Tongue
        if (ShadowOfOptions.tongue_stuff.Value && ShadowOfOptions.tongue_regrowth.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_regrowth_chance.Value)
        {
            if (self.grasps[0].grabbed is TubeWorm)
            {
                if (data.liz["Tongue"] == "False")
                {
                    data.liz["NewTongue"] = "Tube";
                    data.liz["Tongue"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " grew a new Tongue due to eating a Grappling Worm");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) + 10f).ToString();
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating a Grappling Worm because it already has one");
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && data2.liz["Tongue"] == "True")
            {
                if (data.liz["Tongue"] == "False")
                {
                    data.liz["NewTongue"] = data2.liz["NewTongue"];
                    data.liz["Tongue"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " grew a new Tongue due to eating another Lizard that had a Tongue");

                    data2.liz["NewTongue"] = "get";
                    data2.liz["Tongue"] = "False";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + liz.ToString() + " lost it's Tongue due to being eaten by another Lizard that took it's Tongue");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) + 10f).ToString();
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating another Lizard because it already has one");
            }
        }
        #endregion

        #region Jump
        if (ShadowOfOptions.jump_stuff.Value && ShadowOfOptions.jump_regrowth.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.jump_regrowth_chance.Value)
        {
            if (self.grasps[0].grabbed is Yeek || self.grasps[0].grabbed is Cicada || self.grasps[0].grabbed is JetFish || (self.grasps[0].grabbed is Centipede centi && centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing))
            {
                if (data.liz["CanJump"] == "False")
                {
                    data.liz["CanJump"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating another creature that had the ability to Jump");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) + 10f).ToString();
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating another creature because it already has one");
            }
            else if (ShadowOfOptions.eat_lizard.Value && liz != null && data2.liz["CanJump"] == "True")
            {
                if (data.liz["CanJump"] == "False")
                {
                    data.liz["CanJump"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating another Lizard that had the ability to Jump");

                    data2.liz["CanJump"] = "False";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + liz.ToString() + " lost it's ability to Jump due to being eaten by another Lizard that took it's ability to Jump");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                        data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) + 10f).ToString();
                }
                else if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating another Lizard because it already has one");
            }
        }
        #endregion

        #region Melted
        if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_regrowth.Value && ShadowOfOptions.eat_lizard.Value && liz != null && 
            ((data2.liz["MeltedTransformation"] == "True" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_regrowth_chance.Value) ||
            (data2.liz["Melted"] == "True" && (UnityEngine.Random.Range(0, 100)) < ShadowOfOptions.melted_regrowth_chance.Value * 0.5)))
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " was made Melted due to eating another Lizard");

            data.liz["MeltedTime"] = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1";
            data.liz["Electric"] = "False";
            data.liz["Melted"] = "True";
            data.liz["SpiderMother"] = "False";

            data.liz["MeltedR"] = data2.liz["MeltedR"] ?? "0.4078431";
            data.liz["MeltedG"] = data2.liz["MeltedG"] ?? "0.5843138";
            data.liz["MeltedB"] = data2.liz["MeltedB"] ?? "0.1843137";

            if (ShadowOfOptions.melted_transformation_skip.Value)
                data.liz["MeltedTime"] = "-4";
            return;
        }
        #endregion

        #region Electric
        if(ShadowOfOptions.electric_transformation.Value)
        {

            if (ElectricChance())
            {
                data.liz["ElectricCharge"] = (float.Parse(data.liz["ElectricCharge"]) + 1f).ToString();
                return;
            }
            else if (ShadowOfOptions.eat_lizard.Value && (data.liz["Electric"] == "True") && (data.liz["ElectricTransformation"] == "False"))
            {
                if (liz != null && ((data2.liz["ElectricTransformation"] == "True" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.electric_regrowth_chance.Value) ||
                    (data2.liz["Electric"] == "True" && (UnityEngine.Random.Range(0, 100)) < ShadowOfOptions.electric_regrowth_chance.Value * 0.5)))
                {
                    data.liz["ElectricCharge"] = (float.Parse(data.liz["ElectricCharge"]) + 1f).ToString();
                    return;
                }
            }

            if (ShadowOfOptions.electric_transformation.Value && ShadowOfOptions.electric_regrowth.Value)
            {
                if (ElectricChance())
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made Electric due to eating a Centipede");

                    data.liz["ElectricCharge"] = "1";
                    data.liz["Electric"] = "True";
                    data.liz["Melted"] = "False";
                    data.liz["SpiderMother"] = "False";

                    if (ShadowOfOptions.electric_transformation_skip.Value)
                        data.liz["ElectricCharge"] = "999";

                    return;
                }
                else if (ShadowOfOptions.eat_lizard.Value)
                {
                    if (liz != null && ((data2.liz["ElectricTransformation"] == "True" && UnityEngine.Random.value * 100f <= ShadowOfOptions.electric_regrowth_chance.Value) ||
                        (data2.liz["Electric"] == "True" && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.5)))
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " was made Electric due to eating another Lizard");

                        data.liz["ElectricCharge"] = "1";
                        data.liz["Electric"] = "True";
                        data.liz["Melted"] = "False";
                        data.liz["SpiderMother"] = "False";

                        if (ShadowOfOptions.electric_transformation_skip.Value)
                            data.liz["ElectricCharge"] = "999";

                        return;
                    }
                }
            }

            bool ElectricChance()
            {
                return self.grasps[0].grabbed is JellyFish && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.25
                    || self.grasps[0].grabbed is Centipede centi && (centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SmallCentipede && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.electric_regrowth_chance.Value * 0.5
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centipede && UnityEngine.Random.value * 100f <= ShadowOfOptions.electric_regrowth_chance.Value
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.electric_regrowth_chance.Value * 1.5
                    || centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedCentipede && UnityEngine.Random.value * 100f <= ShadowOfOptions.electric_regrowth_chance.Value * 2
                    || (ModManager.DLCShared && centi.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.AquaCenti && UnityEngine.Random.value * 100f <= ShadowOfOptions.electric_regrowth_chance.Value * 2));
            }
        }
        #endregion

        #region Spider
        if (ShadowOfOptions.spider_transformation.Value && ShadowOfOptions.spider_regrowth.Value && data.liz["Electric"] != "True" && data.liz["Melted"] != "True" && data.liz["SpiderMother"] != "True")
        {
            if (self.grasps[0].grabbed is BigSpider spid && (spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BigSpider && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.spider_regrowth_chance.Value * 0.5
                || spid.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.value * 100f <= ShadowOfOptions.spider_regrowth_chance.Value
                || (ModManager.DLCShared && spid.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.value * 100f <= ShadowOfOptions.spider_regrowth_chance.Value * 2)))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating a big spider");

                data.liz["SpiderMotherTime"] = (self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1");
                data.liz["Electric"] = "False";
                data.liz["Melted"] = "False";
                data.liz["SpiderMother"] = "True";

                if (ShadowOfOptions.spider_transformation_skip.Value)
                    data.liz["SpiderMotherTime"] = "-30";

                return;
            }
            else if (ShadowOfOptions.eat_lizard.Value)
            {
                if (liz != null && ((data2.liz["SpiderTransformation"] == "True" && UnityEngine.Random.value * 100f <= ShadowOfOptions.spider_regrowth_chance.Value)
                    || (data2.liz["SpiderMother"] == "True" && (UnityEngine.Random.value * 100f) <= ShadowOfOptions.spider_regrowth_chance.Value * 0.5)))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " was made a Spider Mother due to eating another Lizard");

                    data.liz["SpiderMotherTime"] = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1";
                    data.liz["Electric"] = "False";
                    data.liz["Melted"] = "False";
                    data.liz["SpiderMother"] = "True";

                    if (ShadowOfOptions.spider_transformation_skip.Value)
                        data.liz["SpiderMotherTime"] = "-30";

                    return;
                }
            }
        }
        #endregion
    }

    void LimbSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
        try
        {
            if (!lizardstorage.TryGetValue(self.lizard, out LizardData data))
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

            float num5 = Mathf.Lerp(self.lastHeadDepthRotation, self.headDepthRotation, timeStacker);
            int num6 = 3 - (int)(Mathf.Abs(num5) * 3.9f);
            if (false && num6 < 0 || false && num6 > 3)
            {
                num6 = 0;
            }

            if (data.liz["Beheaded"] == "True")
            {
                sLeaser.sprites[self.SpriteHeadStart + 3].element = Futile.atlasManager.DoesContainElementWithName("LizardHead" + num6 + "." + self.lizard.lizardParams.headGraphics[3] + "Cut") ? Futile.atlasManager.GetElementWithName("LizardHead" + num6 + "." + self.lizard.lizardParams.headGraphics[3] + "Cut") : Futile.atlasManager.GetElementWithName("LizardHead" + num6 + "." + "0" + "Cut");
                sLeaser.sprites[self.SpriteHeadStart + 3].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                sLeaser.sprites[self.SpriteHeadStart].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 1].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 2].isVisible = false;
                sLeaser.sprites[self.SpriteHeadStart + 4].isVisible = false;
                self.lizard.bodyChunks[0].collideWithObjects = false;
                if (ShadowOfOptions.blind.Value && graphicstorage.TryGetValue(self, out GraphicsData value2))
                {
                    sLeaser.sprites[value2.EyesSprites].isVisible = false;
                    sLeaser.sprites[value2.EyesSprites + 1].isVisible = false;
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

                if (ShadowOfOptions.teeth.Value)
                {
                    if (!data.liz.TryGetValue("UpperTeeth", out string upperTeeth))
                    {
                        data.liz.Add("UpperTeeth", "0");
                    }
                    else if (upperTeeth != "0")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 2].element = Futile.atlasManager.GetElementWithName("LizardUpperTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[2].ToString() + "Broken" + data.liz["UpperTeeth"]);
                    }

                    if (!data.liz.TryGetValue("LowerTeeth", out string lowerTeeth))
                    {
                        data.liz.Add("LowerTeeth", "0");
                    }
                    else if (lowerTeeth != "0")
                    {
                        sLeaser.sprites[self.SpriteHeadStart + 1].element = Futile.atlasManager.GetElementWithName("LizardLowerTeeth" + num10 + "." + self.lizard.lizardParams.headGraphics[1].ToString() + "Broken" + data.liz["LowerTeeth"]);
                    }
                }

                if (ShadowOfOptions.blind.Value && graphicstorage.TryGetValue(self, out GraphicsData value3))
                {
                    sLeaser.sprites[self.SpriteHeadStart + 4].element = Futile.atlasManager.GetElementWithName("LizardEyes" + num10 + "." + self.lizard.lizardParams.headGraphics[4] + "Nose");

                    for (int l = value3.EyesSprites; l < value3.EyesSprites + 2; l++)
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
                            sLeaser.sprites[value3.EyesSprites].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[value3.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[value3.EyesSprites].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[value3.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[value3.EyesSprites].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[value3.EyesSprites].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Cut");
                            break;
                        default:
                            sLeaser.sprites[value3.EyesSprites].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites].element = Futile.atlasManager.GetElementWithName(spriteNameR + "Normal");
                            break;
                    }

                    switch (data.liz["EyeLeft"])
                    {
                        case "Blind":
                            sLeaser.sprites[value3.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                        case "Scar":
                            sLeaser.sprites[value3.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "BlindScar":
                            sLeaser.sprites[value3.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar");
                            break;
                        case "Scar2":
                            sLeaser.sprites[value3.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "BlindScar2":
                            sLeaser.sprites[value3.EyesSprites + 1].color = Color.white;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Scar2");
                            break;
                        case "Cut":
                            sLeaser.sprites[value3.EyesSprites + 1].color = (bloodcolours != null) ? bloodcolours[self.lizard.Template.type.ToString()] : self.effectColor;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Cut");
                            break;
                        default:
                            sLeaser.sprites[value3.EyesSprites + 1].color = sLeaser.sprites[self.SpriteHeadStart + 4].color;
                            sLeaser.sprites[value3.EyesSprites + 1].element = Futile.atlasManager.GetElementWithName(spriteNameL + "Normal");
                            break;
                    }

                }

                if (!(data.liz["NewTongue"] == "Tube") || self.lizard.tongue == null || !self.lizard.tongue.Out)
                {
                    return;
                }

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

        Line1:

            if (ShadowOfOptions.cut_in_half.Value)
                GoreLimbSprites(data.availableBodychunks, sLeaser, self, null, timeStacker, camPos);

            if (ShadowOfOptions.dismemberment.Value)
            {
                int num = self.SpriteLimbsColorStart - self.SpriteLimbsStart;
                for (int i = self.SpriteLimbsStart; i < self.SpriteLimbsEnd; i++)
                {
                    int num2 = i - self.SpriteLimbsStart;

                    if (data.liz["ArmState" + num2] == "Normal" || data.liz["ArmState" + num2] == "Spider")
                    {
                        continue;
                    }

                    Vector2 val = Vector2.Lerp(self.limbs[i - self.SpriteLimbsStart].lastPos, self.limbs[i - self.SpriteLimbsStart].pos, timeStacker);
                    int num3 = (i >= self.SpriteLimbsStart + 2) ? 2 : 0;

                    if (self.limbs.Length > 4)
                    {
                        num3 = Math.Min((int)((i - self.SpriteLimbsStart) / 2f), 2);
                    }

                    Vector2 val2 = Vector2.Lerp(self.drawPositions[num3, 1], self.drawPositions[num3, 0], timeStacker);
                    int num4 = (int)(Vector2.Distance(val, val2) / (4f * self.lizard.lizardParams.limbSize)) + 1;
                    num4 = Custom.IntClamp(num4, 1, 9);
                    num4 += 9 * (2 - (int)Mathf.Clamp(Mathf.Abs(self.limbs[i - self.SpriteLimbsStart].flip) * 3f, 0f, 2f));

                    if (i >= self.SpriteLimbsStart + 2)
                    {
                        num4 += 27;
                    }

                    string cutNum = data.liz["ArmState" + num2] == "Cut1" ? "" : "3";
                    string cutColourNum = data.liz["ArmState" + num2] == "Cut1" ? "" : "3";

                    if (num4 < 10)
                    {
                        sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName("LizardArm_0" + num4 + "Cut" + cutNum);
                    }
                    else
                    {
                        sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName("LizardArm_" + num4 + "Cut" + cutNum);
                    }

                    if (num4 < 10)
                    {
                        sLeaser.sprites[i + num].element = Futile.atlasManager.GetElementWithName("LizardArmColor_0" + num4 + "Cut" + cutColourNum);
                        if (bloodcolours != null)
                        {
                            sLeaser.sprites[i + num].color = bloodcolours[self.lizard.Template.type.ToString()];
                        }
                    }
                    else
                    {
                        sLeaser.sprites[i + num].element = Futile.atlasManager.GetElementWithName("LizardArmColor_" + num4 + "Cut" + cutColourNum);
                        if (bloodcolours != null)
                        {
                            sLeaser.sprites[i + num].color = bloodcolours[self.lizard.Template.type.ToString()];
                        }
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

    void SpineSpikes_DrawSprites(On.LizardCosmetics.SpineSpikes.orig_DrawSprites orig, SpineSpikes self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        try
        {
            List<int> availableBodychunks;
            if (!lizardstorage.TryGetValue(self.lGraphics.lizard, out LizardData data))
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

    void LizardEyesInitiateSprites(On.LizardGraphics.orig_InitiateSprites orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);

        try
        {
            if (ShadowOfOptions.blind.Value)
            {
                if (self != null && !graphicstorage.TryGetValue(self, out GraphicsData _))
                {
                    graphicstorage.Add(self, new GraphicsData());
                }

                if (graphicstorage.TryGetValue(self, out GraphicsData value2))
                {
                    value2.EyesSprites = sLeaser.sprites.Length;
                    Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
                    sLeaser.sprites[value2.EyesSprites] = new FSprite("pixel", true);
                    sLeaser.sprites[value2.EyesSprites + 1] = new FSprite("pixel", true);
                    self.AddToContainer(sLeaser, rCam, null);
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    void LizardEyesAddToContainer(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig.Invoke(self, sLeaser, rCam, newContatiner);

        if (ShadowOfOptions.blind.Value && graphicstorage.TryGetValue(self, out GraphicsData value) && sLeaser.sprites.Length > value.EyesSprites && value.EyesSprites != 0)
        {
            newContatiner ??= rCam.ReturnFContainer("Midground");
            newContatiner.AddChild(sLeaser.sprites[value.EyesSprites]);
            newContatiner.AddChild(sLeaser.sprites[value.EyesSprites + 1]);
            sLeaser.sprites[value.EyesSprites].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
            sLeaser.sprites[value.EyesSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[self.SpriteHeadStart + 4]);
        }
    }

    public static void PostViolenceCheck(Lizard receiver, string killType, Creature sender = null)
    {
        if (receiver != null && storedCreatureWasDead && receiver.dead)
        {
            if (lizardstorage.TryGetValue(receiver, out LizardData data) && data.lastDamageType != "Melted" && killType != "Explosion")
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

        if (lizardstorage.TryGetValue(receiver, out LizardData data))
        {
            if (killType == "Bleed")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " died by Bleed. Bleed death is Converted to last damage taken: '" + data.lastDamageType + "'");

                TryAddKillFeedEntry(receiver, data.lastDamageType, sender);
            }

            if (data.liz["Electric"] != "True" && data.liz["Melted"] != "True" && data.liz["SpiderMother"] != "True")
            {
                if (ShadowOfOptions.spider_transformation.Value && (killType == "Stab" || killType == "Blunt" || killType == "Bite") && sender != null)
                {
                    CreatureTemplate.Type type = sender.abstractCreature.creatureTemplate.type;

                    if ((type == CreatureTemplate.Type.Spider && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value * 0.25
                        || type == CreatureTemplate.Type.BigSpider && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value * 0.5
                        || type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value
                        || ModManager.DLCShared && type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value * 2
                        || sender is Lizard liz && (liz.abstractCreature.state.unrecognizedSaveStrings["SpiderTransformation"] == "True" && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value
                        || liz.abstractCreature.state.unrecognizedSaveStrings["SpiderMother"] == "True" && UnityEngine.Random.value * 100f < ShadowOfOptions.spider_transformation_chance.Value * 0.5)))
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + receiver.ToString() + " was made a Spider Mother due to being killed by " + sender);

                        data.liz["SpiderMotherTime"] = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1";
                        data.liz["Electric"] = "False";
                        data.liz["Melted"] = "False";
                        data.liz["SpiderMother"] = "True";

                        if (ShadowOfOptions.spider_transformation_skip.Value)
                            data.liz["SpiderMotherTime"] = "-30";

                        return;
                    }
                }

                if (ShadowOfOptions.melted_transformation.Value && killType == "Melted" && UnityEngine.Random.value * 100f < ShadowOfOptions.melted_transformation_chance.Value)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " was made Melted due to dying to Acid");

                    data.liz["MeltedTime"] = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "1";
                    data.liz["Electric"] = "False";
                    data.liz["Melted"] = "True";
                    data.liz["SpiderMother"] = "False";

                    if (sender != null && sender is Lizard liz && lizardstorage.TryGetValue(liz, out LizardData data2))
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

                    if (ShadowOfOptions.melted_transformation_skip.Value)
                        data.liz["MeltedTime"] = "-4";

                    return;
                }

                if (ShadowOfOptions.electric_transformation.Value && killType == "Electric" && UnityEngine.Random.value * 100f < ShadowOfOptions.electric_transformation_chance.Value)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " was made Electric due to dying to Electricity");

                    data.liz["ElectricCharge"] = "1";
                    data.liz["Electric"] = "True";
                    data.liz["Melted"] = "False";
                    data.liz["SpiderMother"] = "False";

                    if (ShadowOfOptions.electric_transformation_skip.Value)
                        data.liz["ElectricCharge"] = "999";

                    return;
                }
            }

            if (!(killType == "Fell"))
            {
                return;
            }

            int num = UnityEngine.Random.Range(0, 2);
            if (ShadowOfOptions.tongue_stuff.Value && num == 0 && receiver.tongue == null)
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " grew a new Tongue due to Falling out of map");

                data.liz["Tongue"] = "True";
                data.liz["NewTongue"] = "get";
            }
            else if (ShadowOfOptions.tongue_stuff.Value && num == 0 && receiver.tongue != null)
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " did not grow a new Tongue due to Falling out of map because it already has one");
            }
            else if (ShadowOfOptions.jump_stuff.Value && num == 1 && receiver.jumpModule == null)
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

    void WormGrassKill(On.WormGrass.WormGrassPatch.orig_Update orig, WormGrass.WormGrassPatch self)
    {
        if (ShadowOfOptions.grass_immune.Value)
        {
            List<Creature> list = new();
            for (int i = 0; i < self.trackedCreatures.Count; i++)
            {
                Creature creature = self.trackedCreatures[i].creature;
                if (creature is Lizard liz && lizardstorage.TryGetValue(liz, out LizardData data) && !data.GrassCheck)
                {
                    list.Add(creature);
                }
            }
            orig.Invoke(self);
            for (int j = 0; j < self.trackedCreatures.Count; j++)
            {
                Creature creature = self.trackedCreatures[j].creature;
                if (creature != null && creature.dead && list.Contains(creature) && UnityEngine.Random.value * 100f < ShadowOfOptions.grass_immune_chance.Value)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + "WormGrass Immune granted to " + creature);

                    Lizard key = (list[list.IndexOf(creature)] is Lizard liz) ? liz : null;

                    if (lizardstorage.TryGetValue(key, out LizardData data))
                    {
                        data.GrassCheck = true;
                        data.liz["LifeChance"] = (float.Parse(data.liz["LifeChance"]) - 0.05).ToString();
                        data.liz["Grass"] = "True";
                        data.lastDamageType = null;
                    }
                }
            }
        }
        else
        {
            orig.Invoke(self);
        }
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
