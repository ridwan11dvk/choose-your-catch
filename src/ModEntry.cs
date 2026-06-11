using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Tools;

namespace TargetFishSync;

internal sealed class ModEntry : Mod
{
    private const string SelectionMessageType = "TargetFishSelection";

    private ModConfig Config = null!;
    private FishRepository FishRepository = null!;
    private readonly TargetFishService TargetService = new();
    private Harmony Harmony = null!;
    private CatchSortMode CurrentSort = CatchSortMode.Name;
    private CatchFilterMode CurrentFilter = CatchFilterMode.All;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        FishRepository = new FishRepository(Monitor, () => Config);

        TargetFishPatches.Service = TargetService;
        TargetFishPatches.Monitor = Monitor;
        TargetFishPatches.GetLocalDefaultQuality = () => Config.DefaultQuality;
        TargetFishPatches.IsFishAvailableForLocalPlayer = itemId =>
            FishRepository.IsFishAvailable(itemId, Game1.player.currentLocation, Game1.player, Config.AllowAllFish);
        TargetFishPatches.OnLocalFishUnavailable = displayName =>
            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(Helper.Translation.Get("hud.unavailable", new { fish = displayName })));

        Harmony = new Harmony(ModManifest.UniqueID);
        PatchGameMethods();

        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    private void PatchGameMethods()
    {
        var getFish = AccessTools.Method(
            typeof(GameLocation),
            nameof(GameLocation.GetFishFromLocationData),
            new[]
            {
                typeof(string),
                typeof(Vector2),
                typeof(int),
                typeof(Farmer),
                typeof(bool),
                typeof(bool),
                typeof(GameLocation),
                typeof(StardewValley.Internal.ItemQueryContext)
            });

        if (getFish is not null)
        {
            Harmony.Patch(
                getFish,
                postfix: new HarmonyMethod(typeof(TargetFishPatches), nameof(TargetFishPatches.GetFishFromLocationDataPostfix)));
        }
        else
        {
            Monitor.Log("Couldn't patch GameLocation.GetFishFromLocationData; target fish replacement will not run.", LogLevel.Warn);
        }

        var minigameStart = AccessTools.Method(typeof(FishingRod), nameof(FishingRod.startMinigameEndFunction));
        if (minigameStart is not null)
        {
            Harmony.Patch(
                minigameStart,
                prefix: new HarmonyMethod(typeof(TargetFishPatches), nameof(TargetFishPatches.StartMinigameEndFunctionPrefix)));
        }

        var createFish = AccessTools.Method(typeof(FishingRod), "CreateFish");
        if (createFish is not null)
        {
            Harmony.Patch(
                createFish,
                postfix: new HarmonyMethod(typeof(TargetFishPatches), nameof(TargetFishPatches.CreateFishPostfix)));
        }
        else
        {
            Monitor.Log("Couldn't patch FishingRod.CreateFish; target quality will not be applied.", LogLevel.Warn);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null)
        {
            return;
        }

        if (!Config.OpenMenuKey.JustPressed())
        {
            return;
        }

        Helper.Input.Suppress(e.Button);
        if (IsActivelyFishing(Game1.player))
        {
            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(
                Helper.Translation.Get("hud.stop-fishing")));
            return;
        }

        OpenMenu();
    }

    private static bool IsActivelyFishing(Farmer player)
    {
        if (player.CurrentTool is not FishingRod rod)
        {
            return false;
        }

        return FishingStateGuard.IsActive(
            rod.isTimingCast,
            rod.isCasting,
            rod.castedButBobberStillInAir,
            rod.isFishing,
            rod.hit,
            rod.isNibbling,
            rod.isReeling,
            rod.pullingOutOfWater,
            rod.fishCaught,
            rod.showingTreasure,
            rod.treasureCaught);
    }

    private void OpenMenu()
    {
        var fish = FishRepository.GetFishForCurrentContext(Config.AllowAllFish);
        Game1.activeClickableMenu = new TargetFishMenu(
            fish,
            Config.DefaultQuality,
            entry =>
            {
                SetLocalSelection(new TargetFishSelection(entry.ItemId, entry.DisplayName, Config.DefaultQuality));
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(
                    Helper.Translation.Get("hud.selected", new { fish = entry.DisplayName })));
            },
            () =>
            {
                SetLocalSelection(null);
                Game1.addHUDMessage(HUDMessage.ForCornerTextbox(Helper.Translation.Get("hud.disabled")));
            },
            CurrentSort,
            CurrentFilter,
            sort => CurrentSort = sort,
            filter => CurrentFilter = filter,
            Helper.Translation);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModConfigMenu();
    }

    private void SetupGenericModConfigMenu()
    {
        var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
        {
            return;
        }

        api.Register(
            ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config));

        api.AddKeybindList(
            ModManifest,
            () => Config.OpenMenuKey,
            value => Config.OpenMenuKey = value,
            () => Helper.Translation.Get("config.openMenuKey.name"),
            () => Helper.Translation.Get("config.openMenuKey.tooltip"));

        api.AddBoolOption(
            ModManifest,
            () => Config.AllowAllFish,
            value => Config.AllowAllFish = value,
            () => Helper.Translation.Get("config.allowAllFish.name"),
            () => Helper.Translation.Get("config.allowAllFish.tooltip"));

        api.AddBoolOption(
            ModManifest,
            () => Config.ShowOnlyFish,
            value => Config.ShowOnlyFish = value,
            () => Helper.Translation.Get("config.showOnlyFish.name"),
            () => Helper.Translation.Get("config.showOnlyFish.tooltip"));

        api.AddBoolOption(
            ModManifest,
            () => Config.RespectSpawningRules,
            value => Config.RespectSpawningRules = value,
            () => Helper.Translation.Get("config.respectSpawningRules.name"),
            () => Helper.Translation.Get("config.respectSpawningRules.tooltip"));

        api.AddTextOption(
            ModManifest,
            () => Config.DefaultQuality.ToString(),
            value =>
            {
                if (Enum.TryParse(value, out FishQuality quality))
                {
                    Config.DefaultQuality = quality;
                }
            },
            () => Helper.Translation.Get("config.defaultQuality.name"),
            () => Helper.Translation.Get("config.defaultQuality.tooltip"),
            Enum.GetNames<FishQuality>());
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo("Data/Locations") || name.IsEquivalentTo("Data/Fish")))
        {
            FishRepository.ClearCache();
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        FishRepository.ClearCache();
        TargetService.Clear();
    }

    private void SetLocalSelection(TargetFishSelection? selection)
    {
        var playerId = Game1.player.UniqueMultiplayerID;
        TargetService.Set(playerId, selection);
        Helper.Multiplayer.SendMessage(
            TargetFishMessage.From(playerId, selection),
            SelectionMessageType,
            new[] { ModManifest.UniqueID });
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModManifest.UniqueID || e.Type != SelectionMessageType)
        {
            return;
        }

        var message = e.ReadAs<TargetFishMessage>();
        var hostId = Game1.MasterPlayer.UniqueMultiplayerID;
        if (message.PlayerId != e.FromPlayerID && e.FromPlayerID != hostId)
        {
            Monitor.Log(
                $"Ignored target fish message for player {message.PlayerId} sent by player {e.FromPlayerID}.",
                LogLevel.Warn);
            return;
        }

        var selection = message.ToSelection();
        if (selection is not null && !ItemRegistry.Exists(selection.ItemId))
        {
            Monitor.Log(
                $"Ignored target fish '{selection.ItemId}' from player {message.PlayerId} because the item isn't installed locally.",
                LogLevel.Warn);
            return;
        }

        TargetService.Set(message.PlayerId, selection);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            foreach (var pair in TargetService.GetAll())
            {
                Helper.Multiplayer.SendMessage(
                    TargetFishMessage.From(pair.Key, pair.Value),
                    SelectionMessageType,
                    new[] { ModManifest.UniqueID },
                    new[] { e.Peer.PlayerID });
            }

            return;
        }

        var localSelection = TargetService.Get(Game1.player.UniqueMultiplayerID);
        if (localSelection is not null)
        {
            Helper.Multiplayer.SendMessage(
                TargetFishMessage.From(Game1.player.UniqueMultiplayerID, localSelection),
                SelectionMessageType,
                new[] { ModManifest.UniqueID },
                new[] { e.Peer.PlayerID });
        }
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        TargetService.Remove(e.Peer.PlayerID);
    }
}
