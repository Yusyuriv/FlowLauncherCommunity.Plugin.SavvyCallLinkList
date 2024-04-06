using System.Text.Json.Serialization;

namespace FlowLauncherCommunity.Plugin.SavvyCalLinkList;

public record ApiResponse(ApiResponseItem[] Entries, ApiResponseMetadata Metadata);

public record ApiResponseItem {
    public string Id { get; init; }
    public string State { get; init; }
    public string Slug { get; init; }
    public string Name { get; init; }
    [JsonPropertyName("private_name")]
    public string? PrivateName { get; init; }
    public string Description { get; init; }
    public ApiResponseScope Scope { get; init; }
}

public record ApiResponseMetadata(string? Before, string? After, int Limit);

public record ApiResponseScope(string Id, string Name, string Slug);

public record SavvyLink(string Id, string Name, string PrivateName, string Link);