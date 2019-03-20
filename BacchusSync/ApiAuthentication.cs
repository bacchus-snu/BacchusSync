using System.Text;
using System.Net;

namespace pGina.Plugin.BacchusSync
{
    class ApiAuthentication
    {
        public static bool Authenticate(string username, string password)
        {
            var request = WebRequest.Create(Settings.AuthenticationServerAddress);

            string filteredUsername = username.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string filteredPassword = password.Replace("\\", "\\\\").Replace("\"", "\\\"");

            string payload = string.Format("{{\"username\": \"{0}\", \"password\": \"{1}\"}}", filteredUsername, filteredPassword);

            var payloadBytes = Encoding.UTF8.GetBytes(payload.ToCharArray());

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentLength = payloadBytes.Length;
            request.ContentType = "application/json";

            using (var stream = request.GetRequestStream())
            {
                stream.Write(payloadBytes, 0, payloadBytes.Length);
                stream.Close();
            }

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
