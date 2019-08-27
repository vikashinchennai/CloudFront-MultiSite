namespace Sitecore.Foundation.CloudFront.Commands
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Web.UI.Sheer;
    using System;
    public class ClearAndSyncAllMasterItems : BaseCdnMediaItem
    {
        public override void Execute(CommandContext context)
        {
            try
            {                
                if (!EnvironmentSettings.CanEnableCDNOnServer)
                    return;

                var op = ClearCdnAndSyncOnWeb(true, false);
                ShowAlert(op, Constants.Message.ClearAndSyncAllMasterItemsAlertMsg);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.ClearAndSyncAllMasterException + ex.ToString(), typeof(ClearCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }
    }

    public class ClearAndSyncAllWebItems : BaseCdnMediaItem
    {
        public override void Execute(CommandContext context)
        {
            try
            {
                if (!EnvironmentSettings.CanEnableCDNOnServer)
                    return;

                var op = ClearCdnAndSyncOnWeb(false, true);
                ShowAlert(op, Constants.Message.ClearAndSyncAllWebItemsAlertMsg);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.ClearAndSyncAllWebItemsException + ex.ToString(), typeof(ClearCdnMediaItem));
                SheerResponse.Alert(ex.ToString());
            }
        }
    }
}