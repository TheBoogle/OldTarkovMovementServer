using SPTarkov.Common.Models.Logging;
using SPTarkov.DI;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace ClassicMovementServer;

/// <summary>
/// Metadata for this mod.
/// </summary>
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.boogle.classicmovement";
    public string Name { get; init; } = "ClassicMovement";
    public string Author { get; init; } = "Boogle";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.1.3");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://sp-mod.com/mod/1860/classic-movement";
    public string? License { get; init; } = "MIT";
}

/// <summary>
/// Loads the config file on startup and makes it available to the router.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class OldTarkovMovementLoader(
    ISptLogger<OldTarkovMovementLoader> Logger,
    JsonUtil jsonUtil,
    ModHelper ModHelper)
    : IOnLoad
{
    public static OldTarkovMovementConfig? LoadedConfig;

    public class OldTarkovMovementConfig
    {
        public bool NostalgiaMode { get; set; }
        public bool DoesAimingSlowYouDown { get; set; }
        public bool QuickTilting { get; set; }
        public bool NoInertia { get; set; }
        public bool BotsUseOldMovement { get; set; }
        public bool DoBushesSlowYouDown { get; set; }
        public bool RemoveJitteryRotation { get; set; }
    }

    public Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        var PathToMod = ModHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var ConfigFolder = Path.Combine(PathToMod, "Config");

        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
        }

        var ConfigPath = Path.Combine(ConfigFolder, "settings.jsonc");

        var ConfigData = ModHelper.GetRawFileData(ConfigFolder, "settings.jsonc");

        LoadedConfig = jsonUtil.Deserialize<OldTarkovMovementConfig>(ConfigData);

        Logger.Success($"Loaded Classic Movement config from: {ConfigPath}");

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds a route for fetching the loaded config.
/// </summary>
[Injectable]
public class OldTarkovMovementRouter : StaticRouter
{
    private static HttpResponseUtil _HttpResponseUtil;

    public OldTarkovMovementRouter(JsonUtil JsonUtil, HttpResponseUtil HttpResponseUtil)
        : base(JsonUtil, GetCustomRoutes(JsonUtil))
    {
        _HttpResponseUtil = HttpResponseUtil;
    }

    private static List<RouteAction> GetCustomRoutes(JsonUtil JsonUtil)
    {
        return
        [
            new RouteAction(
                "/ClassicMovement/GetConfig",
                async (url, info, sessionId, output, cancellationToken) =>
                    await HandleRoute(url, JsonUtil, info, sessionId)
            )
        ];
    }

    private static ValueTask<object> HandleRoute(string Url, JsonUtil jsonUtil, IRequestData Info, MongoId SessionId)
    {
        if (OldTarkovMovementLoader.LoadedConfig is null)
        {
            return new ValueTask<object>("Config not loaded");
        }

        var Json = jsonUtil.Serialize(OldTarkovMovementLoader.LoadedConfig);
        return new ValueTask<object>(Json);
    }
}
