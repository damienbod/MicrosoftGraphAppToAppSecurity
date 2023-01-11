# Microsoft Graph App to App Security

## Using managed identities

[managed identity](https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-powershell)

```csharp

public MsGraphEmailService(IOptions<GraphApplicationServicesConfiguration> graphAppServicesConfiguration)
{
    _configuration = graphAppServicesConfiguration.Value;
    var scopes = _configuration.Scopes?.Split(' ');

    var credential = new ChainedTokenCredential(
        new ManagedIdentityCredential(),
        new EnvironmentCredential());

    _graphServiceClient = new GraphServiceClient(chainedTokenCredential, scopes);

    var options = new TokenCredentialOptions
    {
        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };

    // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
    var clientSecretCredential = new ClientSecretCredential(
        _configuration.TenantId, _configuration.ClientId, _configuration.ClientSecret, options);

    _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
}
```

## Using certificates or secrets

```csharp
```

[secrets or certificates](https://learn.microsoft.com/en-us/azure/active-directory/develop/sample-v2-code#service--daemon)

## Links

https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-powershell

https://learn.microsoft.com/en-us/azure/active-directory/develop/sample-v2-code#service--daemon

https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph

https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS

https://oceanleaf.ch/azure-managed-identity/

https://learningbydoing.cloud/blog/stop-using-client-secrets-start-using-managed-identities/