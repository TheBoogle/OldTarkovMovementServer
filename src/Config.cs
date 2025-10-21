using SPTarkov.DI;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Net;
using System.Reflection;

namespace OldTarkovMovementServer;

/// <summary>
/// Metadata for this mod.
/// </summary>
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.boogle.oldtarkovmovementserver";
    public override string Name { get; init; } = "OlTarkovMovementServer";
    public override string Author { get; init; } = "Boogle";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.1.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://forge.sp-tarkov.com/mod/1860/old-tarkov-movement-no-inertia";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

/// <summary>
/// Loads the config file on startup and makes it available to the router.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
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

    public Task OnLoad()
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

        Logger.Success($"Loaded Old Tarkov Movement config from: {ConfigPath}");

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
                "/OldTarkovMovement/GetConfig",
                async (url, info, sessionId, output) =>
                    await HandleRoute(url, JsonUtil, info, sessionId)
            )
        ];
    }

    private static ValueTask<string> HandleRoute(string Url, JsonUtil jsonUtil, IRequestData Info, MongoId SessionId)
    {
        if (OldTarkovMovementLoader.LoadedConfig is null)
        {
            return new ValueTask<string>("Config not loaded");
        }

        var Json = jsonUtil.Serialize(OldTarkovMovementLoader.LoadedConfig);
        return new ValueTask<string>(Json);
    }
}
