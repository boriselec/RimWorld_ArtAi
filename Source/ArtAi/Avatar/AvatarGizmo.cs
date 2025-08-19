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
            AvatarDrawer.Draw(_thing, topLeft);
            return new GizmoResult(0);
        }

        public override float GetWidth(float maxWidth) => 75f;
    }
}