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

## Using Graph SDK with certificates or secrets

Using Graph SDK client with a secret
```csharp
private GraphServiceClient GetGraphClientWithClientSecretCredential()
{
    string[] scopes = new[] { "https://graph.microsoft.com/.default" };
    var tenantId = _configuration["AzureAd:TenantId"];

    // Values from app registration
    var clientId = _configuration.GetValue<string>("AzureAd:ClientId");
    var clientSecret = _configuration.GetValue<string>("AzureAd:ClientSecret");

    var options = new TokenCredentialOptions
    {
        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };

    // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret, options);

    return new GraphServiceClient(clientSecretCredential, scopes);
}
```

Using Graph SDK client with a certificate

```csharp
private async Task<GraphServiceClient> GetGraphClientWithClientCertificateCredentialAsync()
{
    string[] scopes = new[] { "https://graph.microsoft.com/.default" };
    var tenantId = _configuration["AzureAd:TenantId"];

    var options = new TokenCredentialOptions
    {
        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };

    // Values from app registration
    var clientId = _configuration.GetValue<string>("AzureAd:ClientId");

    var certififacte = await GetCertificateAsync();
    var clientCertificateCredential = new ClientCertificateCredential(
        tenantId, clientId, certififacte, options);

    // var clientCertificatePath = _configuration.GetValue<string>("AzureAd:CertificateName");
    // https://learn.microsoft.com/en-us/dotnet/api/azure.identity.clientcertificatecredential?view=azure-dotnet
    // var clientCertificateCredential = new ClientCertificateCredential(
    //    tenantId, clientId, clientCertificatePath, options);

    return new GraphServiceClient(clientCertificateCredential, scopes);
}

private async Task<X509Certificate2> GetCertificateAsync()
{
    var identifier = _configuration["AzureAd:ClientCertificates:0:KeyVaultCertificateName"];

    if (identifier == null)
        throw new ArgumentNullException(nameof(identifier));

    var vaultBaseUrl = _configuration["CallApi:ClientCertificates:0:KeyVaultUrl"];
    if(vaultBaseUrl == null)
        throw new ArgumentNullException(nameof(vaultBaseUrl));

    var secretClient = new SecretClient(vaultUri: new Uri(vaultBaseUrl), credential: new DefaultAzureCredential());

    // Create a new secret using the secret client.
    var secretName = identifier;
    //var secretVersion = "";
    KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);

    var privateKeyBytes = Convert.FromBase64String(secret.Value);

    var certificateWithPrivateKey = new X509Certificate2(privateKeyBytes,
        string.Empty, X509KeyStorageFlags.MachineKeySet);

    return certificateWithPrivateKey;
}
```

[secrets or certificates](https://learn.microsoft.com/en-us/azure/active-directory/develop/sample-v2-code#service--daemon)


## Using Graph through MSAL using IConfidentialClientApplication

Microsoft.Identity.Client: 

```csharp
 var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
    .WithClientSecret(config.ClientSecret)
    .WithAuthority(new Uri(config.Authority))
    .Build();

app.AddInMemoryTokenCache();
```
or 

```csharp
var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
    .WithCertificate(certificate)
    .WithAuthority(new Uri(config.Authority))
    .Build(); 
  
app.AddInMemoryTokenCache();
```

```csharp
GraphServiceClient graphServiceClient =
    new GraphServiceClient("https://graph.microsoft.com/V1.0/", 
        new DelegateAuthenticationProvider(async (requestMessage) =>
        {
            // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
            AuthenticationResult result = await app.AcquireTokenForClient(scopes)
                .ExecuteAsync();

            // Add the access token in the Authorization header of the API request.
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", result.AccessToken);
        }));
}
```
## Links

https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-powershell

https://learn.microsoft.com/en-us/azure/active-directory/develop/sample-v2-code#service--daemon

https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph

https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS

https://oceanleaf.ch/azure-managed-identity/

https://learningbydoing.cloud/blog/stop-using-client-secrets-start-using-managed-identities/