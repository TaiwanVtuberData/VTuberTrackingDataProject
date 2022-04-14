using Common.Types;

namespace FetchBasicData;
readonly record struct Data(YouTubeData? YouTube, TwitchData? Twitch);
