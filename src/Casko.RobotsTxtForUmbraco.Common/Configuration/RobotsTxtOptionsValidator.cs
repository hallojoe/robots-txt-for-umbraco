using Casko.RobotsTxtForUmbraco.Common.Services;
using Microsoft.Extensions.Options;

namespace Casko.RobotsTxtForUmbraco.Common.Configuration;

public sealed class RobotsTxtOptionsValidator(
    IRobotsTxtBindingFileResolver bindingFileResolver) : IValidateOptions<RobotsTxtOptions>
{
    public ValidateOptionsResult Validate(string? name, RobotsTxtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        foreach (var bindingEntry in options.Hosts)
        {
            var bindingKey = bindingEntry.Key;
            var binding = bindingEntry.Value;

            if (binding is null)
            {
                failures.Add($"RobotsTxt:Bindings:{bindingKey} must not be null.");
                continue;
            }

            if (bindingKey.Length > 0 && string.IsNullOrWhiteSpace(bindingKey))
            {
                failures.Add("RobotsTxt:Bindings keys must not be whitespace.");
            }

            foreach (var include in binding.Include)
            {
                if (string.IsNullOrWhiteSpace(include))
                {
                    failures.Add($"RobotsTxt:Bindings:{bindingKey}:Include contains an empty file reference.");
                    continue;
                }

                try
                {
                    var fullPath = bindingFileResolver.ResolvePath(include);
                    if (!File.Exists(fullPath))
                    {
                        failures.Add($"RobotsTxt:Bindings:{bindingKey}:Include references missing file '{include.Trim()}'.");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    failures.Add(ex.Message);
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
