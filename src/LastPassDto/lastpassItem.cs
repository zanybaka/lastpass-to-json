// https://json2csharp.com/json-to-csharp was used to convert JSON to c#

namespace lastpass_to_json
{
    public class LastPassItem
    {
        public string url;
        public string username;
        public string password;
        public string totp;
        public string extra;
        public string name;
        public string grouping;
        public string fav;
    }
}