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
        public String URL;
        public int Port = 80; // can be overridden

        public int numResponseAttributes = 0;

        //public String User = "";
        //public String Password = "";

        public delegate void errorHandler(SimplSharpString errMsg);
        public errorHandler OnError { get; set; }

        public delegate void responseHandler(SimplSharpString errMsg);
        public responseHandler OnResponse { get; set; }

        public delegate void HTTPClientStringCallback(SimplSharpString userobj, HTTP_CALLBACK_ERROR error);
        public HTTPClientStringCallback httpCallback { get; set; }

        public string jsonParse(string input)
        {

            CrestronConsole.PrintLine("jsonParse parsing: " + input);
            string parsedBody = "";
            Int32 index = 0;

            string respString = JsonConvert.SerializeObject(input);
            if (respString.IndexOf("error") > 0)
            {
                index = respString.IndexOf('[', respString.IndexOf("error"));

                string tmp = "";
                while (index < respString.Length)
                {
                    Char current = respString[index];

                    if (current != ']')
                    {
                        index++;
                        tmp += current;
                        continue;
                    }
                    else
                    {
                        // Push last result
                        index++;
                        return tmp;
                    }
                }
            }
            else if (respString.IndexOf("result") > 0)
            {
                index = respString.IndexOf('[', respString.IndexOf("result"));

                string tmp = "";
                while (index < respString.Length)
                {
                    Char current = respString[index];

                    if (current != ']')
                    {
                        index++;
                        tmp += current;
                        continue;
                    }
                    else
                    {
                        // Push last result
                        index++;
                        return tmp;
                    }
                }
            }

            CrestronConsole.PrintLine("jsonParse - parsedBody: " + parsedBody);
                
            return parsedBody;
        }

        private static int TryParse(string str)
        {
            int result;

            try
            {
                result = Int32.Parse(str);
                return result;
            }
            catch(Exception ex){
                CrestronConsole.PrintLine("Error in Sony IP Control Command: " + ex.StackTrace);
                return -1;
            }
        }

        public string getAttributeValue(List<string> queryPath, string bodyToQuery)
        {
            JObject jsonObject = JObject.Parse(bodyToQuery);

            if ( queryPath.Count > 1)
            {
                string newBody = "";
                int result = TryParse(queryPath.First());
                if (result >= 0)
                {
                    newBody = (string)jsonObject[queryPath.First()[result]];
                }
                else
                {
                    newBody = (string)jsonObject[queryPath.First()];
                }
                queryPath.RemoveAt(0);
                getAttributeValue(queryPath, newBody);
            }
            string attribute = (string)jsonObject[queryPath.First()];
            return attribute;
        }

        public string getAttributeValue(string queryPath, string bodyToQuery)
        {
            JObject jsonObject = JObject.Parse(bodyToQuery);
            List<string> queryArr = new List<string>();
            string[] tmpArr = queryPath.Split('.');
            foreach (string s in tmpArr)
            {
                queryArr.Add(s);
            }

            if ( queryArr.Count > 1)
            {
                string newBody = "";
                int result = TryParse(queryArr.First());
                if (result >= 0)
                {
                    newBody = (string)jsonObject[queryArr.First()[result]];
                }
                else
                {
                    newBody = (string)jsonObject[queryArr.First()];
                }
                queryArr.RemoveAt(0);
                getAttributeValue(queryArr, newBody);
            }
            string attribute = (string)jsonObject[queryArr.First()];
            return attribute;
        }

        // Headers are a semicolon delimited list of header key-value pairs, i.e.
        // "Accept: application/json; Content-Type: application/json;"

        public int Post(string body, string headers)
        {
            HttpClient client = new HttpClient();
            HttpClientRequest req = new HttpClientRequest();
            HttpClientResponse resp;

            if (headers != null && headers != String.Empty)
            {
                string[] headerArr = headers.Split(';');
                foreach(string h in headerArr){
                    HttpHeader head = new HttpHeader(h);
                    req.Header.AddHeader(head);
                }
            }

            try
            {
                /*RequestType rType;

                switch (type.ToLower())
                {
                    case "get":
                        rType = RequestType.Get;
                        break;
                    case "post":
                        rType = RequestType.Post;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid request type");
                }*/

                client.KeepAlive = false;
                client.Port = Port;
                
                /*if (User.Length > 0)
                {
                    client.UserName = User;
                    client.Password = Password;
                }
                else
                {
                    client.UserName = "";
                    client.Password = "";
                }*/
                
                req.Url.Parse(URL);
                
                CrestronConsole.PrintLine(req.Url.ToString());
                CrestronConsole.PrintLine(body);
                CrestronConsole.PrintLine(req.RequestType.ToString());
                req.RequestType = RequestType.Post;
                
                req.ContentString = body;
                resp = client.Dispatch(req);

                if (OnResponse != null)
                    OnResponse(new SimplSharpString(resp.ContentString));
            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));

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

                client.KeepAlive = false;
                client.Port = Port;

                req.Url.Parse(URL);

                CrestronConsole.PrintLine(req.Url.ToString());
                CrestronConsole.PrintLine(body);
                CrestronConsole.PrintLine(req.RequestType.ToString());
                
                req.RequestType = RequestType.Get;

                //req.ContentString = body;
                resp = client.Dispatch(req);

                if (OnResponse != null)
                    OnResponse(new SimplSharpString(resp.ContentString));
            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));

                return -1;
            }


            return 0;
        }

        public int SendCommand(string baseURL,string resource, string cmd, string psk)
        {
            HttpClient client = new HttpClient();
            HttpClientRequest req = new HttpClientRequest();
            HttpClientResponse resp;
            string reqUrl = "";
            //CrestronConsole.PrintLine("Got to SendCommand");
            CrestronConsole.PrintLine("SendCommand method - baseURL: " + baseURL);
            CrestronConsole.PrintLine("SendCommand method - resource: " + resource);
            CrestronConsole.PrintLine("SendCommand method - psk: " + psk);
            CrestronConsole.PrintLine("SendCommand method - cmd: " + cmd);

            if (baseURL.EndsWith("/"))
            {
                reqUrl = string.Concat(baseURL, resource);
            }
            else
            {
                reqUrl = string.Concat(baseURL, "/", resource);
            }

            req.Header.AddHeader(new HttpHeader("X-Auth-PSK: " + psk));
            foreach (HttpHeader h in req.Header)
            {
                CrestronConsole.PrintLine("SendCommand Header: " + h);
            }
            string body = jsonParse(cmd);
            CrestronConsole.PrintLine("SendCommand method - parsed body: " + body);
            try {
                client.KeepAlive = false;
                client.Port = Port;
                req.Url.Parse(reqUrl);
                req.RequestType = RequestType.Post;

                req.ContentString = body;
                resp = client.Dispatch(req);
                CrestronConsole.PrintLine("SendCommand response code: " + resp.Code);

                if (OnResponse != null)
                    OnResponse(new SimplSharpString(resp.ContentString));

                return 1;
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Error in Sony IP Control Command: " + ex.StackTrace);
            return 0;
            }
        }
        //public int SonyIPControl_SendCommand(string cmd, string psk)
        //{
        //    HttpClient client = new HttpClient();
        //    HttpClientRequest req = new HttpClientRequest();
        //    HttpClientResponse resp;

        //    req.Header.AddHeader(new HttpHeader("Content-Type: text/xml; charset=UTF-8"));
        //    req.Header.AddHeader(new HttpHeader("SOAPACTION: \"urn:schemas-sony-com:service:IRCC:1#X_SendIRCC\""));
        //    req.Header.AddHeader(new HttpHeader("X-Auth-PSK: " + psk));

        //    string body = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
        //        "<s:Body><u:X_SendIRCC xmlns:u=\"urn:schemas-sony-com:service:IRCC:1\"><IRCCCode>" +
        //        cmd +
        //        "</IRCCCode></u:X_SendIRCC></s:Body></s:Envelope>";

        //    try
        //    {
        //        client.KeepAlive = false;
        //        client.Port = Port;
        //        req.Url.Parse(URL);
        //        req.RequestType = RequestType.Post;

        //        req.ContentString = body;
        //        resp = client.Dispatch(req);

        //        if (OnResponse != null)
        //            OnResponse(new SimplSharpString(resp.ContentString));
        //    }
        //    catch (Exception e)
        //    {
        //        if (OnError != null)
        //            OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));

        //        return -1;
        //    }


        //    return 0;
        //}
    }
}