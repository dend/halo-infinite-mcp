using Den.Dev.Grunt.Models.HaloInfinite;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.Forerunner.MCP.Core;
using OpenSpartan.Forerunner.MCP.Helpers;
using OpenSpartan.Forerunner.MCP.Models;
using Serilog;
using SkiaSharp;
using System.Text.Json;

namespace OpenSpartan.Forerunner.MCP.Tools
{
    public class MyCareerRankTool : ITool
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private const int ThumbnailSize = 128;

        public string Name => "opsp_my_career_rank";
        public string Description => "Returns the player's current Halo Infinite career rank (or level) and progress to the top level (Hero). The player earns experience with every match and might want to know how long until Hero rank.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            CurrentPlayerRank playerRank = new();

            RewardTrackResultContainer careerSnapshot = null;

            var careerRankTask = HaloInfiniteAPIBridge.SafeAPICall(() => HaloInfiniteAPIBridge.HaloClient.EconomyGetPlayerCareerRank([$"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})"], "careerRank1"));
            var rankCollectionTask = HaloInfiniteAPIBridge.SafeAPICall(() => HaloInfiniteAPIBridge.HaloClient.GameCmsGetCareerRanks("careerRank1"));
            await Task.WhenAll(careerRankTask, rankCollectionTask);

            var careerRankTaskResult = careerRankTask.Result;
            var rankCollectionTaskResult = rankCollectionTask.Result;

            string qualifiedRankImagePath = null;
            string qualifiedAdornmentImagePath = null;

            if (careerRankTaskResult != null && careerRankTaskResult.Response.Code == 200)
            {
                careerSnapshot = careerRankTaskResult.Result;
            }

            if (rankCollectionTaskResult != null && (rankCollectionTaskResult.Response.Code == 200 || rankCollectionTaskResult.Response.Code == 304))
            {
                playerRank.MaxRank = rankCollectionTaskResult.Result.Ranks.Count;

                if (careerSnapshot != null)
                {
                    var currentRank = 0;

                    // If we're talking about Hero rank, then we don't need to append an extra 1.
                    if (careerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank != 272)
                    {
                        currentRank = careerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank + 1;
                    }
                    else
                    {
                        currentRank = careerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank;
                    }

                    var currentCareerStage = rankCollectionTaskResult.Result.Ranks.FirstOrDefault(c => c.Rank == currentRank);

                    if (currentCareerStage != null)
                    {
                        if (currentCareerStage.Rank != 272)
                        {
                            playerRank.Title = $"{currentCareerStage.TierType} {currentCareerStage.RankTitle.Value} {currentCareerStage.RankTier.Value}";
                            playerRank.CurrentRankExperience = careerRankTaskResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                            playerRank.RequiredRankExperience = currentCareerStage.XpRequiredForRank;
                        }
                        else
                        {
                            // Hero rank is just "Hero" - no need to interpolate with other strings.
                            playerRank.Title = currentCareerStage.RankTitle.Value;
                            playerRank.CurrentRankExperience = playerRank.RequiredRankExperience = currentCareerStage.XpRequiredForRank;
                        }

                        playerRank.ExperienceTotalRequired = rankCollectionTaskResult.Result.Ranks.Sum(item => item.XpRequiredForRank);

                        var relevantRanks = rankCollectionTaskResult.Result.Ranks.TakeWhile(c => c.Rank < currentRank);
                        playerRank.ExperienceEarnedToDate = relevantRanks.Sum(rank => rank.XpRequiredForRank) + careerRankTaskResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;

                        // Handle known bug in the Halo Infinite CMS for rank images
                        if (currentCareerStage.RankLargeIcon == "career_rank/CelebrationMoment/219_Cadet_Onyx_III.png")
                        {
                            currentCareerStage.RankLargeIcon = "career_rank/CelebrationMoment/19_Cadet_Onyx_III.png";
                        }

                        qualifiedRankImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankLargeIcon);
                        qualifiedAdornmentImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankAdornmentIcon);

                        FileSystemHelpers.EnsureDirectoryExists(qualifiedRankImagePath);
                        FileSystemHelpers.EnsureDirectoryExists(qualifiedAdornmentImagePath);

                        await HaloInfiniteAPIBridge.DownloadHaloAPIImage(currentCareerStage.RankLargeIcon, qualifiedRankImagePath);
                        await HaloInfiniteAPIBridge.DownloadHaloAPIImage(currentCareerStage.RankAdornmentIcon, qualifiedAdornmentImagePath);
                    }
                }
                else
                {
                    Log.Logger.Error("Could not build out the career snapshot.");
                }
            }

            var contentItems = new List<Content>();

            if (careerRankTaskResult != null && playerRank != null && !string.IsNullOrWhiteSpace(playerRank.Title))
            {
                contentItems.Add(new Content()
                {
                    Text = JsonSerializer.Serialize(playerRank, _jsonOptions),
                    Type = "text",
                    MimeType = "application/json"
                });

                if (System.IO.File.Exists(qualifiedRankImagePath))
                {
                    try
                    {
                        // Resize rank icon and convert to base64
                        string base64RankImage = await ImageHelpers.ResizeImageToBase64(
                            qualifiedRankImagePath,
                            ThumbnailSize,
                            null,
                            100,
                            SKEncodedImageFormat.Png);

                        if (!string.IsNullOrEmpty(base64RankImage))
                        {
                            contentItems.Add(new Content()
                            {
                                Type = "image",
                                MimeType = "image/png",
                                Data = base64RankImage,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error($"Failed to process rank image: {ex.Message}");
                    }
                }

                if (System.IO.File.Exists(qualifiedAdornmentImagePath))
                {
                    try
                    {
                        // Resize adornment icon and convert to base64
                        string base64AdornmentImage = await ImageHelpers.ResizeImageToBase64(
                            qualifiedAdornmentImagePath,
                            ThumbnailSize,
                            ThumbnailSize,
                            100,
                            SKEncodedImageFormat.Png);

                        if (!string.IsNullOrEmpty(base64AdornmentImage))
                        {
                            contentItems.Add(new Content()
                            {
                                Type = "image",
                                MimeType = "image/png",
                                Data = base64AdornmentImage,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error($"Failed to process adornment image: {ex.Message}");
                    }
                }

                return new CallToolResponse()
                {
                    Content = contentItems
                };
            }
            else
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = "No API endpoints could be obtained.", Type = "text" }]
                };
            }
        }
    }
}