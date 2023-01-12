using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;

namespace GraphClientCrendentials.Pages;

public class IndexModel : PageModel
{
    private readonly AadGraphSdkApplicationClient _aadGraphApiApplicationClient;

    public IndexModel(AadGraphSdkApplicationClient aadGraphApiApplicationClient)
    {
        _aadGraphApiApplicationClient = aadGraphApiApplicationClient;
    }

    public int UsersCount { get; set; }

    public async Task OnGetAsync()
    {
        UsersCount = await _aadGraphApiApplicationClient.GetUsersAsync();

    }
}