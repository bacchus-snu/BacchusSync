using System.Text;
using System.Net;
using Newtonsoft.Json.Linq;

namespace pGina.Plugin.BacchusSync
{
    class ApiAuthentication
    {
        public static bool Authenticate(string username, string password)
        {
            var request = WebRequest.Create(Settings.AuthenticationServerAddress);

            JObject payload = new JObject();
            payload.Add("username", username);
            payload.Add("password", password);
            var payloadBytes = Encoding.UTF8.GetBytes(payload.ToString().ToCharArray());

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentLength = payloadBytes.Length;

            var stream = request.GetRequestStream();

            stream.Write(payloadBytes, 0, payloadBytes.Length);
            stream.Close();

            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
