namespace Sitecore.Foundation.CloudFront.Pipeline
{
    using Sitecore.Foundation.CloudFront.Caching;
    using Sitecore.Foundation.CloudFront.Helper;
    using System;

    public class ClearCdnSyncOnTarget
    {
        public void ClearCdnCache(object sender, EventArgs args)
        {
            CustomCacheManager.ClearAllCache(Constants.Cache.CloudFrontKey);
        }
    }
}