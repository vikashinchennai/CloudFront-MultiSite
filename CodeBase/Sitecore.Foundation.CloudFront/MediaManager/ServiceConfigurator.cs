namespace Sitecore.Foundation.CloudFront.MediaManager
{
    using Sitecore.Foundation.CloudFront.Handler;
    using Sitecore.Foundation.CloudFront.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Abstractions;
    using Sitecore.DependencyInjection;

    public class ServiceConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(s => (BaseMediaManager)new CustomMediaManager(ServiceProviderServiceExtensions.GetService<BaseFactory>(s)));
            serviceCollection.AddSingleton<ICdnMediaItem, CdnMediaItem>();
            serviceCollection.AddSingleton<IAwsS3CdnServerHandler, AwsS3CdnServerHandler>();
            serviceCollection.AddSingleton<ICdnHelper, CdnHelper>();
        }
    }
}