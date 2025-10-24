//using studioft.scenemanager.nodes.identity;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Sample
{
   //////////////////////////////////////////////////////////////////////////////////////////
   public class HTTPServerConfig
   {
      public bool enableCertificateValidation = false;
      internal string HostName { get; set; } = string.Empty;
      internal AuthenticationHeaderValue authHeaderValue = null;
      public HTTPServerConfig(bool enableCertificateValidation, string authToken)
      {
         this.enableCertificateValidation = enableCertificateValidation;
         //if(!string.IsNullOrEmpty(authToken) )
         //TODO listen to all server token updates until server level token mapping is implemented
         {
            //IdentityManager.IdentityChanged += IdentityManager_IdentityChanged;
         }
         setAuthToken(authToken);
      }

      private void IdentityManager_IdentityChanged(object sender, EventArgs e)
      {
         //IdentityEventArgs identityArgs = (IdentityEventArgs)e;
         ////TODO Skipping server level auth token setting
         ////if (identityArgs.HostName.Equals(HostName, StringComparison.InvariantCultureIgnoreCase))
         //{
         //   setAuthToken(identityArgs.Token);
         //}
      }

      private string _authToken = "";
      private object _authToken_lock = new object();

      public string getAuthToken() { return _authToken; }
      public void setAuthToken(string token)
      {
         lock (_authToken_lock)
         {
            _authToken = token;
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
               authHeaderValue = new AuthenticationHeaderValue("Bearer", _authToken);
            }
            else
            {
               authHeaderValue = null;
            }
         }
      }

      public override bool Equals(object obj)
      {
         return obj is HTTPServerConfig config &&
                enableCertificateValidation == config.enableCertificateValidation &&
                HostName == config.HostName &&
                EqualityComparer<AuthenticationHeaderValue>.Default.Equals(authHeaderValue, config.authHeaderValue) &&
                _authToken == config._authToken;
      }

      public override int GetHashCode()
      {
         int hashCode = 1892367946;
         hashCode = hashCode * -1521134295 + enableCertificateValidation.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HostName);
         hashCode = hashCode * -1521134295 + EqualityComparer<AuthenticationHeaderValue>.Default.GetHashCode(authHeaderValue);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_authToken);
         return hashCode;
      }
   }

   //////////////////////////////////////////////////////////////////////////////////////////
   public class HTTPServerConfigs
   {
      private readonly Dictionary<string, HTTPServerConfig> _configs;
      private readonly HTTPServerConfig _defaultConfig;

      public HTTPServerConfigs()
      {
         this._configs = new Dictionary<string, HTTPServerConfig>(StringComparer.InvariantCultureIgnoreCase);
         this._defaultConfig = new HTTPServerConfig(false, null);
      }

      public HTTPServerConfig getConfig(string hostname)
      {
         if (_configs.TryGetValue(hostname, out HTTPServerConfig config))
            return config;
         else
            return _defaultConfig;
      }

      public HTTPServerConfig getOrCreateConfig(string hostname, string token)
      {
         if (_configs.TryGetValue(hostname, out HTTPServerConfig config))
         {
            config.setAuthToken(token);
            return config;
         }

         config = new HTTPServerConfig(false, token) { HostName = hostname };
         _configs[hostname] = config;
         return config;
      }
      public HTTPServerConfig getOrCreateConfig(string hostname)
      {
         return getOrCreateConfig(hostname, null);
      }

      public HTTPServerConfig getConfigFromURI(Uri uri)
      {
         return getConfig(uri.Host);
      }

      public HTTPServerConfig getOrCreateConfigFromUri(Uri uri, string token)
      {
         return getOrCreateConfig(uri.Host, token);
      }
   }

}
