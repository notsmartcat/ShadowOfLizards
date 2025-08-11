using BepInEx;
using BepInEx.Logging;
using Fisobs.Core;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static RoomCamera;
using LizardCosmetics;

namespace ShadowOfLizards;

[BepInPlugin("notsmartcat.shadowoflizards", "Shadow of Lizards", "2.0.0")]
public class ShadowOfLizards : BaseUnityPlugin
{
    #region Classes
    public class LizardData
    {
        public bool beheaded = false;

        //Dictionaty stores most of the important values. they are first set inside the "NewLizard" Hook
        public Dictionary<string, string> liz = new();

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

        public List<int> cosmeticBodychunks = new() { 0, 1, 2 };

        //Transformation Related Values

        //This Ranges from the first stage oka "Melted" to the last stage aka "MeltedTransformation"
        public string transformation = "Start";

        /// <summary>
        //Transformation timers
        //The Spider Transformation one sets the value to the cycle number when the Transformation was gained and the Transformation progresses when 3 cycles have passed, If the Lizard get's rid of all Spiders that are inside it without dying or it dies and lives the Transformation is lost
        //The Electric Transformation sets this value to 1, each time the Lizard gets hit by electric damage or by eating electric creatures the value will go up by 1, if the value is grater then 3 then the Transformation progresses. if it goes to less then 1 the Transformation is lost
        //The Melted Transformation sets the value to the cycle number when the Transformation was gained and the Transformation progresses when 3 cycles have passed. the Transformation cannot be lost
        /// </summary>
        public int transformationTimer = -1;

        //Used to make sure AbstractCreature only makes changes once a cycle, set to the current cycle number after the AbstractCreature is finished making changes. AbstractCreature will only makes changes once in the Arena.
        public int lizardUpdatedCycle = -1;

        //Values for Lizard Legs, these will be added when the lizard is Created to make sure it has the exact same number of Legs as the Lizard
        public List<string> armState = new();

        //Chance for the Lizard to Cheat Death
        public int cheatDeathChance = 0;

        public List<Spear> spearList = new();

        public bool actuallyDead = false;

        public bool isGoreHalf = false;

        public bool wasDead = false;

        public Dictionary<int, int> cutAppendage = new();
        public Dictionary<int, int> cutAppendageCycle = new();
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
        public int eyeSprites;

        public int cutHalfSprites;

        public List<int> spiderLeg = new() { 0, 0, 0, 0, 0, 0 };

        public float legLength = 1;

        public int electricColorTimer = 0;

        public bool once = false;
        public bool camoOnce = false;    

        public float lightFlash;

        public LightSource lightSource;

        public float shockCharge = 0;

        public List<int> deadLeg = new();
    }
    public class SpiderAsLeg
    {
        public Creature liz;
    }
    public class ElectricSpit
    {
        public bool shocked = false;

        public Color origColor;

        public int electricColorTimer = 0;

        public bool once = false;

        public float lightFlash;

        public LightSource lightSource;
    }
    public class OneTimeUseData
    {
        public List<Lizard> lizStorage = new();
    }
    public class CreatureDenCheck
    {
        public bool denCheck = false;
    }
    #endregion

    #region ConditionalWeakTable
    public static readonly ConditionalWeakTable<AbstractCreature, LizardData> lizardstorage = new();
    public static readonly ConditionalWeakTable<LizardGraphics, GraphicsData> graphicstorage = new();
    public static readonly ConditionalWeakTable<Spider, SpiderAsLeg> spidLeg = new();
    public static readonly ConditionalWeakTable<LizardSpit, ElectricSpit> shockSpit = new();
    public static readonly ConditionalWeakTable<PhysicalObject, OneTimeUseData> singleUse = new();
    public static readonly ConditionalWeakTable<AbstractCreature, CreatureDenCheck> denCheck = new();
    #endregion

    #region Misc Values
    public static bool storedCreatureWasDead = false;

    public static Dictionary<string, Color> bloodcolours;

    public static string all = "ShadowOf: ";

    private bool init = false;

    public static bool bloodModCheck = false;

    public static List<AbstractCreature> goreLizardList;

    public static List<string> validTongues = new() { "WhiteLizard", "Salamander", "BlueLizard", "CyanLizard", "RedLizard"};

    public static List<string> defaultWaterBreather = new() { "Salamander", "EelLizard" };

    internal static new ManualLogSource Logger;

    public static ShadowOfOptions optionsMenuInstance;
    #endregion

    public void OnEnable()
    {
        try
        {
            Logger = base.Logger;

            #region Registering Content
            Content.Register(new IContent[1] { new LizCutLegFisobs() });
            Content.Register(new IContent[1] { new LizCutHeadFisobs() });
            Content.Register(new IContent[1] { new LizCutEyeFisobs() });
            #endregion

            #region Applying other hooks
            LizardHooks.Apply();
            LizardGraphicsHooks.Apply();
            LizardAIHooks.Apply();
            LizardSpitHooks.Apply();
            LizardTongueHooks.Apply();

            MiscHooks.Apply();

            ILHooks.Apply();

            //Transformations
            TransformationSpider.Apply();
            TransformationElectric.Apply();
            TransformationMelted.Apply();
            TransformationRot.Apply();
            #endregion

            On.RainWorld.OnModsInit += ModInit;
            On.RainWorldGame.ctor += BloodModCheck;

            On.Player.Update += DebugKeys;
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

                if (ModManager.DLCShared)
                    validTongues.Add("ZoopLizard");

                if (ModManager.Watcher)
                    validTongues.Add("IndigoLizard");
            }
            optionsMenuInstance = new ShadowOfOptions(this);
            MachineConnector.SetRegisteredOI("notsmartcat.shadowoflizards", optionsMenuInstance);
        }
        catch (Exception e) { Logger.LogError(e); }
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

