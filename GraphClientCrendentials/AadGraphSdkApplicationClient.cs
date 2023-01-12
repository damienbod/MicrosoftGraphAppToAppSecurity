﻿using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Graph;
using System.Security.Cryptography.X509Certificates;

namespace GraphClientCrendentials;

public class AadGraphSdkApplicationClient
{
    private readonly IConfiguration _configuration;

    public AadGraphSdkApplicationClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> GetUsersAsync()
    {
        var graphServiceClient = GetGraphClientWithClientSecretCredential();

        IGraphServiceUsersCollectionPage users = await graphServiceClient.Users
            .Request()
            .GetAsync();

        return users.Count;
    }

    /// <summary>
    /// Using client secret
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Using Graph SDK client with a certificate
    /// https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-net-client-assertions
    /// </summary>
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
}