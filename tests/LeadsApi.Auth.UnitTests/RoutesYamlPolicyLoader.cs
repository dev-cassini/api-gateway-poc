using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LeadsApi.Auth.UnitTests;

internal static class RoutesYamlPolicyLoader
{
    public static ApiRoutesPolicy Load(string? routesYamlPath = null)
    {
        var filePath = routesYamlPath ?? FindRoutesYamlPath();
        var yaml = File.ReadAllText(filePath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new RawAuthRuleYamlConverter())
            .IgnoreUnmatchedProperties()
            .Build();

        var raw = deserializer.Deserialize<RawRoutesDocument>(yaml);
        var routes = raw.Routes.Select(static route => new ApiRoutePolicy
        {
            Name = route.Name,
            Method = route.Method,
            Path = route.Path,
            Plugins = new ApiRoutePluginsPolicy
            {
                Auth = MapAuth(route.Plugins.Auth),
                Opa = MapOpa(route.Plugins.Opa)
            }
        }).ToList();

        return new ApiRoutesPolicy(routes);
    }

    private static AuthPolicy MapAuth(List<RawAuthRule>? rawRules)
    {
        var policy = new AuthPolicy
        {
            HasPolicy = rawRules is { Count: > 0 }
        };
        if (rawRules is null)
        {
            return policy;
        }

        foreach (var rule in rawRules)
        {
            if (rule.Scalar is not null &&
                string.Equals(rule.Scalar, "authenticated user", StringComparison.OrdinalIgnoreCase))
            {
                policy.RequiresAuthenticationUser = true;
            }

            if (rule.KeyValues is null)
            {
                continue;
            }

            if (rule.KeyValues.TryGetValue("scope", out var scopeValue) && !string.IsNullOrWhiteSpace(scopeValue))
            {
                foreach (var scope in scopeValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    policy.RequiredScopes.Add(scope);
                }
            }

            if (rule.KeyValues.TryGetValue("role", out var roleValue) && !string.IsNullOrWhiteSpace(roleValue))
            {
                foreach (var role in roleValue.Split("or", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    policy.RequiredRoles.Add(role);
                }
            }
        }

        return policy;
    }

    private static OpaPolicy MapOpa(List<Dictionary<string, string>>? rawRules)
    {
        var policy = new OpaPolicy();
        if (rawRules is null)
        {
            return policy;
        }

        foreach (var rule in rawRules)
        {
            if (rule.TryGetValue("staff type", out var staffType) && !string.IsNullOrWhiteSpace(staffType))
            {
                policy.RequiredStaffTypes.Add(staffType);
            }
        }

        return policy;
    }

    private static string FindRoutesYamlPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "LeadsApi", "routes.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate src/LeadsApi/routes.yaml from the current test directory.");
    }

    private sealed class RawAuthRuleYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(RawAuthRule);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is Scalar scalar)
            {
                parser.MoveNext();
                return new RawAuthRule { Scalar = scalar.Value };
            }

            if (parser.Current is MappingStart)
            {
                var map = rootDeserializer(typeof(Dictionary<string, string>)) as Dictionary<string, string> ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return new RawAuthRule { KeyValues = map };
            }

            throw new YamlException($"Unsupported auth rule node type: {parser.Current?.GetType().Name}");
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            throw new NotSupportedException("Serialization is not required for auth route policy tests.");
        }
    }
}

internal sealed class RawRoutesDocument
{
    public List<RawRoute> Routes { get; set; } = [];
}

internal sealed class RawRoute
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public RawPlugins Plugins { get; set; } = new();
}

internal sealed class RawPlugins
{
    public List<RawAuthRule>? Auth { get; set; }
    public List<Dictionary<string, string>>? Opa { get; set; }
}

internal sealed class RawAuthRule
{
    public string? Scalar { get; set; }
    public Dictionary<string, string>? KeyValues { get; set; }
}
