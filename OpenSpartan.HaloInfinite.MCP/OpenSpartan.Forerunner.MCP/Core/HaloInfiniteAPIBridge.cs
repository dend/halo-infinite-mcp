using Den.Dev.Grunt.Authentication;
using Den.Dev.Grunt.Core;
using Den.Dev.Grunt.Models;
using Den.Dev.Grunt.Models.HaloInfinite;
using Den.Dev.Grunt.Models.Security;
using Microsoft.Identity.Client;
using Serilog;

namespace OpenSpartan.Forerunner.MCP.Core
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
                    Log.Logger.Error("Failed to obtain Xbox user token.");
                    return false;
                }

                var haloTicketTask = manager.RequestXstsToken(ticket.Token);
                var extendedTicketTask = manager.RequestXstsToken(ticket.Token, false);

                var haloTicket = await haloTicketTask;
                var extendedTicket = await extendedTicketTask;

                if (haloTicket == null)
                {
                    Log.Logger.Error("Failed to obtain Halo XSTS token.");
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
                        Log.Logger.Information($"Your clearance is {clearance.FlightConfigurationId} and it's set in the client.");
                        return true;
                    }
                    else
                    {
                        Log.Logger.Error("Could not obtain the clearance.");
                        return false;
                    }
                }
                else
                {
                    Log.Logger.Error("Extended ticket is null. Cannot authenticate.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Error initializing Halo client: {ex.Message}");
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
                        Log.Logger.Error("Could not reacquire tokens.");
                        return default;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to make Halo Infinite API call. {ex.Message}");
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

        internal static async Task DownloadHaloAPIImage(string serviceImagePath, string localImagePath, bool isOnWaypoint = false)
        {
            try
            {
                // Check if local image file exists
                if (System.IO.File.Exists(localImagePath))
                {
                    return;
                }

                HaloApiResultContainer<byte[], RawResponseContainer>? image = null;

                Func<Task<HaloApiResultContainer<byte[], RawResponseContainer>>> apiCall = isOnWaypoint ?
                    async () => await HaloInfiniteAPIBridge.HaloClient.GameCmsGetGenericWaypointFile(serviceImagePath) :
                    async () => await HaloInfiniteAPIBridge.HaloClient.GameCmsGetImage(serviceImagePath);

                image = await HaloInfiniteAPIBridge.SafeAPICall(apiCall);

                // Check if the image retrieval was successful
                if (image != null && image.Result != null && image.Response.Code == 200)
                {
                    // In case the folder does not exist, make sure we create it.
                    FileInfo file = new(localImagePath);
                    file.Directory.Create();

                    await System.IO.File.WriteAllBytesAsync(localImagePath, image.Result);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to download and set image '{serviceImagePath}' to '{localImagePath}'. Error: {ex.Message}");
            }
        }
    }
}
