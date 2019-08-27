using Amazon;

namespace Sitecore.Foundation.CloudFront.Model
{
    public class CdnPropertiesModel: CloudFrontSettings
    {
        public string DefaultCdnDomain { get; set; }
        public bool IsActive { get; set; }
        public CdnType CdnType { get;set; }
    }

    public class CloudFrontSettings
    {
        public string S3BucketName { get; set; }
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        public RegionEndpoint Endpoint { get; set; }
    }

    public enum CdnType
    {
        CloudFront
    }
}