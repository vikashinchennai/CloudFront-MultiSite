
namespace Sitecore.Foundation.CloudFront.Helper
{
    using Sitecore.Foundation.CloudFront.Model;
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;
    using Sitecore.Foundation.CloudFront.Extensions;
    using Sitecore.Foundation.CloudFront.Caching;
    using System.Web.Configuration;

    internal class CdnHelper : ICdnHelper
    {
        /// <summary>
        /// To Get the CDN Domain for the Site, this is mainly to handle the Multi site environment
        /// </summary>
        /// <returns></returns>
        public string GetCdnDomainUrl()
        {
            //Site Specific Domain
            var domainUrl = Context.Site.Properties[Constants.SiteProperties.CdnDomainUrl];

            //If the Cdn Url doesnt found & has a property to define CDN url as Current Domain prefix
            if (string.IsNullOrEmpty(domainUrl)
                && string.Equals(Context.Site.Properties[Constants.SiteProperties.CanPrefixCdnAsSubDomain], "Yes", StringComparison.OrdinalIgnoreCase))
            {

                domainUrl = Context.HttpContext.Request.Url.Host;
                if (domainUrl.StartsWith(Constants.Path.WwwDot))
                    domainUrl = domainUrl.Replace(Constants.Path.WwwDot, Constants.Path.CdnPrefixForUrl);
                else
                    domainUrl = Constants.Path.CdnPrefixForUrl + domainUrl;

                UpdateSiteCdnDomainUrl(domainUrl);
            }

            //Get Default Domain to Use
            if (string.IsNullOrEmpty(domainUrl))
            {
                return GetCdnModelForSite()?.DefaultCdnDomain ?? string.Empty;
            }

            return domainUrl;
        }

        public CdnPropertiesModel GetCdnModelForSite(bool updateSiteProperty = true, Item item = null)
        {
            CdnPropertiesModel cloudFrontCachevalue = CustomCacheManager.GetCache<CdnPropertiesModel>(Constants.Cache.CloudFrontKey);

            if (cloudFrontCachevalue != null && !string.IsNullOrEmpty(cloudFrontCachevalue.DefaultCdnDomain))
            {
                if (updateSiteProperty && string.IsNullOrEmpty(Context.Site.Properties[Constants.SiteProperties.CdnDomainUrl]))
                {
                    UpdateSiteCdnDomainUrl(cloudFrontCachevalue.DefaultCdnDomain);
                }
                return cloudFrontCachevalue;
            }
            var cdnFolder = item == null
                            ? Database.GetDatabase(Constants.Path.MasterDb).GetItem(Constants.Template.CdnModuleItem)
                            : item.Database.GetItem(Constants.Template.CdnModuleItem);
             if (cdnFolder != null && cdnFolder.HasChildren)
            {
                Item awsItem = null;
               awsItem = cdnFolder.GetChildren().FirstOrDefault(f =>
                          f.TemplateName == Constants.Template.CloudFrontTemplate && f.Fields != null &&
                          f.Fields[Constants.TemplateFields.IsEnabled].IsChecked() &&
                          f.Fields[Constants.TemplateFields.EnviromentName].HasValue &&
                          f.Fields[Constants.TemplateFields.DefaultCdnDomain].HasValue &&
                          f.Fields[Constants.TemplateFields.EnviromentName].Value.Split(',')
                                    .Any(a =>
                                    a.Trim().Equals(WebConfigurationManager.AppSettings["env:define"] ?? string.Empty, StringComparison.OrdinalIgnoreCase)));
                if (awsItem == null)
                {
                    awsItem = cdnFolder.GetChildren().FirstOrDefault(f =>
                          f.TemplateName == Constants.Template.CloudFrontTemplate && f.Fields != null &&
                          f.Fields[Constants.TemplateFields.IsEnabled].IsChecked() && f.Fields[Constants.TemplateFields.DefaultCdnDomain].HasValue &&
                         !f.Fields[Constants.TemplateFields.EnviromentName].HasValue);
                }
                if (awsItem != null)
                {
                    var region = awsItem.GetFieldValue(Constants.TemplateFields.Region);

                    var endPoint = Amazon.RegionEndpoint.EnumerableAllRegions
                        .FirstOrDefault(f => f.DisplayName.Equals(region, StringComparison.OrdinalIgnoreCase)
                                          || f.SystemName.Equals(region, StringComparison.OrdinalIgnoreCase));
                    CdnPropertiesModel model = new CdnPropertiesModel()
                    {
                        CdnType = CdnType.CloudFront,
                        DefaultCdnDomain = awsItem.GetFieldValue(Constants.TemplateFields.DefaultCdnDomain),
                        IsActive = true,
                        S3BucketName = awsItem.GetFieldValue(Constants.TemplateFields.S3BucketName),
                        PublicKey = awsItem.GetFieldValue(Constants.TemplateFields.PublicKey),
                        SecretKey = awsItem.GetFieldValue(Constants.TemplateFields.SecretKey),
                        Endpoint = endPoint
                    };

                    CustomCacheManager.SetCache(Constants.Cache.CloudFrontKey, model);
                    if (updateSiteProperty)
                    {
                        UpdateSiteCdnDomainUrl(cloudFrontCachevalue.DefaultCdnDomain);
                    }
                    return model;
                }
            }
          return null;
        }

