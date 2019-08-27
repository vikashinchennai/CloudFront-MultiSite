namespace Sitecore.Foundation.CloudFront.Handler
{
    using Sitecore.Foundation.CloudFront.Helper;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Events;
    using Sitecore.Globalization;
    using Sitecore.Pipelines.GetResponseCacheHeaders;
    using Sitecore.Resources;
    using Sitecore.Resources.Media;
    using Sitecore.Resources.Media.Streaming;
    using Sitecore.Security.Accounts;
    using Sitecore.SecurityModel;
    using Sitecore.Sites;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.Authentication;
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Sitecore.Foundation.CloudFront.Extensions;

    public class CustomMediahandler : MediaRequestHandler, IHttpHandler
    {
        private readonly ICdnHelper cdnHelper;
        public CustomMediahandler()
        {
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
        }
        public override void ProcessRequest(HttpContext context)
        {
            Assert.ArgumentNotNull((object)context, nameof(context));
            CustomMediahandler.RedirectIfUserShouldBeLoggedIn(context);
            if (this.DoProcessRequest(context))
                return;
            context.Response.StatusCode = 404;
            context.Response.ContentType = Helper.Constants.Properties.ContentTypeTextHtml;
        }

        private static void RedirectIfUserShouldBeLoggedIn(HttpContext context)
        {
            Assert.ArgumentNotNull((object)context, nameof(context));
            SiteContext site = Context.Site;
            if (site == null)
                return;
            User user = Context.User;
            if (site.Name == Helper.Constants.Properties.Shell && !user.IsAuthenticated)
            {
                if (CustomMediahandler.TryRelogin())
                    return;
                WebUtil.RedirectToLoginPage(context);
            }
            if (site.DisplayMode != DisplayMode.Preview || user.IsAuthenticated || CustomMediahandler.TryRelogin())
                return;
            Context.SetActiveSite(Helper.Constants.Properties.Shell);
            WebUtil.RedirectToLoginPage(context, new List<string>((IEnumerable<string>)new string[1]
            {
        Helper.Constants.Properties.ScMode
            }));
        }

        private static bool TryRelogin()
        {
            string currentTicketId = TicketManager.GetCurrentTicketId();
            return !string.IsNullOrEmpty(currentTicketId)
                && TicketManager.IsTicketValid(currentTicketId) 
                && TicketManager.Relogin(currentTicketId);
        }

        protected override bool DoProcessRequest(HttpContext context)
        {
            Assert.ArgumentNotNull((object)context, nameof(context));
            MediaRequest mediaRequest = this.GetMediaRequest(context.Request);
            if (mediaRequest == null)
                return false;
            string url = (string)null;
            Media media1 = MediaManager.GetMedia(mediaRequest.MediaUri);
            if (media1 == null)
            {
                using (new SecurityDisabler())
                    media1 = MediaManager.GetMedia(mediaRequest.MediaUri);
                if (media1 == null)
                {
                    url = Settings.ItemNotFoundUrl;
                }
                else
                {
                    Assert.IsNotNull((object)Context.Site, Helper.Constants.Properties.Site);
                    if (!Context.User.IsAuthenticated 
                        && Context.Site.RequireLogin 
                        && !string.IsNullOrEmpty(Context.Site.LoginPage))
                    {
                        url = Context.Site.LoginPage;
                        if (Settings.Authentication.SaveRawUrl)
                        {
                            UrlString urlString = new UrlString(url);
                            urlString.Append(Helper.Constants.Properties.Url, HttpUtility.UrlEncode(Context.RawUrl));
                            url = urlString.GetUrl();
                        }
                    }
                    else
                        url = Settings.NoAccessUrl;
                }
            }
            else
            {
                bool flag = mediaRequest.Options.Thumbnail || media1.MediaData.HasContent;
                string lowerInvariant = media1.MediaData.MediaItem.InnerItem[Helper.Constants.TemplateFields.Path].ToLowerInvariant();
                if (!flag && !string.IsNullOrEmpty(lowerInvariant))
                {
                   Media media2 = MediaManager.GetMedia(new MediaUri(lowerInvariant, Language.Current, Sitecore.Data.Version.Latest, Context.Database));
                    if (media2 != null)
                        media1 = media2;
                }
                else if (mediaRequest.Options.UseDefaultIcon && !flag)
                    url = Themes.MapTheme(Settings.DefaultIcon).ToLowerInvariant();
                else if (!mediaRequest.Options.UseDefaultIcon && !flag)
                    url = Settings.ItemNotFoundUrl;
            }
            if (string.IsNullOrEmpty(url))
                return this.DoProcessRequest(context, mediaRequest, media1);
            HttpContext.Current.Response.Redirect(url);
            return true;
        }

       protected override bool DoProcessRequest(HttpContext context, MediaRequest request, Media media)
        {
            Assert.ArgumentNotNull((object)context, nameof(context));
            Assert.ArgumentNotNull((object)request, nameof(request));
            Assert.ArgumentNotNull((object)media, nameof(media));

            //OverRiding the Default Stream behavior to return CDN inage
            ProcessCdnRedirect(context, media);

            if (this.Modified(context, media, request.Options) == Tristate.False)
            {
                this.RaiseEvent(Helper.Constants.Properties.MediaRequest, request);
                this.SendMediaHeaders(media, context);
                context.Response.StatusCode = 304;
                return true;
            }


            this.ProcessImageDimensions(request, media);
            MediaStream mediaStream = this.GetMediaStream(media, request);
            if (mediaStream == null)
                return false;
            this.RaiseEvent(Helper.Constants.Properties.MediaRequest, request);
            if (Settings.Media.EnableRangeRetrievalRequest && Settings.Media.CachingEnabled)
            {
                using (mediaStream)
                {
                    this.SendMediaHeaders(media, context);
                    new RangeRetrievalResponse(RangeRetrievalRequest.BuildRequest(context, media), mediaStream).ExecuteRequest(context);
                    return true;
                }
            }
            else
            {
                this.SendMediaHeaders(media, context);
                this.SendStreamHeaders(mediaStream, context);
                using (mediaStream)
                {
                    context.Response.AddHeader(Helper.Constants.Properties.ContentLength, mediaStream.Stream.Length.ToString());
                    WebUtil.TransmitStream(mediaStream.Stream, context.Response, Settings.Media.StreamBufferSize);
                }
                return true;
            }
        }

        private void ProcessCdnRedirect(HttpContext context, Media media)
        {
            var item = media?.MediaData?.MediaItem?.InnerItem;
            if (cdnHelper.CanUseCdnForMedia(item))
            {
                var cdnDomainUrl = cdnHelper.GetCdnDomainUrl();
                
                if (!string.IsNullOrEmpty(cdnDomainUrl))
                {
                    if (Uri.TryCreate(new Uri(cdnDomainUrl), cdnHelper.GetCdnRelativePathForItem(item), out Uri op))
                    {
                        HttpContext.Current.Response.RedirectPermanent(cdnHelper.BuildCdnUrlWithQueryString(op.ToString(),
                                                                        context.Request.QueryString.ToString(), item.GetFieldValue(Helper.Constants.MediaTemplateFields.UpdatedOn)), true);
                    }
                }
            }
        }

        private void ProcessImageDimensions(MediaRequest request, Media media)
        {
            Assert.ArgumentNotNull((object)request, nameof(request));
            Assert.ArgumentNotNull((object)media, nameof(media));
            Item innerItem = media.MediaData.MediaItem.InnerItem;
            int.TryParse(innerItem[Helper.Constants.TemplateFields.Height], out int result1);
            int.TryParse(innerItem[Helper.Constants.TemplateFields.Width], out int result2);
            bool flag = false;
            int maxHeight = Settings.Media.Resizing.MaxHeight;
            if (maxHeight != 0 && request.Options.Height > Math.Max(maxHeight, result1))
            {
                flag = true;
                request.Options.Height = Math.Max(maxHeight, result1);
            }
            int maxWidth = Settings.Media.Resizing.MaxWidth;
            if (maxWidth != 0 && request.Options.Width > Math.Max(maxWidth, result2))
            {
                flag = true;
                request.Options.Width = Math.Max(maxWidth, result2);
            }
            if (!flag)
                return;
            Log.Warn(string.Format(Helper.Constants.Message.ProcessImageDimensionsWarning, (object)request.InnerRequest.RawUrl), (object)this);
        }

        protected override Tristate Modified(HttpContext context, Media media, MediaOptions options)
        {
            string header1 = context.Request.Headers[Helper.Constants.Properties.IfNoneMatchHeader];
            if (!string.IsNullOrEmpty(header1) && header1 != media.MediaData.MediaId)
                return Tristate.True;
            string header2 = context.Request.Headers[Helper.Constants.Properties.IfModifiedSinceHeader];
            if (!string.IsNullOrEmpty(header2))
            {
                if (WebUtil.TryGetValueOfIfModifiedSinceHeader(context.Request, out DateTime ifModifiedSince))
                    return MainUtil.GetTristate(!CompareDatesWithRounding(ifModifiedSince, media.MediaData.Updated, new TimeSpan(0, 0, 1)));
                Log.Warn(string.Format(Helper.Constants.Message.ModifiedWarnMessage, (object)header2), (object)typeof(MediaRequestHandler));
            }
            return Tristate.Undefined;
        }

        protected override void SendMediaHeaders(Media media, HttpContext context)
        {
            this.SendMediaHeaders(media, (HttpContextBase)new HttpContextWrapper(context));
        }

        protected override void SendMediaHeaders(Media media, HttpContextBase context)
        {
            GetResponseCacheHeadersArgs args = new GetResponseCacheHeadersArgs(media.MediaData.MediaItem.InnerItem, new ResponseCacheHeaders()
            {
                Vary = this.GetVaryHeader(media, context)
            })
            {
                RequestType = new RequestTypes?(RequestTypes.Media)
            };
            GetResponseCacheHeadersPipeline.Run(args);
            HttpCachePolicyBase cache = context.Response.Cache;
            if (args.CacheHeaders.LastModifiedDate.HasValue)
                cache.SetLastModified(args.CacheHeaders.LastModifiedDate.Value);
            if (!string.IsNullOrEmpty(args.CacheHeaders.ETag))
                cache.SetETag(args.CacheHeaders.ETag);
            if (args.CacheHeaders.Cacheability.HasValue)
                cache.SetCacheability(args.CacheHeaders.Cacheability.Value);
            if (args.CacheHeaders.MaxAge.HasValue)
                cache.SetMaxAge(args.CacheHeaders.MaxAge.Value);
            if (args.CacheHeaders.ExpirationDate.HasValue)
                cache.SetExpires(args.CacheHeaders.ExpirationDate.Value);
            if (!string.IsNullOrEmpty(args.CacheHeaders.CacheExtension))
                cache.AppendCacheExtension(args.CacheHeaders.CacheExtension);
            if (string.IsNullOrEmpty(args.CacheHeaders.Vary))
                return;
            context.Response.AppendHeader("vary", args.CacheHeaders.Vary);
        }

        [Obsolete("Vary header should be set in getResponseCacheHeaders pipeline.")]
        protected override string GetVaryHeader(Media media, HttpContextBase context)
        {
            return Settings.MediaResponse.VaryHeader;
        }

        protected override void SendStreamHeaders(MediaStream stream, HttpContext context)
        {
            stream.Headers.CopyTo(context.Response);
        }
        
        protected override MediaRequest GetMediaRequest(HttpRequest request)
        {
            return MediaManager.ParseMediaRequest(request);
        }

        protected override void RaiseEvent(string eventName, MediaRequest request)
        {
            Event.RaiseEvent(eventName, (object)request);
        }

        protected override MediaStream GetMediaStream(Media media, MediaRequest request)
        {
            return media.GetStream(request.Options);
        }
    }
}
