using Common.Types;
using LanguageExt;

string tracklistPath = args.Length >= 1 ? args[0] : "./DATA/TW_VTUBER_TRACK_LIST.csv";

Validation<ValidationError, TrackList> trackListResult = TrackList.Load(csvFilePath: tracklistPath);

Console.WriteLine($"Validating track list: {tracklistPath}");

trackListResult.Match(
    trackList =>
    {
        Console.WriteLine($"Validation successful");
        Console.WriteLine($"Total entries: {trackList.GetCount()}");

        Environment.Exit(0);
    },
    errors =>
    {
        Console.WriteLine($"Validation failed");
        Console.WriteLine($"Errors:");
        Console.WriteLine(errors.Aggregate("", (a, b) => a + "\n" + b));

        Environment.Exit(1);
    }
    );
