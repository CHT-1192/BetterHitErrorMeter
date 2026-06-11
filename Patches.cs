using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BetterHitErrorMeter
{
    public static class Patches
    {
        private static ErrorMeterSize _lastSize = ErrorMeterSize.Off;
        private static ErrorMeterShape _lastShape = (ErrorMeterShape)(-1);
        private static readonly Dictionary<scrHitErrorMeter, (Sprite? straight, Sprite? curved)> _originals = new();

        [HarmonyPatch(typeof(scrController), "Awake_Rewind")]
        [HarmonyPostfix]
        public static void ControllerAwake_Postfix(scrController __instance)
        {
            var em = __instance.errorMeter;
            if (em == null) return;
            _lastSize = ErrorMeterSize.Off;
            _lastShape = (ErrorMeterShape)(-1);
            ReplaceSprites(em);
        }

        [HarmonyPatch(typeof(scrHitErrorMeter), "UpdateLayout")]
        [HarmonyPostfix]
        public static void UpdateLayout_Postfix(scrHitErrorMeter __instance)
        {
            var size = Persistence.hitErrorMeterSize;
            var shape = Persistence.hitErrorMeterShape;
            if (size == ErrorMeterSize.Off) return;
            if (size == _lastSize && shape == _lastShape) return;
            _lastSize = size;
            _lastShape = shape;
            ReplaceSprites(__instance);
        }

        public static void RestoreAll()
        {
            foreach (var kv in _originals)
            {
                if (kv.Key == null) continue;
                RestoreSingle(kv.Key.straightMeter, kv.Value.straight);
                RestoreSingle(kv.Key.curvedMeter, kv.Value.curved);
            }
            _originals.Clear();
        }

        private static void ReplaceSprites(scrHitErrorMeter instance)
        {
            var size = Persistence.hitErrorMeterSize;
            if (size == ErrorMeterSize.Off) return;

            float scale = size switch
            {
                ErrorMeterSize.Small => 0.75f,
                ErrorMeterSize.Large => 1.5f,
                ErrorMeterSize.ExtraLarge => 2.0f,
                _ => 1.0f
            };

            if (!_originals.ContainsKey(instance))
                _originals[instance] = (null, null);

            ReplaceSingle(instance, instance.straightMeter, MeterRenderer.GenerateStraight(scale), isStraight: true);
            ReplaceSingle(instance, instance.curvedMeter, MeterRenderer.GenerateCurved(scale), isStraight: false);
        }

        private static void ReplaceSingle(scrHitErrorMeter instance, GameObject? obj, Texture2D tex, bool isStraight)
        {
            if (obj == null) return;
            var img = obj.GetComponentInChildren<Image>();
            if (img == null) return;

            var entry = _originals[instance];
            if (isStraight && entry.straight == null)
                _originals[instance] = (img.sprite, entry.curved);
            else if (!isStraight && entry.curved == null)
                _originals[instance] = (entry.straight, img.sprite);

            var old = img.sprite;
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 1f), 100f);
            if (old != null && old != _originals[instance].straight && old != _originals[instance].curved)
                Object.Destroy(old);
        }

        private static void RestoreSingle(GameObject? obj, Sprite? original)
        {
            if (obj == null || original == null) return;
            var img = obj.GetComponentInChildren<Image>();
            if (img == null) return;
            img.material = null;
            img.sprite = original;
        }
    }
}
