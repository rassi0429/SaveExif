using FrooxEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SaveExif
{
    public class SavedMetadata
    {
        public const string CURRENT_VERSION = "2.1.0";

        public string LocationName { get; set; }
        public string LocationUrl { get; set; }
        public string HostUserId { get; set; }
        public string HostUserName { get; set; }
        public string TimeTaken { get; set; }
        public string TakeUserId { get; set; }
        public string TakeUserName { get; set; }
        public string ResoniteVersion { get; set; }
        public List<string> PresentUserIdArray { get; set; }
        public List<string> PresentUserNameArray { get; set; }
        public string Version { get; set; }

        public SavedMetadata(string locationName, string locationUrl, string hostUserId, string hostUserName, string timeTaken, string takeUserId, string takeUserName, string resoniteVersion, List<string> presentUserIdArray, List<string> presentUserNameArray, string version)
        {
            LocationName = locationName;
            LocationUrl = locationUrl;
            HostUserId = hostUserId;
            HostUserName = hostUserName;
            TimeTaken = timeTaken;
            TakeUserId = takeUserId;
            TakeUserName = takeUserName;
            ResoniteVersion = resoniteVersion;
            PresentUserIdArray = presentUserIdArray;
            PresentUserNameArray = presentUserNameArray;
            Version = version;
        }

        public SavedMetadata(PhotoMetadata photoMetadata)
        {
            LocationName = photoMetadata.LocationName;
            LocationUrl = photoMetadata.LocationURL.Value.ToString();
            var hostUser = photoMetadata.LocationHost.User.Target;
            HostUserId = hostUser.UserID;
            HostUserName = hostUser.UserName;
            TimeTaken = photoMetadata.TimeTaken.Value.ToLocalTime().ToString();
            var takeUser = photoMetadata.TakenBy.User.Target;
            TakeUserId = takeUser.UserID;
            TakeUserName = takeUser.UserName;
            ResoniteVersion = photoMetadata.AppVersion;
            var presentUsers = photoMetadata.UserInfos.Select(info => info.User.User.Target);
            PresentUserIdArray = presentUsers.Select(u => u.UserID).ToList();
            PresentUserNameArray = presentUsers.Select(u => u.UserName).ToList();
            Version = CURRENT_VERSION;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}
