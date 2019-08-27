
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
    public class AwsS3CdnServerHandler : IAwsS3CdnServerHandler, IDisposable
    {
        private bool _disposed;
        private CdnPropertiesModel cdnPropertiesModel { get; set; }

        private readonly ICdnHelper cdnHelper;
        public AwsS3CdnServerHandler()
        {
            cdnHelper = ServiceLocator.ServiceProvider.GetService<ICdnHelper>();
            cdnPropertiesModel = cdnHelper.GetCdnModelForSite(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
        }
        private bool IscdnPropertyValid()
        {
            if (cdnPropertiesModel == null)
                cdnPropertiesModel = cdnHelper.GetCdnModelForSite(false);

            return cdnPropertiesModel != null;
        }
        public bool SaveFile(string contentType, string fullFileName, Stream mediaStream, bool updateMaster = true, bool updateWeb = true)
        {
            try
            {
                if (IscdnPropertyValid() && (updateMaster || updateWeb))
                {
                    using (mediaStream)
                    {                       
                        if (updateWeb)
                        {
                            using (IAmazonS3 client = this.CreateClient())
                            {
                                var putObjectRequest = this.CreatePutObjectRequest(cdnPropertiesModel.S3BucketName, cdnHelper.GetCdnUrlOnDatabase(fullFileName, false), mediaStream, contentType);
                                client.PutObject(putObjectRequest);
                            }
                        }

                        //Copy the Item for Master from Web, if both are requested for Update
                        if (updateMaster & updateWeb)//For Master Database
                        {
                            using (IAmazonS3 client = this.CreateClient())
                            {
                                client.CopyObject(cdnPropertiesModel.S3BucketName, cdnHelper.GetCdnUrlOnDatabase(fullFileName, false), cdnPropertiesModel.S3BucketName, cdnHelper.GetCdnUrlOnDatabase(fullFileName, true));
                            }
                        }
                        else if (updateMaster)
                        {
                            using (IAmazonS3 client = this.CreateClient())
                            {

                                var putObjectRequest = this.CreatePutObjectRequest(cdnPropertiesModel.S3BucketName, cdnHelper.GetCdnUrlOnDatabase(fullFileName, true), mediaStream, contentType, false);
                                client.PutObject(putObjectRequest);
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    Log.Info(cdnPropertiesModel == null ? "CdnPropertiy is Null" : "not null", this);
                }
            }
            catch (AmazonS3Exception ex)
            {
                Log.Error(Helper.Constants.Message.SaveFileS3Exception + ((object)ex).ToString(), typeof(AwsS3CdnServerHandler));
                SheerResponse.ShowError((Exception)ex);
            }
            catch (StackOverflowException ex)
            {
                Log.Error(Helper.Constants.Message.SaveFileStackTraceException + ((object)ex).ToString(), typeof(AwsS3CdnServerHandler));
                SheerResponse.ShowError((Exception)ex);
            }
            catch (Exception ex)
            {
                Log.Error(Helper.Constants.Message.SaveFileException + ex.ToString(), typeof(AwsS3CdnServerHandler));
                SheerResponse.ShowError(ex);
            }
            return false;
        }
        public void DeleteFile(string fullFileName)
        {
            try
            {
                if (IscdnPropertyValid())
                {
                    using (IAmazonS3 client = this.CreateClient())
                    {
                        client.DeleteObject(cdnPropertiesModel.S3BucketName, fullFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Helper.Constants.Message.DeleteSingleFileException + ex.Message, typeof(AwsS3CdnServerHandler));
            }
        }

        public void DeleteMultipleFiles(List<string> files)
        {
            try
            {
                if (IscdnPropertyValid())
                {
                    var keysAndVersions = files?.Where(f => !string.IsNullOrEmpty(f))
                                            ?.Select(f => new KeyVersion() { Key = f })?.ToList();

                    if (keysAndVersions != null && keysAndVersions.Any())
                        using (var cloudFrontClient = CreateClient())
                        {

                            DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
                            {
                                BucketName = cdnPropertiesModel.S3BucketName,
                                Objects = keysAndVersions
                            };
                            cloudFrontClient.DeleteObjects(multiObjectDeleteRequest);
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Helper.Constants.Message.DeleteMultipleFilesException + ex.Message, typeof(AwsS3CdnServerHandler));
                SheerResponse.ShowError(ex);
            }
        }


        public void DeleteAllOnFolder(string folderName)
        {
            try
            {
                if (IscdnPropertyValid())
                {
                    using (var cloudFrontClient = CreateClient())
                    {
                        do
                        {
                            ListObjectsV2Request request = new ListObjectsV2Request()
                            {
                                BucketName = cdnPropertiesModel.S3BucketName,
                                Prefix = folderName
                            };
                            var urls = cloudFrontClient.ListObjectsV2(request);
                            var keys = urls?.S3Objects?.Select(s => new KeyVersion() { Key = s.Key })?.Distinct()?.ToList();

                            if (keys == null || !keys.Any()) break;

                            DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
                            {
                                BucketName = cdnPropertiesModel.S3BucketName,
                                Objects = keys
                            };
                            cloudFrontClient.DeleteObjects(multiObjectDeleteRequest);

                        } while (true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Helper.Constants.Message.DeleteAllOnFolderException + ex.Message, typeof(AwsS3CdnServerHandler));
                SheerResponse.ShowError(ex);
            }
        }
        
        private PutObjectRequest CreatePutObjectRequest(string bucketName, string key, Stream stream, string contentType, bool closeStream = false)
        {
            var putObjRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = closeStream
            };

            return putObjRequest;
        }

        private IAmazonS3 CreateClient()
        {
            return (IAmazonS3)new AmazonS3Client(cdnPropertiesModel.PublicKey, cdnPropertiesModel.SecretKey, cdnPropertiesModel.Endpoint);
        }
    }
}
