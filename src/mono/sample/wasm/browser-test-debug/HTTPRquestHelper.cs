using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.JSInterop;
using Newtonsoft.Json.Serialization;
namespace Sample
{
    public class HTTPResponseNotValidException : Exception
    {
        public HTTPResponseNotValidException(string message) : base(message) { }
    }
    public class OutOfMemoryException : Exception
    {
        public OutOfMemoryException(string message) : base(message) { }
        public OutOfMemoryException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class HTTPRquestHelper
    {
        public static JsonSerializer Serializer { get; } = JsonSerializer.CreateDefault(GetJsonSerializerSettings());
        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Culture = System.Globalization.CultureInfo.InvariantCulture,
                Formatting = Formatting.None,
                //Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //DefaultValueHandling = DefaultValueHandling.Ignore,
                //NullValueHandling = NullValueHandling.Ignore
                Converters = new[] { new JsonConverterBoundingBox() }
            };
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        private static HttpRequestMessage prepareGetRequest(Uri requestURI, Sample.HTTPServerConfig serverConfig)
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = requestURI,
                Method = HttpMethod.Get
            };
            if (serverConfig.authHeaderValue != null)
            {
                request.Headers.Authorization = serverConfig.authHeaderValue;
            }
#if __EMSCRIPTEN__
         request.Options.Set(new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse"), false);
#endif
            System.Console.WriteLine($"Prepared GET request for {requestURI}. Authorization = {serverConfig.authHeaderValue}");
            return request;
        }

