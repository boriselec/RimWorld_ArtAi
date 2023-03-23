using ArtAi.data;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    public static class AvatarDrawer
    {
        public static bool DoubleSize = true;

        private static CompArt GetCompArt(Thing thing) => thing == null ? null : thing.TryGetComp<CompArt>();

        public static bool NeedDraw(Thing thing)
        {
            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active) return true;
            if (thing is Pawn pawn && pawn.IsColonistPlayerControlled && !pawn.IsPrisoner && !pawn.IsSlave) return true;

            //new features enter here

            return false;
        }

        public static bool NeedDraw(WorldObject worldObject)
        {

            //new features enter here

            return false;
        }

        private static Description GetDescription(Thing thing)
        {
            Description description = default;

            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active)
            {
                description = DescriptionCompArt.GetDescription(compArt);
            }
            if (thing is Pawn pawn && pawn.IsColonistPlayerControlled && !pawn.IsPrisoner && !pawn.IsSlave)
            {
                description = DescriptionAvatar.GetByColonist(pawn);
            }

            //new features enter here

            return description;
        }

        private static Description GetDescription(WorldObject worldObject)
        {
            Description description = default;


            //new features enter here

            return description;
        }

        private static void Draw(Description description, Rect rect, Thing thing, WorldObject worldObject, bool onlyContent = false)
        {
            var click = false;
            if (!onlyContent)
            {
                if (Widgets.ButtonInvisible(rect))
                {
                    click = true;
                    DoubleSize = !DoubleSize;
                }
                GUI.DrawTexture(rect, Command.BGTexShrunk); //BGTex);
                rect = rect.ContractedBy(2);
            }

            if (!description.IsNull)
            { 
                var image = LocalRepository.Instance.GetImage(description);
                if (image.Status == GenerationStatus.Done)
                {
                    GUI.DrawTexture(rect, image.Texture);

                    if (!onlyContent && click && DoubleSize)
                    {
                        Find.WindowStack.Add(new Dialog_ShowImage(image.Texture));
                    }
                }
            }
        }

        public static void Draw(Thing thing, Rect rect, bool onlyContent = false) => Draw(GetDescription(thing), rect, thing, null, onlyContent);

        public static void Draw(WorldObject worldObject, Rect rect, bool onlyContent = false) => Draw(GetDescription(worldObject), rect, null, worldObject, onlyContent);

    }
}
