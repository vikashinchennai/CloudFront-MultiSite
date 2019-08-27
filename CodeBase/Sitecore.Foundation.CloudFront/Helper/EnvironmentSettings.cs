namespace Sitecore.Foundation.CloudFront.Helper
{
    using System.Configuration;
    internal class EnvironmentSettings
    {
        public static bool CanEnableCDNOnServer => bool.TryParse(ConfigurationManager.AppSettings["CDNEnabled"]?.ToString(), out bool op) && op;
    }
}