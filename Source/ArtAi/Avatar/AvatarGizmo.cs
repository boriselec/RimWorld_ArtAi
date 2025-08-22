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

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            AvatarDrawer.DrawAvatar(GetPawn(), topLeft);
            return new GizmoResult(0);
        }

        private Pawn GetPawn()
        {
            if (_thing is Pawn pawn)
            {
                return pawn;
            }
            else if (_thing is Corpse corpse)
            {
                return corpse.InnerPawn;
            }
            else if (_thing is Building_CorpseCasket casket)
            {
                return casket.Corpse?.InnerPawn;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public override float GetWidth(float maxWidth) => 75f;
    }
}