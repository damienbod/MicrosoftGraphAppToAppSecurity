using Azure.Identity;
using Microsoft.Graph;

namespace GraphManagedIdentity;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly IConfiguration _configuration;

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration)
    {
        _configuration = configuration;
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

        var chainedTokenCredential = new ChainedTokenCredential(
            new ManagedIdentityCredential(),
            new EnvironmentCredential());

        var graphServiceClient = new GraphServiceClient(chainedTokenCredential, scopes);

        return graphServiceClient;
    }
}
