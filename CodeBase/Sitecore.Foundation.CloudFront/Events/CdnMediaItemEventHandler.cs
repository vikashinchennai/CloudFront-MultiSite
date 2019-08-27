namespace Sitecore.Foundation.CloudFront.Events
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Events;
    using System;
    using System.Linq;
    using Sitecore.Foundation.CloudFront.Extensions;
    using Sitecore.Foundation.CloudFront.Caching;

    public class CdnMediaItemEventHandler
    {
        private readonly ICdnHelper cdnHelper;
        private readonly ICdnMediaItem cdnMediaItem;
        public CdnMediaItemEventHandler()
        {
            cdnMediaItem = ServiceLocator.ServiceProvider.GetService<ICdnMediaItem>();
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
        }

        public void OnItemRenamed(object sender, EventArgs args)
        {
            ProcessHandler(args, 0, true);
        }
        public void OnItemCopied(object sender, EventArgs args)
        {
            ProcessHandler(args, 1, true);
        }

        public void OnItemSaved(object sender, EventArgs args)
        {
            ProcessSitecoreEventArgsBasedRequest(args, 0, false);
        }
        public void OnItemDeleted(object sender, EventArgs args)
        {
            ProcessSitecoreEventArgsBasedRequest(args, 0, true);
        }

        private void ProcessSitecoreEventArgsBasedRequest(EventArgs args, int parameterPosition, bool isDeleteOperation = true)
        {
            if (args == null)
                return;

            var sitecoreEventArgs = args as SitecoreEventArgs;
            if (sitecoreEventArgs == null)
                return;
            var item = sitecoreEventArgs.Parameters[parameterPosition] as Item;

            ItemSaveOnMediaTemplate(item);

            if (!EnvironmentSettings.CanEnableCDNOnServer)
                return;
            try
            {

                if (!cdnHelper.IsItemAnMediaWithCdnField(item))
                    return;

                bool status = true;
                foreach (var eachItem in cdnHelper.GetItemWithAllVersionAndLanguage(item))
                {
                    if (isDeleteOperation)//Item Deleted
                    {
                        cdnMediaItem.DeleteFile(eachItem.GetFieldValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer));
                    }
                    else//Item Save Action
                    {
                        //To Skip the Update on AWS if no changes went.
                        if (//eachItem.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer) &&
                             eachItem.GetFieldValue(Constants.MediaTemplateFields.UpdatedOn).Equals(eachItem.GetFieldValue(Constants.CdnBaseTemplateFields.CdnSyncedOn)))
                            continue;

                        var op = cdnMediaItem.BeginUploadToCdn(eachItem);
                        status = status && (op == null || !op.Any());
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ProcessHandler(EventArgs args, int arrayPosition, bool canDeleteExisting, bool canSaveMediaItemDetails = true)
        {
            if (!EnvironmentSettings.CanEnableCDNOnServer)
                return;
            if (args == null)
                return;
            var item = Event.ExtractParameter(args, arrayPosition) as Item;

            if (!cdnHelper.IsItemAnMediaWithCdnField(item))
                return;

            if (canDeleteExisting)
            {
                var existingCdnFiles = cdnHelper.GetItemWithAllVersionAndLanguage(item)
                          ?.Select(s => s.GetFieldValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                          ?.Distinct()
                          ?.ToList();
                cdnMediaItem.DeleteMultipleFiles(existingCdnFiles);
            }

            if (canSaveMediaItemDetails)
            {
                foreach (var eachItem in cdnHelper.GetItemWithAllVersionAndLanguage(item))
                {
                    cdnMediaItem.BeginUploadToCdn(eachItem);
                }

            }
        }

        private void ItemSaveOnMediaTemplate(Item item)
        {
            if (item.TemplateID.Equals(new Guid("{0397DCCC-2BEA-4E1A-918C-051A9ED4D340}")))
            {
                CustomCacheManager.ClearAllCache(Constants.Cache.CloudFrontKey);
            }
        }
    }
}