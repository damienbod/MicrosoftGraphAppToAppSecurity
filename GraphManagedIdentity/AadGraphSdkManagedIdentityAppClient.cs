namespace GraphManagedIdentity;

public class AadGraphSdkManagedIdentityAppClient
{
    private readonly GraphApplicationClientService _graphService;

    public AadGraphSdkManagedIdentityAppClient(IConfiguration configuration, GraphApplicationClientService graphService)
    {
        _graphService = graphService;
    }

    public async Task<long?> GetUsersAsync()
    {
        var graphServiceClient = _graphService.GetGraphClientWithManagedIdentityOrDevClient();

        var users = await graphServiceClient.Users
            .GetAsync();

        return users!.Value!.Count;
    }
}