        public void UpdateSiteCdnDomainUrl(string value)
        {
            Context.Site.Properties.Remove(Constants.SiteProperties.CdnDomainUrl);
            Context.Site.Properties.Add(Constants.SiteProperties.CdnDomainUrl, value);
        }

        /// <summary>
        /// To Validate and Confirm whether to Use CDN Or Not
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanUseCdnForMedia(MediaItem item)
        {
            if (bool.TryParse(Context.Site.Properties[Constants.SiteProperties.IsCdnEnabled], out bool op) && op)
            {
                var itemDb = item?.Database?.Name?.ToLower();
                if (itemDb == null || !IsCdnEnabled(itemDb))
                    return false;

                if ((itemDb != Constants.Path.WebDb && !(Context.PageMode.IsPreview || Context.PageMode.IsNormal))
                                        || !item.InnerItem.HasValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer)
                                        || item.InnerItem.IsChecked(Constants.CdnBaseTemplateFields.IgnoreCDNOnLoad))
                    return false;

                return true;
            }
            return false;
        }


        /// <summary>
        /// To Build the CDN Path with Database Name as primary folder for Non Web Database
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string BuildCdnMediaPathWithFilename(MediaItem item)
        {
            // URL Pattern --> 00000-00000-00000-00000/en/1/201901011212/Test.jpg

            return string.Format("{0}/{1}/{2}/{3}/{4}.{5}",
                        item.ID.Guid.ToString("D"),
                        item.InnerItem.Language.Name,
                        item.InnerItem.Version.Number,
                        item.InnerItem.GetFieldValue(Constants.MediaTemplateFields.UpdatedOn),
                        item.Name,
                        item.Extension
                        ).ToLower();
        }
        public string GetCdnUrlOnDatabase(string url, bool isMaster)
        {
            return string.Format("{0}{1}", isMaster ? Constants.Path.MasterDbCdnPath : Constants.Path.WebDbCdnPath, url);
        }
        public bool IsItemAMediaFolderItem(Item item)
        {
            if (item == null || item.Paths == null)
                return false;

            if (item.Paths.Path.Contains(GetMediaFolderPath(item)))
                return true;

            return (item.TemplateID.Equals(Constants.Template.MediaFolderTemplate));

        }
        public bool IsItemAnMediaWithCdnField(Item item, MediaItem mediaItem = null)
        {
            if (item == null || item.Paths == null || !item.Paths.IsMediaItem || !item.IsFieldExists(Constants.CdnBaseTemplateFields.UrlOnCDNServer))
                return false;

            //To Block the Process to Work only for Master Item Save
            if (!item.Database.Name.Equals(Constants.Path.MasterDb, StringComparison.OrdinalIgnoreCase))
                return false;
            if (mediaItem == null)
                mediaItem = (MediaItem)(item);

            if (mediaItem == null || mediaItem.Path == null || !mediaItem.Path.Contains(GetMediaFolderPath(item)) || string.IsNullOrEmpty(mediaItem.Extension))
                return false;

            return true;
        }

