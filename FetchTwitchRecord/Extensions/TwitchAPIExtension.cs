using log4net;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Videos.GetVideos;

namespace FetchTwitchRecord.Extensions;
internal static class TwitchAPIExtension {
    public static ulong? GetChannelFollwerCount(this TwitchAPI api, string broadcasterId, ILog log) {
        TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers.GetChannelFollowersResponse? response = ExecuteTwitchLibThrowableWithRetry(
            () => api.Helix.Channels.GetChannelFollowersAsync(
                broadcasterId: broadcasterId,
                first: 1
                ).Result,
            log: log
            );

        if (response?.Total is null) {
            return null;
        } else {
            return (ulong)response.Total;
        }
    }

    public static GetVideosResponse? GetChannelPastLivestreams(this TwitchAPI api, string userId, string afterCursor, ILog log) {
        return ExecuteTwitchLibThrowableWithRetry(
            () => api.Helix.Videos.GetVideosAsync(
                    userId: userId,
                    after: afterCursor,
                    first: 100,
                    period: TwitchLib.Api.Core.Enums.Period.Month, // this parameter doesn't work at all
                    sort: TwitchLib.Api.Core.Enums.VideoSort.Time,
                    type: TwitchLib.Api.Core.Enums.VideoType.Archive // On-demand videos (VODs) of past streams
                ).Result,
            log: log
            );
    }

    private static T? ExecuteTwitchLibThrowableWithRetry<T>(Func<T> func, ILog log) where T : class? {
        int RETRY_TIME = 10;
        TimeSpan RETRY_DELAY = new(hours: 0, minutes: 0, seconds: 3);

        for (int i = 0; i < RETRY_TIME; i++) {
            try {
                return func.Invoke();
            } catch (Exception e) {
                log.Error($"Failed to execute {func.Method.Name}. {i} tries. Retry after {RETRY_DELAY.TotalSeconds} seconds.");
                log.Error(e.Message, e);
                Task.Delay(RETRY_DELAY);
            }
        }

        return null;
    }
}
