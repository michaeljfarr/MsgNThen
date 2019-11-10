using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MsgNThen.Broker
{
    public class AwsCredentialRoot
    {
        public List<AwsCredential> AwsCredentials { get; set; }
    }
    public class AwsCredential
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