        private static bool validateResponse(HttpResponseMessage response, string contentMediaType, Uri requestURI, out string errorMsg)
        {
            errorMsg = string.Empty;

            // throw HttpRequestException if !IsSuccessStatusCode
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType;
            if (response.Content.Headers.ContentLength == 0 || contentType == null)
            {
                errorMsg = $"Invalid (null) response from server for {requestURI}.";
                if (response.Content.Headers.ContentLength == 0)
                    errorMsg += " Content length is zero.";
                if (contentType == null)
                    errorMsg += " Content type is null.";
                // stop
                Console.WriteLine(errorMsg);
                Console.WriteLine("Will break");
                System.Diagnostics.Debug.Assert(false, "Intentional exit at this line");

                Console.WriteLine($"Delay starting at {DateTime.Now}");
                // Pause to allow debug attach
                var milliseconds = 30000;
                Thread.Sleep(milliseconds);
            }
            else if (contentType.MediaType == contentMediaType)
            {
                Console.WriteLine($"Validated response from server for {requestURI}.");
                return true;
            }
            else
                errorMsg = $"Invalid content type ({contentType.MediaType}) from server. Expected content type is '{contentMediaType}' for {requestURI}";

            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        internal static async Task<T> getPacketInner<T>(string requestURI_s, Sample.HTTPServerConfigs serverConfigs, CancellationToken cancellationToken
           , string contentMediaType
           , bool piggybackedResponse
           , System.Func<HttpContent, Task<T>> responseParser
           ) where T : class
        {
            T result = null;
            Uri requestURI = new Uri(requestURI_s);
            
            var serverConfig = serverConfigs.getOrCreateConfig("http://localhost:37703"); //serverConfigs.getConfigFromURI(requestURI);
            try
            {
                Console.WriteLine($"getPacketInner - Sending GET request to {requestURI}");
                Console.WriteLine($"ServerConfig: hostname {serverConfig.HostName}");
                HttpClient httpClient = HttpClientFactory.Instance.GetOrCreateHttpClient(requestURI, serverConfig);
                if (httpClient == null)
                {
                    Console.WriteLine("HttpClient is null!");
                    throw new InvalidOperationException("HttpClient instance is null.");
                }
                Console.WriteLine("HttpClient obtained.");
                httpClient.Timeout = TimeSpan.FromSeconds(1);
                HttpRequestMessage request = prepareGetRequest(requestURI, serverConfig);
                Console.WriteLine($"Prepared Get request {request.RequestUri}");

                HttpCompletionOption httpCompletionOption = piggybackedResponse ?
                   HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead;
                HttpResponseMessage response;
                try
                {
                    response = await httpClient.SendAsync(request, httpCompletionOption, cancellationToken);
                    Console.WriteLine("Got response! Will validate:");
                    // ... rest of your code
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during SendAsync: {ex}");
                    throw;
                }
                
                if (validateResponse(response, contentMediaType, requestURI, out string errorMsg))
                {
                    result = await responseParser(response.Content);
                }
                else
                {
                    Console.WriteLine("Got error: " + errorMsg);
#if DEBUG
			   string responsePayload = await response.Content.ReadAsStringAsync();
               if (!string.IsNullOrEmpty(responsePayload))
                  errorMsg += $" payload: {responsePayload}";
#endif
                    // added requestURI to message payload
                    errorMsg = $"Will throw Unexpected error: Request URL: {requestURI} Error: {errorMsg}";
                    Console.WriteLine(errorMsg);
                    throw new HTTPResponseNotValidException(errorMsg);
                }
            }
            catch (HTTPResponseNotValidException e)
            {
                string msg = $"Caught Unexpected error: Request URL: {requestURI} Error: {e.Message}";
                Console.WriteLine(msg);
                Trace.TraceError(msg, e);
                throw;
            }
#if __NX__
         //not supported in .NET48: The type or namespace name 'JavaScript' does not exist in the namespace 'System.Runtime.InteropServices'
         catch (System.Runtime.InteropServices.JavaScript.JSException e)
         {
            string message = e.Message;
            bool isOutOfMemory = message.Contains("Overflow:");

            // added requestURI to message payload
            string msg = $"Unexpected error: Request URL: {requestURI} Error: {e.Message}";
            //logger.Error(msg, e);

            if (isOutOfMemory)
               throw new OutOfMemoryException(msg, e);
            else
               throw new Exception(msg, e);
         }
#endif
            catch (Exception e)
            {
                // added requestURI to message payload
                string msg = $"Unexpected error: Request URL: {requestURI} Error: {e.Message}";
                //logger.Error(msg, e);
                throw new Exception(msg, e);
            }
            return result;
        }



        //////////////////////////////////////////////////////////////////////////////////////////
        internal static async Task<string> getJsonAsync(string requestURI_s, Sample.HTTPServerConfigs serverConfigs, bool piggybackedResponse, CancellationToken cancellationToken)
        {
            Func<HttpContent, Task<string>> responseParser = async (content) =>
            {
#if __NX__
            return await content.ReadAsStringAsync(cancellationToken);
#else
                return await content.ReadAsStringAsync();
#endif
            };
            return await getPacketInner(requestURI_s, serverConfigs, cancellationToken, "application/json", piggybackedResponse, responseParser);
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        internal static async Task<T> getJsonAsync<T>(string requestURI_s, Sample.HTTPServerConfigs serverConfigs, bool piggybackedResponse, CancellationToken cancellationToken) where T : class
        {
            Func<HttpContent, Task<T>> responseParser = async (content) =>
            {
#if __NX__
            using (Stream stream = await content.ReadAsStreamAsync(cancellationToken))
#else
                using (Stream stream = await content.ReadAsStreamAsync())
#endif
                {
                    using (var v_streamReader = new StreamReader(stream, Encoding.UTF8))
                    using (var v_jsonTextReader = new JsonTextReader(v_streamReader))
                    {
                        return Serializer.Deserialize<T>(v_jsonTextReader);
                    }
                }
            };

            return await getPacketInner(requestURI_s, serverConfigs, cancellationToken, "application/json", piggybackedResponse, responseParser);
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        internal static async Task<byte[]> getBufferAsync(string requestURI_s, Sample.HTTPServerConfigs serverConfigs, bool piggybackedResponse, CancellationToken cancellationToken)
        {
            Func<HttpContent, Task<byte[]>> responseParser = async (content) =>
            {
#if __NX__
            using (Stream stream = await content.ReadAsStreamAsync(cancellationToken))
#else
                using (Stream stream = await content.ReadAsStreamAsync())
#endif
                {
                    using (var v_binReader = new MemoryStream())
                    {
                        int defaultBufferSize = 1024 * 1024; // 81920; // try changing buffer size to see if it affects corruption issue
                        await stream.CopyToAsync(v_binReader, defaultBufferSize, cancellationToken).ConfigureAwait(false);
                        return v_binReader.ToArray();
                    }
                }
            };

            return await getPacketInner(requestURI_s, serverConfigs, cancellationToken, "application/octet-stream", piggybackedResponse, responseParser);
        }


    } // class HTTPRquestHelper

}
