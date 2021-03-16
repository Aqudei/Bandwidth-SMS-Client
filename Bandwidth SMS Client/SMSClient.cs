using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Intrinsics.X86;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using Newtonsoft.Json;
using Prism.Events;
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
        private readonly IEventAggregator _eventAggregator;

        private string _token;
        //public RestClient RestClient = new RestClient("http://127.0.0.1:8000") { UnsafeAuthenticatedConnectionSharing = true };
        public RestClient RestClient = new RestClient("https://smstrifecta.ga");
        private readonly BackgroundWorker _worker;

        public SMSClient(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _worker = new BackgroundWorker();
            _worker.DoWork += _worker_DoWork;
            _worker.WorkerReportsProgress = true;
            _worker.RunWorkerAsync();
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1000);
                var request = new RestRequest("/sms/pushes", Method.GET, DataFormat.Json);
                var response = RestClient.Execute<IEnumerable<Push>>(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    continue;

                foreach (var push in response.Data)
                {
                    if (push.Name == "message-created")
                    {
                        var messageItem = JsonConvert.DeserializeObject<MessageItem>(push.Body);

                        var messageEventPayload = new MessageEventPayload
                        {
                            EventType = MessageEventPayload.MessageEventType.Created,
                            MessageItem = messageItem
                        };

                        _eventAggregator.GetEvent<MessageEvent>().Publish(messageEventPayload);
                    }
                }
            }
        }

        public event EventHandler<MessageEvent> MessageUpdate;

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
                            MessageItems = g.AsEnumerable().OrderBy(mi => mi.Message_Date)
                        };
            return query.AsEnumerable();
        }

        public void SendMessage(string recipient, string body)
        {
            var request = new RestRequest("/sms/messages/", Method.POST, DataFormat.Json);
            request.AddParameter("body", body);
            request.AddParameter("to", recipient);
            var response = RestClient.Execute<MessageItem>(request);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new HttpRequestException();
            }
        }

        public void DeleteMessage(in int messageId)
        {
            var request = new RestRequest($"/sms/message/{messageId}", Method.DELETE, DataFormat.Json);

            var response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new HttpRequestException();
            }
        }
    }


}
