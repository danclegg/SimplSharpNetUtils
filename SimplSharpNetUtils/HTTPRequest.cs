/* HTTPRequest.cs
 *
 * Handle a simple HTTPRequest.  This is a bridge between SimplSharp and Simpl+
 *
 * Note that a username/password can be supplied.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimplSharpNetUtils
{
    public class HTTPRequest
    {
        private const string TRUE = "true";
        private const string FALSE = "false";

        public String URL;
        public int Port = 80; // can be overridden

        public int numResponseAttributes = 0;

        public int errorExists = 0;
        public string errorMessage = string.Empty;

        //public String User = "";
        //public String Password = "";

        public delegate void errorHandler(SimplSharpString errMsg);
        public errorHandler OnError { get; set; }

        public delegate void responseHandler(SimplSharpString errMsg);
        public responseHandler OnResponse { get; set; }

        public delegate void HTTPClientStringCallback(SimplSharpString userobj, HTTP_CALLBACK_ERROR error);
        public HTTPClientStringCallback httpCallback { get; set; }

        public int Post(string body, string headers)
        {
            HttpClient client = new HttpClient();
            HttpClientRequest req = new HttpClientRequest();
            HttpClientResponse resp;


            // Headers are a semicolon delimited list of header key-value pairs, i.e.
            // "Accept: application/json; Content-Type: application/json;"

            if (headers != null && headers != String.Empty)
            {
                string[] headerArr = headers.Split(';');
                foreach (string h in headerArr)
                {
                    HttpHeader head = new HttpHeader(h);
                    req.Header.AddHeader(head);
                }
            }

            try
            {
                // Set client & request parameters
                client.KeepAlive = false;
                client.Port = Port;

                req.Url.Parse(URL);
                req.RequestType = RequestType.Post;
                req.ContentString = body;

                // Make the request
                resp = client.Dispatch(req);

                // Cleanup leading/trailling spaces & quotes from response and return response
                if (OnResponse != null)
                {
                    string contentString = resp.ContentString;
                    contentString = contentString.Trim();
                    if (contentString.StartsWith("\""))
                    {
                        contentString = contentString.Remove(0, 1);
                    }
                    if (contentString.EndsWith("\""))
                    {
                        contentString = contentString.Remove(contentString.LastIndexOf("\""), 1);
                    }

                    OnResponse(new SimplSharpString(contentString));
                }
            }
            catch (Exception e)
            {
                // Print any errors to the Crestron Console
                if (OnError != null)
                {
                    this.errorExists = 1;
                    this.errorMessage = string.Concat(e.ToString(), e.InnerException.ToString());
                    CrestronConsole.PrintLine("HTTP Connection Error - Cannot connect to " + req.Url);
                    OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));
                }

                return -1;
            }


            return 0;
        }
        public int Get(string body, string headers)
        {
            HttpClient client = new HttpClient();
            HttpClientRequest req = new HttpClientRequest();
            HttpClientResponse resp;

            if (headers != null && headers != String.Empty)
            {
                string[] headerArr = headers.Split(';');
                foreach (string h in headerArr)
                {
                    HttpHeader head = new HttpHeader(h);
                    req.Header.AddHeader(head);
                }
            }

            try
            {
                // Set client & request parameters
                client.KeepAlive = false;
                client.Port = Port;

                req.Url.Parse(URL);
                req.RequestType = RequestType.Get;

                // Send request
                resp = client.Dispatch(req);

                // Cleanup leading/trailling spaces & quotes from response and return response
                if (OnResponse != null)
                {
                    string contentString = resp.ContentString;
                    contentString = contentString.Trim();
                    if (contentString.StartsWith("\""))
                    {
                        contentString = contentString.Remove(0, 1);
                    }

                    if (contentString.EndsWith("\""))
                    {
                        contentString = contentString.Remove(contentString.LastIndexOf("\""), 1);
                    }

                    OnResponse(new SimplSharpString(contentString));
                }
            }
            catch (Exception e)
            {
                // Print any errors to the Crestron Console
                if (OnError != null)
                {
                    this.errorExists = 1;
                    this.errorMessage = string.Concat(e.ToString(), e.InnerException.ToString());
                    CrestronConsole.PrintLine("HTTP Connection Error - Cannot connect to " + req.Url);
                    OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));
                }

                return -1;
            }


            return 0;
        }

        // The SendCommand method is purely a convenience for Sony FWD/XBR Flat-panel displays
        // It is a customized version of the POST method

        public int SendCommand(string baseURL, string resource, string cmd, string psk)
        {
            HttpClient client = new HttpClient();
            HttpClientRequest req = new HttpClientRequest();
            HttpClientResponse resp;
            string reqUrl = "";

#if DEBUG
            CrestronConsole.PrintLine("Got to SendCommand");
            CrestronConsole.PrintLine("SendCommand method - baseURL: " + baseURL);
            CrestronConsole.PrintLine("SendCommand method - resource: " + resource);
            CrestronConsole.PrintLine("SendCommand method - psk: " + psk);
            CrestronConsole.PrintLine("SendCommand method - cmd: " + cmd);
#endif

            try
            {
                if (baseURL.EndsWith("/"))
                {
                    reqUrl = string.Concat(baseURL, resource);
                }
                else
                {
                    reqUrl = string.Concat(baseURL, "/", resource);
                }
#if DEBUG
                CrestronConsole.PrintLine("SendCommand url: " + reqUrl);
#endif
                req.Header.AddHeader(new HttpHeader("X-Auth-PSK: " + psk));

#if DEBUG
                foreach (HttpHeader h in req.Header)
                {
                    CrestronConsole.PrintLine("SendCommand Header: " + h);
                }
#endif

                string body = cmd;

#if DEBUG
                CrestronConsole.PrintLine("SendCommand method - parsed body: " + body);
#endif

                client.KeepAlive = false;
                client.Port = Port;
                req.Url.Parse(reqUrl);
                req.RequestType = RequestType.Post;

                req.ContentString = body;
                resp = client.Dispatch(req);

#if DEBUG
                CrestronConsole.PrintLine("SendCommand response code: " + resp.Code);
                CrestronConsole.PrintLine("SendCommand response: " + resp.ContentString);
#endif

                if (OnResponse != null)
                {
                    string contentString = resp.ContentString;
                    contentString = contentString.Trim();
                    if (contentString.StartsWith("\""))
                    {
                        contentString = contentString.Remove(0, 1);
                    }

                    if (contentString.EndsWith("\""))
                    {
                        contentString = contentString.Remove(contentString.LastIndexOf("\""), 1);
                    }

                    OnResponse(new SimplSharpString(contentString));
                }
            }

            catch (Exception ex)
            {
                if (OnError != null)
                {
                    this.errorExists = 1;
                    this.errorMessage = string.Concat(ex.ToString(), ex.InnerException.ToString());

                    OnError(new SimplSharpString(ex.ToString() + "\n\r" + ex.StackTrace));
                }

                CrestronConsole.PrintLine("Error in SimplSharpNetUtils.SendCommand: " + ex.StackTrace);
                return -1;
            }
            return 0;
        }
    }
}
