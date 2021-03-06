﻿using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using PyTK.Extensions;
using StardewModdingAPI;
using SObject = StardewValley.Object;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using StardewValley;

namespace PyTK.Overrides
{
    class OvSpritebatch
    {
        internal static IModHelper Helper { get; } = PyTKMod._helper;
        internal static IMonitor Monitor { get; } = PyTKMod._monitor;
        internal static bool replaceNext = false;
        internal static Item nextItem;
        internal static CustomObjectData nextData;
        internal static Dictionary<object, CustomObjectData> dataChache = new Dictionary<object, CustomObjectData>();

        [HarmonyPatch]
        internal class SpriteBatchFixMono
        {
            internal static MethodInfo TargetMethod()
            {
                if (Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, MonoGame.Framework") != null)
                    return AccessTools.Method(Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, MonoGame.Framework"), "DrawInternal");
                else
                    return AccessTools.Method(typeof(FakeSpriteBatch), "DrawInternal");
            }

            internal static void Prefix(ref SpriteBatch __instance, KeyValuePair<Texture2D, Rectangle?> __state, ref Texture2D texture, ref Vector4 destinationRectangle, ref Rectangle? sourceRectangle, ref Color color, ref float rotation, ref Vector2 origin, ref SpriteEffects effect, ref float depth)
            {
                __state = new KeyValuePair<Texture2D, Rectangle?>(texture, sourceRectangle);

                if (!sourceRectangle.HasValue || texture == null)
                    return;

                Rectangle sr = sourceRectangle.Value;
                Texture2D st = texture.clone();

                if (!CustomObjectData.collection.Exists(a => a.Value.sdvSourceRectangle == sr && st == a.Value.sdvTexture))
                    return;

                CustomObjectData data = CustomObjectData.collection.Find(a => a.Value.sdvSourceRectangle == sr && st == a.Value.sdvTexture).Value;

                if (data.color != Color.White)
                    color = data.color.multiplyWith(color);

                texture = data.texture;
                sourceRectangle = data.sourceRectangle;
            }

            internal static void Postfix(ref Texture2D texture, ref Rectangle? sourceRectangle, KeyValuePair<Texture2D, Rectangle?> __state)
            {
                texture = __state.Key;
                sourceRectangle = __state.Value;
            }
        }
        
        [HarmonyPatch]
        internal class SpriteBatchFix
        {
            internal static MethodInfo TargetMethod()
            {
                if (Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, Microsoft.Xna.Framework.Graphics") != null)
                    return AccessTools.Method(Type.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatch, Microsoft.Xna.Framework.Graphics"), "InternalDraw");
                else
                    return AccessTools.Method(typeof(FakeSpriteBatch), "InternalDraw");
            }

            internal static void Prefix(ref SpriteBatch __instance, KeyValuePair<Texture2D,Rectangle?> __state, ref Texture2D texture, ref Vector4 destination, ref bool scaleDestination, ref Rectangle? sourceRectangle, ref Color color, ref float rotation, ref Vector2 origin, ref SpriteEffects effects, ref float depth)
            {
                if (!replaceNext)
                    return;

                __state = new KeyValuePair<Texture2D, Rectangle?>(texture, sourceRectangle);
                
                if (sourceRectangle.HasValue && sourceRectangle == nextData.sdvSourceRectangle && texture == nextData.sdvTexture)
                {
                    texture = nextData.texture;
                    sourceRectangle = nextData.sourceRectangle;
                    color = nextData.color != Color.White ? nextData.color : color;
                    replaceNext = false;
                }
            }

            internal static void Postfix(ref Texture2D texture, ref Rectangle? sourceRectangle, KeyValuePair<Texture2D, Rectangle?> __state)
            {
                if (!replaceNext)
                    return;

                if (__state.Key == null || !__state.Value.HasValue)
                    return;

                texture = __state.Key;
                sourceRectangle = __state.Value;;
            }

        }

        internal class FakeSpriteBatch
        {
            internal void DrawInternal(Texture2D texture, Vector4 destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effect, float depth, bool autoFlush)
            {
                return;
            }

            internal void InternalDraw(Texture2D texture, ref Vector4 destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, SpriteEffects effects, float depth)
            {
                return;
            }
        }
        
        internal class DrawFix1
        {
            public static void prefix(ref SObject __instance)
            {
                CustomObjectData c;

                if (dataChache.ContainsKey(__instance))
                    c = dataChache[__instance];
                else
                {
                    SObject obj = __instance;
                    c = CustomObjectData.collection.Find(o => o.Value.sdvId == obj.parentSheetIndex && o.Value.bigCraftable == obj.bigCraftable).Value;
                    dataChache.AddOrReplace(__instance, c);
                }

                if (c != null)
                {
                    replaceNext = true;
                    nextItem = __instance;
                    nextData = c;
                }
                else
                {
                    replaceNext = false;
                }
            }

            public static void postfix(ref SObject __instance)
            {
                    replaceNext = false;
            }

            public static void init(string name, Type type, List<string> toPatch)
            {
                HarmonyInstance harmony = HarmonyInstance.Create("Platonymous.PyTK.Draw." + name);
                List<MethodInfo> replacer = typeof(DrawFix1).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
                MethodInfo prefix = typeof(DrawFix1).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList().Find(m => m.Name == "prefix");
                MethodInfo postfix = typeof(DrawFix1).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList().Find(m => m.Name == "postfix");
                List<MethodInfo> originals = type.GetMethods().ToList();

                foreach(MethodInfo method in originals)
                    if(toPatch.Contains(method.Name))
                        harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));                        
            }

            

        }

    }
}
