namespace Sitecore.Foundation.CloudFront.Helper
{
    using Sitecore.Foundation.CloudFront.Handler;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.Data;
    using Sitecore.Data.Events;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.SecurityModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Sitecore.Foundation.CloudFront.Extensions;

    internal class CdnMediaItem : ICdnMediaItem
    {
        private readonly IAwsS3CdnServerHandler CdnHandler;
        private readonly ICdnHelper cdnHelper;

        public CdnMediaItem()
        {
            CdnHandler = ServiceLocator.ServiceProvider.GetService<IAwsS3CdnServerHandler>();
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
      }

        /// <summary>
        /// SyncItemWithVersions
        /// </summary>
        /// <param name="item"></param>
        /// <param name="UpdateItemAsSilent">True->Clear CDN, False->Sync CDN</param>
        /// <returns></returns>
        public List<string> SyncItemWithVersions(Item item, bool UpdateItemAsSilent = true)
        {
            var op = new List<string>();
            if (item != null && item.IsFieldExists(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
            {
                foreach (var eachItem in cdnHelper.GetItemWithAllVersionAndLanguage(item))
                {
                    bool isFileModified = !eachItem.GetFieldValue(Constants.MediaTemplateFields.UpdatedOn).Equals(eachItem.GetFieldValue(Constants.CdnBaseTemplateFields.CdnSyncedOn));


                    //To Remove the CDN if the Last Update is Changed
                    if (UpdateItemAsSilent || isFileModified)
                    {
                        this.DeleteFile(eachItem.GetFieldValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer));
                    }

                    if (UpdateItemAsSilent)
                    {
                        if (!SaveMediaItemDetails(eachItem, string.Empty))
                        {
                            
                            op.Add(eachItem.ID.ToString());
                        }
                    }
                    else
                    {
                        if (isFileModified || !eachItem.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                        {
                            op.AddRange(BeginUploadToCdn(eachItem));
                        }
                    }
                }
            }
            return op;
        }

        public List<string> BeginUploadToCdn(Item item, bool canUpdateItemWithCdnInfo = true, bool updateMaster = true, bool updateWeb = true)
        {
           var op = new List<string>();
            if (item == null)
                return op;

            var mediaItem = (MediaItem)item;
            if (mediaItem == null)
                return op;

            Stream mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null || mediaStream.Length == 0L)
            {
                //If Media Blob is missing, then we make sure the CDN url is removed.
                if (canUpdateItemWithCdnInfo && item.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                    if (!SaveMediaItemDetails(item, string.Empty))
                    {
                        op.Add(item.ID.ToString());
                    }

                return op;
            }

            string urlOnCdn = cdnHelper.BuildCdnMediaPathWithFilename(item);

            if (string.IsNullOrEmpty(urlOnCdn))
                return op;
            if (!this.SaveFile(mediaItem.MimeType, urlOnCdn, mediaStream, updateMaster, updateWeb))
            {
                op.Add(item.ID.ToString());
                return op;
            }

            if (canUpdateItemWithCdnInfo ? !SaveMediaItemDetails(mediaItem.InnerItem, urlOnCdn) : true)
            {
                op.Add(item.ID.ToString());
            }
            return op;
        }

        public List<string> SyncChildrenItems(Item item, bool UpdateItemAsSilent = true)
        {
            var op = new List<string>();
            if (item?.Axes != null)
            {
                List<Task<List<string>>> taskList = new List<Task<List<string>>>();
                int threadCnt = 0;
                try
                {
                    foreach (Item descendant in item.Axes.GetDescendants().Where(f => f.IsFieldExists(Constants.CdnBaseTemplateFields.UrlOnCDNServer)))
                    {
                        taskList.Add(Task.Factory.StartNew(() => SyncItemWithVersions(descendant, UpdateItemAsSilent)));
                        threadCnt++;

                        if (threadCnt >= 50)
                        {
                            // wait till all the async queries return
                            Task.WaitAll(taskList.ToArray());
                            op.AddRange(taskList.SelectMany(s => s.Result).Distinct());
                            taskList = new List<Task<List<string>>>();
                            threadCnt = 0;
                        }
                    }
                    Task.WaitAll(taskList.ToArray());
                    op.AddRange(taskList.SelectMany(s => s.Result).Distinct());
                }
                catch (Exception ex)
                {
                    Log.Error(Constants.Message.SyncChildrenItemsException + ex.Message, typeof(CdnMediaItem));
                }

            }
            return op;
        }


        public bool SaveFile(string contentType, string fullFileName, Stream mediaStream, bool updateMaster = true, bool updateWeb = true)
        {
            return CdnHandler.SaveFile(contentType, fullFileName, mediaStream, updateMaster, updateWeb);
        }
        public void DeleteFile(string url)
        {
            if (!string.IsNullOrEmpty(url) && CdnHandler != null)
                CdnHandler.DeleteFile(string.Format("{0}{1}", Constants.Path.MasterDbCdnPath, url));
        }
        public bool SaveMediaItemDetails(Item item, string urlOnCdn)
        {
            try
            {
                using (new SecurityDisabler())
                using (new DatabaseCacheDisabler())
                using (new EventDisabler())
                using (new BulkUpdateContext())
                {
                    try
                    {
                        item.Editing.BeginEdit();

                        item[Constants.CdnBaseTemplateFields.UrlOnCDNServer] = urlOnCdn;
                        var updated = DateUtil.IsoNow;

                        DateField dt = dt = item.Fields[Constants.MediaTemplateFields.UpdatedOn];
                        dt.Value = updated;

                        dt = item.Fields[Constants.CdnBaseTemplateFields.CdnSyncedOn];
                        dt.Value = updated;
                        item.Editing.AcceptChanges(true, true);
                        item.Editing.EndEdit(true, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Constants.Message.SaveMediaItemDetailsInnerException + ex.Message, typeof(CdnMediaItem));
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(Constants.Message.SaveMediaItemDetailsException + ex.Message, typeof(CdnMediaItem));
            }
            return false;
        }

        public void DeleteMultipleFiles(List<string> urls, bool deleteMaster = true, bool deleteWeb = false)
        {
            if (urls != null && urls.Any() && CdnHandler != null)
            {
                if (deleteMaster)
                    CdnHandler.DeleteMultipleFiles(urls.Select(url => string.Format("{0}{1}", Constants.Path.MasterDbCdnPath, url)).Distinct().ToList());

                if (deleteWeb)
                    CdnHandler.DeleteMultipleFiles(urls.Select(url => string.Format("{0}{1}", Constants.Path.WebDbCdnPath, url)).Distinct().ToList());
            }
        }
        public List<string> ClearCdnAndSyncOnWeb(bool canCleanMaster, bool canCleanWeb)
        {
            List<string> op = new List<string>();

            if (!EnvironmentSettings.CanEnableCDNOnServer)
                return op;

            if (canCleanMaster)
                op.AddRange(CleanCdnOnDatabase(true));

            if (canCleanWeb)
                op.AddRange(CleanCdnOnDatabase(false));

            return op;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isMaster">Master-->True & Web-->False</param>
        private List<string> CleanCdnOnDatabase(bool isMaster)
        {
            bool isWeb = false;
            string databaseName = Constants.Path.MasterDb;
            if (!isMaster)
            {
                isWeb = true;
                databaseName = Constants.Path.WebDb;
            }
            List<string> op = new List<string>();
            if (cdnHelper.IsCdnEnabledAtSitecore(databaseName))
            {
                //Clean All Master and update Master Files
                if(isMaster)
                {
                   CdnHandler.DeleteAllOnFolder(Constants.Path.MasterDbCdnPath);
                }
                if(isWeb)
                {
                    CdnHandler.DeleteAllOnFolder(Constants.Path.WebDbCdnPath);
                }
                var item = Database.GetDatabase(databaseName).GetItem(Constants.Item.MediaLibraryItemId);

                List<Task<List<string>>> taskList = new List<Task<List<string>>>();
                int threadCnt = 0;
              
                foreach (var eachItem in item.Axes.GetDescendants())
                {
                    taskList.Add(Task.Factory.StartNew(() => EachItemForFullCdnPush(isMaster, isWeb, eachItem)));

                    threadCnt++;

                    if (threadCnt >= 50)
                    {
                        // wait till all the async queries return
                        Task.WaitAll(taskList.ToArray());
                        op.AddRange(taskList.SelectMany(f => f.Result).Distinct());
                        taskList = new List<Task<List<string>>>();
                        threadCnt = 0;
                    }

                }
                Task.WaitAll(taskList.ToArray());
                op.AddRange(taskList.SelectMany(f => f.Result).Distinct());
            }
            return op;
        }

        private bool HasKeyFound(Dictionary<string,object> input, string  key)
        {
            return input != null && input.ContainsKey(key);
        }

        private List<string> EachItemForFullCdnPush(bool isMaster, bool isWeb, Item eachItem)
        {
            List<string> pendingToProcess = new List<string>();
            foreach (var _item in cdnHelper.GetItemWithAllVersionAndLanguage(eachItem))
            {
                if (_item == null)
                    continue;

                if (!_item.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                    continue;

                var mediaItem = (MediaItem)_item;
                if (mediaItem == null)
                    continue;

                using (Stream mediaStream = mediaItem.GetMediaStream())
                {
                    if (mediaStream == null || mediaStream.Length == 0L)
                    {
                        continue;
                    }

                    string urlOnCdn = _item.GetFieldValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer);
                    if (!SaveFile(mediaItem.MimeType, urlOnCdn, mediaStream, isMaster, isWeb))
                    {
                        pendingToProcess.Add(_item.ID.ToString());
                    }
                }
            }

            return pendingToProcess;
        }
    }
    public interface ICdnMediaItem
    {
        List<string> BeginUploadToCdn(Item item, bool canUpdateItemWithCdnInfo = true, bool updateMaster = true, bool updateWeb = true);
        void DeleteFile(string url);
        bool SaveMediaItemDetails(Item item, string urlOnCdn);
        bool SaveFile(string contentType, string fullFileName, Stream mediaStream, bool updateMaster, bool updateWeb);
        List<string> SyncItemWithVersions(Item item, bool UpdateItemAsSilent = true);
        List<string> SyncChildrenItems(Item item, bool UpdateItemAsSilent = true);
        void DeleteMultipleFiles(List<string> urls, bool deleteMaster = true, bool deleteWeb = false);
        List<string> ClearCdnAndSyncOnWeb(bool canCleanMaster, bool canCleanWeb);

    }
}