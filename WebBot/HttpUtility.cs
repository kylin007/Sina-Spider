using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Skay.WebBot
{
    public class HttpUtility
    {
        private CookieContainer _cookie;

        public HttpUtility()
        {
            _cookie = new CookieContainer();
        }
        public HttpWebRequest ResquestInit(string url, string contentType = "text/html;charset=utf-8",
            string UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.1916.114 Safari/537.36",
            string Accept="text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
         )
        {
            Uri target = new Uri(url);
            HttpWebRequest resquest = (HttpWebRequest)WebRequest.Create(target);
            resquest.UserAgent = UserAgent;
            resquest.Accept = Accept;
            resquest.AllowAutoRedirect = true;
            resquest.KeepAlive = true;
            resquest.ReadWriteTimeout = 120000;
            resquest.ContentType = contentType;
            resquest.Referer = url;

            return resquest;
        }

        public string GetResponseString(HttpWebRequest request, CookieContainer cookie,string encoding="utf-8")
        {
            string responseMsg = string.Empty;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Cookies = cookie.GetCookies(request.RequestUri);
                Stream responseStream = response.GetResponseStream();

                using (StreamReader sr = new StreamReader(responseStream, System.Text.Encoding.GetEncoding(encoding)))
                {
                    responseMsg = sr.ReadToEnd();
                }

                response.Close();
            }
            catch (Exception ex) { throw ex; }
            return responseMsg;
        }

        public string GetHtmlText(string url
            ,string encoding="utf-8"
            ,string contentType = "text/html;charset=utf-8"
            , string UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.1916.114 Safari/537.36"
            ,string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
            )
        {
            HttpWebRequest requestSearch = ResquestInit(url,contentType,UserAgent,Accept);
            requestSearch.CookieContainer = _cookie;
            string responseMsg = GetResponseString(requestSearch, _cookie,encoding);

            return responseMsg;
        }
    }
}
