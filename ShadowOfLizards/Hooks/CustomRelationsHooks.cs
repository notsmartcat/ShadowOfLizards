using IL.MoreSlugcats;
using IL.Watcher;
using UnityEngine;
using static CreatureTemplate;
using static RelationshipTracker;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;
internal class CustomRelationsHooks
{
    public static void Apply()
    {
        On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAIUpdateDynamicRelationship;

        On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += BigSpiderAIUpdateDynamicRelationship;
    }

    static Relationship BigSpiderAIUpdateDynamicRelationship(On.BigSpiderAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, BigSpiderAI self, DynamicRelationship dRelation)
    {
        if (!ShadowOfOptions.spider_transformation.Value || dRelation.trackerRep.representedCreature.realizedCreature == null)
        {
            return orig(self, dRelation);
        }

        Creature crit = dRelation.trackerRep.representedCreature.realizedCreature;

        if (crit is Lizard)
        {
            if (LizardSpiderMotherTemplateCheck(crit, true) && self.StaticRelationship(dRelation.trackerRep.representedCreature).type != Relationship.Type.Afraid)
            {
                return new Relationship(Relationship.Type.StayOutOfWay, 0.9f);
            } //Spiders Stay out of way of Spider Lizards
            else if (LizardSpiderTransformationTemplateCheck(crit, true))
            {
                return new Relationship(Relationship.Type.Ignores, 0.0f);
            } //Spiders Ignore Spider Transformation Lizards
        }
        else if (crit is Player)
        {
            for (int i = 0; i < self.bug.room.abstractRoom.creatures.Count; i++)
            {
                if (self.bug.room.abstractRoom.creatures[i].realizedCreature != null && self.bug.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && data.spiderLikness > 0 && liz.AI != null && liz.AI.friendTracker.friend == crit && (self.bug.room.abstractRoom.creatures[i].rippleLayer == self.bug.abstractPhysicalObject.rippleLayer || self.bug.room.abstractRoom.creatures[i].rippleBothSides || self.bug.abstractPhysicalObject.rippleBothSides))
                {
                    return new Relationship(Relationship.Type.Ignores, 0.0f);
                }
            }
        }

        return orig(self, dRelation);
    }

    static Relationship LizardAIUpdateDynamicRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, DynamicRelationship dRelation)
    {
        if ((!ShadowOfOptions.spider_transformation.Value && !ShadowOfOptions.electric_transformation.Value && !ShadowOfOptions.melted_transformation.Value) || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || data.transformation == "Null" || data.transformation == "ElectricTransformation" || data.transformation == "Melted" || (self.friendTracker.giftOfferedToMe != null && self.friendTracker.giftOfferedToMe.active && self.friendTracker.giftOfferedToMe.item == dRelation.trackerRep.representedCreature.realizedCreature) || dRelation.trackerRep.representedCreature.realizedCreature == null)
        {
            return orig(self, dRelation);
        }

        Creature crit = dRelation.trackerRep.representedCreature.realizedCreature;

        if (data.transformation == "Spider")
        {
            if (crit is Lizard)
            {
                if (LizardSpiderMotherTemplateCheck(crit, false))
                {
                    return new Relationship(Relationship.Type.StayOutOfWay, 0.9f);
                } //Spider Lizards Stay ut of way of other Spider Lizards
                if (LizardSpiderTransformationTemplateCheck(crit, false))
                {
                    return new Relationship(Relationship.Type.Attacks, 1f);
                } //Spider Lizards Attack Spider Transformation Lizards
            }
            else if(SpiderTemplateCheck(crit))
            {
                return new Relationship(Relationship.Type.Attacks, 1f);
            } //Spider Lizards Attack Spiders
        }
        else if (data.transformation == "SpiderTransformation")
        {
            if (crit is Lizard)
            {
                if (LizardSpiderTransformationTemplateCheck(crit, true) && data.spiderLikness > 0)
                {
                    return new Relationship(Relationship.Type.Pack, 0.5f);
                } //Spider Transformation Lizards Pack with other Spider Transformation Lizards
                else if (LizardSpiderMotherTemplateCheck(crit, true) && self.StaticRelationship(dRelation.trackerRep.representedCreature).type != Relationship.Type.Afraid)
                {
                    return new Relationship(Relationship.Type.StayOutOfWay, 0.9f);
                } //Spider Transformation Lizards Stay out of way of Spider Mother Lizards
            }
            else if (SpiderTemplateCheck(crit) && data.spiderLikness > 0)
            {
                return new Relationship(Relationship.Type.Ignores, 0.0f);
            } //Spider Transformation Lizards Ignore Spiders
            else if (crit is Player)
            {
                for (int i = 0; i < self.lizard.room.abstractRoom.creatures.Count; i++)
                {
                    if (self.lizard.room.abstractRoom.creatures[i].realizedCreature != null && self.lizard.room.abstractRoom.creatures[i].realizedCreature is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data2) && data2.spiderLikness > 0 && liz.AI != null && liz.AI.friendTracker.friend == crit && (self.lizard.room.abstractRoom.creatures[i].rippleLayer == self.lizard.abstractPhysicalObject.rippleLayer || self.lizard.room.abstractRoom.creatures[i].rippleBothSides || self.lizard.abstractPhysicalObject.rippleBothSides))
                    {
                        return new Relationship(Relationship.Type.StayOutOfWay, 0.6f);
                    }
                }
            }
        }
        else if (data.transformation == "Electric")
        {
            if (CentipedeTemplateCheck(crit))
            {
                return new Relationship(Relationship.Type.Eats, 0.9f);
            } //Electric Lizards want to Eat Centipedes
        }
        else if (data.transformation == "MeltedTransformation")
        {
            return IsThisBigCreatureForShelter(dRelation.trackerRep.representedCreature) ? new Relationship(Relationship.Type.Attacks, 0.8f) : new Relationship(Relationship.Type.Eats, 0.9f);
        } //Melted Lizards want to Eat everyone other then Slugcats 

        return orig(self, dRelation);
    }

    private static bool IsThisBigCreatureForShelter(AbstractCreature creature)
    {
        Type type = creature.creatureTemplate.type;
        return type == Type.Deer || type == Type.BrotherLongLegs || type == Type.DaddyLongLegs || type == Type.RedCentipede || type == Type.MirosBird || type == Type.PoleMimic || type == Type.TentaclePlant || creature.creatureTemplate.IsVulture || (ModManager.DLCShared && MSCIsThisBigCreatureForShelter()) || (ModManager.Watcher && WatcherIsThisBigCreatureForShelter());

        bool MSCIsThisBigCreatureForShelter()
        {
            if (type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs)
            {
                return true;
            }
            if (type == DLCSharedEnums.CreatureTemplateType.MirosVulture)
            {
                return true;
            }

            return false;
        }
        bool WatcherIsThisBigCreatureForShelter()
        {
            return false;
        }
    }

    static bool SpiderTemplateCheck(Creature crit)
    {
        return crit != null && (crit is Spider || crit is BigSpider);
    }

    static bool CentipedeTemplateCheck(Creature crit)
    {
        return crit != null && crit is Centipede;
    }

    static bool LizardSpiderTransformationTemplateCheck(Creature crit, bool isSpiderRelated)
    {
        if (crit != null && crit is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            return data.transformation == "SpiderTransformation" && (!isSpiderRelated || data.spiderLikness > 0);
        }
        return false;
    }

    static bool LizardSpiderMotherTemplateCheck(Creature crit, bool isSpiderRelated)
    {
        if (crit != null && crit is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data))
        {
            return data.transformation == "Spider" && (!isSpiderRelated || data.spiderLikness > 0);
        }
        return false;
    }

    //Unused Code
    /*
    public static bool LizardElectricTransformationTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) && data.transformation == "ElectricTransformation")
        {
            return true;
        }
        return false;
    }
    public static bool LizardElectricTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
        {
            return true;
        }
        return false;
    }

    public static bool MeltedTemplateCheck(Creature crit, DynamicRelationship rel)
    {
        if (crit != null && crit.Template.type != Type.Slugcat && rel.currentRelationship.type != Relationship.Type.Ignores && rel.currentRelationship.type != Relationship.Type.DoesntTrack)
        {
            return true;
        }
        return false;
    }
    */
}
