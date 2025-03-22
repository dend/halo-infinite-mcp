using Den.Dev.Grunt.Authentication;
using Den.Dev.Grunt.Core;
using Den.Dev.Grunt.Models;
using Den.Dev.Grunt.Models.HaloInfinite;
using Den.Dev.Grunt.Models.Security;
using Microsoft.Identity.Client;

namespace OpenSpartan.HaloInfinite.MCP.Core
{
    internal static class HaloInfiniteAPIBridge
    {
        internal static XboxTicket XboxUserContext { get; set; }
        internal static HaloInfiniteClient HaloClient { get; set; }

        internal static async Task<bool> InitializeHaloClient(AuthenticationResult authResult)
        {
            try
            {
                HaloAuthenticationClient haloAuthClient = new();
                XboxAuthenticationClient manager = new();

                var ticket = await manager.RequestUserToken(authResult.AccessToken) ?? await manager.RequestUserToken(authResult.AccessToken);

                if (ticket == null)
                {
                    Console.WriteLine("Failed to obtain Xbox user token.");
                    return false;
                }

                var haloTicketTask = manager.RequestXstsToken(ticket.Token);
                var extendedTicketTask = manager.RequestXstsToken(ticket.Token, false);

                var haloTicket = await haloTicketTask;
                var extendedTicket = await extendedTicketTask;

                if (haloTicket == null)
                {
                    Console.WriteLine("Failed to obtain Halo XSTS token.");
                    return false;
                }

                var haloToken = await haloAuthClient.GetSpartanToken(haloTicket.Token, 4);

                if (extendedTicket != null)
                {
                    XboxUserContext = extendedTicket;

                    HaloClient = new HaloInfiniteClient(haloToken.Token, extendedTicket.DisplayClaims.Xui[0].XUID, userAgent: $"{Configuration.PackageName}/{Configuration.Version}-{Configuration.BuildId}");

                    PlayerClearance? clearance = null;

                    clearance = (await SafeAPICall(async () => await HaloClient.SettingsActiveClearance(Configuration.HaloInfiniteAPIRelease)))?.Result;

                    if (clearance != null && !string.IsNullOrWhiteSpace(clearance.FlightConfigurationId))
                    {
                        HaloClient.ClearanceToken = clearance.FlightConfigurationId;
                        Console.WriteLine($"Your clearance is {clearance.FlightConfigurationId} and it's set in the client.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Could not obtain the clearance.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Extended ticket is null. Cannot authenticate.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Halo client: {ex.Message}");
                return false;
            }
        }

        public static async Task<HaloApiResultContainer<T, RawResponseContainer>> SafeAPICall<T>(Func<Task<HaloApiResultContainer<T, RawResponseContainer>>> orionAPICall)
        {
            try
            {
                HaloApiResultContainer<T, RawResponseContainer> result = await orionAPICall();

                if (result != null && result.Response != null && result.Response.Code == 401)
                {
                    if (await ReAcquireTokens())
                    {
                        result = await orionAPICall();
                    }
                    else
                    {
                        Console.WriteLine("Could not reacquire tokens.");
                        return default;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to make Halo Infinite API call. {ex.Message}");
                return default;
            }
        }

        internal static async Task<bool> ReAcquireTokens()
        {
            var authResult = await AuthHandler.InitializePublicClientApplication();
            if (authResult != null)
            {
                var result = await InitializeHaloClient(authResult);

                return result;
            }
            else
            {
                return false;
            }
        }
    }
}
