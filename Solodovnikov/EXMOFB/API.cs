using EXMOFB;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace EXMOFB
{
    public class API
    {
        private static readonly HttpClient client = new HttpClient();
        public string api_key = Form2.ApiKey;
        public string api_sec = Form2.ApiSec; 

        public string ToQueryString(IDictionary<string, string> dic)
        {
            var array = (from key in dic.Keys
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(dic[key])))
                .ToArray();
            return string.Join("&", array);
        }

        public string ApiQuery(string apiName, IDictionary<string, string> req)
        {
            using (var wb = new WebClient())
            {
                req.Add("nonce", Convert.ToString(GetTimestamp()));
                var message = ToQueryString(req);

                var sign = Sign(api_sec, message);

                wb.Headers.Add("Sign", sign);
                wb.Headers.Add("Key", api_key);

                var data = ToNameValueCollection(req);
                try
                {
                    var response = wb.UploadValues(string.Format("http://api.exmo.com/v1/{0}", apiName), "POST", data);
                    return Encoding.UTF8.GetString(response);
                }
                catch { return string.Empty; }  
            }
        }

       public  NameValueCollection ToNameValueCollection<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            var nameValueCollection = new NameValueCollection();

            foreach (var kvp in dict)
            {
                string value = string.Empty;
                if (kvp.Value != null)
                    value = kvp.Value.ToString();

                nameValueCollection.Add(kvp.Key.ToString(), value);
            }

            return nameValueCollection;
        }

        public static long GetTimestamp()
        {
            var d = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
            return (long)d;
        }

        public static string Sign(string key, string message)
        {
            using (HMACSHA512 hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                byte[] b = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return ByteToString(b);
            }
        }

        public static string ByteToString(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2");
            }
            return (sbinary).ToLowerInvariant();
        }

    }
}
