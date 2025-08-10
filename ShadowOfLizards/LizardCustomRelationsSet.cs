using static CreatureTemplate;
using static RelationshipTracker;
using static Tracker;

namespace ShadowOfLizards;
internal class LizardCustomRelationsSet
{
    static Creature RelationNullCheck(DynamicRelationship rel)
    {
        Creature crit = null;
        if (rel != null)
        {
            CreatureRepresentation trackerRep = rel.trackerRep;
            if (trackerRep != null)
            {
                AbstractCreature representedCreature = trackerRep.representedCreature;
                crit = representedCreature?.realizedCreature;
            }
        }
        return crit;
    }

    public static bool TemplateCheck(Creature crit, Type type)
    {
        if (crit != null && crit.Template.type == type)
        {
            return true;
        }
        return false;
    }

    public static bool SpiderTemplateCheck(Creature crit)
    {
        if (crit != null && (crit.Template.type == CreatureTemplate.Type.Spider || crit.Template.type == CreatureTemplate.Type.BigSpider || crit.Template.type == CreatureTemplate.Type.SpitterSpider || ModManager.DLCShared && crit.Template.type == DLCSharedEnums.CreatureTemplateType.MotherSpider))
        {
            return true;
        }
        return false;
    }

    public static bool CentipedeTemplateCheck(Creature crit)
    {
        if (crit != null && (crit.Template.type == CreatureTemplate.Type.Centipede || crit.Template.type == CreatureTemplate.Type.SmallCentipede || crit.Template.type == CreatureTemplate.Type.Centiwing || crit.Template.type == CreatureTemplate.Type.RedCentipede || ModManager.DLCShared && crit.Template.type == DLCSharedEnums.CreatureTemplateType.AquaCenti))
        {
            return true;
        }
        return false;
    }

    public static bool LizardSpiderTransformationTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) && data.transformation == "SpiderTransformation")
        {
            return true;
        }
        return false;
    }

    public static bool LizardSpiderMotherTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) && data.transformation == "Spider")
        {
            return true;
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
    */

    public static bool MeltedTemplateCheck(Creature crit, DynamicRelationship rel)
    {
        if (crit != null && crit.Template.type != CreatureTemplate.Type.Slugcat && rel.currentRelationship.type != Relationship.Type.Ignores && rel.currentRelationship.type != Relationship.Type.DoesntTrack)
        {
            return true;
        }
        return false;
    }

    public static void Apply(Type type, Lizard self)
    {
        if (self == null || !ShadowOfLizards.lizardstorage.TryGetValue(self.abstractCreature, out ShadowOfLizards.LizardData data))
        {
            return;
        }

        if (data.transformation == "Spider")
        {
            On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return TemplateCheck(RelationNullCheck(dRelation), type) ? new Relationship(Relationship.Type.StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            }; //Spiders Stay out of way of Spider Lizards

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderMotherTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            }; //Spider Lizards Stay ut of way of other Spider Lizards
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return SpiderTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Attacks, 1f) : orig.Invoke(self, dRelation);
            }; //Spider Lizards Attack Spiders
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderTransformationTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Attacks, 1f) : orig.Invoke(self, dRelation);
            }; //Spider Lizards Attack Spider Transformation Lizards
        }
        else if (data.transformation == "SpiderTransformation")
        {
            On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return TemplateCheck(RelationNullCheck(dRelation), type) ? new Relationship(Relationship.Type.Ignores, 0.0f) : orig.Invoke(self, dRelation);
            }; //Spiders Ignore Spider Transformation Lizards

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return SpiderTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Ignores, 0.0f) : orig.Invoke(self, dRelation);
            }; //Spider Transformation Lizards Ignore Spiders
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderTransformationTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Pack, 0.5f) : orig.Invoke(self, dRelation);
            }; //Spider Transformation Lizards Pack with other Spider Transformation Lizards
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderMotherTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            }; //Spider Transformation Lizards Stay out of way of Spider Mother Lizards
        }
        else if (data.transformation == "Electric")
        {
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return CentipedeTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Eats, 0.9f) : orig.Invoke(self, dRelation);
            }; //Electric Lizards want to Eat Centipedes
        }
        else if (data.transformation == "MeltedTransformation")
        {
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return MeltedTemplateCheck(RelationNullCheck(dRelation), dRelation) ? new Relationship(Relationship.Type.Eats, 0.9f) : orig.Invoke(self, dRelation);
            }; //Melted Lizards want to Eat everyone other then Slugcats 
        }
    }
}