        public bool IsMediaVersioned(Item item, MediaItem mediaItem = null)
        {
            if (mediaItem == null)
                mediaItem = (MediaItem)(item);

            if (item.IsFieldExists(Constants.MediaTemplateFields.VersionedMediaFieldId))
                return true;

            if (item.IsFieldExists(Constants.MediaTemplateFields.UnVersionedMediaFieldId))
                return false;

            throw new Exception(Constants.Message.IsMediaVersionedException);
        }

        public string GetMediaFolderPath(MediaItem item)
        {
            return item?.Database?.GetItem(new ID(ItemIDs.MediaLibraryRoot.ToGuid()))?.Paths?.Path ?? string.Empty;
        }

        public MediaLibraryItem GetMediaItem(MediaItem item)
        {
            return new MediaLibraryItem()
            {
                Name = item.Name,
                ItemId = item.ID.ToGuid(),
                ParentItemId = item.InnerItem.ParentID.ToGuid(),
                TemplateId = item.InnerItem.TemplateID.ToGuid(),
                Language = item.InnerItem.Language.ToString(),
                Path = item.InnerItem.Paths.ParentPath,
                FileName = string.Format("{0}.{1}", (object)item.Name, (object)item.Extension).ToLower(),
                Extension = item.Extension
            };
        }

        public Item[] GetItemWithAllVersionAndLanguage(Item item)
        {
            if (IsMediaVersioned(item))
                return item.Versions.GetVersions(true);
            return new Item[] { item };
        }

        public string GetCdnRelativePathForItem(Item item)
        {
            //This is to make sure the Master is always pointing to Master
            return string.Format("{0}{1}",
                                    (item.Database.Name.Equals(Constants.Path.MasterDb, StringComparison.OrdinalIgnoreCase) ? Constants.Path.MasterDbCdnPath : Constants.Path.WebDbCdnPath)
                                    , item.GetFieldValue(Constants.CdnBaseTemplateFields.UrlOnCDNServer));
        }

        public string BuildCdnUrlWithQueryString(string url, string queryString, string updatedAt)
        {
            NameValueCollection queryStringCollection = HttpUtility.ParseQueryString(queryString);
            queryStringCollection.Remove(Constants.Properties.QueryStringLastModifiedKey);
            queryStringCollection.Add(Constants.Properties.QueryStringLastModifiedKey, updatedAt);

            return url + "?" + queryStringCollection.ToString();
        }

        public bool IsCdnEnabled(string database = Constants.Path.MasterDb)
        {
            return EnvironmentSettings.CanEnableCDNOnServer
                     && IsCdnEnabledAtSitecore(database);
        }
        public bool IsCdnEnabledAtSitecore(string database = Constants.Path.MasterDb)
        {
            return (Database.GetDatabase(database)?.GetItem(Constants.Template.CdnModuleItem)?.IsChecked(Constants.MediaTemplateFields.CdnEnabled) ?? false);

        }
    }

    public interface ICdnHelper
    {
        string GetCdnDomainUrl();
        CdnPropertiesModel GetCdnModelForSite(bool updateSiteProperty = true, Item item = null);
        void UpdateSiteCdnDomainUrl(string value);
        bool CanUseCdnForMedia(MediaItem item);
        string BuildCdnMediaPathWithFilename(MediaItem item);
        string GetCdnUrlOnDatabase(string url, bool isMaster);
        bool IsItemAMediaFolderItem(Item item);
        bool IsItemAnMediaWithCdnField(Item item, MediaItem mediaItem = null);
        bool IsMediaVersioned(Item item, MediaItem mediaItem = null);
        string GetMediaFolderPath(MediaItem item);
        MediaLibraryItem GetMediaItem(MediaItem item);
        Item[] GetItemWithAllVersionAndLanguage(Item item);
        string GetCdnRelativePathForItem(Item item);
        string BuildCdnUrlWithQueryString(string url, string queryString, string updatedAt);
        bool IsCdnEnabled(string database = Constants.Path.MasterDb);
        bool IsCdnEnabledAtSitecore(string database = Constants.Path.MasterDb);
    }

}