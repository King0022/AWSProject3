using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;

namespace InsuraceDataService
{
    internal class Authenticate
    {
        public IAmazonSQS SQSClient { get; set; }

        public IAmazonSQS Client (string profileName) 
        {
            AWSCredentials credentials = GetCredentials(profileName);
            SQSClient = new AmazonSQSClient(credentials);

            return SQSClient;
        }

        private AWSCredentials GetCredentials(string profileName)
        {

            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentNullException("The profile name can not be null");

            SharedCredentialsFile credentialfile = new SharedCredentialsFile();
            CredentialProfile profile = credentialfile.ListProfiles().Find( profile=> profile.Name.Equals(profileName));

            return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
        }
    }
}
