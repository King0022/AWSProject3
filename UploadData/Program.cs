using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;

namespace UploadData
{
    internal class Program
    {
        private static string BUCKETNAME = Environment.GetEnvironmentVariable("BUCKETNAME");
        private static IAmazonS3 client;
        static async Task Main(string[] args)
        {
            //check to make sure there are enough arguments
            if (args.Length < 1)
                throw new ArgumentException("Usage: Uploade.exe <path>");

            //check to see if path to file exists
            if (!File.Exists(string.Format(@"{0}", args[0])))
                throw new ArgumentException("Path not valid, Please make sure you you enter a correct path");


            AWSCredentials credentials = GetAWSCredentialsByName("default");
            client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);
            await uploadFileToS3(args[0]);
        }

        private static async Task uploadFileToS3(string path)
        {
            try
            {
                PutObjectRequest putRequest = new PutObjectRequest()
                {
                    BucketName = BUCKETNAME,
                    Key = string.Format("PatientRecord{0}", DateTime.Now.ToBinary()),
                    FilePath = path,
                    ContentType = "text/xml",
                };

                PutObjectResponse putResponse = await client.PutObjectAsync(putRequest);

            }
            catch (AmazonS3Exception s3ex)
            {
                Console.WriteLine(s3ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static AWSCredentials GetAWSCredentialsByName(string profileName)
        {
            if (String.IsNullOrEmpty(profileName))
            {
                throw new ArgumentNullException("profileName cannot be null or empty");
            }

            SharedCredentialsFile credFile = new SharedCredentialsFile();
            CredentialProfile profile = credFile.ListProfiles().Find(p => p.Name.Equals(profileName));

            return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
        }
    }
}
