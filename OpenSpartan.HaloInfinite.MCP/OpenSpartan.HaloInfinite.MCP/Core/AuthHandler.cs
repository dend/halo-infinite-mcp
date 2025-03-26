using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace OpenSpartan.Forerunner.MCP.Core
{
    internal static class AuthHandler
    {
        internal static async Task<AuthenticationResult> InitializePublicClientApplication()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(Configuration.CacheFileName, Configuration.AppDataDirectory).Build();

            var pcaBootstrap = PublicClientApplicationBuilder
                .Create(Configuration.ClientID)
                .WithDefaultRedirectUri()
                .WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount);

            var pca = pcaBootstrap.Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            IAccount accountToLogin = (await pca.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResult = null;

            try
            {
                authResult = await pca.AcquireTokenSilent(Configuration.Scopes, accountToLogin)
                                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    authResult = await pca.AcquireTokenInteractive(Configuration.Scopes)
                                                .WithAccount(accountToLogin)
                                                .ExecuteAsync();
                }
                catch (MsalClientException ex)
                {
                    // Authentication was not successsful, we have no token.
                    // TODO: Update this to use proper logging.
                    Console.WriteLine(ex.Message);
                }
            }

            return authResult;
        }
    }
}
