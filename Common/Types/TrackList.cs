using LanguageExt;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace Common.Types;
public class TrackList
{
    private Dictionary<string, VTuberData> internalDictionary = new();
    public IEnumerator<VTuberData> GetEnumerator()
    {
        return internalDictionary.Values.GetEnumerator();
    }

    public int GetCount()
    {
        return internalDictionary.Count;
    }

    public static readonly Dictionary<string, int> csvHeaderIndexs = new()
    {
        { "ID", 0 },
        { "Display Name", 1 },
        { "Alias Names", 2 },
        { "YouTube Channel ID", 3 },
        { "Twitch Channel ID", 4 },
        { "Twitch Channel Name", 5 },
        { "Debut Date", 6 },
        { "Graduation Date", 7 },
        { "Active", 8 },
        { "Group Name", 9 },
        { "Nationality", 10 },
        { "Importance Level", 11 },
    };

    public TrackList(string csvFilePath, int requiredLevel, bool throwOnValidationFail)
    {
        Validation<ValidationError, TrackList> loadResult = Load(csvFilePath, requiredLevel);

        loadResult.Match(
            result =>
            {
                this.internalDictionary = result.internalDictionary;
            },
            error =>
            {
                this.internalDictionary.Clear();

                if (throwOnValidationFail)
                    throw new Exception($"Failed to load TrackList: {csvFilePath}");
            }
            );
    }

    private TrackList(Dictionary<string, VTuberData> dict)
    {
        internalDictionary = dict;
    }

    public static Validation<ValidationError, TrackList> Load(string csvFilePath, int requiredLevel)
    {
        TextFieldParser reader = new(csvFilePath)
        {
            HasFieldsEnclosedInQuotes = true,
            Delimiters = new string[] { "," },
            CommentTokens = new string[] { "#" },
            TrimWhiteSpace = false,
            TextFieldType = FieldType.Delimited,
        };

        // consume header
        string[]? headerBlock = reader.ReadFields();

        if (headerBlock is null)
        {
            return new ValidationError($"Invalid CSV header. Empty header.");
        }

        if (headerBlock.Length != csvHeaderIndexs.Count)
        {
            return new ValidationError($"Invalid CSV header. Expected: {csvHeaderIndexs.Count}. Actual: {headerBlock.Length}");
        }


        List<Validation<ValidationError, VTuberData>> lstResult = new();
        while (!reader.EndOfData)
        {
            string[]? entryBlock = reader.ReadFields();

            if (entryBlock is not null)
                lstResult.Add(Validate(entryBlock));
        }

        return lstResult.
            Sequence()
            .Match(
            lstData =>
            {
                return CheckFieldsDuplicate(lstData).Match<Validation<ValidationError, TrackList>>(
                    validatedLstData =>
                    {
                        Dictionary<string, VTuberData> dictLoaded = new();

                        foreach (VTuberData data in validatedLstData)
                        {
                            if (data.ImportanceLevel <= requiredLevel)
                            {
                                dictLoaded.Add(data.DisplayName, data);
                            }
                        }

                        return new TrackList(dictLoaded);
                    },
                    errors =>
                    {
                        return errors;
                    }
                    );
            },
            errors =>
            {
                return errors;
            });
    }

