﻿using Common.Types;
using System.Xml;
using TwitchLib.Api;

namespace FetchTwitchStatistics;
public class Fetcher
{
    private readonly TwitchAPI api;

    public Fetcher(string clientId, string secret)
    {
        api = new TwitchAPI();
        api.Settings.ClientId = clientId;
        api.Settings.Secret = secret;
    }

    public bool GetAll(string userId, out TwitchStatistics statistics, out TopVideosList topVideoList)
    {
        var (successStatistics, followerCount) = GetChannelStatistics(userId);
        var (successRecentViewCount, medianViewCount, highestViewCount, highestViewdVideoID, topVideoList_) = GetChannelRecentViewStatistic(userId);

        if (successStatistics && successRecentViewCount)
        {
            statistics = new TwitchStatistics
            {
                FollowerCount = followerCount,
                RecentMedianViewCount = medianViewCount,
                RecentHighestViewCount = highestViewCount,
                HighestViewedVideoURL = (highestViewdVideoID != "") ? $"https://www.twitch.tv/videos/{highestViewdVideoID}" : "",
            };
            topVideoList = topVideoList_;
            return true;
        }
        else
        {
            statistics = new TwitchStatistics();
            topVideoList = new TopVideosList();
            return false;
        }
    }

    private (bool Success, ulong FollowerCount) GetChannelStatistics(string userId)
    {
        TwitchLib.Api.Helix.Models.Users.GetUserFollows.GetUsersFollowsResponse? usersFollowsResponseResult = null;

        bool hasResponse = false;
        for (int i = 0; i < 2; i++)
        {
            try
            {
                var usersFollowsResponse =
                    api.Helix.Users.GetUsersFollowsAsync(
                        first: 100,
                        toId: userId
                        );
                usersFollowsResponseResult = usersFollowsResponse.Result;

                hasResponse = true;
                break;
            }
            catch
            {
            }
        }

        if (!hasResponse || usersFollowsResponseResult is null)
        {
            return (false, 0);
        }

        return (true, (ulong)usersFollowsResponseResult.TotalFollows);
    }

    private (bool Success, ulong MedianViewCount, ulong HighestViewCount, string HighestViewedVideoID, TopVideosList TopVideosList_)
        GetChannelRecentViewStatistic(string userId)
    {
        List<Tuple<string, ulong>> viewCountList = new();

        string afterCursor = "";
        TopVideosList topVideosList = new();
        while (afterCursor != null)
        {
            TwitchLib.Api.Helix.Models.Videos.GetVideos.GetVideosResponse? videoResponseResult = null;

            bool hasResponse = false;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var videosResponse =
                        api.Helix.Videos.GetVideoAsync(
                            userId: userId,
                            after: afterCursor,
                            first: 100,
                            period: TwitchLib.Api.Core.Enums.Period.Month, // this parameter doesn't work at all
                            sort: TwitchLib.Api.Core.Enums.VideoSort.Time,
                            type: TwitchLib.Api.Core.Enums.VideoType.Archive // Archive type probably is past broadcasts
                            );
                    videoResponseResult = videosResponse.Result;

                    hasResponse = true;
                    break;
                }
                catch
                {
                }
            }

            if (!hasResponse || videoResponseResult is null)
            {
                return (false, 0, 0, "", new());
            }

            afterCursor = videoResponseResult.Pagination.Cursor;

            foreach (TwitchLib.Api.Helix.Models.Videos.GetVideos.Video video in videoResponseResult.Videos)
            {
                string videoId = video.Id;
                ulong viewCount = (ulong)video.ViewCount;
                DateTime publishTime = DateTime.Parse(video.PublishedAt);

                if ((DateTime.UtcNow - publishTime) < TimeSpan.FromDays(30))
                {
                    // there is currently no way to know which video is streaming
                    // 0 view count is observed to be a livestream
                    if (viewCount != 0)
                    {
                        viewCountList.Add(new Tuple<string, ulong>(videoId, viewCount));
                    }

                    try
                    {
                        topVideosList.Insert(new VideoInformation
                        {
                            Owner = userId,
                            Url = $"https://www.twitch.tv/videos/{video.Id}",
                            Title = video.Title,
                            ThumbnailUrl = video.ThumbnailUrl,
                            PublishDateTime = XmlConvert.ToDateTime(video.PublishedAt, XmlDateTimeSerializationMode.Utc),
                            ViewCount = viewCount,
                        });
                    }
                    catch
                    {

                    }
                }
            }
        }

        if (viewCountList.Count <= 0)
        {
            // return true because it is a successful result
            return (true, 0, 0, "", new());
        }

        ulong medianViews = GetMedian(viewCountList);

        viewCountList.Sort(CompareTupleSecondValue);
        Tuple<string, ulong> highestViewPair = viewCountList.Last();
        return (true, medianViews, highestViewPair.Item2, highestViewPair.Item1, topVideosList);
    }

    private static ulong GetMedian(List<Tuple<string, ulong>> list)
    {
        list.Sort(CompareTupleSecondValue);

        if (list.Count == 0)
            return 0;

        if (list.Count == 1)
            return list[0].Item2;

        if (list.Count % 2 == 1)
            return list[list.Count / 2].Item2;
        else
            return (list[list.Count / 2 - 1].Item2 + list[list.Count / 2].Item2) / 2;
    }

    private static int CompareTupleSecondValue(Tuple<string, ulong> v1, Tuple<string, ulong> v2)
    {
        return Comparer<ulong>.Default.Compare(v1.Item2, v2.Item2);
    }
}