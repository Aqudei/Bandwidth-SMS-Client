using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using Bandwidth_SMS_Client.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace Bandwidth_SMS_Client
{
    public class MyTokenAuthenticator : IAuthenticator
    {
        private readonly string _token;

        public MyTokenAuthenticator(string token)
        {
            _token = token;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            request.AddHeader("Authorization", $"Token {_token}");
        }
    }

    public class TokenResponse
    {
        public string Token { get; set; }
    }

    public class SMSClient
    {
        private string _token;
        //public RestClient RestClient = new RestClient("http://127.0.0.1:8000") { UnsafeAuthenticatedConnectionSharing = true };
        public RestClient RestClient = new RestClient("https://smstrifecta.ga");

        public SMSClient()
        {

        }

        public void Login(string username, string password)
        {
            var request = new RestRequest("/api-token-auth/", Method.POST);
            request.AddParameter("username", username);
            request.AddParameter("password", password);
            var response = RestClient.Execute<TokenResponse>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException();
            }

            _token = response.Data.Token;
            RestClient.Authenticator = new MyTokenAuthenticator(_token);
        }

        private void AttachTokenAuth(RestRequest request)
        {
            request.AddHeader("Authorization", $"Token {_token}");
        }

        public IEnumerable<MessageThread> GetThreads()
        {
            var request = new RestRequest("/sms/messages", Method.GET, DataFormat.Json);
            var response = RestClient.Execute<IEnumerable<MessageItem>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException();
            }

            var query = from a in response.Data
                        group a by a.GroupingPhone into g
                        select new MessageThread
                        {
                            Recipient = g.Key,
                            Avatar = "",
                            MessageItems = g.AsEnumerable()
                        };
            return query.AsEnumerable();
        }

        public void SendMessage(string recipient, string body)
        {
            var request = new RestRequest("/sms/messages/", Method.POST, DataFormat.Json);
            request.AddParameter("body", body);
            request.AddParameter("to", recipient);
            var response = RestClient.Execute<MessageItem>(request);
            if(response.StatusCode!=HttpStatusCode.Created)
            {
                throw new HttpRequestException();
            }
        }
    }
}
