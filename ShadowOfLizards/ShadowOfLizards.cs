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

    internal static new ManualLogSource Logger;
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
            }
            MachineConnector.SetRegisteredOI("notsmartcat.shadowoflizards", ShadowOfOptions.instance);
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

    #region ViolenceCheck
    public static void PreViolenceCheck(Creature receiver)
    {
        storedCreatureWasDead = receiver == null || receiver.dead;
    }

    public static void PostViolenceCheck(Lizard receiver, string killType, Creature sender = null)
    {
        if (receiver != null && receiver.abstractCreature != null && storedCreatureWasDead && receiver.dead)
        {
            if (lizardstorage.TryGetValue(receiver.abstractCreature, out LizardData data) && data.lastDamageType != "Melted" && killType != "Explosion")
            {
                data.lastDamageType = killType;
            }
            ViolenceCheck(receiver, killType, sender);
        }
    }

    public static void ViolenceCheck(Lizard receiver, string killType, Creature sender = null)
    {
        if (receiver == null || receiver.abstractCreature == null || killType == null)
        {
            return;
        }

        if (lizardstorage.TryGetValue(receiver.abstractCreature, out LizardData data))
        {
            if (killType == "Bleed")
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + receiver.ToString() + " died by Bleed. Bleed death is Converted to last damage taken: '" + data.lastDamageType + "'");

                ViolenceCheck(receiver, data.lastDamageType, sender);
            }

            if (data.transformation == "Null" || data.transformation == "Spider" || data.transformation == "Electric")
            {
                if (ShadowOfOptions.spider_transformation.Value && sender != null && (killType == "Stab" || killType == "Blunt" || killType == "Bite") && data.transformation == "Null")
                {
                    CreatureTemplate.Type type = sender.abstractCreature.creatureTemplate.type;

                    if (type == CreatureTemplate.Type.Spider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 0.25
                        || type == CreatureTemplate.Type.BigSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 0.5
                        || type == CreatureTemplate.Type.SpitterSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value
                        || ModManager.DLCShared && type == DLCSharedEnums.CreatureTemplateType.MotherSpider && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spider_transformation_chance.Value * 1.5
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
    #endregion

    #region Gore and Misc
    void EliteLizard(Lizard self)
    {

    }

    public static bool HealthBasedChance(Lizard self, float chance)
    {
        if (UnityEngine.Random.Range(0, 100) < chance * (ShadowOfOptions.health_based_chance.Value ? ((ShadowOfOptions.health_based_chance_dead.Value && self.dead) ? 1 : Mathf.Lerp(ShadowOfOptions.health_based_chance_min.Value / 100, ShadowOfOptions.health_based_chance_max.Value / 100, self.LizardState.health)) : 1))
        {
            return true;
        }

        return false;
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
        try
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

            string template = self.Template.type.ToString();

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

                Debug.Log(all + "LizCutLeg object could not be properily created due to the: " + lizardArm + " Leg Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
            }


            LizCutLegAbstract lizardCutLegAbstract = new(self.room.world, pos, self.room.game.GetNewID())
            {
                hue = 1f,
                saturation = 0.5f,
                scaleX = sLeaser.sprites[num17].scaleX,
                scaleY = sLeaser.sprites[num17].scaleY,
                LizType = template,
                LizBodyColourR = r2,
                LizBodyColourG = g2,
                LizBodyColourB = b2,
                LizBloodColourR = lizBloodColourR,
                LizBloodColourG = lizBloodColourG,
                LizBloodColourB = lizBloodColourB,
                LizEffectColourR = r,
                LizEffectColourG = g,
                LizEffectColourB = b,
                LizSpriteName = lizardArmCut,
                LizColourSpriteName = lizardArmColorCut,
                LizBreed = self.Template.type.value,
                blackSalamander = ((LizardGraphics)self.graphicsModule).blackSalamander,
            };

            self.room.abstractRoom.AddEntity(lizardCutLegAbstract);
            lizardCutLegAbstract.RealizeInRoom();

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "LizCutLeg Created");

            void LimbCutBloodEmitter(Lizard self, BodyChunk hitChunk)
            {
                self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(11f, 15f), UnityEngine.Random.Range(7f, 14f)));
                self.room.AddObject(new BloodEmitter(null, hitChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(9f, 20f)));
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    public static void Decapitation(Lizard self)
    {
        try
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

                Debug.Log(all + "LizCutHead object could not be properily created due to the: " + sprites[graphicsModule.SpriteHeadStart + 3].element.name + " Head Sprite not having a valid variation, if able please report to the mod author of Shadow Of Lizards");
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

            if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out string eye) && eye != "Incompatible" && graphicstorage.TryGetValue(graphicsModule as LizardGraphics, out GraphicsData data2))
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

                LizBodyColourR = r2,
                LizBodyColourG = g2,
                LizBodyColourB = b2,

                LizEffectColourR = r,
                LizEffectColourG = g,
                LizEffectColourB = b,

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
                mass = self.bodyChunks[0].mass,

                LizBreed = self.Template.type.value
            };

            self.room.abstractRoom.AddEntity(lizCutHeadAbstract);
            lizCutHeadAbstract.RealizeInRoom();

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self.ToString() + "'s Cut Head Object was Created");

            void DecapitationBloodEmitter(Lizard self)
            {
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(25f, 20f), UnityEngine.Random.Range(3f, 6f)));
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(7f, 16f)));
                self.room.AddObject(new BloodEmitter(null, self.firstChunk, UnityEngine.Random.Range(5f, 8f), UnityEngine.Random.Range(11f, 26f)));
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
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
        catch (Exception e) { Logger.LogError(e); }

        static void EyeCutBloodEmitter(Lizard self, Color colour)
        {
            self.room.AddObject(new BloodParticle(self.bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(5f, 10f)), colour, self.Template.type.value, null, 2.3f));
        }
    }
    #endregion
}
