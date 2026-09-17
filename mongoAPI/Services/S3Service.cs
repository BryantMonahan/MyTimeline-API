using Amazon.S3;
using Amazon.S3.Model;
using mongoAPI.Models;
using mongoAPI.Types;
using FFMpegCore;

namespace mongoAPI.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _s3Settings;

        public S3Service(IAmazonS3 s3Client, S3Settings s3Settings)
        {
            _s3Client = s3Client;
            _s3Settings = s3Settings;
        }

        public PresignedUrl GetPresignedUrlPut(string username, string extension)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = $"{username}/{Guid.NewGuid()}{extension}",
                Expires = DateTime.Now.AddMinutes(3),
                Verb = HttpVerb.PUT
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            return new PresignedUrl { Key = request.Key, Url = presignedUrl };
        }

        public string GetPresignedUrlGet(string key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = key,
                Expires = DateTime.Now.AddMinutes(3),
                Verb = HttpVerb.GET
            };
            var presignedUrl = _s3Client.GetPreSignedURL(request);
            return presignedUrl;
        }

        async public Task<FileSizeAndLength?> GetObjectMetadata(string objectKey)
        {
            try
            {
                string url = GetPresignedUrlGet(objectKey);
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _s3Settings.BucketName,
                    Key = objectKey
                };
                var metadata = await _s3Client.GetObjectMetadataAsync(request);
                var analysis = await FFProbe.AnalyseAsync(new Uri(url));
                return new FileSizeAndLength { SecLength = analysis.Duration.Seconds, SizeInBytes = (int)metadata.ContentLength };
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving metadata", e.Message);
                return null;
            }
        }

        // Deletes an object from the S3 bucket based on its key
        async public Task<bool> DeleteObject(string objectKey)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _s3Settings.BucketName,
                    Key = objectKey
                };
                DeleteObjectResponse response = await _s3Client.DeleteObjectAsync(request);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error deleting object in S3 bucket", e.Message);
                return false;
            }
        }
    }

}
