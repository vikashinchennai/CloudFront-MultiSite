namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Caching;
    using Sitecore.ContentSearch;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Security.Accounts;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BaseCdnMediaItem : Command
    {
        private readonly ICdnMediaItem cdnMediaItem;
        public override void Execute(CommandContext context) { }
        public readonly ICdnHelper cdnHelper;
        public BaseCdnMediaItem()
        {
            cdnMediaItem = ServiceLocator.ServiceProvider.GetService<ICdnMediaItem>();
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
        }

        protected void ResetCache()
        {
            var index = ContentSearchManager.GetIndex(Constants.Index.MasterIndex);
            index.RebuildAsync(IndexingOptions.Default, System.Threading.CancellationToken.None).Wait();
            CacheManager.ClearAllCaches();
        }
        protected void ShowAlert(List<string> Ids, string message)
        {
            if(Ids!=null && Ids.Any())
            {
                message = message + Constants.Message.PendingItemsToProcess + string.Join(", ", Ids);
            }
            SheerResponse.Alert(message);
        }
        protected List<String> ClearCdnAndSyncOnWeb(bool canCleanMaster, bool canCleanWeb)
        {
            return cdnMediaItem.ClearCdnAndSyncOnWeb(canCleanMaster, canCleanWeb);
        }

        protected List<string> SyncItemWithVersions(Item item, bool UpdateItemAsSilent = true)
        {
            return cdnMediaItem.SyncItemWithVersions(item, UpdateItemAsSilent);
        }

        protected List<string> SyncChildrenItems(Item item, bool UpdateItemAsSilent = true)
        {
            return cdnMediaItem.SyncChildrenItems(item, UpdateItemAsSilent);
        }
        protected List<string> SyncItemsWithChildren(Item item, bool UpdateItemAsSilent = true)
        {
            var isMediaFolder = cdnHelper.IsItemAMediaFolderItem(item);

            if (!isMediaFolder && !cdnHelper.IsItemAnMediaWithCdnField(item))
                return new List<string>();

            return (isMediaFolder)
                        ? SyncChildrenItems(item, UpdateItemAsSilent)
                        : SyncItemWithVersions(item, UpdateItemAsSilent).Union(SyncChildrenItems(item, UpdateItemAsSilent)).Distinct().ToList();
        }

        public void Refresh(Item item, string indexName = "sitecore_master_index")
        {
            var index = ContentSearchManager.GetIndex(indexName);
            if (index != null)
            {
                foreach (var _item in cdnHelper.GetItemWithAllVersionAndLanguage(item))
                {
                    var indexableItem = (SitecoreIndexableItem)item;
                    if (indexableItem != null)
                        index.Refresh(indexableItem);
                }
            }
        }
        public override CommandState QueryState(CommandContext context)
        {
            if (!EnvironmentSettings.CanEnableCDNOnServer)
                return CommandState.Hidden;

            Error.AssertObject((object)context, nameof(context));
            if (context.Items.Length == 0)
                return CommandState.Disabled;

            var item = context.Items[0];
            if (!item.Database.Name.Equals(Constants.Path.MasterDb))
                return CommandState.Disabled;
                       

            if(item.Paths.Path.StartsWith(Constants.Path.MediaLibraryStartPath))
                return CommandState.Enabled;

            if (item.ID.ToString() == Constants.Item.MediaLibraryItemId)
                return CommandState.Enabled;

            if (!item.Paths.IsMediaItem)
                return CommandState.Disabled;

            if (Context.User.IsAdministrator)
                return CommandState.Enabled;

            return base.QueryState(context);
        }
    }
}