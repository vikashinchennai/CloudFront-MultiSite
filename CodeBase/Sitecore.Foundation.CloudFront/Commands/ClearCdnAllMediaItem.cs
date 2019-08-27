namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;
    public class ClearCdnAllMediaItem : BaseCdnMediaItem
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
                               
                var op = SyncItemsWithChildren(item, true);
                ShowAlert(op, Constants.Message.ClearCdnAllMediaItemAlertMsg);
                ResetCache();
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.ClearCdnAllMediaItemException + ex.ToString(), typeof(ClearCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }
    }
}