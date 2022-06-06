using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PatientReader;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    IAmazonSQS SQSClient { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();

        // create an SQS client default constructor called
        SQSClient = new AmazonSQSClient();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;

        // Create SQS client even if S3Client changes
        this.SQSClient = new AmazonSQSClient();
    }
    
    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {

        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null)
        {
            return null;
        }


        try
        {
            string bucket = s3Event.Bucket.Name;
            string objectKey = s3Event.Object.Key;
            Dictionary<string, string> data = null;

            var response = await this.S3Client.GetObjectMetadataAsync(bucket, objectKey);
            Stream stream = await this.S3Client.GetObjectStreamAsync(bucket, objectKey, null);

            using (StreamReader reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();
                if (response.Headers.ContentType.ToLower().Contains("xml"))
                {
                    XmlParser parser = new XmlParser(content);
                    data = parser.getData();
                }

                reader.Close();

                if (data == null)
                    throw new ArgumentNullException("The parser could parse the xml file");

                Patient patient = new Patient {
                    PatientID = data["patientID"],
                    PatientName = data["name"],
                    MediccalRecord = { },
                    HasMediccalRecord = false
                };


                /*      Connect and send information to SQS */
                GetQueueUrlResponse downwardQueueUrl = await SQSClient.GetQueueUrlAsync(Environment.GetEnvironmentVariable("SQSNAME"));

                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                SendMessageRequest sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = downwardQueueUrl.QueueUrl,
                    MessageBody = JsonSerializer.Serialize(patient, options)
                };

                SendMessageResponse sendToQueue = await SQSClient.SendMessageAsync(sendMessageRequest);

            }

            return response.Headers.ContentType;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Functin.cs: {0}", ex.Message);
            return null;
        }
    }
}