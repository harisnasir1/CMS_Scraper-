using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.Net.Mime;
namespace CMS_Scrappers.Services.Implementations
{
    public class S3Service: S3Interface
    {
        private readonly ILogger<S3Service> _logger;
        private readonly S3Settings _settings; 

        public S3Service(ILogger<S3Service> logger,S3Settings settings)
        {
            _settings = settings;
            _logger = logger;
        }
        public async Task<string> Uploadimage(Stream images)
        {
            var region = RegionEndpoint.GetBySystemName(_settings.Region);
            var client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, region);
            var fileTransferUtility = new TransferUtility(client);
            var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid()}";
            var key = $"CMS/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{fileName}.png";
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = images,
                Key = key,
                BucketName = _settings.BucketName,
                ContentType = "image/png"
            };
            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
        }
    }
}
