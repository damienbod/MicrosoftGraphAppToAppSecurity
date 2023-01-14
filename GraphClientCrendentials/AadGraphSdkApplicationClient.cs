using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Graph;
using System.Security.Cryptography.X509Certificates;

namespace GraphClientCrendentials;

public class AadGraphSdkApplicationClient
{
    private readonly IConfiguration _configuration;
    private readonly GraphApplicationClientService _graphService;

    public AadGraphSdkApplicationClient(IConfiguration configuration, GraphApplicationClientService graphService)
    {
        _configuration = configuration;
        _graphService = graphService;
    }

    public async Task<int> GetUsersAsync()
    {
        var graphServiceClient = _graphService.GetGraphClientWithClientSecretCredential();

        IGraphServiceUsersCollectionPage users = await graphServiceClient.Users
            .Request()
            .GetAsync();

        return users.Count;
    }
}
