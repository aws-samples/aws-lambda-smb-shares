## Integrating AWS Lambda with SMB file shares!
### Prerequisites
•	AWS credentials that provide the necessary permissions to create the resources. This example uses admin credentials.

•	An available SMB file share with valid credentials and a route to the VPC. See the [getting started exercise](https://docs.aws.amazon.com/fsx/latest/WindowsGuide/getting-started.html) to setup Amazon FSx for Windows File Server.

•	DotNet .NET SDK installed if deploying Lambda with .NET core template.

•	The [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html) installed. As of this writing, tested with SAM CLI, version 1.29.0.

•	Clone this repo. 

#### Deployment steps
1.	Navigate to the cloned repo directory. 
2.	Rename SAM template to `template.yaml` based on which runtime you want to test.
    - To deploy the template containing the Python function:
    `mv templatePython.yaml template.yaml`
    - To deploy the template containing the .NET Core function:
    `mv templateDotNet.yaml template.yaml`
      
      *Note: SAM CLI commands accept optional parameter (`--template`) to specify a non-default template name. In this demo deployment, I let SAM CLI default to `template.yaml`
3.	Build the AWS SAM application:
`sam build`
4.	Deploy the AWS SAM application:
`sam deploy –guided`


## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the LICENSE file.

