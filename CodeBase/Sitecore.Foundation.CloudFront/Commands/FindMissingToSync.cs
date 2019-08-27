namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.IO;
    using System.Linq;
    using Sitecore.Foundation.CloudFront.Extensions;

    public class FindMissingToSync : BaseCdnMediaItem
    {

        public override void Execute(CommandContext context)
        {
            try
            {
                if (!EnvironmentSettings.CanEnableCDNOnServer)
                    return;
                var item = Database.GetDatabase(Constants.Path.MasterDb).GetItem(Constants.Item.MediaLibraryItemId);
                if (item != null)
                {
                    try
                    {
                        var pendingItems = item.Axes.GetDescendants()
                            .Where(f => !IsMediaSynced(f))
                            .Select(f => f.ID.ToString()).Distinct().ToList();

                        ShowAlert(pendingItems,
                            (pendingItems == null || !pendingItems.Any()) ? "Nothing to Sync" : "Pending to Sync");
                        return;
                    }
                    catch { }
                    ShowAlert(null, Constants.Message.FindMissingToSyncAlert);
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.FindMissingToSyncException + ex.Message, typeof(SyncCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }

        private bool IsMediaSynced(Item item)
        {
            if (!item.IsFieldExists(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                return true;

            foreach (var eachItem in cdnHelper.GetItemWithAllVersionAndLanguage(item))
            {
                if (eachItem == null)
                    continue;

                var mediaItem = (MediaItem)item;
                if (mediaItem == null)
                    continue;

                Stream mediaStream = mediaItem.GetMediaStream();
                if (mediaStream == null || mediaStream.Length == 0L)
                {
                    continue;
                }
                if (!item.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                    return false;
            }
            return true;
        }

        public override CommandState QueryState(CommandContext context)
        {
            if (!EnvironmentSettings.CanEnableCDNOnServer)
                return CommandState.Hidden;

            return CommandState.Enabled;
        }
    }
}