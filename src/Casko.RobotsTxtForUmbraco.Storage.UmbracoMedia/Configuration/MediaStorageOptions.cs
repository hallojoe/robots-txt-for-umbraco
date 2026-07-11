using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Storage.UmbracoMedia.Configuration;

public sealed class MediaStorageOptions
{
    public const string Key = "RobotsTxt:Storage:Media";
    
    public string FolderName { get; set; } = "robots.txt";

    public string MediaTypeAlias { get; set; } = "file";

    public string? MediaTypePropertyAlias { get; set; }
}


public sealed class MediaStorageOptionsValidator : IValidateOptions<MediaStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, MediaStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.FolderName))
        {
            return ValidateOptionsResult.Success;
        }

        if (!string.IsNullOrWhiteSpace(options.MediaTypeAlias))
        {
            return ValidateOptionsResult.Success;
        }
        
        var failures = new List<string>() { "RobotsTxt.MediaTypeAlias must be set." };

        return ValidateOptionsResult.Fail(failures);
    }
}


public static class MediaStorageOptionsResolver
{
    public static MediaStorageOptions? Resolve(MediaStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options;
    }
}