using GraphManagedIdentity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphManagedIdentity.Pages;

public class IndexModel : PageModel
{
    private readonly AadGraphSdkManagedIdentityAppClient _aadGraphSdkManagedIdentityAppClient;

    public IndexModel(AadGraphSdkManagedIdentityAppClient aadGraphSdkManagedIdentityAppClient)
    {
        _aadGraphSdkManagedIdentityAppClient = aadGraphSdkManagedIdentityAppClient;
    }

    public int UsersCount { get; set; }

    public async Task OnGetAsync()
    {
        UsersCount = await _aadGraphSdkManagedIdentityAppClient.GetUsersAsync();

    }
}