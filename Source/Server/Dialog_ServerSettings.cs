using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArtAi.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi.Server
{
    // Which server the mod uses, plus start/stop and settings of the local server
    public class Dialog_ServerSettings : Window
    {
        private static readonly string[] Presets = { "auto", "high", "mid", "low" };

        private static readonly string[] Devices = { LocalServerConfig.DeviceGpu, LocalServerConfig.DeviceCpu };

        private const float HeaderHeight = 35f;
        private const float RowHeight = 30f;
        private const float RowButtonWidth = 110f;
        private const float RowLabelPct = 0.3f;

        private const float StatusRequestSeconds = 2f;

        // edited copy, written to the file by the Save button;
        // null until the server creates the file (it chooses the device then)
        private LocalServerConfig config;

        private string portBuffer;
        // a buffer per field: a shared one would carry the text over when the device is switched
        private string gpuWidthBuffer;
        private string gpuHeightBuffer;
        private string cpuWidthBuffer;
        private string cpuHeightBuffer;
        private string stepsBuffer;
        private string openAiPortBuffer;

        private Vector2 scrollPosition;
        private float viewHeight = 2000f;
        private float nextStatusRequest;

        public override Vector2 InitialSize => new Vector2(760f, 800f);

        public Dialog_ServerSettings()
        {
            if (File.Exists(LocalServer.ConfigPath))
            {
                LoadConfig();
            }
            doCloseX = true;
            draggable = true;
            resizeable = true;
        }

        // the server address is edited in place, save it once
        public override void PostClose()
        {
            base.PostClose();
            ArtAiMod.Instance.WriteSettings();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Time.realtimeSinceStartup >= nextStatusRequest)
            {
                LocalServer.RequestStatus();
                nextStatusRequest = Time.realtimeSinceStartup + StatusRequestSeconds;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight), "AiArtServerTitle".Translate());
            Text.Font = GameFont.Small;

            float top = inRect.y + HeaderHeight + 10f;
            Rect outRect = new Rect(inRect.x, top, inRect.width, inRect.height - top);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            // one column: otherwise, when the content grows (e.g. a test result appears), Listing moves
            // the rest into a new column outside the view and the measured height collapses
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(viewRect);
            try
            {
                DrawContents(listing);
            }
            finally
            {
                // keep the GUI groups balanced if drawing fails, otherwise the whole window content disappears
                viewHeight = listing.CurHeight;
                listing.End();
                Widgets.EndScrollView();
            }
        }

        private void DrawContents(Listing_Standard listing)
        {
            Header(listing, "AiArtServerUse");
            ServerChoice.Draw(listing);
            listing.GapLine();
            DrawLocalServer(listing);
            listing.GapLine();
            DrawSettings(listing);
            listing.GapLine();
            Header(listing, "AiArtLocalManualTitle");
            listing.Label("AiArtLocalManual".Translate());
            listing.GapLine();
            Header(listing, "AiArtLocalInstructionsTitle");
            listing.Label("AiArtLocalInstructions".Translate());
            listing.GapLine();
            DrawTextModel(listing);
        }

        private static void DrawLocalServer(Listing_Standard listing)
        {
            Header(listing, "AiArtLocalTitle");
            listing.Label("AiArtLocalStatus".Translate(LocalStatusText()));
            listing.Gap();
            listing.Label("AiArtLocalReadFirst".Translate().Colorize(ColoredText.WarningColor));
            listing.Label("AiArtLocalReadFirstText".Translate());
            listing.Gap();

            bool running = LocalServer.Responding || LocalServer.IsStarting;
            Rect row = listing.GetRect(RowHeight);
            float width = (row.width - 20f) / 3f;
            if (Widgets.ButtonText(new Rect(row.x, row.y, width, row.height),
                    "AiArtLocalStart".Translate(), active: !running))
            {
                LocalServer.TryStart();
            }
            if (Widgets.ButtonText(new Rect(row.x + width + 10f, row.y, width, row.height),
                    "AiArtLocalStop".Translate(), active: running))
            {
                LocalServer.Stop();
            }
            if (Widgets.ButtonText(new Rect(row.x + 2 * (width + 10f), row.y, width, row.height),
                    "AiArtLocalOpenFolder".Translate()))
            {
                TryOpenFolder();
            }
        }

        private void LoadConfig()
        {
            LocalServer.ReloadConfig();
            config = LocalServer.Config.Copy();
        }

        private void DrawSettings(Listing_Standard listing)
        {
            Header(listing, "AiArtLocalSettings");
            // a running server has written the file before it started to answer
            if (config == null && LocalServer.Responding)
            {
                LoadConfig();
            }
            if (config == null)
            {
                listing.Label("AiArtLocalNoConfig".Translate());
                return;
            }
            listing.Label("AiArtLocalSettingsFile".Translate(LocalServer.ConfigPath));
            if (listing.ButtonTextLabeled("AiArtLocalDevice".Translate(), DeviceLabel(config.device)))
            {
                Find.WindowStack.Add(new FloatMenu(Devices
                    .Select(device => new FloatMenuOption(DeviceLabel(device), () => config.device = device))
                    .ToList()));
            }
            // only the settings of the selected device: the server uses only them
            if (config.device == LocalServerConfig.DeviceCpu)
            {
                DrawModel(listing, config.cpu_gguf_file, file => config.cpu_gguf_file = file);
                listing.TextFieldNumericLabeled("AiArtLocalWidth".Translate(), ref config.cpu_width,
                    ref cpuWidthBuffer, 256, 2048);
                listing.TextFieldNumericLabeled("AiArtLocalHeight".Translate(), ref config.cpu_height,
                    ref cpuHeightBuffer, 256, 2048);
            }
            else
            {
                if (listing.ButtonTextLabeled("AiArtLocalPreset".Translate(), PresetLabel(config.gpu_preset)))
                {
                    Find.WindowStack.Add(new FloatMenu(Presets
                        .Select(preset => new FloatMenuOption(PresetLabel(preset), () => config.gpu_preset = preset))
                        .ToList()));
                }
                DrawModel(listing, config.gpu_gguf_file, file => config.gpu_gguf_file = file);
                listing.TextFieldNumericLabeled("AiArtLocalWidth".Translate(), ref config.gpu_width,
                    ref gpuWidthBuffer, 256, 2048);
                listing.TextFieldNumericLabeled("AiArtLocalHeight".Translate(), ref config.gpu_height,
                    ref gpuHeightBuffer, 256, 2048);
            }
            listing.TextFieldNumericLabeled("AiArtLocalSteps".Translate(), ref config.steps, ref stepsBuffer, 1, 50);
            listing.TextFieldNumericLabeled("AiArtLocalPort".Translate(), ref config.port, ref portBuffer, 1024, 65535);
            listing.TextFieldNumericLabeled("AiArtLocalOpenAiPort".Translate(), ref config.openai_port,
                ref openAiPortBuffer, 0, 65535);
            config.style = listing.TextEntryLabeled("AiArtLocalStyle".Translate(), config.style);
            config.hf_home = listing.TextEntryLabeled("AiArtLocalModelsFolder".Translate(), config.hf_home);
            listing.Gap();
            if (listing.ButtonText("AiArtLocalSave".Translate()))
            {
                TrySave();
            }
        }

        private static void DrawTextModel(Listing_Standard listing)
        {
            Header(listing, "AiArtLlmTitle");
            string baseUrl = LocalServer.OpenAiUrl;
            if (baseUrl == null)
            {
                listing.Label("AiArtLlmOff".Translate());
                return;
            }
            listing.Label("AiArtLlmText".Translate());
            CopyField(listing, "AiArtLlmBaseUrl", baseUrl);
            CopyField(listing, "AiArtLlmChatUrl", baseUrl + "/chat/completions");
            CopyField(listing, "AiArtLlmModelName", "qwen3-4b");
            listing.Label("AiArtLlmNote".Translate());
        }

        // Read-only text field: the text can be selected and copied, but not changed
        private static void CopyField(Listing_Standard listing, string labelKey, string value)
        {
            Rect row = listing.GetRect(RowHeight);
            Widgets.Label(row.LeftPart(RowLabelPct), labelKey.Translate());
            Rect field = new Rect(row.x + row.width * RowLabelPct, row.y,
                row.width * (1f - RowLabelPct) - RowButtonWidth - 10f, row.height);
            // the edited text is ignored, so the field always shows the value
            Widgets.TextField(field, value);
            if (Widgets.ButtonText(row.RightPartPixels(RowButtonWidth), "AiArtCopy".Translate()))
            {
                GUIUtility.systemCopyBuffer = value;
                Messages.Message("AiArtCopied".Translate(value), MessageTypeDefOf.NeutralEvent, false);
            }
            listing.Gap(4f);
        }

        private static void DrawModel(Listing_Standard listing, string current, Action<string> select)
        {
            if (listing.ButtonTextLabeled("AiArtLocalModel".Translate(), current))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    ModelOption(LocalServerConfig.ModelFileQ5, "AiArtLocalModelQ5", select),
                    ModelOption(LocalServerConfig.ModelFileQ4, "AiArtLocalModelQ4", select)
                }));
            }
        }

        private static FloatMenuOption ModelOption(string file, string descriptionKey, Action<string> select)
        {
            return new FloatMenuOption(file + " — " + descriptionKey.Translate(), () => select(file));
        }

        private static string PresetLabel(string preset)
        {
            return ("AiArtLocalPreset_" + preset).Translate();
        }

        private static string DeviceLabel(string device)
        {
            return ("AiArtLocalDevice_" + device).Translate();
        }

        private static string LocalStatusText()
        {
            // one snapshot: the background check may replace LastCheck meanwhile
            ServerCheck check = LocalServer.LastCheck;
            if (check?.Kind == ServerCheck.Result.LocalServer)
                return "AiArtLocalRunning".Translate(check.Status.Text());
            if (LocalServer.IsStarting) return "AiArtLocalStarting".Translate();
            if (check == null) return "AiArtLocalChecking".Translate();
            if (check.Kind == ServerCheck.Result.Answers) return "AiArtLocalPortTaken".Translate();
            return "AiArtLocalNotRunning".Translate();
        }

        private static void Header(Listing_Standard listing, string key)
        {
            listing.Label(key.Translate().Colorize(ColoredText.TipSectionTitleColor));
        }

        private static void TryOpenFolder()
        {
            try
            {
                LocalServer.Install();
                ShellUtil.OpenFolder(LocalServer.Folder);
            }
            catch (Exception e)
            {
                Log.Warning("AiArt: cannot open the local server folder: " + e);
                Messages.Message(e.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void TrySave()
        {
            try
            {
                LocalServer.SaveConfig(config.Copy());
                Messages.Message("AiArtLocalSaved".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }
            catch (Exception e)
            {
                Log.Warning("AiArt: cannot save the local server settings: " + e);
                Messages.Message(e.Message, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
