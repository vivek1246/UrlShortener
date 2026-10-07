using UrlShortener.Api.Services.Interfaces;

namespace UrlShortener.Api.Services
{
    public class UrlValidationService : IUrlValidationService
    {
        private static readonly HashSet<string> BlockedDomains =
            new(StringComparer.OrdinalIgnoreCase) { "malware.com" };

        private static readonly HashSet<string> PrivateHosts =
            new(StringComparer.OrdinalIgnoreCase)
            { "localhost", "127.0.0.1", "0.0.0.0", "169.254.169.254" };

        private static readonly string[] PrivateRanges =
            ["192.168.", "10.", "172.16.", "172.17.", "172.18.",
         "172.19.", "172.20.", "172.21.", "172.22.", "172.23.",
         "172.24.", "172.25.", "172.26.", "172.27.", "172.28.",
         "172.29.", "172.30.", "172.31."];

        public ValidationResult Validate(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new ValidationResult(false, "URL cannot be empty");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return new ValidationResult(false, "Invalid URL format");

            if (uri.Scheme is not ("http" or "https"))
                return new ValidationResult(false,
                    "Only HTTP and HTTPS URLs are supported");

            var host = uri.Host.ToLowerInvariant();

            if (PrivateHosts.Contains(host) ||
                PrivateRanges.Any(r => host.StartsWith(r)))
                return new ValidationResult(false,
                    "Private and internal URLs cannot be shortened");

            if (BlockedDomains.Contains(host))
                return new ValidationResult(false, "This domain is not allowed");

            return new ValidationResult(true);
        }
    }
}
