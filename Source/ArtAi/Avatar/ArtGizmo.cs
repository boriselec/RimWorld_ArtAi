using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    public class ArtGizmo : Gizmo
    {
        private readonly Thing _thing;

        public ArtGizmo(Thing thing) => _thing = thing;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            AvatarDrawer.DrawArt(_thing, topLeft);
            return new GizmoResult(0);
        }

        public override float GetWidth(float maxWidth) => 75f;
    }
}