        static void Blood()
        {
            bloodcolours = BloodData.Load();
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "BloodData loaded");
        }
    }

    #region Checks
    public static bool BloodColoursCheck(string Template)
    {
        return ShadowOfOptions.blood.Value && bloodcolours != null && bloodcolours.ContainsKey(Template);
    }

    public static bool CanCamoCheck(LizardData data, string Template)
    {
        return !data.liz.TryGetValue("CanCamo", out string CanCamo) && Template == "WhiteLizard" || CanCamo == "True";
    }

    public static bool RotModuleCheck(Lizard liz)
    {
        return !ModManager.Watcher || liz.LizardState.rotType == LizardState.RotType.None || liz.LizardState.rotType == LizardState.RotType.Slight;
    }
    #endregion

    void DebugKeys(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig.Invoke(self, eu);

        if (!ShadowOfOptions.debug_keys.Value || self == null || self.room == null || self.room.game == null || !self.room.game.devToolsActive)
        {
            return;
        }

        try
        {
            if (Input.GetKey("n"))
            {
                List<AbstractCreature> list = new(self.abstractCreature.Room.creatures);
                foreach (AbstractCreature creature in list)
                {
                    if (creature.realizedCreature == null)
                    {
                        continue;
                    }

                    if (creature.realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.beheaded == false && !data.isGoreHalf)
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + liz.ToString() + "'s Neck Hit by Debug");

                        data.beheaded = true;
                        Decapitation(liz);
                        liz.Die();
                    }
                }
            }

            if (Input.GetKey("m"))
            {
                List<AbstractCreature> list = new(self.abstractCreature.Room.creatures);
                foreach (AbstractCreature creature in list)
                {
                    if (creature.realizedCreature != null && creature.realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
                    {
                        if (ShadowOfOptions.tongue_ability.Value && data.liz.TryGetValue("Tongue", out _) && liz.lizardParams.tongue)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + liz.ToString() + "'s Mouth Hit by Debug");

                            data.liz["Tongue"] = "Null";
                            liz.lizardParams.tongue = false;
                            liz.tongue.Retract();
                        }

                        if (data.liz.TryGetValue("CanSpit", out _) && liz.AI.redSpitAI != null)
                        {
                            liz.animation = Lizard.Animation.Standard;
                            liz.AI.behavior = LizardAI.Behavior.Frustrated;
                            liz.AI.modules.Remove(liz.AI.redSpitAI);
                            liz.AI.redSpitAI = null;
                            data.liz["CanSpit"] = "False";
                        }

                        if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeLeft", out _) && data.liz["EyeLeft"] == "Normal")
                        {
                            data.liz["EyeLeft"] = "Cut";
                            self.Blind(5);

                            EyeCut(liz, "EyeLeft");
                        }

                        if (ShadowOfOptions.cut_in_half.Value && RotModuleCheck(liz) && data.availableBodychunks.Contains(1) && data.availableBodychunks.Contains(2))
                        {
                            CutInHalf(liz, data, liz.bodyChunks[1]);
                            liz.Die();
                        }
                    }
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    #region ViolenceCheck
    public static void PreViolenceCheck(Lizard receiver, LizardData data)
    {
        data.wasDead = receiver == null || receiver.dead;
    }

    public static void PostViolenceCheck(Lizard receiver, LizardData data, string killType, Creature sender = null)
    {
        if (receiver != null && data != null && receiver.abstractCreature != null && !data.wasDead && receiver.dead)
        {
            if (data.lastDamageType != "Melted" && killType != "Explosion")
            {
                data.lastDamageType = killType;
            }

            ViolenceCheck(receiver, data, killType, sender);
        }

        if (receiver != null && data != null && receiver.abstractCreature != null && !data.actuallyDead && data.wasDead && receiver.dead && killType == "Den")
        {
            receiver.Die();
        }
    }

    public static void ViolenceCheck(Lizard receiver, LizardData data, string killType, Creature sender = null)
    {
        if (receiver == null || receiver.abstractCreature == null || killType == null)
        {
            return;
        }
        if (killType == "Bleed")
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + receiver.ToString() + " died by Bleed. Bleed death is Converted to last damage taken: '" + data.lastDamageType + "'");

            ViolenceCheck(receiver, data, data.lastDamageType, sender);
        }

        if (data.transformation == "Null" || data.transformation == "Spider" || data.transformation == "Electric")
        {
            if (ShadowOfOptions.spider_transformation.Value && sender != null && data.transformation == "Null")
            {
                CreatureTemplate.Type type = sender.abstractCreature.creatureTemplate.type;
                string chanceText = "Spider Transformation after being killed by " + sender;

                if (type == CreatureTemplate.Type.Spider && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value * 0.25f, chanceText) || type == CreatureTemplate.Type.BigSpider && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value * 0.5f, chanceText) || type == CreatureTemplate.Type.SpitterSpider && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value, chanceText) || ModManager.DLCShared && type == DLCSharedEnums.CreatureTemplateType.MotherSpider && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value * 1.5f, chanceText) || sender is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2) && (data2.transformation == "SpiderTransformation" && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value, chanceText) || data2.transformation == "Spider" && Chance(receiver, ShadowOfOptions.spider_transformation_chance.Value * 0.5f, chanceText)))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " was made a Spider Mother due to being killed by " + sender);

                    data.transformation = "Spider";
                    data.transformationTimer = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                    return;
                }
            }

            if (ShadowOfOptions.melted_transformation.Value && killType == "Melted" && CWTCycleCheck(data, "PreMeltedCycle", CycleNum(receiver.abstractCreature)) && Chance(receiver, ShadowOfOptions.melted_transformation_chance.Value, "Melted Transformation after Dying to Acid"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " was made Melted due to dying to Acid");

                data.transformation = "Melted";
                data.transformationTimer = receiver.abstractCreature.world.game.IsStorySession ? receiver.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : 1;

                if (!data.liz.TryGetValue("MeltedR", out string _))
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
            else if (ShadowOfOptions.melted_transformation.Value && killType == "Melted")
            {
                data.liz["PreMeltedCycle"] = CycleNum(receiver.abstractCreature).ToString();
            }

            if (ShadowOfOptions.electric_transformation.Value && killType == "Electric" && Chance(receiver, ShadowOfOptions.electric_transformation_chance.Value, "Electric Transformation after Dying to Electricity"))
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

        List<string> fallList = new();

        if (ShadowOfOptions.tongue_ability.Value)
            fallList.Add("Tongue");

        if (ShadowOfOptions.jump_ability.Value)
            fallList.Add("Jump");

        if (ShadowOfOptions.climb_ability.Value)
        {
            fallList.Add("ClimbWall");
            fallList.Add("ClimbCeiling");
        }

        if (fallList.Count == 0)
        {
            return;
        }

        switch (fallList[UnityEngine.Random.Range(0, fallList.Count)])
        {
            case "tongue":
                if (receiver.tongue == null)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " grew a new Tongue due to Falling out of map");

                    data.liz["Tongue"] = "get";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + receiver.ToString() + " did not grow a new Tongue due to Falling out of map because it already has one");
                }
                break;
            case "Jump":
                if (receiver.jumpModule == null)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " has gained the Jump Ability due to Falling out of map");

                    data.liz["CanJump"] = "True";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + receiver.ToString() + " did not gain the Jump Ability due to Falling out of map because it already can Jump");
                }
                break;
            case "ClimbWall":
                if (receiver.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Wall].legality != PathCost.Legality.Allowed)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " has gained the Climb Walls Ability due to Falling out of map");

                    data.liz["CanClimbWall"] = "True";
                }
                else if (receiver.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Climb].legality != PathCost.Legality.Allowed)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " has gained the Climb Poles Ability due to Falling out of map");

                    data.liz["CanClimbPole"] = "True";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + receiver.ToString() + " did not gain either the Climb Walls or Climb Poles Ability due to Falling out of map because it already has both");
                }
                break;
            case "ClimbCeiling":
                if (receiver.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Ceiling].legality != PathCost.Legality.Allowed)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " has gained the Climb Ceiling Ability due to Falling out of map");

                    data.liz["CanClimbCeiling"] = "True";
                }
                else if (receiver.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Climb].legality != PathCost.Legality.Allowed)
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + receiver.ToString() + " has gained the Climb Poles Ability due to Falling out of map");

                    data.liz["CanClimbPole"] = "True";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + receiver.ToString() + " did not gain either the Climb Ceiling or Climb Poles Ability due to Falling out of map because it already has both");
                }
                break;
            default:
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " did not gain any Ability due to Falling out of map because none if the related Abilities were turned on");
                break;
        }
    }
    #endregion

    #region Misc
    void EliteLizard(Lizard self)
    {

    }

    public static bool Chance(Lizard self, float chance, string whatFor)
    {
        int roll = UnityEngine.Random.Range(0, 100);

        if (roll < chance)
        {
            if (ShadowOfOptions.chance_logs.Value)
                Debug.Log(all + self + " Success! " + roll + "/" + chance + " for " + whatFor);

            return true;
        }
        if (ShadowOfOptions.chance_logs.Value)
            Debug.Log(all + self + " Failure! " + roll + "/" + chance + " for " + whatFor);

        return false;
    }

    public static bool HealthBasedChance(Lizard self, float chance, string whatFor)
    {
        int roll = UnityEngine.Random.Range(0, 100);

        bool apply = ShadowOfOptions.health_based_chance.Value && (!ShadowOfOptions.health_based_chance_dead.Value || !self.dead);

        float rawMultiplier = apply ? Mathf.Lerp(ShadowOfOptions.health_based_chance_max.Value, ShadowOfOptions.health_based_chance_min.Value, self.LizardState.health) : 100;

        float multiplier = rawMultiplier / 100;

        chance *= multiplier;

        if (roll < chance)
        {
            if (ShadowOfOptions.chance_logs.Value)
                Debug.Log(all + self + " Success! " + roll + "/" + chance + " for Health Based Chance. Health Based Chance Multiplier is: " + rawMultiplier + "% for " + whatFor);

            return true;
        }
        if (ShadowOfOptions.chance_logs.Value)
            Debug.Log(all + self + " Failure! " + roll + "/" + chance + " for Health Based Chance. Health Based Chance Multiplier is: " + rawMultiplier + "% for " + whatFor);

        return false;
    }

    public static bool CWTCycleCheck(LizardData data, string name, int cycleNumber)
    {
        return !data.liz.TryGetValue(name, out string gotValue) || int.TryParse(gotValue, out int number) && number != cycleNumber;
    }

    public static void UnderwaterDen(LizardData data, Lizard self)
    {
        if (data.denCheck == false)
        {
            data.denCheck = true;
        }
        else
        {
            return;
        }

        List<string> list = new();

        if (ShadowOfOptions.swim_ability.Value)
            list.Add("CanSwim");
        if (ShadowOfOptions.water_breather.Value)
            list.Add("WaterBreather");

        if (list.Count == 0)
        {
            return;
        }

        switch (list[UnityEngine.Random.Range(0, list.Count)])
        {
            case "CanSwim":
                if (!data.liz.TryGetValue("CanSwim", out string CanSwim) && !defaultWaterBreather.Contains(self.Template.type.ToString()) || CanSwim != "True")
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the Swim Ability due to being brought to a Underwater Den");

                    data.liz["CanSwim"] = "True";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + self.ToString() + " did gained the Swim Ability due to being brought to a Underwater Den because it already can Swim");
                }
                break;
            case "WaterBreather":
                if (!data.liz.TryGetValue("WaterBreather", out string WaterBreather) && !defaultWaterBreather.Contains(self.Template.type.ToString()) || WaterBreather != "True")
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " gained the Drowning Immunity due to being brought to a Underwater Den");

                    data.liz["WaterBreather"] = "True";
                }
                else if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + self.ToString() + " did not gain the Drowning Immunity due to being brought to a Underwater Den because it already is Immune to Drowning");
                }
                break;
            default:
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " did not gain either the Climb Ceiling or Climb Poles due to Falling out of map because it already has both");
                break;
        }
    }

    public static void TemplatePathingUpdate(Lizard self, LizardData data)
    {     
        if (ShadowOfOptions.swim_ability.Value && data.liz.TryGetValue("CanSwim", out string CanSwim))
        {
            bool canSwim = CanSwim == "True";

            if (canSwim)
            {
                PathCost dropToWater = self.Template.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater];
                if (dropToWater.legality == PathCost.Legality.Unallowed || dropToWater.legality == PathCost.Legality.Unwanted)
                {
                    List<TileConnectionResistance> list2 = new()
                            {
                            new TileConnectionResistance(MovementConnection.MovementType.DropToWater, 20f, PathCost.Legality.Allowed)
                            };
                    for (int n = 0; n < list2.Count; n++)
                    {
                        self.Template.pathingPreferencesConnections[(int)list2[n].movementType] = list2[n].cost;
                    }
                }
            }
            else
            {
                PathCost dropToWater = self.Template.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater];
                if (dropToWater.legality == PathCost.Legality.Allowed)
                {
                    self.Template.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater].resistance *= 2;
                }
            }
        } //CanSwim: Set Here

        if (ShadowOfOptions.climb_ability.Value)
        {
            if (data.liz.TryGetValue("CanClimbPole", out string CanClimbPole))
            {
                bool canClimb = CanClimbPole == "True";

                if (canClimb)
                {
                    List<TileTypeResistance> list = new();
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Climb] = new LizardBreedParams.SpeedMultiplier(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.8f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Climb, 1f, PathCost.Legality.Allowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
                else
                {
                    List<TileTypeResistance> list = new();
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Climb] = new LizardBreedParams.SpeedMultiplier(0, 1f, 1f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Climb, 0f, PathCost.Legality.Unallowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
            }

            if (data.liz.TryGetValue("CanClimbWall", out string CanClimbWall))
            {
                bool canClimb = CanClimbWall == "True";

                if (canClimb)
                {
                    List<TileTypeResistance> list = new();
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall] = new LizardBreedParams.SpeedMultiplier(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.6f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Wall, 1f, PathCost.Legality.Allowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
                else
                {
                    List<TileTypeResistance> list = new();
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall] = new LizardBreedParams.SpeedMultiplier(0, 1f, 1f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Wall, 0f, PathCost.Legality.Unallowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
            }

            if (data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling))
            {
                bool canClimb = CanClimbCeiling == "True";

                if (canClimb)
                {
                    List<TileTypeResistance> list = new();

                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Ceiling] = new LizardBreedParams.SpeedMultiplier(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall].speed != 0f ? self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall].speed * 0.9f : self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.6f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Ceiling, 1.2f, PathCost.Legality.Allowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
                else
                {
                    List<TileTypeResistance> list = new();

                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Ceiling] = new LizardBreedParams.SpeedMultiplier(0, 1f, 1f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Ceiling, 0f, PathCost.Legality.Unallowed));

                    for (int l = 0; l < list.Count; l++)
                    {
                        self.Template.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                        if (self.Template.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                        {
                            self.Template.maxAccessibleTerrain = (int)list[l].accessibility;
                        }
                    }
                }
            }
        } //CanClimb: Set Here
    }

    public static int CycleNum(AbstractCreature self)
    {
        return self.world.game.IsStorySession ? self.world.game.GetStorySession.saveState.cycleNumber : -1;
    }

    public static Color CamoElectric(LizardGraphics self, GraphicsData data, Color col)
    {
        return Color.Lerp(Color.Lerp(col, new Color(0.7f, 0.7f, 1f), (float)(data.electricColorTimer / 50f)), self.whiteCamoColor, self.whiteCamoColorAmount);
    }

    #endregion

    #region Gore
    public static void CutInHalf(Lizard self, LizardData data, BodyChunk hitChunk)
    {
        try
        {
            IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[hitChunk.index].pos);
            WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

            int index = hitChunk.index != 1 && (!data.availableBodychunks.Contains(hitChunk.index + 1) || UnityEngine.Random.value < 0.5) ? hitChunk.index : hitChunk.index + 1;

            AbstractCreature abstractLizard = new(self.room.world, self.Template, null, pos, self.abstractCreature.ID);

            if (!lizardstorage.TryGetValue(abstractLizard, out LizardData data2))
            {
                lizardstorage.Add(abstractLizard, new LizardData());
                lizardstorage.TryGetValue(abstractLizard, out LizardData dat);
                data2 = dat;
            }

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= 50;

            abstractLizard.state.meatLeft = self.State.meatLeft / 2;
            self.State.meatLeft /= 2;

            data2.isGoreHalf = true;
            data2.transformation = data.transformation;
            data2.liz = new(data.liz);
            data2.availableBodychunks = new();
            data2.armState = new(data.armState);
            data2.actuallyDead = true;

            for (int i = index; i < self.bodyChunks.Count() && data.availableBodychunks.Contains(i); i++)
            {
                data2.availableBodychunks.Add(i);
                data.availableBodychunks.Remove(i);
            }

            self.room.abstractRoom.AddEntity(abstractLizard);

            abstractLizard.RealizeInRoom();

            if (ShadowOfOptions.blood_emitter.Value && BloodColoursCheck(self.Template.type.ToString()))
                BloodEmitter();

            if (false && ShadowOfOptions.blood_emitter.Value && abstractLizard.realizedCreature != null && BloodColoursCheck(self.Template.type.ToString()))
                OtherBloodEmitter();

            void BloodEmitter()
            {
                self.room.AddObject(new BloodEmitter(null, self.bodyChunks[index], UnityEngine.Random.Range(25f, 20f), UnityEngine.Random.Range(3f, 6f)));
                self.room.AddObject(new BloodEmitter(null, self.bodyChunks[index], UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(7f, 16f)));
                self.room.AddObject(new BloodEmitter(null, self.bodyChunks[index], UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(11f, 26f)));
            }

            void OtherBloodEmitter()
            {
                abstractLizard.realizedCreature.room.AddObject(new BloodEmitter(null, abstractLizard.realizedCreature.bodyChunks[index + 1], UnityEngine.Random.Range(25f, 20f), UnityEngine.Random.Range(3f, 6f)));
                abstractLizard.realizedCreature.room.AddObject(new BloodEmitter(null, abstractLizard.realizedCreature.bodyChunks[index + 1], UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(7f, 16f)));
                abstractLizard.realizedCreature.room.AddObject(new BloodEmitter(null, abstractLizard.realizedCreature.bodyChunks[index + 1], UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(11f, 26f)));
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void CutInHalfGraphics(LizardGraphics graphics, SpriteLeaser sLeaser, List<int> availableBodychunks, Vector2 camPos, float timeStacker)
    {
        try
        {
            float num3 = graphics.headConnectionRad * 0.5f + graphics.lizard.bodyChunkConnections[0].distance + graphics.lizard.bodyChunkConnections[1].distance / 2;

            for (int c = 0; c < graphics.cosmetics.Count; c++)
            {
                #region GetBackPos
                if (graphics.cosmetics[c] is BodyStripes bodyStripes)
                {
                    for (int i = bodyStripes.startSprite + bodyStripes.scalesPositions.Length - 1; i >= bodyStripes.startSprite; i--)
                    {
                        float num = Mathf.InverseLerp((float)bodyStripes.startSprite, (float)(bodyStripes.startSprite + bodyStripes.scalesPositions.Length - 1), (float)i);

                        float num2 = bodyStripes.scalesPositions[i - bodyStripes.startSprite].y;

                        float num33 = num3 / graphics.BodyAndTailLength;

                        if (!availableBodychunks.Contains(1) && num2 <= num33)
                        {
                            (sLeaser.sprites[i] as TriangleMesh).isVisible = false;
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num33)
                        {
                            (sLeaser.sprites[i] as TriangleMesh).isVisible = false;
                        }
                    }
                }
                else if (graphics.cosmetics[c] is LongShoulderScales longShoulderScales)
                {
                    for (int i = longShoulderScales.startSprite + longShoulderScales.scalesPositions.Length - 1; i >= longShoulderScales.startSprite; i--)
                    {
                        float num = Mathf.InverseLerp((float)longShoulderScales.startSprite, (float)(longShoulderScales.startSprite + longShoulderScales.scalesPositions.Length - 1), (float)i);

                        float num2 = longShoulderScales.scalesPositions[i - longShoulderScales.startSprite].y;

                        float num33 = num3 / graphics.BodyAndTailLength;

                        if (!availableBodychunks.Contains(1) && num2 <= num33)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (longShoulderScales.colored)
                            {
                                sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].isVisible = false;
                            }
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num33)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (longShoulderScales.colored)
                            {
                                sLeaser.sprites[i + longShoulderScales.scalesPositions.Length].isVisible = false;
                            }
                        }
                    }
                }
                else if (graphics.cosmetics[c] is ShortBodyScales shortBodyScales)
                {
                    for (int i = shortBodyScales.startSprite + shortBodyScales.scalesPositions.Length - 1; i >= shortBodyScales.startSprite; i--)
                    {
                        float num = Mathf.InverseLerp((float)shortBodyScales.startSprite, (float)(shortBodyScales.startSprite + shortBodyScales.scalesPositions.Length - 1), (float)i);

                        float num2 = shortBodyScales.scalesPositions[i - shortBodyScales.startSprite].y;

                        float num33 = num3 / graphics.BodyAndTailLength;

                        if (!availableBodychunks.Contains(1) && num2 <= num33)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (shortBodyScales.colored)
                            {
                                sLeaser.sprites[i + shortBodyScales.scalesPositions.Length].isVisible = false;
                            }
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num33)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (shortBodyScales.colored)
                            {
                                sLeaser.sprites[i + shortBodyScales.scalesPositions.Length].isVisible = false;
                            }
                        }
                    }
                }
                #endregion

                #region SpineLength
                else if (graphics.cosmetics[c] is BumpHawk bumpHawk)
                {
                    for (int i = bumpHawk.startSprite + bumpHawk.numberOfSprites - 1; i >= bumpHawk.startSprite; i--)
                    {
                        float num = Mathf.InverseLerp((float)bumpHawk.startSprite, (float)(bumpHawk.startSprite + bumpHawk.numberOfSprites - 1), (float)i);
                        float num2 = Mathf.Lerp(0.05f, bumpHawk.spineLength, num);

                        if (!availableBodychunks.Contains(1) && num2 <= num3)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num3)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                else if (graphics.cosmetics[c] is SpineSpikes spineSpikes)
                {
                    for (int i = spineSpikes.startSprite + spineSpikes.bumps - 1; i >= spineSpikes.startSprite; i--)
                    {
                        float num = Mathf.InverseLerp((float)spineSpikes.startSprite, (float)(spineSpikes.startSprite + spineSpikes.bumps) - 1, (float)i);
                        float num2 = Mathf.Lerp(0.05f, spineSpikes.spineLength, num);

                        if (!availableBodychunks.Contains(1) && num2 <= num3)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (spineSpikes.colored > 0)
                            {
                                sLeaser.sprites[i + spineSpikes.bumps].isVisible = false;
                            }
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num3)
                        {
                            sLeaser.sprites[i].isVisible = false;
                            if (spineSpikes.colored > 0)
                            {
                                sLeaser.sprites[i + spineSpikes.bumps].isVisible = false;
                            }
                        }
                    }
                }
                #endregion

                #region Hide if certain bodychunk is missing
                #region Chunk1
                else if (graphics.cosmetics[c] is LongHeadScales longHeadScales)
                {
                    if (!availableBodychunks.Contains(1))
                    {
                        for (int i = longHeadScales.startSprite; i < longHeadScales.startSprite + longHeadScales.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                else if (graphics.cosmetics[c] is JumpRings jumpRings)
                {
                    if (!availableBodychunks.Contains(1))
                    {
                        for (int i = jumpRings.startSprite; i < jumpRings.startSprite + jumpRings.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }

                }
                else if (graphics.cosmetics[c] is WingScales wingScales)
                {
                    if (!availableBodychunks.Contains(1))
                    {
                        for (int i = wingScales.startSprite; i < wingScales.startSprite + wingScales.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                #endregion

                #region Chunk2
                else if (graphics.cosmetics[c] is TailFin tailFin)
                {
                    if (!availableBodychunks.Contains(2))
                    {
                        for (int i = tailFin.startSprite; i < tailFin.startSprite + tailFin.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                else if (graphics.cosmetics[c] is TailGeckoScales tailGeckoScales)
                {
                    if (!availableBodychunks.Contains(2))
                    {
                        for (int i = tailGeckoScales.startSprite; i < tailGeckoScales.startSprite + tailGeckoScales.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                else if (graphics.cosmetics[c] is TailTuft tailTuft)
                {
                    if (!availableBodychunks.Contains(2))
                    {
                        for (int i = tailTuft.startSprite; i < tailTuft.startSprite + tailTuft.numberOfSprites; i++)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                    }
                }
                #endregion
                #endregion

                if (!ModManager.Watcher)
                {
                    continue;
                }

                else if (graphics.cosmetics[c] is Watcher.SkinkStripes skinkStripes)
                {
                    float s = -1;

                    float num4 = num3 / graphics.BodyAndTailLength;

                    if (!availableBodychunks.Contains(1))
                    {
                        for (int i = skinkStripes.segs - 1; i > -1; i--)
                        {
                            float num2 = Mathf.InverseLerp(0f, (float)skinkStripes.segs, (float)i);

                            if (num2 <= num4)
                            {
                                if (s == -1)
                                    s = Mathf.InverseLerp(0f, (float)skinkStripes.segs, (float)i + 1);

                                LizardGraphics.LizardSpineData lizardSpineData = skinkStripes.lGraphics.SpinePosition(s, timeStacker);
                                Vector2 pos = lizardSpineData.pos;
                                Vector2 perp = lizardSpineData.perp;
                                float rad = lizardSpineData.rad;

                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).MoveVertice(i * 2, pos - perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).MoveVertice(i * 2 + 1, pos + perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).MoveVertice(i * 2, pos - perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).MoveVertice(i * 2 + 1, pos + perp * rad - camPos);
                            }
                        }
                    }
                    else if (!availableBodychunks.Contains(2))
                    {
                        for (int i = 0; i < skinkStripes.segs; i++)
                        {
                            float num2 = Mathf.InverseLerp(0f, (float)skinkStripes.segs, (float)i);

                            if (num2 > num4)
                            {
                                if (s == -1)
                                    s = Mathf.InverseLerp(0f, (float)skinkStripes.segs, (float)i - 1);

                                LizardGraphics.LizardSpineData lizardSpineData = skinkStripes.lGraphics.SpinePosition(s, timeStacker);
                                Vector2 pos = lizardSpineData.pos;
                                Vector2 perp = lizardSpineData.perp;
                                float rad = lizardSpineData.rad;

                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).MoveVertice(i * 2, pos - perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite] as TriangleMesh).MoveVertice(i * 2 + 1, pos + perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).MoveVertice(i * 2, pos - perp * rad - camPos);
                                (sLeaser.sprites[skinkStripes.startSprite + 1] as TriangleMesh).MoveVertice(i * 2 + 1, pos + perp * rad - camPos);
                            }
                        }
                    }
                }
                else if (graphics.cosmetics[c] is Watcher.SkinkSpeckles skinkSpeckles)
                {
                    for (int i = 0; i < skinkSpeckles.spots; i++)
                    {
                        float num2 = skinkSpeckles.spotInfo[i].x;

                        float num4 = num3 / graphics.BodyAndTailLength;

                        if (!availableBodychunks.Contains(1) && num2 <= num4)
                        {
                            sLeaser.sprites[skinkSpeckles.startSprite + i].isVisible = false;
                        }
                        if (!availableBodychunks.Contains(2) && num2 > num4)
                        {
                            sLeaser.sprites[skinkSpeckles.startSprite + i].isVisible = false;
                        }
                    }
                }
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void LimbCut(Lizard self, LizardData data, BodyChunk hitChunk, int limbNum, string spriteVariant)
    {
        try
        {
            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= 5;

            LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

            self.LizardState.limbHealth[limbNum] = 0f;

            graphicsModule.limbs[limbNum].currentlyDisabled = true;

            SpriteLeaser sLeaser = data.sLeaser;

            IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[hitChunk.index].pos);
            WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

            string template = self.Template.type.ToString();

            string lizardArm = sLeaser.sprites[graphicsModule.SpriteLimbsStart + limbNum].element.name;
            string lizardArmColor = sLeaser.sprites[graphicsModule.SpriteLimbsColorStart + limbNum].element.name;

            if (lizardArm == "LizardArm_28A")
                lizardArm = "LizardArm_28";

            string lizardArmCut = lizardArm + (spriteVariant == "Cut1" ? "Cut2" : "Cut4"); ;
            string lizardArmColorCut = lizardArmColor + (spriteVariant == "Cut1" ? "Cut2" : "Cut4"); ;

            if (!Futile.atlasManager.DoesContainElementWithName(lizardArm + (spriteVariant == "Cut1" ? "Cut2" : "Cut4")))
            {
                lizardArmCut = "LizardArm_28Cut2";
                lizardArmColorCut = "LizardArmColor_28Cut2";

                Debug.Log(all + "LizCutLeg object could not be properily created due to the: " + lizardArm + " Leg Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
                Logger.LogError(all + "LizCutLeg object could not be properily created due to the: " + lizardArm + " Leg Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
            }

            LizCutLegAbstract lizCutLegAbstract = new(self.room.world, pos, self.room.game.GetNewID())
            {
                hue = 1f,
                saturation = 0.5f,

                scaleX = sLeaser.sprites[graphicsModule.SpriteLimbsStart + limbNum].scaleX,
                scaleY = sLeaser.sprites[graphicsModule.SpriteLimbsStart + limbNum].scaleY,

                breed = template,

                bodyColourR = graphicsModule.ivarBodyColor.r,
                bodyColourG = graphicsModule.ivarBodyColor.g,
                bodyColourB = graphicsModule.ivarBodyColor.b,

                bloodColourR = BloodColoursCheck(template) ? bloodcolours[template].r : -1f,
                bloodColourG = BloodColoursCheck(template) ? bloodcolours[template].g : -1f,
                bloodColourB = BloodColoursCheck(template) ? bloodcolours[template].b : -1f,

                effectColourR = self.effectColor.r,
                effectColourG = self.effectColor.g,
                effectColourB = self.effectColor.b,

                spriteName = lizardArmCut,
                colourSpriteName = lizardArmColorCut,

                blackSalamander = graphicsModule.blackSalamander,

                canCamo = CanCamoCheck(data, template)
            };

            self.room.abstractRoom.AddEntity(lizCutLegAbstract);
            lizCutLegAbstract.RealizeInRoom();

            if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                LimbCutBloodEmitter();

            if (graphicstorage.TryGetValue(graphicsModule, out GraphicsData data2))
                (lizCutLegAbstract.realizedObject as LizCutLeg).electricColorTimer = data2.electricColorTimer;

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "LizCutLeg Created");

            void LimbCutBloodEmitter()
            {
                self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(11f, 15f), UnityEngine.Random.Range(7f, 14f)));
                self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(9f, 20f)));
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void Decapitation(Lizard self)
    {
        try
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + " was Decapitated");

            if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || data.sLeaser == null)
            {
                return;
            }

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= 50;

            data.availableBodychunks.Remove(0);

            SpriteLeaser sLeaser = data.sLeaser;

            data.liz["beheadedCycle"] = self.abstractCreature.world.game.IsStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber.ToString() : "-1";
            IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[0].pos);
            WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

            string template = self.Template.type.ToString();

            LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

            int spriteHeadStart = graphicsModule.SpriteHeadStart;

            FSprite[] sprites = data.sLeaser.sprites;

            bool validHead = true;
            if (!Futile.atlasManager.DoesContainElementWithName(sprites[graphicsModule.SpriteHeadStart + 3].element.name + "Cut2"))
            {
                validHead = false;

                Debug.Log(all + "LizCutHead object could not be properily created due to the: " + sprites[graphicsModule.SpriteHeadStart + 3].element.name + " Head Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
                Logger.LogError(all + "LizCutHead object could not be properily created due to the: " + sprites[graphicsModule.SpriteHeadStart + 3].element.name + " Head Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
            }

            bool eyeCheck = graphicstorage.TryGetValue(graphicsModule, out GraphicsData data2) && ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible";

            LizCutHeadAbstract lizCutHeadAbstract = new(self.room.world, pos, self.room.game.GetNewID())
            {
                hue = 1f,
                saturation = 0.5f,

                scaleX = sLeaser.sprites[spriteHeadStart].scaleX,
                scaleY = sLeaser.sprites[spriteHeadStart].scaleY,

                breed = template,

                bodyColourR = graphicsModule.BodyColor(0f).r,
                bodyColourG = graphicsModule.BodyColor(0f).r,
                bodyColourB = graphicsModule.BodyColor(0f).r,

                effectColourR = self.effectColor.r,
                effectColourG = self.effectColor.g,
                effectColourB = self.effectColor.b,

                bloodColourR = BloodColoursCheck(template) ? bloodcolours[template].r : -1,
                bloodColourG = BloodColoursCheck(template) ? bloodcolours[template].g : -1,
                bloodColourB = BloodColoursCheck(template) ? bloodcolours[template].b : -1,

                eyeRightColourR = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites].color.r : 0,
                eyeRightColourG = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites].color.g : 0,
                eyeRightColourB = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites].color.b : 0,

                eyeLeftColourR = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites + 1].color.r : 0,
                eyeLeftColourG = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites + 1].color.g : 0,
                eyeLeftColourB = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites + 1].color.b : 0,

                headSprite0 = sprites[graphicsModule.SpriteHeadStart].element.name,
                headSprite1 = sprites[graphicsModule.SpriteHeadStart + 1].element.name,
                headSprite2 = sprites[graphicsModule.SpriteHeadStart + 2].element.name,
                headSprite3 = validHead ? sprites[graphicsModule.SpriteHeadStart + 3].element.name : "LizardHead0.1",
                headSprite4 = sprites[graphicsModule.SpriteHeadStart + 4].element.name,
                headSprite5 = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites].element.name : null,
                headSprite6 = eyeCheck ? data.sLeaser.sprites[data2.eyeSprites + 1].element.name : null,

                blackSalamander = graphicsModule.blackSalamander,

                rad = self.bodyChunks[0].rad,
                mass = self.bodyChunks[0].mass / 2,

                canCamo = CanCamoCheck(data, template),

                jawOpenAngle = self.lizardParams != null ? self.lizardParams.jawOpenAngle : 100,
                jawOpenMoveJawsApart = self.lizardParams != null ? self.lizardParams.jawOpenMoveJawsApart : 20
            };

            self.room.abstractRoom.AddEntity(lizCutHeadAbstract);
            lizCutHeadAbstract.RealizeInRoom();

            if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                BloodEmitter();

            if (graphicstorage.TryGetValue(graphicsModule, out GraphicsData data3))
                (lizCutHeadAbstract.realizedObject as LizCutHead).electricColorTimer = data3.electricColorTimer;

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + "'s Cut Head Object was Created");

            void BloodEmitter()
            {
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(25f, 20f), UnityEngine.Random.Range(3f, 6f)));
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(7f, 16f)));
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(11f, 26f)));
            }
        }
        catch (Exception e) { Logger.LogError(e); }
    }

    public static void EyeCut(Lizard self, string Eye)
    {
        try
        {
            if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || self.graphicsModule == null || !graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2))
            {
                return;
            }

            IntVector2 tilePosition = self.room.GetTilePosition(self.bodyChunks[0].pos);

            WorldCoordinate pos = new(self.room.abstractRoom.index, tilePosition.x, tilePosition.y, 0);

            LizardGraphics graphicsModule = (LizardGraphics)self.graphicsModule;

            bool isSalamander = self.Template.type.ToString() == "Salamander";

            bool bloodColour = BloodColoursCheck(self.Template.type.ToString());

            bool rightEye = Eye == "EyeRight";

            LizCutEyeAbstract lizCutEyeAbstract = new(self.room.world, pos, self.room.game.GetNewID())
            {
                bodyColourR = graphicsModule.BodyColor(0f).r,
                bodyColourG = graphicsModule.BodyColor(0f).g,
                bodyColourB = graphicsModule.BodyColor(0f).b,

                bloodColourR = bloodColour ? bloodcolours[self.Template.type.ToString()].r : isSalamander ? graphicsModule.SalamanderColor.r : graphicsModule.effectColor.r,
                bloodColourG = bloodColour ? bloodcolours[self.Template.type.ToString()].g : isSalamander ? graphicsModule.SalamanderColor.g : graphicsModule.effectColor.g,
                bloodColourB = bloodColour ? bloodcolours[self.Template.type.ToString()].b : isSalamander ? graphicsModule.SalamanderColor.b : graphicsModule.effectColor.b,

                eyeColourR = rightEye ? data.sLeaser.sprites[data2.eyeSprites].color.r : data.sLeaser.sprites[data2.eyeSprites + 1].color.r,
                eyeColourG = rightEye ? data.sLeaser.sprites[data2.eyeSprites].color.g : data.sLeaser.sprites[data2.eyeSprites + 1].color.g,
                eyeColourB = rightEye ? data.sLeaser.sprites[data2.eyeSprites].color.b : data.sLeaser.sprites[data2.eyeSprites + 1].color.b
            };

            self.room.abstractRoom.AddEntity(lizCutEyeAbstract);
            lizCutEyeAbstract.RealizeInRoom();

            if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                EyeCutBloodEmitter(self, new Color(lizCutEyeAbstract.bloodColourR, lizCutEyeAbstract.bloodColourG, lizCutEyeAbstract.bloodColourB));

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + "'s Cut Eye Object was Created");
        }
        catch (Exception e) { Logger.LogError(e); }

        static void EyeCutBloodEmitter(Lizard self, Color colour)
        {
            self.room.AddObject(new BloodParticle(self.bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(5f, 10f)), colour, self.Template.type.value, null, 2.3f));
        }
    }

    public static void Eviscerate(Lizard self)
    {
        if (ShadowOfOptions.blood.Value && ShadowOfOptions.blood_emitter.Value && BloodColoursCheck(self.Template.type.ToString()))
            BloodEmitter(lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) ? data : null, bloodcolours[self.Template.type.ToString()]);

        self.Destroy();

        void BloodEmitter(LizardData data, Color colour)
        {
            for(int i = 0; i < (data != null ? data.availableBodychunks.Count * 100 : 300); i++)
            {
                self.room.AddObject(new BloodParticle(self.bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f)), colour, self.Template.type.value, null, UnityEngine.Random.Range(100f, 700f)));
            }
        }
    }
    #endregion
}