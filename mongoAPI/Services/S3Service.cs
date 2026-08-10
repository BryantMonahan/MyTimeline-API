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

        public PresignedUrl GetPresignedUrl(string contentType, string username)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = $"{username}/{Guid.NewGuid().ToString()}",
                Expires = DateTime.Now.AddMinutes(3),
                ContentType = contentType,
                Verb = HttpVerb.PUT
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            return new PresignedUrl { Key = request.Key, Url = presignedUrl };
        }
    }
}
