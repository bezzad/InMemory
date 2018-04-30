using System.Web;

namespace InMemory
{
    public static class Extensions
    {
        public static string ClientIpAddress(this HttpContext context)
        {
            if (context == null) // for integration test
                return null;

            var ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                var addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    ipAddress = addresses[0];
                    return ipAddress;
                }
            }

            ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];
            return ipAddress;
        }
    }
}