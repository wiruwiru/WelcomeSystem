﻿using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;

namespace WelcomeSystem;

[MinimumApiVersion(290)]
public class WelcomeSystemBase : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleName => "WelcomeSystem";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleDescription => "Displays a welcome message on the player's screen";
    public required BaseConfigs Config { get; set; }
    private readonly Dictionary<int, bool> playerMessageShown = new();

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddCommand("welcome_reload", "Reloads the WelcomeSystem plugin configuration.", (player, commandInfo) =>
        {
            if (player == null || commandInfo == null) return;

            var permissionValidator = new RequiresPermissions("@css/root");
            if (!permissionValidator.CanExecuteCommand(player))
            {
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["NoPermissions"]}");
                return;
            }

            try
            {
                Server.ExecuteCommand("css_plugins reload WelcomeSystem");
                commandInfo.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["Reloaded"]}");
            }
            catch (Exception ex)
            {
                commandInfo.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["FailedToReload"]}: {ex.Message}");
            }
        });

        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawned @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player || player.IsBot)
            return HookResult.Continue;

        if (playerMessageShown.TryGetValue(player.Slot, out var hasShown) && hasShown)
        {
            return HookResult.Continue;
        }

        playerMessageShown[player.Slot] = true;

        AddTimer(0.8f, () =>
        {
            CCSPlayerPawn? pawn = player?.PlayerPawn.Value;
            CPlayer_CameraServices? camera = pawn?.CameraServices;
            if (player == null || pawn == null || camera == null)
            {
                return;
            }

            foreach (var textConfig in Config.Texts)
            {
                CPointWorldText? worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
                if (worldText == null || !worldText.IsValid) continue;

                worldText.MessageText = textConfig.MessageText;
                worldText.Enabled = true;
                worldText.FontSize = textConfig.FontSize;
                worldText.FontName = "Lucida Console";

                Color parsedColor = Color.FromName(textConfig.Color);
                worldText.Color = parsedColor.IsEmpty ? Color.White : parsedColor;

                worldText.Fullbright = true;
                worldText.WorldUnitsPerPx = textConfig.WorldUnitsPerPx;
                worldText.DepthOffset = 5.0f;

                worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
                worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
                worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

                QAngle eyeAngles = pawn.EyeAngles;
                Vector forward = new(), right = new(), up = new();
                NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

                float viewOffsetZ = camera.OldPlayerViewOffsetZ;

                Vector eyePosition = new();
                eyePosition += forward * textConfig.OffsetForward;
                eyePosition += right * textConfig.OffsetRight;
                eyePosition += up * (textConfig.OffsetZ + viewOffsetZ);

                QAngle angles = new();
                angles.Y = eyeAngles.Y + 270;
                angles.Z = eyeAngles.Z + 90;
                angles.X = 0;

                worldText.Teleport(pawn.AbsOrigin! + eyePosition, angles, new Vector(0, 0, 0));
                worldText.DispatchSpawn();

                worldText.AcceptInput("SetParent", pawn, null, "!activator");
                worldText.AcceptInput("SetParentAttachmentMaintainOffset", pawn, null, "viewmodel"); // axis_of_intent

                bool textDestroyed = false;

                AddTimer(0.1f, () =>
                {
                    if (textDestroyed || worldText == null || !worldText.IsValid) return;

                    if (player.Buttons.HasFlag(PlayerButtons.Use))
                    {
                        worldText.AcceptInput("Kill");
                        textDestroyed = true;
                    }
                }, TimerFlags.REPEAT);

                AddTimer(Config.DisplayDuration, () =>
                {
                    if (!textDestroyed && worldText != null && worldText.IsValid)
                    {
                        worldText.AcceptInput("Kill");
                        textDestroyed = true;
                    }
                });

                RegisterListener<Listeners.CheckTransmit>((CCheckTransmitInfoList infoList) =>
                {
                    foreach ((CCheckTransmitInfo info, CCSPlayerController? targetPlayer) in infoList)
                    {
                        if (targetPlayer == null || targetPlayer.Slot != player.Slot)
                        {
                            info.TransmitEntities.Remove(worldText);
                        }
                    }
                });
            }
        });
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event.Userid is not CCSPlayerController player)
            return HookResult.Continue;

        if (playerMessageShown.ContainsKey(player.Slot))
        {
            playerMessageShown.Remove(player.Slot);
        }

        return HookResult.Continue;
    }

    private void OnMapEnd()
    {
        playerMessageShown.Clear();
    }

}