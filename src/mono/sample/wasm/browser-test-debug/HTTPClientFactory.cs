using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sample
{

    internal interface IHttpClientFactory
    {
        HttpClient GetOrCreateHttpClient(Uri requestURI, HTTPServerConfig serverConfig);
    }


    //////////////////////////////////////////////////////////////////////////////////////////
    internal class HttpClientFactory : IHttpClientFactory
    {
        private static readonly Lazy<HttpClientFactory> _instance = new Lazy<HttpClientFactory>(() => new HttpClientFactory());
        private readonly ConcurrentDictionary<HttpHandlerDescriptor, HttpClient> httpClients = new ConcurrentDictionary<HttpHandlerDescriptor, HttpClient>();

        private HttpClientFactory() { }

        public static HttpClientFactory Instance => _instance.Value;

        public HttpClient GetOrCreateHttpClient(Uri requestURI, Sample.HTTPServerConfig serverConfig)
        {
            Console.WriteLine($"[HttpClientFactory] GetOrCreateHttpClient for {requestURI}, certValidation: {serverConfig.enableCertificateValidation}");
            HttpHandlerDescriptor handlerDescriptor = new HttpHandlerDescriptor(requestURI, serverConfig);
            Console.WriteLine($"[HttpClientFactory] HandlerDescriptor: baseAddress={handlerDescriptor.baseAddress}, isHTTPS={handlerDescriptor.isHTTPS}");
            return httpClients.GetOrAdd(handlerDescriptor, CreateNewHTTPClient);
        }



        //////////////////////////////////////////////////////////////////////////////////////////
        private class HttpHandlerDescriptor
        {
            internal readonly bool isHTTPS;
            internal readonly string baseAddress;
            internal readonly Sample.HTTPServerConfig serverConfig;

            public HttpHandlerDescriptor(Uri url, HTTPServerConfig serverConfig)
            {
                // url example https://localhost:7044/popmeshes/zgl462/zgl462.common.jpop

                this.serverConfig = serverConfig;
                this.baseAddress = url.GetLeftPart(UriPartial.Authority); // https://localhost:7044
                this.isHTTPS = baseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is HttpHandlerDescriptor descriptor &&
                       baseAddress == descriptor.baseAddress &&
                       EqualityComparer<HTTPServerConfig>.Default.Equals(serverConfig, descriptor.serverConfig);
            }

            public override int GetHashCode()
            {
                int hashCode = -1554200700;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(baseAddress);
                hashCode = hashCode * -1521134295 + EqualityComparer<HTTPServerConfig>.Default.GetHashCode(serverConfig);
                return hashCode;
            }

        } // class HttpHandlerDescriptor



        //////////////////////////////////////////////////////////////////////////////////////////
        private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>
           certificateValidationCallback = (msg, cert, chain, policyErrors) =>
           {
               //Policy errors covers major validations, chaining errors,remote certificate name etc
               if (policyErrors != SslPolicyErrors.None)
               {
                   Trace.TraceError("XND0325", $"Certificate error:{policyErrors.ToString()} for {msg.RequestUri}.");
                   return false;
               }

               /* Implement additional custom checks from this point on.
                * Validating an SSL certificate here means emulating the work that is normally done 
                * by a web browser on a specific URL before loading the given web page.
                * This means selecting and implementing specific checks from the multitude of possible checks.
                * 
                * below are some examples
                */

               if (cert == null)
               {
                   return false;
               }
               // Custom check: Ensure the certificate has not expired
               else if (DateTime.Now > cert.NotAfter)
               {
                   return false; // Certificate has expired
               }
               // Custom check: Ensure the certificate has a specific signature algorithm
               else if (cert.SignatureAlgorithm.FriendlyName != "sha256RSA")
               {
                   return false; // Certificate signature algorithm does not match
               }
               // Custom check: Ensure the certificate has a minimum key size
               else if (cert.PublicKey.Key.KeySize < 2048)
               {
                   return false; // Certificate key size is too small
               }
               // Custom check: Ensure the certificate is not revoked
               foreach (var status in chain.ChainStatus)
               {
                   if (status.Status == X509ChainStatusFlags.Revoked)
                   {
                       return false; // Certificate is revoked
                   }
               }

               return true;
           };


        //////////////////////////////////////////////////////////////////////////////////////////
        private HttpClient CreateNewHTTPClient(HttpHandlerDescriptor handlerDescriptor)
        {
            Console.WriteLine($"[HttpClientFactory] CreateNewHTTPClient for {handlerDescriptor.baseAddress}, certValidation: {handlerDescriptor.serverConfig.enableCertificateValidation}");
            HttpClientHandler handler = new HttpClientHandler();
            Console.WriteLine($"[HttpClientFactory] Created HttpClientHandler for {handlerDescriptor.baseAddress}");
            try
            {
                Console.WriteLine($"[HttpClientFactory] Creating new HttpClient");
                Console.Out.Flush();
                HttpClient client = new HttpClient(handler);
                //client.Timeout = TimeSpan.FromSeconds(1000);
                return client;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpClientFactory] Exception creating HttpClient: {ex}");
                throw;
            }
            Console.WriteLine($"[HttpClientFactory] returning"); ;
        }

    } // class HttpClientFactory

}
