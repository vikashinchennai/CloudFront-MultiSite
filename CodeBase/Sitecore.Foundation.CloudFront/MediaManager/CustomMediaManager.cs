namespace Sitecore.Foundation.CloudFront.MediaManager
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Abstractions;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.IO;
    using Sitecore.Resources.Media;
    using Sitecore.Resources.Media.MediaCreators;
    using Sitecore.Web;
    using Sitecore.Xml;
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Xml;
    public class CustomMediaManager : BaseMediaManager
    {
        private MediaCache cache = new MediaCache();
        private ImageEffects effects = new ImageEffects();
        private MimeResolver mimeResolver = new MimeResolver();
        private MediaConfig config;
        private MediaCreator creator;
        private readonly ICdnHelper cdnHelper;
        public CustomMediaManager(BaseFactory factory)
        {
            XmlNode configNode = factory.GetConfigNode("mediaLibrary");
            this.config = configNode == null ? new MediaConfig() : new MediaConfig(configNode);
            this.creator = this.ResolveCreator(factory, configNode);
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
        }

        public override MediaCache Cache
        {
            get
            {
                return this.cache;
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.cache = value;
            }
        }

        public override MediaConfig Config
        {
            get
            {
                return this.config;
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.config = value;
            }
        }

        public override MediaCreator Creator
        {
            get
            {
                return this.creator;
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.creator = value;
            }
        }

        public override ImageEffects Effects
        {
            get
            {
                return this.effects;
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.effects = value;
            }
        }

        public override string MediaLinkPrefix
        {
            get
            {
                return this.Config.MediaLinkPrefix;
            }
        }

        public override MimeResolver MimeResolver
        {
            get
            {
                return this.mimeResolver;
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.mimeResolver = value;
            }
        }

        public override Media GetMedia(MediaItem item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return this.GetMedia(this.GetMediaData(item));
        }

        public override Media GetMedia(MediaUri mediaUri)
        {
            Assert.ArgumentNotNull((object)mediaUri, nameof(mediaUri));
            MediaData mediaData = this.GetMediaData(mediaUri);
            if (mediaData == null)
                return (Media)null;
            return this.GetMedia(mediaData);
        }

        protected virtual Media GetMedia(MediaData mediaData)
        {
            Assert.ArgumentNotNull((object)mediaData, nameof(mediaData));
            return MediaManager.Config.ConstructMediaInstance(mediaData);
        }

        public override string GetMediaUrl(MediaItem item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return this.GetMediaUrl(this.GetMediaUrl(item, MediaUrlOptions.Empty), item);
        }

        public string GetMediaUrl(string mediaUrl, MediaItem item)
        {
            return GetCdnOrNormalUrl(mediaUrl, item);
        }

        public string GetCdnOrNormalUrl(string mediaUrl, MediaItem item)
        {
            //Preview in Master can load CDN
            if (cdnHelper.CanUseCdnForMedia(item))
            {
                var cdnDomainUrl = cdnHelper.GetCdnDomainUrl();
                if (string.IsNullOrEmpty(cdnDomainUrl))
                    return mediaUrl;

                if (Uri.TryCreate(new Uri(cdnDomainUrl), cdnHelper.GetCdnRelativePathForItem(item), out Uri op))
                    return op.ToString();
            }
            return mediaUrl;
        }
        public override string GetMediaUrl(MediaItem item, MediaUrlOptions options)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            Assert.ArgumentNotNull((object)options, nameof(options));
            Assert.IsTrue(this.Config.MediaPrefixes[0].Length > 0, "media prefixes are not configured properly.");
            string str1 = this.MediaLinkPrefix;
            if (options.AbsolutePath)
                str1 = options.VirtualFolder + str1;
            else if (str1.StartsWith("/", StringComparison.InvariantCulture))
                str1 = StringUtil.Mid(str1, 1);
            string part2 = MainUtil.EncodePath(str1, '/');
            if (options.AlwaysIncludeServerUrl)
                part2 = FileUtil.MakePath(string.IsNullOrEmpty(options.MediaLinkServerUrl) ? WebUtil.GetServerUrl() : options.MediaLinkServerUrl, part2, '/');
            string str2 = StringUtil.EnsurePrefix('.', StringUtil.GetString(options.RequestExtension, item.Extension, "ashx"));
            string str3 = options.ToString();
            if (options.AlwaysAppendRevision)
            {
                string str4 = Guid.Parse(item.InnerItem.Statistics.Revision).ToString("N");
                str3 = string.IsNullOrEmpty(str3) ? "rev=" + str4 : str3 + "&rev=" + str4;
            }
            if (str3.Length > 0)
                str2 = str2 + "?" + str3;
            string str5 = "/sitecore/media library/";
            string path = item.InnerItem.Paths.Path;
            string str6 = MainUtil.EncodePath(!options.UseItemPath || !path.StartsWith(str5, StringComparison.OrdinalIgnoreCase) ? item.ID.ToShortID().ToString() : StringUtil.Mid(path, str5.Length), '/');
            string mediaUrl = part2 + str6 + (options.IncludeExtension ? str2 : string.Empty);
            if (options.LowercaseUrls)
                return this.GetMediaUrl(mediaUrl.ToLowerInvariant(), item);

            return this.GetMediaUrl(mediaUrl, item);
        }

        public override string GetThumbnailUrl(MediaItem item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return this.GetMediaUrl(item, MediaUrlOptions.GetThumbnailOptions(item));
        }

        public override bool HasMediaContent(Item item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return this.GetMediaData((MediaItem)item).HasContent;
        }

        public override bool IsMediaRequest(HttpRequest httpRequest)
        {
            Assert.ArgumentNotNull((object)httpRequest, nameof(httpRequest));
            return this.ParseMediaRequest(httpRequest) != null;
        }

        public override bool IsMediaUrl(string url)
        {
            Assert.ArgumentNotNull((object)url, nameof(url));
            using (List<string>.Enumerator enumerator = this.Config.MediaPrefixes.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (url.IndexOf(enumerator.Current, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            }
        }

        public override MediaRequest ParseMediaRequest(HttpRequest request)
        {
            Assert.ArgumentNotNull((object)request, nameof(request));
            MediaRequest mediaRequest = MediaManager.Config.ConstructMediaRequestInstance(request);
            if (mediaRequest != null && mediaRequest.MediaUri != null)
                return mediaRequest;
            return (MediaRequest)null;
        }

        protected virtual MediaData GetMediaData(MediaItem item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return MediaManager.Config.ConstructMediaDataInstance(item);
        }

        protected virtual MediaData GetMediaData(MediaUri mediaUri)
        {
            Assert.ArgumentNotNull((object)mediaUri, nameof(mediaUri));
            Database database = mediaUri.Database;
            if (database == null)
                return (MediaData)null;
            string mediaPath = mediaUri.MediaPath;
            if (string.IsNullOrEmpty(mediaPath))
                return (MediaData)null;
            Language language = mediaUri.Language;
            if (language == (Language)null)
                language = Context.Language;
            Sitecore.Data.Version version = mediaUri.Version;
            if (version == (Sitecore.Data.Version)null)
                version = Sitecore.Data.Version.Latest;
            Item obj = database.GetItem(mediaPath, language, version);
            if (obj == null)
                return (MediaData)null;
            return MediaManager.Config.ConstructMediaDataInstance((MediaItem)obj);
        }

        private MediaCreator ResolveCreator(BaseFactory factory, XmlNode configNode)
        {
            XmlNode childNode = XmlUtil.GetChildNode("mediaCreator", configNode);
            if (childNode != null)
            {
                MediaCreator mediaCreator = factory.CreateObject(childNode, false) as MediaCreator;
                if (mediaCreator != null)
                    return mediaCreator;
            }
            return (MediaCreator)new SingleLanguageMediaCreator();
        }


    }

}