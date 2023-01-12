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
