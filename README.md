# Microsoft Graph App to App Security

Accessing Microsoft Graph can be initialized for app-to-app (application permissions) security in three different ways. (Trusted clients)

- Using Managed Identities
- Using Azure SDK and Graph SDK directly
- Using Micorosoft.Identity.Client and MSAL to acquire an access token which can be used directly against Graph or using GraphServiceClient with the DelegateAuthenticationProvider class

## Using managed identities

[managed identity](https://learn.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-powershell)

```csharp
using Azure.Identity;
using Microsoft.Graph;

namespace GraphManagedIdentity;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<int> GetUsersAsync()
    {
        var graphServiceClient = GetGraphClientWithManagedIdentity();

        IGraphServiceUsersCollectionPage users = await graphServiceClient.Users
            .Request()
            .GetAsync();

        return users.Count;
    }

    private GraphServiceClient GetGraphClientWithManagedIdentity()
    {
        string[] scopes = new[] { "https://graph.microsoft.com/.default" };

        var chainedTokenCredential = GetChainedTokenCredentials();
        var graphServiceClient = new GraphServiceClient(chainedTokenCredential, scopes);

        return graphServiceClient;
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

            var chainedTokenCredential = new ChainedTokenCredential(
                new ManagedIdentityCredential(),
                devClientSecretCredential);

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