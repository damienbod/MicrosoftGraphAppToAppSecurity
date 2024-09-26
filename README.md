# Microsoft Graph App to App Security

[![.NET](https://github.com/damienbod/MicrosoftGraphAppToAppSecurity/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/MicrosoftGraphAppToAppSecurity/actions/workflows/dotnet.yml)

Accessing Microsoft Graph can be initialized for app-to-app (application permissions) security in three different ways. (Trusted clients)

- Using Managed Identities
- Using Azure SDK and Graph SDK directly
- Using Microsoft.Identity.Client and MSAL to acquire an access token which can be used directly against Graph or using GraphServiceClient with the DelegateAuthenticationProvider class

## History

- 2024-09-26 Updated packages, .NET 8
- 2023-03-02 Updated packages, Microsoft.Graph 5.0.0

## Using managed identities

[managed identity](https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-powershell)

```csharp
using Azure.Identity;
using Microsoft.Graph;

namespace GraphManagedIdentity;

public class GraphApplicationClientService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private GraphServiceClient? _graphServiceClient;

    public GraphApplicationClientService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public GraphServiceClient GetGraphClientWithManagedIdentityOrDevClient()
    {
        if (_graphServiceClient != null)
            return _graphServiceClient;

        string[] scopes = new[] { "https://graph.microsoft.com/.default" };

        var chainedTokenCredential = GetChainedTokenCredentials();
        _graphServiceClient = new GraphServiceClient(chainedTokenCredential, scopes);

        return _graphServiceClient;
    }

    private ChainedTokenCredential GetChainedTokenCredentials()
    {
        if (!_environment.IsDevelopment())
        {
            return new ChainedTokenCredential(new ManagedIdentityCredential());
        }
        else // dev env
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration.GetValue<string>("AzureAd:ClientId");
            var clientSecret = _configuration.GetValue<string>("AzureAd:ClientSecret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var devClientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var chainedTokenCredential = new ChainedTokenCredential(devClientSecretCredential);

            return chainedTokenCredential;
        }
    }
}
```

### Managed Identity dev environment

Why not use the EnvironmentCredential?

The identitiy uses an Azure App registration to setup the secret, client etc and the secret should not be in th ecode. Env are usually saved to the debug profile and this gets pushed.

A better way is to use a developement ClientSecretCredential and read the secret from the user secrets. The ClientSecretCredential only works in the dev env.

## Using Graph SDK with certificates or secrets

Using Graph SDK client with a secret
```csharp
private readonly IConfiguration _configuration;
private GraphServiceClient? _graphServiceClient;

public GraphApplicationClientService(IConfiguration configuration)
{
    _configuration = configuration;
}

public GraphServiceClient GetGraphClientWithClientSecretCredential()
{
    if (_graphServiceClient != null)
        return _graphServiceClient;

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

    _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
    return _graphServiceClient;
}
```

Using Graph SDK client with a certificate

```csharp
public class GraphApplicationClientService
{
    private readonly IConfiguration _configuration;
    private GraphServiceClient? _graphServiceClient;

    public GraphApplicationClientService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GraphServiceClient> GetGraphClientWithClientCertificateCredentialAsync()
    {
        if (_graphServiceClient != null)
            return _graphServiceClient;

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

         _graphServiceClient = new GraphServiceClient(clientCertificateCredential, scopes);
        return _graphServiceClient;
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

The GraphServiceClient can be created using the DelegateAuthenticationProvider

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

https://github.com/Azure/azure-sdk-for-net

https://learn.microsoft.com/en-us/dotnet/api/azure.identity.environmentcredential?view=azure-dotnet

https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/58