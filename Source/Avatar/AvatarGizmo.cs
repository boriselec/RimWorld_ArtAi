using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    public class AvatarGizmo : Gizmo
    {
        private readonly Thing _thing;

        public AvatarGizmo(Thing thing) => _thing = thing;

        public override GizmoResult GizmoOnGUI(
            Vector2 topLeft,
            float maxWidth,
            GizmoRenderParms parms)
        {
            AvatarDrawer.DrawAvatar(GetPawn(), topLeft);
            return new GizmoResult(0);
        }

        private Pawn GetPawn() => _thing switch
        {
            Pawn pawn => pawn,
            Corpse corpse => corpse.InnerPawn,
            Building_CorpseCasket casket => casket.Corpse?.InnerPawn,
            Building_Enterable enterable => enterable.SelectedPawn,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override float GetWidth(float maxWidth) => 75f;
    }
}
