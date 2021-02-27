using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CM_Stay_Strapped
{
    [StaticConstructorOnStartup]
    public static class PawnRenderer_Patches
    {
        [HarmonyPatch(typeof(PawnRenderer))]
        [HarmonyPatch("DrawEquipment", MethodType.Normal)]
        public class PawnRenderer_DrawEquipmentPrefix
        {
            private static float additionalOffsetY = 0.03904f;

            private static float bowAngleCorrection = -90.0f;
            private static float grenadeAngleCorrection = 90.0f;

            // North - East - South - West
            // Back strapping angles and offsets
            private static float[] backAnglesMelee = new float[] { 135f,
                                                                    75f,
                                                                  -135f,
                                                                   -75f};

            private static float[] backAnglesRanged = new float[] {  -60f,
                                                                    -105f,
                                                                      60f,
                                                                     105f};

            private static Vector3[] backOffsetsMelee = new Vector3[] { new Vector3(0.15f, 0f, 0f),
                                                                        new Vector3(-0.15f, -0.03f, 0.14f),
                                                                        new Vector3(-0.15f, 0f, 0.20f),
                                                                        new Vector3(0.15f, 0f, 0.07f) };

            private static Vector3[] backOffsetsRanged = new Vector3[] { new Vector3(0.15f, 0f, 0f),
                                                                         new Vector3(-0.20f, 0f, 0.10f),
                                                                         new Vector3(-0.15f, 0f, 0.20f),
                                                                         new Vector3(0.20f, -0.03f, 0.07f) };

            // Side strapping angles and offsets
            private static float[] sideAnglesMelee = new float[] {  30f,
                                                                   -30f,
                                                                   -30f,
                                                                    30f };

            private static float[] sideAnglesRanged = new float[] { -60f,
                                                                     90f,
                                                                     60f,
                                                                    -90f };

            private static Vector3[] sideOffsetsMelee = new Vector3[] { new Vector3(-0.2f, 0,-0.2f),
                                                                        new Vector3(0f,-0.039f,-0.2f),
                                                                        new Vector3(0.2f, 0f, -0.2f),
                                                                        new Vector3(0f,-0.009f,-0.2f) };

            private static Vector3[] sideOffsetsRanged = new Vector3[] { new Vector3(0.2f, 0f, -0.2f),
                                                                         new Vector3(0f,-0.009f,-0.2f),
                                                                         new Vector3(-0.2f, 0,-0.2f),
                                                                         new Vector3(0f,-0.039f,-0.2f) };



            [HarmonyPrefix]
            public static bool Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc)
            {
                // Bunck of checks to see if the original function should just do its thing
                if (___pawn.Dead || !___pawn.Spawned || ___pawn.equipment == null || ___pawn.equipment.Primary == null || (___pawn.CurJob != null && ___pawn.CurJob.def.neverShowWeapon))
                {
                    return true;
                }

                Stance_Busy stance_Busy = ___pawn.stances.curStance as Stance_Busy;
                if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
                {
                    return true;
                }
                else if (CarryWeaponOpenly(___pawn))
                {
                    return true;
                }

                // Ok if we got here we know the original function isn't going to draw the weapon
                if (!___pawn.InBed() && ___pawn.GetPosture() == PawnPosture.Standing)
                {
                    DrawStrapped(___pawn, ___pawn.equipment.Primary, rootLoc);
                    return false;
                }

                return true;
            }

            private static bool IsTwoHanded(Thing equipment)
            {
                ThingDef def = equipment.def;
                if (def.IsRangedWeapon)
                {
                    return (def.defName.Contains("Bow_") || def.defName.Contains("Blowgun") || def.GetStatValueAbstract(StatDefOf.Mass) > 3f);
                }
                else
                {
                    return (def.GetStatValueAbstract(StatDefOf.Mass) > 1.5f);
                }
            }

            private static void DrawStrapped(Pawn pawn, Thing equipment, Vector3 drawPosition)
            {
                if (IsTwoHanded(equipment))
                    DrawOnBack(pawn, equipment, drawPosition);
                else
                    DrawOnSide(pawn, equipment, drawPosition);
            }

            private static void DrawOnSide(Pawn pawn, Thing equipment, Vector3 drawPosition)
            {
                bool ranged = equipment.def.IsRangedWeapon;
                int rotInt = pawn.Rotation.AsInt;

                float angle = sideAnglesMelee[rotInt];
                if (ranged)
                    angle = sideAnglesRanged[rotInt];

                if (ranged)
                    drawPosition += sideOffsetsRanged[rotInt];
                else
                    drawPosition += sideOffsetsMelee[rotInt];

                if ((ranged && pawn.Rotation != Rot4.North) || (!ranged && pawn.Rotation != Rot4.South))
                    drawPosition.y += additionalOffsetY;

                bool flipped = (pawn.Rotation == Rot4.North || pawn.Rotation == Rot4.West);
                if (!ranged)
                    flipped = !flipped;

                if (equipment.def.defName.Contains("Grenade"))
                {
                    if (flipped)
                        angle += grenadeAngleCorrection;
                    else
                        angle -= grenadeAngleCorrection;
                }

                DrawAngled(pawn, equipment, drawPosition, angle, flipped);
            }

            private static void DrawOnBack(Pawn pawn, Thing equipment, Vector3 drawPosition)
            {
                bool ranged = equipment.def.IsRangedWeapon;
                int rotInt = pawn.Rotation.AsInt;

                float angle = backAnglesMelee[rotInt];
                if (ranged)
                    angle = backAnglesRanged[rotInt];

                if (ranged)
                    drawPosition += backOffsetsRanged[rotInt];
                else
                    drawPosition += backOffsetsMelee[rotInt];

                if (pawn.Rotation != Rot4.South)
                    drawPosition.y += additionalOffsetY;

                bool flipped = (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.West);

                if (equipment.def.defName.Contains("Bow_"))
                {
                    if (flipped)
                        angle += bowAngleCorrection;
                    else
                        angle -= bowAngleCorrection;
                }

                DrawAngled(pawn, equipment, drawPosition, angle, flipped);
            }

            private static void DrawAngled(Pawn pawn, Thing equipment, Vector3 drawPosition, float angle, bool flipped)
            {
                Mesh mesh = MeshPool.plane10;
                if (flipped)
                    mesh = MeshPool.plane10Flip;
                Graphic graphic = equipment.Graphic;

                Graphic_StackCount graphic_StackCount = equipment.Graphic as Graphic_StackCount;
                Material material = (graphic_StackCount == null) ? equipment.Graphic.MatSingle : graphic_StackCount.SubGraphicForStackCount(1, equipment.def).MatSingle;
                Graphics.DrawMesh(mesh, drawPosition, Quaternion.AngleAxis(angle % 360f, Vector3.up), material, 0);
            }

            private static bool CarryWeaponOpenly(Pawn pawn)
            {
                if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
                {
                    return false;
                }
                if (pawn.Drafted)
                {
                    return true;
                }
                if (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon)
                {
                    return true;
                }
                if (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)
                {
                    return true;
                }
                Lord lord = pawn.GetLord();
                if (lord != null && lord.LordJob != null && lord.LordJob.AlwaysShowWeapon)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
