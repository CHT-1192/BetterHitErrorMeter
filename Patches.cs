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

        // Store original sprites per instance so we can restore on toggle-off
        private static readonly Dictionary<scrHitErrorMeter, (Sprite straight, Sprite curved)> _originals = new();

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
                var (straightSprite, curvedSprite) = kv.Value;
                RestoreSingle(kv.Key.straightMeter, straightSprite);
                RestoreSingle(kv.Key.curvedMeter, curvedSprite);
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

            ReplaceSingle(instance, instance.straightMeter, MeterRenderer.GenerateStraightMeter(scale));
            ReplaceSingle(instance, instance.curvedMeter, MeterRenderer.GenerateCurvedMeter(scale));
        }

        private static void ReplaceSingle(scrHitErrorMeter instance, GameObject? obj, Texture2D tex)
        {
            if (obj == null) return;
            var img = obj.GetComponentInChildren<Image>();
            if (img == null) return;

            // Save original if not already saved
            if (!_originals.ContainsKey(instance))
                _originals[instance] = (null!, null!);

            var entry = _originals[instance];
            bool isStraight = obj == instance.straightMeter;
            if (isStraight && entry.straight == null) entry = (img.sprite, entry.curved);
            if (!isStraight && entry.curved == null) entry = (entry.straight, img.sprite);
            _originals[instance] = entry;

            var old = img.sprite;
            img.sprite = SpriteFromTexture(tex);
            if (old != null && old != entry.straight && old != entry.curved)
                Object.Destroy(old);
        }

        private static void RestoreSingle(GameObject? obj, Sprite? original)
        {
            if (obj == null || original == null) return;
            var img = obj.GetComponentInChildren<Image>();
            if (img == null) return;
            img.sprite = original;
        }

        private static Sprite SpriteFromTexture(Texture2D tex)
        {
            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 1f),
                100f);
        }
    }
}
