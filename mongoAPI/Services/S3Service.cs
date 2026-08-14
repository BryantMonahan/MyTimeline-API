using Amazon.S3;
using Amazon.S3.Model;
using mongoAPI.Models;
using mongoAPI.Types;

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

        public PresignedUrl GetPresignedUrl(string username, string extension)
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

        async public Task<GetObjectMetadataResponse?> GetObjectMetadata(string objectKey)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = objectKey
            };
            try
            {
                var metadata = await _s3Client.GetObjectMetadataAsync(request);
                return metadata;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving metadata", e.Message);
                return null;
            }
        }
    }
}
