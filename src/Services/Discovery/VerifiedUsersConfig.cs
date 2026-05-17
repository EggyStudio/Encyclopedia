using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Encyclopedia.Services.Discovery;

public sealed class VerifiedUsersConfig : IVerifiedUsersConfig
{
    public IReadOnlySet<string> VerifiedOwners { get; }
    public IReadOnlySet<string> VerifiedRepos  { get; }

    public VerifiedUsersConfig(string yamlPath)
    {
        if (!File.Exists(yamlPath))
        {
            VerifiedOwners = new HashSet<string>();
            VerifiedRepos  = new HashSet<string>();
            return;
        }

        var yaml = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var doc = deserializer.Deserialize<FileShape>(yaml) ?? new FileShape();

        VerifiedOwners = new HashSet<string>(doc.Owners ?? [], StringComparer.OrdinalIgnoreCase);
        VerifiedRepos  = new HashSet<string>(doc.Repos  ?? [], StringComparer.OrdinalIgnoreCase);
    }

    private sealed class FileShape
    {
        public List<string>? Owners { get; set; }
        public List<string>? Repos  { get; set; }
    }
}
