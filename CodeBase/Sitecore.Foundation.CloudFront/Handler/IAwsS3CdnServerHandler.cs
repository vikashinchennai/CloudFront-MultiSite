namespace Sitecore.Foundation.CloudFront.Handler
{
    using Amazon.S3;
    using Amazon.S3.Model;
    using Sitecore.Foundation.CloudFront.Helper;
    using Sitecore.Foundation.CloudFront.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public interface IAwsS3CdnServerHandler
    {

        bool SaveFile(string contentType, string fullFileName, Stream mediaStream, bool updateMaster = true, bool updateWeb = true);
        void DeleteAllOnFolder(string folderName);
        void DeleteMultipleFiles(List<string> files);
        void DeleteFile(string fullFileName);
    }
}