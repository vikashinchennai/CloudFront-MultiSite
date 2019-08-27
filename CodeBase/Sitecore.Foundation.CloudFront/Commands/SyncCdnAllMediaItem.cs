namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;
    public class SyncCdnAllMediaItem : BaseCdnMediaItem
    {
        public override void Execute(CommandContext context)
        {
            try
            {
                if (!EnvironmentSettings.CanEnableCDNOnServer)
                    return;

                if (context == null)
                    throw new ArgumentNullException(nameof(context));

               var item = context.Items[0];
               
                var status = SyncItemsWithChildren(item, false);
                ShowAlert(status, Constants.Message.SyncCdnAllMediaItemAlert);
                ResetCache();
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.SyncCdnAllMediaItemMessage + ex.Message, typeof(SyncCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }     

    }
}