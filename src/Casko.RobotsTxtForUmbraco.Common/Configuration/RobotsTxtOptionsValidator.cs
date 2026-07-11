using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public sealed class RobotsTxtOptionsValidator : IValidateOptions<RobotsTxtOptions>
{
    public ValidateOptionsResult Validate(string? name, RobotsTxtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Hosts.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (!options.Hosts.ContainsKey(options.DefaultHost))
        {
            failures.Add($"RobotsTxt:{nameof(RobotsTxtOptions.DefaultHost)} must reference an existing host key.");
        }

        foreach (var hostEntry in options.Hosts)
        {
            foreach (var profile in hostEntry.Value.Profiles)
            {
                if (string.IsNullOrWhiteSpace(profile))
                {
                    failures.Add($"RobotsTxt:Hosts:{hostEntry.Key}:Profiles contains an empty profile key.");
                    continue;
                }

                var trimmedProfile = profile.Trim();
                if (!options.Profiles.ContainsKey(trimmedProfile))
                {
                    failures.Add($"RobotsTxt:Hosts:{hostEntry.Key}:Profiles references unknown profile '{trimmedProfile}'.");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
