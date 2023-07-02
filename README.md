# VTuberTrackingDataProject

## Solution Structure

### Common

Commonly used classes and utilities.

### TrackListValidation

Used to validate track list in https://github.com/TaiwanVtuberData/TaiwanVtuberTrackingData/blob/master/DATA/TW_VTUBER_TRACK_LIST.csv .


### FetchBasicData

Get basic data such as https://github.com/TaiwanVtuberData/TaiwanVtuberTrackingData/blob/master/2022-04/basic-data_2022-04-14-09-56-24.csv .

### FetchRecord

Get channel video view count such as https://github.com/TaiwanVtuberData/TaiwanVtuberTrackingData/blob/master/2022-04/record_2022-04-14-05-39-30.csv .

It uses FetchYouTubeRecord and FetchTwitchRecord.

### GenerateJsonFile

Generate JSON API data in https://github.com/TaiwanVtuberData/TaiwanVTuberTrackingDataJson .

### GenerateGraph

Generate CSV file of various statistics.

### GenerateReportForPTT

Generate color coded statistics for PTT.
