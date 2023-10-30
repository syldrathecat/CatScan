using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;

namespace CatScan.Ui;

public partial class MainWindow : Window, IDisposable
{
    private void DrawConfig()
    {
        using var tabId = ImRaii.PushId("Config");

        bool b;
        float f;

        b = Plugin.Configuration.SoundEnabled;
        if (ImGui.Checkbox("Enable Sound Alerts", ref b))
        {
            Plugin.Configuration.SoundEnabled = b;
            Plugin.Configuration.Save();
        }
        using (var soundIndent = ImRaii.PushIndent(24.0f))
        {
            using var soundId = ImRaii.PushId("Sound");
            using var disabled = ImRaii.Disabled(!Plugin.Configuration.SoundEnabled);

            f = Plugin.Configuration.SoundVolume * 100.0f;
            if (ImGui.SliderFloat("Volume", ref f, 0.0f, 100.0f, "%.0f%%"))
            {
                Plugin.Configuration.SoundVolume = f / 100.0f;
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (Plugin.Configuration.SoundEnabled)
                    Plugin.Notifications.PlaySfx("ping1.wav");
                Plugin.Configuration.Save();
            }

            b = Plugin.Configuration.SoundAlertFATE;
            if (ImGui.Checkbox("FATE Boss", ref b))
            {
                Plugin.Configuration.SoundAlertFATE = b;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            b = Plugin.Configuration.SoundAlertS;
            if (ImGui.Checkbox("S Rank", ref b))
            {
                Plugin.Configuration.SoundAlertS = b;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            b = Plugin.Configuration.SoundAlertA;
            if (ImGui.Checkbox("A Rank", ref b))
            {
                Plugin.Configuration.SoundAlertA = b;
                Plugin.Configuration.Save();
            }

            b = Plugin.Configuration.SoundAlertB;
            if (ImGui.Checkbox("B Rank", ref b))
            {
                Plugin.Configuration.SoundAlertB = b;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            b = Plugin.Configuration.SoundAlertMinions;
            if (ImGui.Checkbox("Minion", ref b))
            {
                Plugin.Configuration.SoundAlertMinions = b;
                Plugin.Configuration.Save();
            }
        }

        ImGui.Separator();

        b = Plugin.Configuration.AutoOpenEnabled;
        if (ImGui.Checkbox("Open Window Automatically", ref b))
        {
            Plugin.Configuration.AutoOpenEnabled = b;
            Plugin.Configuration.Save();
        }
        using (var autoOpenIndent = ImRaii.PushIndent(24.0f))
        {
            using var autoOpenId = ImRaii.PushId("AutoOpen");
            using var disabled = ImRaii.Disabled(!Plugin.Configuration.AutoOpenEnabled);

            b = Plugin.Configuration.AutoOpenFATE;
            if (ImGui.Checkbox("Epic FATE", ref b))
            {
                Plugin.Configuration.AutoOpenFATE = b;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            b = Plugin.Configuration.AutoOpenS;
            if (ImGui.Checkbox("S Rank", ref b))
            {
                Plugin.Configuration.AutoOpenS = b;
                Plugin.Configuration.Save();
            }
        }

        ImGui.Separator();

        b = Plugin.Configuration.AutoFlagEnabled;
        if (ImGui.Checkbox("Open Map Flag Automatically", ref b))
        {
            Plugin.Configuration.AutoFlagEnabled = b;
            Plugin.Configuration.Save();
        }
        using (var autoFlagIndent = ImRaii.PushIndent(24.0f))
        {
            using var autoFlagId = ImRaii.PushId("AutoFlag");
            using var disabled = ImRaii.Disabled(!Plugin.Configuration.AutoFlagEnabled);

            b = Plugin.Configuration.AutoFlagFATE;
            if (ImGui.Checkbox("Epic FATE", ref b))
            {
                Plugin.Configuration.AutoFlagFATE = b;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
            b = Plugin.Configuration.AutoFlagS;
            if (ImGui.Checkbox("S Rank", ref b))
            {
                Plugin.Configuration.AutoFlagS = b;
                Plugin.Configuration.Save();
            }
        }

        ImGui.Separator();

        b = Plugin.Configuration.ShowMissingKC;
        if (ImGui.Checkbox("Count missing KC mobs", ref b))
        {
            Plugin.Configuration.ShowMissingKC = b;
            Plugin.Configuration.Save();
        }

        ImGui.TextWrapped("Keeps track of mobs that are no longer visible to you. Use to estimate a possible kill count range when you are not killing alone.");

        ImGui.Separator();

        b = Plugin.Configuration.DebugEnabled;
        if (ImGui.Checkbox("Debug Enabled", ref b))
        {
            Plugin.Configuration.DebugEnabled = b;
            Plugin.Configuration.Save();
        }

        ImGui.TextWrapped("Displays Debug tab and opens plugin window on start-up.");
    }
}