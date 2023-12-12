using Crest;
using HarmonyLib;
using SailwindModdingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ControlWinds.Patches
{
    public enum WindDirections
    {
        None,
        North,
        Headwind
    }

    internal static class ControlWindsPatches
    {
        public static bool speedyWind = false;
        public static WindDirections windDirection = WindDirections.None;
        public static float speed = 3f;

        [HarmonyPatch(typeof(Wind), "UpdateWind")]
        public static class WindPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Wind __instance, Vector3 ___currentGustTarget, float ___finalLerpSpeed)
            {
                if (ControlWindsPatches.windDirection != WindDirections.None || ControlWindsPatches.speedyWind)
                {
                    Vector3 vector = Vector3.Lerp(Wind.currentWind, ___currentGustTarget, Time.deltaTime * ___finalLerpSpeed);
                    float num = 1f;
                    if (ControlWindsPatches.windDirection == WindDirections.Headwind)
                    {
                        Vector3 vector2 = Vector3.one;
                        if (GameState.lastOwnedBoat)
                        {
                            vector2 = GameState.lastOwnedBoat.rotation.eulerAngles;
                        }
                        float num2 = vector2.y / 57.295776f;
                        vector = new Vector3(Mathf.Sin(num2), 0f, Mathf.Cos(num2));
                        if (!ControlWindsPatches.speedyWind)
                        {
                            num = ___currentGustTarget.magnitude;
                        }
                    }else if(windDirection == WindDirections.North)
                    {
                        Vector3 vector2 = Vector3.down;
                        float num2 = vector2.y / 57.295776f;
                        vector = new Vector3(Mathf.Sin(num2), 0f, Mathf.Cos(num2));
                        if (!ControlWindsPatches.speedyWind)
                        {
                            num = ___currentGustTarget.magnitude;
                        }
                    }
                    if (ControlWindsPatches.speedyWind)
                    {
                        num = __instance.maximumMagnitude * ControlWindsPatches.speed;
                    }
                    Wind.currentWind = vector.normalized * num;
                    Wind.windRotation = __instance.transform.rotation;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WindSound), "Update")]
        private static class WindSoundPatch
        {
            [HarmonyPrefix]
            public static void Prefix(WindSound __instance, ref Vector3 ___lastPos)
            {
                if (ControlWindsPatches.speedyWind)
                {
                    ___lastPos = __instance.transform.position;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(WindSound __instance, ref Vector3 ___lastPos, ref AudioSource ___audio)
            {
                if (ControlWindsPatches.speedyWind)
                {
                    ___audio.volume = 0f;
                    ___lastPos = __instance.transform.position;
                }
            }
        }

        [HarmonyPatch(typeof(OceanUpdaterCrest), "UpdateOcean")]
        public static class OceanUpdaterCrestPatch
        {
            [HarmonyPostfix]
            public static void Postfix(OceanUpdaterCrest __instance, Wind ___wind, WavesInertia ___wavesInertia, ref float ___targetInertiaAngle, ref Vector3 ___finalWind, ref ShapeGerstnerBatched ___windWaves, float ___windWindScale, float ___lerpRateWind)
            {
                if (ControlWindsPatches.speedyWind)
                {
                    ___finalWind = Vector3.zero;
                    ___windWaves._weight = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(OceanWavesUpdater), "UpdateOcean")]
        public static class OceanWavesUpdaterPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(OceanWavesUpdater __instance, ref Ocean ___ocean, Transform ___rotationFixer, ref Vector3 ___finalWind, WavesInertia ___wavesInertia)
            {
                if (!ControlWindsPatches.speedyWind)
                {
                    return true;
                }
                __instance.InvokePrivateMethod("GetWavesInertia", Array.Empty<object>());
                ___finalWind = Vector3.zero;
                OceanWavesUpdater.wavesRotationAngle = Mathf.Lerp(OceanWavesUpdater.wavesRotationAngle, ___rotationFixer.eulerAngles.y, __instance.lerpRate);
                ___ocean.scale = Mathf.Lerp(___wavesInertia.currentInertia, 0f, 0.15f) * __instance.oceanScale;
                ___ocean.choppy_scale = ___ocean.scale * __instance.choppyFactor;
                ___ocean.windx = Mathf.Lerp(___ocean.windx, ___finalWind.x, __instance.lerpRate);
                ___ocean.windy = Mathf.Lerp(___ocean.windy, ___finalWind.z, __instance.lerpRate);
                return false;
            }
        }

        [HarmonyPatch(typeof(Sail), "ApplyForce")]
        public static class SailPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Sail __instance, ref Rigidbody ___shipRigidbody, ref float ___outFinalForwardForce, float ___unamplifiedForwardForce, float ___unamplifiedSidewayForce)
            {
                if (ControlWindsPatches.windDirection == WindDirections.None)
                {
                    return true;
                }
                float num = __instance.GetRealSailPower();
                if(__instance.category == SailCategory.junk)
                {
                    num *= 0.75f;
                }
                if(__instance.category == SailCategory.gaff)
                {
                    num *= 0.85f;
                }
                float num2 = 50f;
                float num3 = 1.5f;
                Vector3 position = __instance.windcenter.position;
                if (ControlWindsPatches.windDirection != WindDirections.None)
                {
                    position = ___shipRigidbody.transform.position;
                }
                ___shipRigidbody.AddForceAtPosition(___shipRigidbody.transform.forward * ___unamplifiedForwardForce * num2 * num, position, 0);
                ___shipRigidbody.AddForceAtPosition(___shipRigidbody.transform.right * ___unamplifiedSidewayForce * num3 * num * num2, position, 0);
                ___outFinalForwardForce = ___unamplifiedForwardForce * num * num2;
                return false;
            }
        }

        [HarmonyPatch(typeof(Ocean), "SetWaves")]
        public static class OceanPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Ocean __instance)
            {
                if (ControlWindsPatches.speedyWind)
                {
                    __instance.waveScale = 0f;
                }
            }
        }
    }
}
