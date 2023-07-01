using Common.Types;
using Microsoft.VisualBasic.FileIO;

namespace Common.Utils;
public class CsvUtility {
    public static List<VTuberStatistics> ReadStatisticsList(string filePath) {
        TextFieldParser reader = new(filePath) {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null)
            return new();

        VTuberStatistics.Version version = VTuberStatistics.GetVersionByHeaderLength(headerBlock.Length);
        if (version == VTuberStatistics.Version.Unknown)
            return new();

        List<VTuberStatistics> rLst = new();

        while (!reader.EndOfData) {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is null || entryBlock.Length < 1) {
                continue;
            }

            rLst.Add(new VTuberStatistics(entryBlock, entryBlock[0]));
        }

        return rLst;
    }

    public static Dictionary<string, VTuberStatistics> ReadStatisticsDictionary(string filePath) {
        List<VTuberStatistics> lstStatistics = ReadStatisticsList(filePath);

        return lstStatistics.ToDictionary(
            t => t.Id,
            t => t);
    }
}
