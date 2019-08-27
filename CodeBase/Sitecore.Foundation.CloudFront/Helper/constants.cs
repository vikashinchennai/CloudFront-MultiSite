namespace Sitecore.Foundation.CloudFront.Helper
{
    using Sitecore.Data;
    using System;

    internal static class Constants
    {
        internal static class Properties
        {
            public static string ContentTypeTextHtml => "text/html";
            public static string Shell => "shell";
            public static string ScMode => "sc_mode";
            public static string Site => "site";
            public static string Url => "url";
            public static string MediaRequest => "media:request";
            public static string ContentLength => "Content-Length";

            public static string IfNoneMatchHeader => "If-None-Match";
            public static string IfModifiedSinceHeader => "If-Modified-Since";

            public static string QueryStringLastModifiedKey => "mod";

        }
        internal static class Index
        {
            public static string MasterIndex => "sitecore_master_index";
        }
        internal static class Role
        {
            public static string CustomRole => @"sitecore\Clean Cdn And Push Media";
        }
        internal static class Path
        {       
            public static string MediaLibraryStartPath => "/sitecore/media library";
            public const string MasterDb = "master";
            public const string WebDb = "web";
            public static string MasterDbCdnPath => "master/";
            public static string WebDbCdnPath => "cdn/";

            public static string CdnPrefixForUrl => "https://cdn.";
            public static string WwwDot => "www.";

        }
        internal static class Message
        {
            public static string PendingItemsToProcess => ".\nBelow Ids not processed, please process manually again on them.\n";
            public static string ClearAndSyncAllMasterItemsAlertMsg => "Cleaned & Uploaded To CDN Fully for Item of Master Database";
            public static string ClearAndSyncAllMasterException => "Cloud Front CDN - ClearAndSyncAllMasterItems - Execute Error - .";
            public static string ClearAndSyncAllWebItemsAlertMsg => "Cleaned & Uplaoded To CDN Fully for Item of Master Database";
            public static string ClearAndSyncAllWebItemsException => "Cloud Front CDN - ClearAndSyncAllWebItems - Execute Error - .";

            public static string ClearCdnAllMediaItemAlertMsg => "Item With Its Children Cleaned from CDN Server Successfully";
            public static string ClearCdnAllMediaItemException => "Cloud Front CDN - ClearCdnAllMediaItem - Execute Error - .";
            public static string ClearCdnMediaItemAlertMsg => "Item Cleaned from CDN Server Successfully";
            public static string ClearCdnMediaItemException => "Cloud Front CDN - ClearCdnMediaItem - Execute Error - .";
            public static string FindMissingToSyncAlert => "Nothing To Sync";
            public static string FindMissingToSyncException => "Cloud Front CDN - FindMissingToSync -Execute - ";
            public static string SyncCdnAllMediaItemAlert => "Item & Its Children's Synced with CDN Server Successfully";
            public static string SyncCdnAllMediaItemMessage => "Cloud Front CDN - SyncCDNAllMediaItem -Execute - ";

            public static string SyncCdnMediaItemAlert => "Item Synced with CDN Server Successfully";
            public static string SyncCdnMediaItemException => "Cloud Front CDN - SyncCDNMediaItem -Execute - ";
            public static string SaveFileS3Exception => "Cloud Front CDN - AwsS3CdnServerHandler - SaveFile Error - .";
            public static string SaveFileStackTraceException => "Cloud Front CDN - AwsS3CdnServerHandler - SaveFile Error - .";
            public static string SaveFileException => "Cloud Front CDN - AwsS3CdnServerHandler - SaveFile Error - .";
            public static string DeleteSingleFileException => "Cloud Front CDN - AwsS3CdnServerHandler DeleteSingleFileException Exception : ";

            public static string DeleteMultipleFilesException => "Cloud Front CDN - AwsS3CdnServerHandler DeleteMultipleFiles Exception : ";
            public static string DeleteAllOnFolderException => "Cloud Front CDN - AwsS3CdnServerHandler DeleteAllOnFolder Exception : ";
            public static string ProcessImageDimensionsWarning => "Requested image exceeds allowed size limits. Requested URL:{0}";
            public static string ModifiedWarnMessage => "Can't parse header. The wrong value  - \"If-Modified-Since: {0}\" ";

            public static string IsMediaVersionedException => "Template is Not Valid Media Item of Sitecore Default template";
            public static string SaveMediaItemDetailsException => "Cloud Front CDN - CdnMediaItem -SaveMediaItemDetails Execute - ";
            public static string SaveMediaItemDetailsInnerException => "Cloud Front CDN - CdnMediaItem -SaveMediaItemDetails  InnerExecute - ";
            public static string SyncChildrenItemsException => "Cloud Front CDN - CdnMediaItem -SyncChildrenItems Execute - ";
        }
        internal static class Item
        {
            public static string MediaLibraryItemId => "{3D6658D8-A0BF-4E75-B3E2-D050FABCF4E1}";
        }
        internal static class SiteProperties
        {
            public static string CdnDomainUrl => "CdndomainUrl";
            public static string IsCdnEnabled => "IsCdnEnabled";

            public static string CanPrefixCdnAsSubDomain => "Yes";
        }

        internal static class Cache
        {
            public static string CloudFrontKey => "CloudFront";
            public static string IsCdnEnabledKey => "IsCdnEnabledKey";
        }

        internal static class TemplateFields
        {
            public static string DefaultCdnDomain=> "Default CDN Domain";
            public static string EnviromentName => "Environent Name";
            public static string IsEnabled => "Is Enabled";
            public static string S3BucketName => "S3 Bucket Name";
            public static string PublicKey => "Public Key";
            public static string SecretKey => "Secret Key";
            public static string Region => "Region";
            public static string Path => "path";
            public static string Height => "Height";
            public static string Width => "Width";
        }
        internal static class Template
        {
            public static ID CdnModuleItem => new ID("{506EE912-599C-4815-AB1F-62763443A8FC}");
            public static string CloudFrontTemplate => "CloudFront Settings";
            public static Guid MediaFolderTemplate => new Guid("{FE5DD826-48C6-436D-B87A-7C4210C7413B}");
        }

        internal static class CdnBaseTemplateFields
        {
            public static string CdnSyncedOn => "Cdn Synced On";
            public static string UrlOnCDNServer => "UrlOnCDNServer";
            public static string IgnoreCDNOnLoad => "Ignore CDN On Load";

        }

        internal static class MediaTemplateFields
        {
            public static string CdnEnabled => "CDN Enabled";
            public static ID VersionedMediaFieldId => new ID("{DBBE7D99-1388-4357-BB34-AD71EDF18ED3}");
            public static ID UnVersionedMediaFieldId => new ID("{40E50ED9-BA07-4702-992E-A912738D32DC}");
            public static string UpdatedOn => "__Updated";
        }
    }
}