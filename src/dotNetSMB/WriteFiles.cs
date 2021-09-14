using Amazon;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace lambda_smb_dotnet
{
    public class WriteFiles
    {
        internal string secretName = "FScredentials";

        public struct SecretJSON
        {
            public string username { get; set; }

            public string password { get; set; }

            public string host { get; set; }

            public string share { get; set; }
        }

        public static string GetSecret(string secretName)
        {
            string region = Environment.GetEnvironmentVariable("AWS_REGION");

            string secret = "";

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                response = client.GetSecretValueAsync(request).Result;
            }
            catch (DecryptionFailureException e)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                Console.WriteLine(e.Message);

                throw;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                Console.WriteLine(e.Message);

                throw;
            }
            catch (InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                Console.WriteLine(e.Message);

                throw;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                Console.WriteLine(e.Message);

                throw;
            }
            catch (ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                Console.WriteLine(e.Message);

                throw;
            }
            catch (System.AggregateException ae)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                Console.WriteLine(ae.Message);

                throw;
            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                secret = response.SecretString;
                return secret;
            }
            else
            {
                memoryStream = response.SecretBinary;

                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
                return decodedBinarySecret;

            }
        }

        public static void ListFiles(ISMBFileStore fileStore)
        {
            ///list files and directories

            object directoryHandle;
            FileStatus fileStatus;
            NTStatus status = fileStore.CreateFile(out directoryHandle, out fileStatus, String.Empty, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                List<QueryDirectoryFileInformation> fileList;
                status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                status = fileStore.CloseFile(directoryHandle);

                foreach (QueryDirectoryFileInformation file in fileList) //Print to console logs 
                {
                    string fileName = ((SMBLibrary.FileDirectoryInformation)file).FileName.ToString();
                    Console.WriteLine(fileName);
                }
            }
        }

        public static void WriteTestFile(ISMBFileStore fileStore, ILambdaContext context)
        {

            ///Write dummy file to share
            string filePath = "AWSLambdaRequestId_" + context.AwsRequestId + ".txt";
            string fileContent = "This file is written by Lambda .NET Core!";
            object fileHandle;
            FileStatus fileStatus;
            NTStatus status = fileStore.CreateFile(out fileHandle, out fileStatus, filePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                int numberOfBytesWritten;
                byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes(fileContent);
                status = fileStore.WriteFile(out numberOfBytesWritten, fileHandle, 0, data);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception("Failed to write to file");
                }
                status = fileStore.CloseFile(fileHandle);
            }
        }

        public string FunctionHandler(ILambdaContext context)
        {
            var secretValue = GetSecret(secretName);
            SecretJSON secretJSON = JsonSerializer.Deserialize<SecretJSON>(secretValue);

            SMB2Client client = new SMB2Client();
            bool isConnected = client.Connect(IPAddress.Parse(secretJSON.host), SMBTransportType.DirectTCPTransport); // Attempt connecting to SMB share

            if (isConnected)
            {
                NTStatus status = client.Login(String.Empty, secretJSON.username, secretJSON.password); //Login to file server

                if (status == NTStatus.STATUS_SUCCESS) //Login successful
                {
                    ISMBFileStore fileStore = client.TreeConnect(secretJSON.share, out status); //Connect to share

                    if (status == NTStatus.STATUS_SUCCESS) //Connection to share successful
                    {
                        WriteTestFile(fileStore, context);
                        ListFiles(fileStore);

                    }

                    ///Gracefully disconnect and logoff

                    status = fileStore.Disconnect();
                    client.Logoff();
                }
                else //Failed to login to SMB share
                {
                    return "Couldn't login to file share, please check username and password!";
                }
            }
            else //Failed to establish connection to SMB share
            {
                return "Couldn't connect to file share, please check host address and security groups!";
            }

            return "Successfully written test file to share: " + secretJSON.share;
        }
    }
}
