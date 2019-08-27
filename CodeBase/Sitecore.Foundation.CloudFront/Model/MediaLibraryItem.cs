
namespace Sitecore.Foundation.CloudFront.Model
{
    using System;
    public class MediaLibraryItem
    {
        public string FileName { get; set; }

        public string Path { get; set; }

        public string Extension { get; set; }

        public string Alt { get; set; }

        public string Description { get; set; }

        public string MimeType { get; set; }

        public string Name { get; set; }

        public Guid ItemId { get; set; }

        public Guid ParentItemId { get; set; }

        public Guid TemplateId { get; set; }

        public string Language { get; set; }

        public string ItemUrl { get; set; }
    }
}