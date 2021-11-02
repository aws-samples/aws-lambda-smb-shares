## Integrating AWS Lambda with SMB file shares

[Read the post](https://aws.amazon.com/blogs/storage/enabling-smb-access-for-serverless-workloads/) on AWS Storage blog

### Prerequisites
•	AWS credentials that provide the necessary permissions to create the resources. This example uses admin credentials.

•	An available SMB file share with valid credentials and a route to the VPC. See the [getting started exercise](https://docs.aws.amazon.com/fsx/latest/WindowsGuide/getting-started.html) to setup Amazon FSx for Windows File Server.

•	DotNet .NET SDK installed if deploying Lambda with .NET core template.

•	The [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html) installed. As of this writing, tested with SAM CLI, version 1.29.0.

•	Clone this repo. 

#### Deployment steps
1.	Navigate to the cloned repo directory, or run `sam init` then choose ‘Custom Template Location’ and paste the repo URL.
2.	Build the AWS SAM application, replace <templateName> with Python or DotNet template:
    
    `sam build -t <templateName>`

3.	Deploy the AWS SAM application:
    
    `sam deploy –guided`


## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the LICENSE file.

