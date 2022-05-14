# Purpose

This project is primarily for testing Serilog functionality to Slack and CloudWatch endpoints.

To simplify the deployment, it's built using AWS Co-Pilot

## AWS Copilot Commands

To get started with Copilot, [see the docs](https://aws.github.io/copilot-cli/docs/getting-started/install/).

```
copilot init                # Initializes the infrastructure and first deploy
copilot deploy              # Deploys the solution to AWS
copilot app delete          # Delete the application and resources
```

After the infrastrucure is deployed, it will generate a URL for the application. You can test the application:

```
curl {URL}/log/abracadabra
```

