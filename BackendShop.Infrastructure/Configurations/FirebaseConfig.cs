namespace BackendShop.Infrastructure.Configurations;

/// <summary>
/// Firebase configuration options
/// </summary>
public class FirebaseConfig
{
    /// <summary>
    /// Section name in configuration
    /// </summary>
    public const string SectionName = "Firebase";

    /// <summary>
    /// Firebase Web API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Path to service account key file for Admin SDK
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }

    /// <summary>
    /// Firebase project ID
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;
}