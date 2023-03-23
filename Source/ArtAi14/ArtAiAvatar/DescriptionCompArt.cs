using ArtAi.data;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtAi
{
    public static class DescriptionCompArt
    {
        public static Description GetDescription(CompArt compArt)
        {
            var ArtDescription = compArt.GenerateImageDescription();
            var ThingDescription = compArt.parent.def.description;
            return new Description(ArtDescription, ThingDescription);
        }
    }
}
