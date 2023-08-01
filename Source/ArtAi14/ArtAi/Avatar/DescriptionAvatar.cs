using ArtAi.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    public static class DescriptionAvatar
    {
        private static Dictionary<int, string> ColonistAppearance = new Dictionary<int, string>();
        private static DateTime ColonistAppearanceTime = DateTime.MinValue;

        public static Description GetByColonist(Pawn pawn)
        {
            if ((DateTime.UtcNow - ColonistAppearanceTime).TotalSeconds > 10)
            {
                ColonistAppearanceTime = DateTime.UtcNow;
                ColonistAppearance = new Dictionary<int, string>();
            }
            if (!ColonistAppearance.TryGetValue(pawn.thingIDNumber, out var appearance))
            {
                appearance = Normalize(
                    Race(pawn) + " "
                    + BodyType(pawn) + " "
                    + AppearanceTraits(pawn) + " "
                    + AgeTerms(pawn) + " "
                    + Story(pawn) + " "
                    + SkinColor(pawn) + " "
                    + GetFacialAndHeadHair(pawn) + " "
                    + Clothes(pawn));
                ColonistAppearance[pawn.thingIDNumber] = appearance;
            }

            var thingDesc = "beautiful photorealistic portrait of a";
            return new Description(appearance, thingDesc, LanguageDatabase.DefaultLangFolderName, pawn.ThingID);
        }

        private static string Normalize(string text)
        {
            return
                Regex.Replace(text, @"\s+", " ") // delete multiple spaces
                    .ToLower();                  // to lowercase characters
        }

        private static string SkinColor(Pawn pawn)
        {
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.def.displayCategory.defName == "Cosmetic_Skin")
                {
                    var geneLabel = GetGeneLabel(gene);
                    if (geneLabel.EndsWith("skin"))
                    {
                        // eg blue skinned
                        return geneLabel + "ned";
                    }
                }
            }
            return pawn.story.SkinColorBase.r > 0.6f ? "light-skinned"  // Skin_Melanin1-7
                : pawn.story.SkinColorBase.r > 0.45f ? "brown-skinned"  // Skin_Melanin8
                : "dark-skinned";                                       // Skin_Melanin9
        }

        private static string Race(Pawn pawn)
        {
            switch (pawn.genes.Xenotype.defName)
            {
                case "Neanderthal":
                    // as is
                    return pawn.genes.Xenotype.defName;
                case "Impid":
                    return "two-horned imp";
                case "Sanguophage":
                    return "vampire";
                case "Waster":
                    // wasters are basically ghouls: unattractive gray-skinned post-apocalyptic human-like creatures
                    return "ghoul";
                case "Dirtmole":
                    return "blind human with cataracts";
                case "Yttakin":
                    return "animal furry";
                case "Pigskin":
                    // pigskins are hard to get right
                    // sometimes its goes full pig, sometimes its hardly piglike
                    // probably need negative prompt "animal" or "4 legged" to get this right
                    return "snout humanlike piglin";
                case "Hussar":
                    //todo: only relevant feature of hussars is red eyes, but ai for some reason bad at generating eyes
                default:
                    return "human";
            }
        }

        private static string BodyType(Pawn pawn)
        {
            var bodyType = pawn.story.bodyType.defName;
            return bodyType == "Hulk" ? "brawny"
                : bodyType == "Thin" ? "thin"
                : bodyType == "Fat" ? "fat"
                : "";
        }

        private static string AgeTerms(Pawn pawn)
        {
            int age = pawn.ageTracker.AgeBiologicalYears;
            return 
                pawn.gender == Gender.Female && age < 3 ? "newborn girl"
                : pawn.gender == Gender.Female && age < 7 ? "toddler girl"
                : pawn.gender == Gender.Female && age < 13 ? "child girl"
                : pawn.gender == Gender.Female && age < 18 ? "teenager woman"
                : pawn.gender == Gender.Female && age < 45 ? "woman"
                : pawn.gender == Gender.Female && age < 66 ? "middle-aged woman"
                : pawn.gender == Gender.Female ? "senior woman"
                    
                : pawn.gender == Gender.Male && age < 3 ? "newborn boy"
                : pawn.gender == Gender.Male && age < 7 ? "toddler boy"
                : pawn.gender == Gender.Male && age < 13 ? "child boy"
                : pawn.gender == Gender.Male && age < 18 ? "teenager man"
                : pawn.gender == Gender.Male && age < 45 ? "man"
                : pawn.gender == Gender.Male && age < 66 ? "middle-aged man"
                : pawn.gender == Gender.Male ? "senior man"
                : "";
        }

        // copy of Pawn_StoryTracker#TitleShortCap with no translation
        private static string Story(Pawn pawn)
        {
            var story = pawn.story;
            var gender = pawn.gender;
            var storyAdulthood = story.Adulthood;
            var storyChildhood = story.Childhood;
            if (storyAdulthood != null)
            {
                return StoryTitleUntranslated(storyAdulthood, gender);
            }
            if (storyChildhood != null)
            {
                string storyTitle = StoryTitleUntranslated(storyChildhood, gender);
                if (storyTitle == "newborn"
                    || storyTitle == "child"
                    || storyTitle == "colony child"
                    || storyTitle == "tribe child"
                    || storyTitle == "vatgrown child")
                {
                    return "";
                }
                return storyTitle;
            }
            return "";
        }

        private static string StoryTitleUntranslated(BackstoryDef backstoryDef, Gender gender)
        {
            if (gender == Gender.Female && !backstoryDef.untranslatedTitleShortFemale.NullOrEmpty())
            {
                return backstoryDef.untranslatedTitleShortFemale;
            }
            if (!backstoryDef.untranslatedTitleShort.NullOrEmpty())
            {
                return backstoryDef.untranslatedTitleShort;
            }
            if (gender == Gender.Female && !backstoryDef.untranslatedTitleFemale.NullOrEmpty())
            {
                return backstoryDef.untranslatedTitleFemale;
            }
            return backstoryDef.untranslatedTitle;
        }

        private static string AppearanceTraits(Pawn pawn)
        {
            return pawn.story.traits.allTraits
                .OrderByDescending(t =>
                {
                    switch (t.def.defName)
                    {
                        // important traits for appearance
                        case "Beauty":
                        case "Nudist":
                            return 2;
                        // somewhat important traits for appearance
                        case "Bloodlust":
                        case "Psychopath":
                        case "Cannibal":
                        case "Brawler":
                        case "Ascetic":
                        case "Gay":
                        case "Wimp":
                        case "Nimble":
                        case "Tough":
                        case "NaturalMood":
                        case "Nerves":
                        case "Neurotic":
                            return 1;
                        // anything else
                        default:
                            return 0;
                    }
                })
                .ThenBy(t => t.def.defName)
                .Where(t => !"QuickSleeper".Equals(t.def.defName))
                .Where(t => !"Nudist".Equals(t.def.defName) || pawn.ageTracker.AgeBiologicalYears >= 18)
                .Select(t => t.CurrentData.untranslatedLabel)
                .Take(3)
                .Aggregate("", (a, b) => a + " " + b);
        }

        private static string Clothes(Pawn pawn)
        {
            string clothes = pawn.ageTracker.AgeBiologicalYears < 3 ? "swaddle" : "clothes";
            return "wearing " + GetColorText(pawn.story.favoriteColor, FavoriteColorMap) + " " + clothes;
        }

        private static string GetFacialAndHeadHair(Pawn pawn)
        {
            int age = pawn.ageTracker.AgeBiologicalYears;
            if (age < 3)
            {
                return "";  // newborns doesn't have hair
            }
            var hairstyles = pawn.story.hairDef.styleTags;
            bool hairShort = hairstyles.Contains("HairShort");
            bool hairLong = hairstyles.Contains("HairLong");
            bool hairMid = !(hairShort ^ hairLong);
            bool hairBald = hairstyles.Contains("Shaved") || hairstyles.Any(s => s.Contains("Bald"));

            string hairLength = hairMid ? "shoulder-length" : hairLong ? "long" : "short";
            string hairColor = GetHairColorText(pawn);
            string hair = hairBald
                ? "bald"
                : "with " + hairLength + " " + hairColor + " hair";
            // specify beard color only if bald. otherwise it should be deducted by model from hair color
            string beardColor = hairBald ? hairColor : "";
            bool hasBeard = pawn.style.beardDef.defName != "NoBeard";
            string beardOrShaven = !hasBeard && age >= 13 ? "clean-shaven"
                : hasBeard ? "with " + beardColor + " beard"
                : "";
            string facialHair = pawn.genes.CanHaveBeard ? beardOrShaven : "";

            return hair + " " + facialHair;
        }

        private static string GetHairColorText(Pawn pawn)
        {
            var hairColor = pawn.story.HairColor;
            // gray hair
            // RimWorld.PawnHairColors.RandomGreyHairColor
            if (pawn.ageTracker.AgeBiologicalYears > 40)
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (hairColor.r == hairColor.g && hairColor.g == hairColor.b)
                {
                    var colorComponent = hairColor.r;
                    if (colorComponent >= 0.65f && colorComponent <= 0.85f)
                    {
                        return "gray";
                    }
                }
            }
            // gene hair
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.def.endogeneCategory == EndogeneCategory.HairColor)
                {
                    return GetGeneLabel(gene)
                        .Replace(" hair", "");
                }
            }
            // custom hair
            return GetColorText(hairColor, HairColorMap);
        }

        private static string GetGeneLabel(Gene gene)
        {
            Gene mainGene = gene.Overridden ? gene.overriddenByGene : gene;
            return UntranslatedDefs.Labels.TryGetValue(mainGene.def.defName, gene.def.label);
        }

        private static string GetColorText(Color? color, Dictionary<Color, string> map)
        {
            return color.HasValue ? GetColorText(color.Value, map) : "";
        }

        private static string GetColorText(Color color, Dictionary<Color, string> map)
        {
            var bestKey = map.Keys
                .OrderBy(k => Math.Sqrt((k.r - color.r) * (k.r - color.r) + (k.g - color.g) * (k.g - color.g) + (k.b - color.b) * (k.b - color.b)))
                .First();
            return map[bestKey];
        }

        private static Dictionary<Color, string> HairColorMap = new Dictionary<Color, string>()
        {
            { new Color(0.95f, 0.95f, 0.8f), "blond"},
            { new Color(1f, 0.5f, 0.3f), "ginger"},
            { new Color(0.6f, 0.36f, 0.25f), "auburn"},
            { new Color(0.5f, 0.27f, 0.07f), "brown"},
            { new Color(0.1f, 0.1f, 0.1f), "black"},
        };

        private static Dictionary<Color, string> FavoriteColorMap = new Dictionary<Color, string>()
        {
            { new Color(0f/255f, 0f/255f, 0f/255f), "black"},
            { new Color(128f/255f, 128f/255f, 128f/255f), "gray"},
            { new Color(192f/255f, 192f/255f, 192f/255f), "silver"},
            { new Color(255f/255f, 255f/255f, 255f/255f), "white"},

            { new Color(255f/255f, 0f/255f, 255f/255f), "fuchsia"},
            { new Color(128f/255f, 0f/255f, 128f/255f), "purple"},
            { new Color(255f/255f, 0f/255f, 0f/255f), "red"},
            { new Color(128f/255f, 0f/255f, 0f/255f), "maroon"},
            { new Color(255f/255f, 255f/255f, 0f/255f), "yellow"},
            { new Color(128f/255f, 128f/255f, 0f/255f), "olive"},
            { new Color(0f/255f, 255f/255f, 0f/255f), "lime"},
            { new Color(0f/255f, 128f/255f, 0f/255f), "green"},
            { new Color(0f/255f, 255f/255f, 255f/255f), "aqua"},
            { new Color(0f/255f, 128f/255f, 128f/255f), "teal"},
            { new Color(0f/255f, 0f/255f, 255f/255f), "blue"},
        };

        static DescriptionAvatar()
        {
            var favoriteColorMap = new Dictionary<Color, string>();
            foreach(var color in FavoriteColorMap.Keys)
            {
                favoriteColorMap.Add(color, FavoriteColorMap[color]);
                if (FavoriteColorMap[color] == "black"
                    || FavoriteColorMap[color] == "gray"
                    || FavoriteColorMap[color] == "silver"
                    || FavoriteColorMap[color] == "white") continue;
                favoriteColorMap.Add(Color.Lerp(color, Color.black, 0.5f), FavoriteColorMap[color]);
                favoriteColorMap.Add(Color.Lerp(color, Color.white, 0.5f), FavoriteColorMap[color]);
                favoriteColorMap.Add(Color.Lerp(color, Color.gray, 0.5f), FavoriteColorMap[color]);
                favoriteColorMap.Add(Color.Lerp(Color.gray, Color.Lerp(color, Color.black, 0.5f), 0.5f), FavoriteColorMap[color]);
                favoriteColorMap.Add(Color.Lerp(Color.gray, Color.Lerp(color, Color.white, 0.5f), 0.5f), FavoriteColorMap[color]);
            }
            FavoriteColorMap = favoriteColorMap;
        }
    }
}
