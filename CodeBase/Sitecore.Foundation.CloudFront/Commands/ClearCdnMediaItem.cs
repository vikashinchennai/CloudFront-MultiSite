namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;

    public class ClearCdnMediaItem : BaseCdnMediaItem
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

                if (!cdnHelper.IsItemAnMediaWithCdnField(item))
                    return;

                var op= SyncItemWithVersions(item, true);
                Refresh(item);
                ShowAlert(op, Constants.Message.ClearCdnMediaItemAlertMsg);

            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.ClearCdnMediaItemException + ex.ToString(), typeof(ClearCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }
            
    }
}