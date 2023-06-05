using ArtAi.data;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var bodyType = pawn.story.bodyType.defName;
                var age = pawn.ageTracker.AgeBiologicalYears;
                var ageRound = age < 18 ? 12
                    : age < 25 ? 18
                    : age < 60 ? 25
                    : 60;
                var hairstyles = pawn.story.hairDef.styleTags;
                var hairShort = hairstyles.Contains("HairShort");
                var hairLong = hairstyles.Contains("HairLong");
                var hairMid = !(hairShort ^ hairLong);
                var hairBald = hairstyles.Contains("Shaved") || hairstyles.Any(s => s.Contains("Bald"));
                var appearanceTraits = AppearanceTraits(pawn);

                appearance =
                    (bodyType == "Hulk" ? "inflated physique " // brawny ?
                        : bodyType == "Thin" ? "thin "
                        : bodyType == "Fat" ? "fat "
                        : "")
                    + string.Join(" ", appearanceTraits) + " "
                    + (age >= 12 && age < 18 ? "teenager " : "")
                    + (pawn.gender == Gender.Female && ageRound < 25 ? "girl "
                        : pawn.gender == Gender.Female && ageRound >= 25 ? "woman "
                        : pawn.gender == Gender.Male && ageRound < 18 ? "boy "
                        : pawn.gender == Gender.Male && ageRound >= 18 ? "male "
                        : "")
                    + TitleShortCapUntranslated(pawn) + " "
                    + (pawn.story.SkinColorBase.r > 0.5f ? "light-skinned " : "dark-skinned ")
                    + (hairBald ? "bald " : "with " + (hairMid ? "shoulder-length " : hairLong ? "long " : "short ") + GetHairColorText(pawn) + " hair ")
                    + (pawn.gender == Gender.Female ? "" : pawn.style.beardDef.defName == "NoBeard" ? "clean-shaven " : "with beard ")
                    + (pawn.story.favoriteColor == null ? "" : ("in " + GetColorText(pawn.story.favoriteColor.Value, FavoriteColorMap) + " clothes "))
                    + ("age " + ageRound);
                ColonistAppearance[pawn.thingIDNumber] = appearance;

#if DEBUG
                Log.Message("Avatar appearance colonist " + pawn.Name + ": " + Environment.NewLine + 
                    appearance + Environment.NewLine +
                    Environment.NewLine +
                    $"LabelCap={pawn.LabelCap} " + Environment.NewLine + //Noah<color=#999999FF>, Designer</color> 
                    $"TitleShortCap={TitleShortCapUntranslated(pawn)} " + Environment.NewLine + //Designer 
                    $"bodyType={pawn.story.bodyType.defName} " + Environment.NewLine + //Hulk, Thin, Fat
                    $"HairColor={pawn.story.HairColor} {GetHairColorText(pawn)}" + Environment.NewLine +
                    $"SkinColor={pawn.story.SkinColor} " + Environment.NewLine + //0.3882353
                    $"SkinColorBase={pawn.story.SkinColorBase} " + Environment.NewLine + //0.3882353
                    $"favoriteColor={pawn.story.favoriteColor} {GetColorText(pawn.story.favoriteColor.Value, FavoriteColorMap)}" + Environment.NewLine +
                    $"Age={pawn.ageTracker.AgeBiologicalYears} " + Environment.NewLine +
                    $"beardDef={pawn.style.beardDef.defName} " + Environment.NewLine + //NoBeard
                    $"gender={pawn.gender} " + Environment.NewLine +//Male Female - woman/girl  male/boy
                    $"GenderLabel={pawn.GetGenderLabel()} " + Environment.NewLine +
                    $"hairstyle={string.Join(",", pawn.story.hairDef.styleTags)} " + Environment.NewLine + 
                    $"" + Environment.NewLine +
                    $"");
#endif
            }

            var thingDesc = "beautiful photorealistic portrait of a human";
            return new Description(appearance, thingDesc, LanguageDatabase.DefaultLangFolderName, pawn.ThingID);
        }

        // copy of Pawn_StoryTracker#TitleShortCap with no translation
        private static string TitleShortCapUntranslated(Pawn pawn)
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
                return StoryTitleUntranslated(storyChildhood, gender);
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

        private static List<string> AppearanceTraits(Pawn pawn)
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
                .ToList();
        }

        private static string GetHairColorText(Pawn pawn)
        {
            var hairColor = pawn.story.HairColor;
            // grey hair
            // RimWorld.PawnHairColors.RandomGreyHairColor
            if (pawn.ageTracker.AgeBiologicalYears > 40)
            {
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (hairColor.r == hairColor.g && hairColor.g == hairColor.b)
                {
                    var colorComponent = hairColor.r;
                    if (colorComponent >= 0.65f && colorComponent <= 0.85f)
                    {
                        return "grey";
                    }
                }
            }
            // gene hair
            var hairColorGene = pawn.genes.GetHairColorGene();
            if (hairColorGene != null)
            {
                return UntranslatedDefs.Labels.TryGetValue(hairColorGene.defName, hairColorGene.label)
                    .Replace(" hair", "");
            }
            // custom hair
            return GetColorText(hairColor, HairColorMap);
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
            { new Color(0f/255f, 0f/255f, 128f/255f), "navy"},
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
