using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using MsgNThen.Interfaces;

namespace MsgNThen.Broker
{
    class S3DeliveryScheme : IUriDeliveryScheme
    {
        private readonly Dictionary<string, AwsCredential> _credentials;

        public S3DeliveryScheme(IOptionsMonitor<AwsCredentialRoot> credentials)
        {
            if (credentials.CurrentValue.AwsCredentials == null)
            {
                throw new ArgumentNullException(nameof(credentials.CurrentValue.AwsCredentials));
            }
            _credentials = credentials.CurrentValue.AwsCredentials.ToDictionary(a=>a.Name);
        }
        public string Scheme => "s3";
        public async Task Deliver(Uri destination, MsgNThenMessage message)
        {
            //https://s3.us-east-2.amazonaws.com/my-bucket-name/filename
            //s3://john.doe@my-bucket-name/filename[
            //s3://<credentialName>@<bucketname>/filename
            var credentials = GetCredentials(destination);
            var bucketName = destination.Host;
            var pathAndQuery = Uri.UnescapeDataString(destination.PathAndQuery).TrimStart('/');
            var messageId = message.Headers[HeaderConstants.MessageId];
            var messageGroupId = message.Headers[HeaderConstants.MessageGroupId];
            var correlationId = message.Headers[HeaderConstants.CorrelationId];
            var fileKey = string.Format(pathAndQuery, messageId, messageGroupId, correlationId);
            var queryDictionary = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(destination.Query);
            if (message.Body.CanSeek)
            {
                message.Body.Position = 0;
            }
            using (var client = new AmazonS3Client(credentials.awsAccessKeyId, credentials.awsSecretAccessKey, RegionEndpoint.USEast1))
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = message.Body,
                    Key = fileKey,
                    BucketName = bucketName,
                    CannedACL = S3CannedACL.BucketOwnerFullControl
                };
                if (queryDictionary.TryGetValue(QueryConstants.S3CannedACL, out var val))
                {
                    var cannedAcl = S3CannedACL.FindValue(val);
                    if (cannedAcl!=null)
                    {
                        uploadRequest.CannedACL = cannedAcl;
                    }
                }

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            }
        }

        private (string awsAccessKeyId, string awsSecretAccessKey) GetCredentials(Uri uri)
        {
            if (uri.UserInfo == null)
            {
                if (_credentials.TryGetValue("Default", out var cred2))
                {
                    return (cred2.Key, cred2.Secret);
                }
                throw new Exception($"User info not provided for {uri}");
            }
            var splitPos = uri.UserInfo.IndexOf(":", StringComparison.Ordinal);
            var accessId = uri.UserInfo;
            if (splitPos >= 0)
            {
                accessId = uri.UserInfo.Substring(0, splitPos);
                var accessKey = (string)null;
                accessKey = uri.UserInfo.Substring(splitPos + 1);
                return (accessId, accessKey);
            }

            if (_credentials.TryGetValue(accessId, out var cred))
            {
                return (cred.Key, cred.Secret);
            }
            throw new Exception($"Could not find aws credential {uri.UserInfo} for {uri}");
        }
    }
}