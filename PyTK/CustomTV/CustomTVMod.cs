﻿using PyTK.Extensions;
using PyTK.Types;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using SFarmer = StardewValley.Farmer;
using StardewModdingAPI;

namespace PyTK.CustomTV
{
    public static class CustomTVMod
    {
        internal static IModHelper Helper { get; } = PyTKMod._helper;
        internal static IMonitor Monitor { get; } = PyTKMod._monitor;

        private static string weatherString { get; } = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13105");
        private static string fortuneString { get; } = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13107");
        private static string queenString { get; } = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13114");
        private static string landString { get; } = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13111");
        private static string rerunString { get; } = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13117");

        private static TemporaryAnimatedSprite tvScreen
        {
            get
            {
                return Helper.Reflection.GetField<TemporaryAnimatedSprite>(tv, "screen").GetValue();
            }
            set
            {
                Helper.Reflection.GetField<TemporaryAnimatedSprite>(tv, "screen").SetValue(value);
            }
        }

        private static TemporaryAnimatedSprite tvOverlay
        {
            get
            {
                return Helper.Reflection.GetField<TemporaryAnimatedSprite>(tv, "screenOverlay").GetValue();
            }
            set
            {
                Helper.Reflection.GetField<TemporaryAnimatedSprite>(tv, "screenOverlay").SetValue(value);
            }
        }

        private static TV tv;

        private static int currentpage = 0;
        private static List<List<Response>> pages = new List<List<Response>>();
        private static Dictionary<string, TVChannel> channels = new Dictionary<string, TVChannel>();

        internal static void load()
        {
            loadDefaultChannels();
        }

        private static void loadDefaultChannels()
        {
            addChannel("weather", weatherString, showOriginalProgram);
            addChannel("fortune", fortuneString, showOriginalProgram);
            addChannel("land", landString, showOriginalProgram);
            addChannel("queen", queenString, showOriginalProgram);
            addChannel("rerun", rerunString, showOriginalProgram);
        }

        private static void showOriginalProgram(TV tv, TemporaryAnimatedSprite sprite, SFarmer who, string a)
        {
            switch (a)
            {
                case "weather": a = "Weather"; break;
                case "queen": a = "The"; break;
                case "rerun": a = "The"; break;
                case "land": a = "Livin'"; break;
                case "fortune": a = "Fortune"; break;
                default: a = "Weather"; break;
            }

            tv.selectChannel(who, a);
        }

        internal static bool checkForAction(TV active, SFarmer who, bool justCheckingForActivity = false)
        {
            if (justCheckingForActivity)
                return true;

            tv = active;
            showChannels(0);

            return true;
        }

        private static void showChannels(int page)
        {
            currentpage = page;
            string question = Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13120", new object[0]);
            List<string> defaults = new List<string>(new string[5] { "fortune", "weather", "queen", "rerun", "land" });

            Response more = new Response("more", "(More)");
            Response leave = new Response("leave", Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13118", new object[0]));

            pages = new List<List<Response>>();
            List<Response> responses = new List<Response>();

            if (channels.ContainsKey("weather"))
                responses.Add(new Response("weather", channels["weather"].text));

            if (channels.ContainsKey("fortune"))
                responses.Add(new Response("fortune", channels["fortune"].text));

            string text = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            if ((text.Equals("Mon") || text.Equals("Thu")) && channels.ContainsKey("land"))
                responses.Add(new Response("land", channels["land"].text));

            if (text.Equals("Sun") && channels.ContainsKey("queen"))
                responses.Add(new Response("queen", channels["queen"].text));

            if (text.Equals("Wed") && Game1.stats.DaysPlayed > 7u && channels.ContainsKey("rerun"))
                responses.Add(new Response("rerun", channels["rerun"].text));

            foreach (string id in channels.Keys)
            {
                if (defaults.Contains(id)) { continue; }

                responses.Add(new Response(id, channels[id].text));

                if (responses.Count > 7)
                {
                    if (!responses.Contains(more))
                        responses.Add(more);

                    if (!responses.Contains(leave))
                        responses.Add(leave);

                    pages.Add(new List<Response>(responses.ToArray()));
                    responses = new List<Response>();
                }

            }

            if (!responses.Contains(leave))
                responses.Add(leave);

            if (responses.Count > 1)
                pages.Add(new List<Response>(responses.ToArray()));

            Game1.currentLocation.createQuestionDialogue(question, pages[page].ToArray(), new GameLocation.afterQuestionBehavior(selectChannel), null);
            Game1.player.Halt();
        }

        public static void addChannel(string id, string name, Action<TV, TemporaryAnimatedSprite, SFarmer, string> action)
        {
            channels.AddOrReplace(id, new TVChannel(id, name, action));
        }

        public static void addChannel(TVChannel tvChannel)
        {
                channels.AddOrReplace(tvChannel.id, tvChannel);
        }

        public static void changeAction(string id, Action<TV, TemporaryAnimatedSprite, SFarmer, string> action)
        {
            if (channels.ContainsKey(id))
                channels[id].action = action;
        }

        public static void removeKey(string key)
        {
            channels.Remove(key);
        }

        public static void showProgram(TemporaryAnimatedSprite sprite, string text, Action afterDialogues = null, TemporaryAnimatedSprite overlay = null)
        {
            if (tv == null)
                return;

            if (afterDialogues == null)
                afterDialogues = TVChannel.endProgram;

            tvScreen = sprite;

            if (overlay != null)
                tvOverlay = overlay;

            Game1.drawObjectDialogue(Game1.parseText(text));
            Game1.afterDialogues = new Game1.afterFadeFunction(afterDialogues);
        }

        public static void showProgram(TVChannel tvChannel)
        {
            if (tv == null)
                return;

            tvScreen = tvChannel.sprite;

            if (tvChannel.overlay != null)
                tvOverlay = tvChannel.overlay;

            Game1.drawObjectDialogue(Game1.parseText(tvChannel.text));
            Game1.afterDialogues = new Game1.afterFadeFunction(tvChannel.afterDialogues);
        }

        public static void showProgram(string id)
        {
            showProgram(channels[id]);
        }

        public static void endProgram()
        {
            if(tv != null)
                tv.turnOffTV();
            tv = null;
        }

        public static void selectChannel(SFarmer who, string answer)
        {
            string a = answer.Split(' ')[0];
            Monitor.Log("Select Channel:" + a, LogLevel.Trace);

            if (a == "more")
                showChannels(currentpage + 1);
            else if (channels.ContainsKey(a))
                channels[a].action.Invoke(tv, tvScreen, who, a);
        }


    }
}
