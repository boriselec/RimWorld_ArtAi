using ArtAi.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    + (pawn.gender == Gender.Female && ageRound < 25 ? "girl "
                        : pawn.gender == Gender.Female && ageRound >= 25 ? "woman "
                        : pawn.gender == Gender.Male && ageRound < 18 ? "boy "
                        : pawn.gender == Gender.Male && ageRound >= 18 ? "male "
                        : "")
                    + (pawn.story.TitleShortCap + " ")
                    + (pawn.story.SkinColorBase.r > 0.5f ? "light-skinned " : "dark-skinned ")
                    + (hairBald ? "bald " : "with " + (hairMid ? "shoulder-length " : hairLong ? "long " : "short ") + GetColorText(pawn.story.HairColor, HairColorMap) + " hair ")
                    + (pawn.gender == Gender.Female ? "" : pawn.style.beardDef.defName == "NoBeard" ? "clean-shaven " : "with beard ")
                    + (pawn.story.favoriteColor == null ? "" : ("in " + GetColorText(pawn.story.favoriteColor.Value, FavoriteColorMap) + " clothes "))
                    + ("age " + ageRound);
                ColonistAppearance[pawn.thingIDNumber] = appearance;

#if DEBUG
                Log.Message("Avatar appearance colonist " + pawn.Name + ": " + Environment.NewLine + 
                    appearance + Environment.NewLine +
                    Environment.NewLine +
                    $"LabelCap={pawn.LabelCap} " + Environment.NewLine + //Noah<color=#999999FF>, Designer</color> 
                    $"TitleShortCap={pawn.story.TitleShortCap} " + Environment.NewLine + //Designer 
                    $"bodyType={pawn.story.bodyType.defName} " + Environment.NewLine + //Hulk, Thin, Fat
                    $"HairColor={pawn.story.HairColor} {GetColorText(pawn.story.HairColor, HairColorMap)}" + Environment.NewLine +
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

            return new Description(appearance, "beautiful portrait of a human", LanguageDatabase.DefaultLangFolderName);
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
                .Select(t => t.CurrentData.untranslatedLabel)
                .Take(3)
                .ToList();
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
            { new Color(1f, 0.5f, 0.3f), "red"},
            { new Color(0.6f, 0.36f, 0.25f), "red"},
            { new Color(0.5f, 0.27f, 0.07f), "brown"},
            { new Color(0.1f, 0.1f, 0.1f), "brunette"},
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
