using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using System.Xml;

namespace InsuraceDataService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly string DOWNWARDQUEUE = Environment.GetEnvironmentVariable("DOWNWARDQUEUE");
        private readonly string UPWARDQUEUE = Environment.GetEnvironmentVariable("UPWARDQUEUE");

        private readonly string DATABASEFILEPATH = Environment.GetEnvironmentVariable("DATABASE_FILE_PATH");
        private readonly string logPath = @"C:\Temp\InsuranceServiceLog.log";

        private GetQueueUrlResponse UPWARDQUEUE_INFO;
        private GetQueueUrlResponse DOWNWARDQUEUE_INFO;

        private readonly int LONGPOOLING_WAITTIME = 20;
        Authenticate authenticate;
        IAmazonSQS SqsClient;


        

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            authenticate = new Authenticate();
            SqsClient = authenticate.Client("default");

            UPWARDQUEUE_INFO = await SqsClient.GetQueueUrlAsync(UPWARDQUEUE);
            DOWNWARDQUEUE_INFO = await SqsClient.GetQueueUrlAsync(DOWNWARDQUEUE);

            do
            {
                // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                // WriteToLog("Hello from service CS455SystemCheckService");

                ReceiveMessageResponse message = await GetMessage();
                if (message.Messages.Count != 0) 
            {
                    if (await ProcessMessage(message.Messages[0]))
                        await DeleteMessage(message.Messages[0]);
                }

                await Task.Delay(1000, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }

        private void WriteToLog(string json, string action)
        {
            if (!System.IO.File.Exists(logPath)) 
            {
                System.IO.File.Create(logPath);
            }

            using(StreamWriter write = new StreamWriter(logPath, append: true))
            {
                
                write.WriteLine($"{DateTime.Now} : {action} message: {json}");
            }
            }

        public async Task<ReceiveMessageResponse> GetMessage() 
        {
            return await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = DOWNWARDQUEUE_INFO.QueueUrl,
                WaitTimeSeconds = LONGPOOLING_WAITTIME
            });
        }
        
        private async Task<bool> ProcessMessage(Message message) 
        {
            
            Patient patientRecord = JsonSerializer.Deserialize<Patient>(message.Body);
            WriteToLog(message.Body, "Read");

            Dictionary<string, string> record = GetPatientRecord(patientRecord.PatientID);

            Patient patient = new Patient
            {
                PatientName = "",
                PatientID = patientRecord.PatientID,
                MediccalRecord = (record != null) ? record : new Dictionary<string, string>(),
                HasMediccalRecord = (record != null) ? true : false
            };

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            SendMessageRequest messageRequest = new SendMessageRequest
            {
                QueueUrl = UPWARDQUEUE_INFO.QueueUrl,
                MessageBody = JsonSerializer.Serialize(patient, options)
            };

            SendMessageResponse sendToSQS = await SqsClient.SendMessageAsync(messageRequest);


            if (sendToSQS.HttpStatusCode.ToString().Equals("OK")) 
            {
                WriteToLog(JsonSerializer.Serialize(patient, options), "Posted");
                return true;
            }
                
            else
                WriteToLog("Failed Message Post", "FAILED");

            return false;
        }

        private async Task DeleteMessage(Message message) 
        {
            await SqsClient.DeleteMessageAsync(DOWNWARDQUEUE_INFO.QueueUrl, message.ReceiptHandle);
        }

        private Dictionary<string, string> GetPatientRecord(string patientID)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(DATABASEFILEPATH);

            if (xmlDoc.DocumentElement == null)
                throw new ArgumentException("Xml Document Load Failure.");

            XmlElement root = xmlDoc.DocumentElement;
            XmlNode patient = root.SelectSingleNode($"patient[@id=\"{patientID}\"]");

            if (patient == null) return null;

            XmlNode policy = patient.SelectSingleNode("policy");
            XmlNode provider = patient.SelectSingleNode("policy/provider");

            return new Dictionary<string, string>
            {
                { "policyNumber", policy.Attributes["policyNumber"].InnerText},
                { "provider", provider.InnerText}
            };
        }
    }
}