    private static Validation<ValidationError, IEnumerable<VTuberData>> CheckFieldsDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        return (
            CheckIdDuplicate(lstVtuberData),
            CheckNamesDuplicate(lstVtuberData),
            CheckYouTubeIdDuplicate(lstVtuberData),
            CheckTwitchIdDuplicate(lstVtuberData),
            CheckTwitchNameDuplicate(lstVtuberData)
            ).Apply(
            (
                dummy1,
                dummy2,
                dummy3,
                dummy4,
                dummy5
                )
            => lstVtuberData
            );
    }

    private static Validation<ValidationError, OptionNone> CheckIdDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        List<string> duplicateId = lstVtuberData
            .GroupBy(p => p.Id)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key)
            .ToList();

        if (duplicateId.Any())
        {
            return new ValidationError($"Duplicate IDs: {string.Join(", ", duplicateId)}");
        }
        else
        {
            return new OptionNone();
        }
    }

    private static Validation<ValidationError, OptionNone> CheckNamesDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        List<string> lstName = lstVtuberData.Map(p => p.DisplayName).ToList();
        List<string> lstAliasNames = lstVtuberData.Map(p => p.LstAliasName).SelectMany(p => p).ToList();

        List<string> duplicateNames = (lstName.Append(lstAliasNames)).GroupBy(p => p)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            return new ValidationError($"Duplicate names and alias names: {string.Join(", ", duplicateNames)}");
        }
        else
        {
            return new OptionNone();
        }
    }

    private static Validation<ValidationError, OptionNone> CheckYouTubeIdDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        List<string> duplicateYouTubeIds = lstVtuberData
            .Where(p => p.YouTubeChannelId != "")
            .GroupBy(p => p.YouTubeChannelId)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key)
            .ToList();

        if (duplicateYouTubeIds.Any())
        {
            return new ValidationError($"Duplicate YouTube channel IDs: {string.Join(", ", duplicateYouTubeIds)}");
        }
        else
        {
            return new OptionNone();
        }
    }

    private static Validation<ValidationError, OptionNone> CheckTwitchIdDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        List<string> duplicateTwitchIds = lstVtuberData
            .Where(p => p.TwitchChannelId != "")
            .GroupBy(p => p.TwitchChannelId)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key)
            .ToList();

        if (duplicateTwitchIds.Any())
        {
            return new ValidationError($"Duplicate Twitch channel IDs: {string.Join(", ", duplicateTwitchIds)}");
        }
        else
        {
            return new OptionNone();
        }
    }

    private static Validation<ValidationError, OptionNone> CheckTwitchNameDuplicate(IEnumerable<VTuberData> lstVtuberData)
    {
        List<string> duplicateTwitchName = lstVtuberData
            .Where(p => p.TwitchChannelName != "")
            .GroupBy(p => p.TwitchChannelName)
            .Where(p => p.Count() > 1)
            .Select(p => p.Key)
            .ToList();

        if (duplicateTwitchName.Any())
        {
            return new ValidationError($"Duplicate Twitch channel names: {string.Join(", ", duplicateTwitchName)}");
        }
        else
        {
            return new OptionNone();
        }
    }

    private static Validation<ValidationError, VTuberData> Validate(string[] entryBlock)
    {
        return (
            ValidateId(entryBlock[csvHeaderIndexs["ID"]]),
            ValidateDisplayName(entryBlock[csvHeaderIndexs["Display Name"]]),
            ValidateAliasNames(entryBlock[csvHeaderIndexs["Alias Names"]]),
            ValidateYouTubeChannelId(entryBlock[csvHeaderIndexs["YouTube Channel ID"]]),
            ValidateTwitchInformation(entryBlock[csvHeaderIndexs["Twitch Channel ID"]], entryBlock[csvHeaderIndexs["Twitch Channel Name"]]),
            ValidateDebutDate(entryBlock[csvHeaderIndexs["Debut Date"]]),
            ValidateGraduateDate(entryBlock[csvHeaderIndexs["Graduation Date"]]),
            ValidateActivity(entryBlock[csvHeaderIndexs["Active"]]),
            ValidateGroupName(entryBlock[csvHeaderIndexs["Group Name"]]),
            ValidateNationality(entryBlock[csvHeaderIndexs["Nationality"]]),
            ValidateImportance(entryBlock[csvHeaderIndexs["Importance Level"]]))
            .Apply(
            (
                id,
                displayName,
                aliasNames,
                YouTubeChannelId,
                twitchInformation,
                debutDate,
                graduateDate,
                isActive,
                groupName,
                nationality,
                importanceLevel
                ) => new VTuberData(
                    id,
                    displayName,
                    aliasNames,
                    YouTubeChannelId,
                    twitchInformation.Id,
                    twitchInformation.Name,
                    debutDate,
                    graduateDate,
                    isActive,
                    groupName,
                    nationality,
                    importanceLevel
                    )
                );
    }

    private static Validation<ValidationError, string> ValidateId(string rawId)
    {
        if (rawId.Length != 32)
        {
            return new ValidationError($"ID should be a valid UUID with lowercase and no '-': {rawId}");
        }
        else
        {
            return rawId;
        }
    }

    private static Validation<ValidationError, string> ValidateDisplayName(string rawName)
    {
        if (rawName.Length == 0)
        {
            return new ValidationError($"Display name should not be empty: {rawName}");
        }
        else
        {
            return rawName;
        }
    }

    private static Validation<ValidationError, List<string>> ValidateAliasNames(string rawNames)
    {
        List<string> result = rawNames.Split(',').ToList().Where( p => p !="").ToList();

        if (result.Where(p => p.Length == 0).Any())
        {
            return new ValidationError($"Alias names should not be empty: {rawNames}");
        }
        else
        {
            return result;
        }
    }

    private static Validation<ValidationError, string> ValidateYouTubeChannelId(string rawId)
    {
        // YouTube Channel ID format: 24 characters
        // Ex: UCRf7OJA3azS4RsGd_G96FUw
        if (rawId.Length != 24)
        {
            if (rawId.Length != 0)
            {
                return new ValidationError($"Invalid YouTube Channel ID: {rawId}");
            }
        }

        return rawId;
    }

    private readonly record struct TwitchInformation(string Id, string Name);
    private static Validation<ValidationError, TwitchInformation> ValidateTwitchInformation(string rawId, string rawName)
    {
        // Both Twitch Channel ID and Twitch Channel Name should present at the same time
        // Ex: 436730505, vtuber_amiru
        int TwitchIdNameNonEmptyCount = (new List<string> { rawId, rawName }).Where(p => p.Length != 0).Count();
        if (TwitchIdNameNonEmptyCount != 2)
        {
            if (TwitchIdNameNonEmptyCount != 0)
            {
                return new ValidationError($"Twitch ID or name missing: {rawId}, {rawName}");
            }
        }

        return new TwitchInformation(Id: rawId, Name: rawName);
    }

    private static Validation<ValidationError, DateTime> ValidateDebutDate(string rawDate)
    {
        bool isValid = TryParseDate(rawDate, out DateTime parsedDate);

        if (!isValid)
        {
            return new ValidationError($"Invalid deubt date: {rawDate}");
        }
        else
        {
            return parsedDate;
        }
    }

    private static Validation<ValidationError, DateTime> ValidateGraduateDate(string rawDate)
    {
        bool isValid = TryParseDate(rawDate, out DateTime parsedDate);

        if (!isValid)
        {
            return new ValidationError($"Invalid graduate date: {rawDate}");
        }
        else
        {
            return parsedDate;
        }
    }

    private static Validation<ValidationError, bool> ValidateActivity(string rawActivity)
    {
        bool isValid = int.TryParse(rawActivity, out int result);
        bool isActive = result == 1 ? true : false;

        if (!isValid)
        {
            return new ValidationError($"Invalid activity: {rawActivity}");
        }
        else
        {
            return isActive;
        }
    }

    private static Validation<ValidationError, string> ValidateGroupName(string rawName)
    {
        return rawName;
    }

    private static Validation<ValidationError, string> ValidateNationality(string rawNationality)
    {
        if (rawNationality.Length == 0)
        {
            return new ValidationError($"Nationality should not be empty: {rawNationality}");
        }
        else
        {

            return rawNationality;
        }
    }

    private static Validation<ValidationError, int> ValidateImportance(string rawImportance)
    {
        bool isValid = int.TryParse(rawImportance, out int result);

        if (!isValid)
        {
            return new ValidationError($"Invalid importance level: {rawImportance}");
        }
        else
        {
            return result;
        }
    }

    private static bool TryParseDate(string dateString, out DateTime result)
    {
        if (dateString.Length == 0)
        {
            result = DateTime.UnixEpoch;
            return true;
        }

        return DateTime.TryParseExact(dateString,
            new string[] { "yyyy/MM/dd", "yyyy/M/dd", "yyyy/MM/d", "yyyy/M/d" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    public string GetDisplayName(string name)
    {
        if (internalDictionary.ContainsKey(name))
            return name;

        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            string displayName = keyValuePair.Key;
            VTuberData vtuberData = keyValuePair.Value;

            foreach (string aliasName in vtuberData.LstAliasName)
            {
                if (aliasName == name)
                    return displayName;
            }
        }

        throw new KeyNotFoundException($"Could not find {name}");
    }
    public string GetDisplayNameByYouTubeChannelId(string id)
    {
        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            string displayName = keyValuePair.Key;
            VTuberData vtuberData = keyValuePair.Value;

            if (vtuberData.YouTubeChannelId == id)
                return displayName;
        }

        throw new KeyNotFoundException($"Could not find YouTube Channel ID: {id}");
    }

    public string GetDisplayNameByTwitchChannelId(string id)
    {
        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            string displayName = keyValuePair.Key;
            VTuberData vtuberData = keyValuePair.Value;

            if (vtuberData.TwitchChannelId == id)
                return displayName;
        }

        throw new KeyNotFoundException($"Could not find Twitch Channel ID: {id}");
    }
    public string GetId(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].Id;
    }

    public string GetYouTubeChannelIdByName(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].YouTubeChannelId;
    }
    public string GetTwitchChannelIdByName(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].TwitchChannelId;
    }
    public string GetTwitchChannelNameByName(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].TwitchChannelName;
    }
    public string GetGroupNameByName(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].GroupName;
    }

    public DateTime GetDebutDate(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].DebuteDate;
    }

    public DateTime GetGraduationDate(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].GraduationDate;
    }

    public string GetNationalityByName(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].Nationality;
    }

    public bool GetIsActive(string name)
    {
        string displayName = GetDisplayName(name);

        return internalDictionary[displayName].IsActive;
    }

    public int GetVtuberWithGroupCount()
    {
        int vtuberWithGroupCount = 0;
        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            string groupName = keyValuePair.Value.GroupName;

            if (groupName != "")
                vtuberWithGroupCount++;
        }

        return vtuberWithGroupCount;
    }

    public List<string> GetDisplayNameList()
    {
        return internalDictionary.Keys.ToList();
    }

    public List<string> GetGroupNameList()
    {
        List<string> rList = new();
        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            if (!rList.Contains(keyValuePair.Value.GroupName))
                rList.Add(keyValuePair.Value.GroupName);
        }

        rList.Remove("");
        return rList;
    }

    public List<string> GetYouTubeChannelIdList()
    {
        List<string> rList = new();
        foreach (KeyValuePair<string, VTuberData> keyValuePair in internalDictionary)
        {
            rList.Add(keyValuePair.Value.YouTubeChannelId);
        }

        return rList;
    }
}
