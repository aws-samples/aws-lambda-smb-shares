import json
import boto3
import os
import base64
import smbclient
from botocore.exceptions import ClientError
from smbclient.path import (
    isdir,
)

def lambda_handler(event, context):

    # Get SMB Fileshare secret from AWS Secrets Manager 
    secret = get_secret("FScredentials")

    # Parse JSON key value pairs.
    jsonString = json.loads(secret)
    username = jsonString["username"]
    password = jsonString["password"]
    host = jsonString["host"]
    destShare = jsonString["share"] 

    # Set destination directory and filename values from Lambda context, else assign demo static values
    try:
        destDir = event['directory']
        destFile = event['filename']    
    except:
        destDir = 'TestDir'
        destFile = 'TestFile.txt'
    

    # Constructing paths:  
    destDirectoryPath = os.path.join(host, destShare, destDir)
    destFilePath =  os.path.join(destDirectoryPath, destFile)

    # Create a session to the server with explicit credentials
    try:
        smbclient.register_session(server=host, username=username, password=password)
    except:
        print("Error establishing session")

    try: # Create the directory if it doesn't exist
        
        if not isdir(destDirectoryPath):
            smbclient.mkdir(destDirectoryPath)

    except: 
        print("Error listing or creating directory on share")

    else: # Directory created or present, Create a new file or append to existing file.
        
        with smbclient.open_file(destFilePath, mode="a") as fd:
                fd.write("Added by AWS Lambda requestID: %s \n" %context.aws_request_id)

    finally:
        for file_info in smbclient.scandir(destDirectoryPath):
            if file_info.is_file():
                print("File: %s" % file_info.name)
            elif file_info.is_dir():
                print("Dir: %s" % file_info.name)
            else:
                print("Symlink: %s" % file_info.name)
            
        # Gracefully close the SMB connection with the server.
        smbclient.reset_connection_cache()
    
    
    return "Successfully stored {} file under {} folder to {} share on {} host!".format(destFile,destDir,destShare,host)

def get_secret(secret_name):

    region_name = os.environ['AWS_REGION']

    # Create a Secrets Manager client
    session = boto3.session.Session()
    client = session.client(
        service_name='secretsmanager',
        region_name=region_name
    )

    # In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
    # See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
    # We rethrow the exception by default.

    try:
        get_secret_value_response = client.get_secret_value(
            SecretId=secret_name
        )

    except ClientError as e:
        if e.response['Error']['Code'] == 'DecryptionFailureException':
            # Secrets Manager can't decrypt the protected secret text using the provided KMS key.
            # Deal with the exception here, and/or rethrow at your discretion.
            raise e
        elif e.response['Error']['Code'] == 'InternalServiceErrorException':
            # An error occurred on the server side.
            # Deal with the exception here, and/or rethrow at your discretion.
            raise e
        elif e.response['Error']['Code'] == 'InvalidParameterException':
            # You provided an invalid value for a parameter.
            # Deal with the exception here, and/or rethrow at your discretion.
            raise e
        elif e.response['Error']['Code'] == 'InvalidRequestException':
            # You provided a parameter value that is not valid for the current state of the resource.
            # Deal with the exception here, and/or rethrow at your discretion.
            raise e
        elif e.response['Error']['Code'] == 'ResourceNotFoundException':
            # We can't find the resource that you asked for.
            # Deal with the exception here, and/or rethrow at your discretion.
            raise e
    else:
        # Decrypts secret using the associated KMS CMK.
        # Depending on whether the secret is a string or binary, one of these fields will be populated.
        if 'SecretString' in get_secret_value_response:
            secret = get_secret_value_response['SecretString']
            return secret
        else:
            decoded_binary_secret = base64.b64decode(get_secret_value_response['SecretBinary'])
            return decoded_binary_secret
        