using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Intrinsics.X86;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Resources;
using System.Windows.Threading;
using Bandwidth_SMS_Client.Events;
using Bandwidth_SMS_Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PhoneNumbers;
using Prism.Events;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;

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
        //public RestClient RestClient = new RestClient("http://127.0.0.1:8000");
        //public RestClient RestClient = new RestClient("https://smstrifecta.ga");
        public RestClient RestClient = new RestClient("http://sms.tripbx.com:8080") { PreAuthenticate = false };
        private readonly BackgroundWorker _worker;
        public event EventHandler<MessageEventPayload> MessageEvent;
        public event EventHandler<ConversationEventPayload> ConversationEvent;
        public event EventHandler<ContactUpdatedPayload> ContactUpdatedEvent;

        JsonSerializerSettings _serializerSetting = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new UnderscorePropertyNamesContractResolver()
        };

        public SMSClient()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += _worker_DoWork;
            _worker.WorkerReportsProgress = true;

            RestClient.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        }

        private DateTime ReadLastPull()
        {
            try
            {
                return Properties.Settings.Default.LastPull == new DateTime(1753, 1, 1) ? DateTime.UtcNow : Properties.Settings.Default.LastPull;
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var since = ReadLastPull();
            SaveLastPull(since);
            while (true)
            {
                Thread.Sleep(4000);
                var request = new RestRequest($"/sms/pushes/{since.ToString("s", CultureInfo.InvariantCulture)}", Method.GET, DataFormat.Json);
                var response = RestClient.Execute<IEnumerable<Push>>(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    since = DateTime.UtcNow;
                    SaveLastPull(since);
                    continue;
                }

                if (!response.Data.Any())
                {
                    since = DateTime.UtcNow;
                    SaveLastPull(since);
                    continue;
                }

                since = response.Data.Max(d => d.DateCreated).ToUniversalTime() + TimeSpan.FromSeconds(1);
                SaveLastPull(since);

                foreach (var push in response.Data)
                {
                    switch (push.Name)
                    {
                        case "message-created":
                            {
                                var messageItem = JsonConvert.DeserializeObject<MessageItem>(push.Body);

                                var messageEventPayload = new MessageEventPayload
                                {
                                    EventType = MessageEventPayload.MessageEventType.Created,
                                    MessageItem = messageItem
                                };

                                MessageEvent?.Invoke(this, messageEventPayload);
                                break;
                            }
                        case "message-deleted":
                            {
                                var messageItem = JsonConvert.DeserializeObject<MessageItem>(push.Body);

                                var messageEventPayload = new MessageEventPayload
                                {
                                    EventType = MessageEventPayload.MessageEventType.Deleted,
                                    MessageItem = messageItem
                                };

                                MessageEvent?.Invoke(this, messageEventPayload);
                                break;
                            }
                        case "conversation-created":
                            {
                                var conversation = JsonConvert.DeserializeObject<Conversation>(push.Body);

                                var conversationEventPayload = new ConversationEventPayload
                                {
                                    EventType = ConversationEventPayload.ConversationEventType.Created,
                                    ConversationItem = conversation
                                };

                                ConversationEvent?.Invoke(this, conversationEventPayload);
                                break;
                            }
                        case "conversation-updated":
                            {
                                var conversation = JsonConvert.DeserializeObject<Conversation>(push.Body);

                                var conversationEventPayload = new ConversationEventPayload
                                {
                                    EventType = ConversationEventPayload.ConversationEventType.Updated,
                                    ConversationItem = conversation
                                };

                                ConversationEvent?.Invoke(this, conversationEventPayload);
                                break;
                            }
                        case "contact-created":
                            {
                                var contact = JsonConvert.DeserializeObject<Contact>(push.Body);
                                var payload = new ContactUpdatedPayload
                                {
                                    Contact = contact,
                                    UpdateType = ContactUpdatedPayload.UpdateTypes.Created
                                };
                                ContactUpdatedEvent?.Invoke(this, payload);
                                break;
                            }
                        case "contact-updated":
                            {
                                var contact = JsonConvert.DeserializeObject<Contact>(push.Body);
                                var payload = new ContactUpdatedPayload
                                {
                                    Contact = contact,
                                    UpdateType = ContactUpdatedPayload.UpdateTypes.Updated
                                };
                                ContactUpdatedEvent?.Invoke(this, payload);
                                break;
                            }

                    }
                }
            }
        }

        private class UnderscorePropertyNamesContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.Replace("_", "").ToLower();
            }

            protected override string ResolveDictionaryKey(string dictionaryKey)
            {
                return dictionaryKey.Replace("_", "").ToLower();
            }
        }

        private static void SaveLastPull(DateTime since)
        {
            Properties.Settings.Default.LastPull = since;
            Properties.Settings.Default.Save();
        }

        public Task LoginAsync(string username, string password)
        {
            return Task.Run(() => Login(username, password));
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
            _worker.RunWorkerAsync();
        }

        private void AttachTokenAuth(RestRequest request)
        {
            request.AddHeader("Authorization", $"Token {_token}");
        }

        public IEnumerable<MessageThread> ListThreads()
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
                            MessageItems = g.AsEnumerable().OrderBy(mi => mi.MessageDate)
                        };
            return query.AsEnumerable();
        }

        public Task SendMessageAsync(string recipient, string body)
        {
            return Task.Run(() => SendMessage(recipient, body));
        }

        public void SendMessage(string recipient, string body)
        {
            var formattedPhone = FormatPhone(recipient);
            var request = new RestRequest("/sms/messages/", Method.POST, DataFormat.Json);
            request.AddParameter("Body", body);
            request.AddParameter("To", formattedPhone);
            var response = RestClient.Execute<MessageItem>(request);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }
        }

        private string FormatPhone(string recipient)
        {
            var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
            var parsed = phoneNumberUtil.Parse(recipient, "US");
            if (phoneNumberUtil.IsValidNumberForRegion(parsed, "US") && phoneNumberUtil.IsValidNumber(parsed))
            {
                return phoneNumberUtil.Format(parsed, PhoneNumberFormat.E164);
            }

            throw new Exception($"Phone number <{recipient}> is not valid ");
        }

        public void DeleteMessage(in int messageId)
        {
            var request = new RestRequest($"/sms/message/{messageId}", Method.DELETE, DataFormat.Json);

            var response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }
        }

        public Task<IEnumerable<Conversation>> ListConversationsAsync()
        {
            return Task.Run(ListConversations);
        }

        public IEnumerable<Conversation> ListConversations()
        {
            var request = new RestRequest($"/sms/conversations/", Method.GET, DataFormat.Json);
            var response = RestClient.Execute<IEnumerable<Conversation>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }

            return response.Data.Where(c => c.MessageCount > 0);
        }

        public Task<IEnumerable<MessageItem>> ListMessagesAsync(int conversationId)
        {
            return Task.Run(() => ListMessages(conversationId));
        }

        public IEnumerable<MessageItem> ListMessages(int conversationId)
        {
            var request = new RestRequest($"/sms/messages/{conversationId}", Method.GET, DataFormat.Json);
            var response = RestClient.Execute<IEnumerable<MessageItem>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"{response.ErrorMessage} / {response.ErrorException?.Message}");
            }

            return response.Data;
        }

        public Task<IEnumerable<Contact>> ListContactsAsync()
        {
            return Task.Run(ListContacts);
        }

        public IEnumerable<Contact> ListContacts()
        {
            var request = new RestRequest("/sms/contacts/", Method.GET, DataFormat.Json);
            var response = RestClient.Execute<IEnumerable<Contact>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"{response.ErrorMessage} / {response.ErrorException?.Message}");
            }

            return response.Data;
        }

        public Task<Contact> SaveContactAsync(Contact contact)
        {
            return Task.Run(() => SaveContact(contact));
        }

        public Contact SaveContact(Contact contact)
        {
            var resource = contact.Id == 0 ? $"/sms/contacts/" : $"/sms/contacts/{contact.Id}/";
            var method = contact.Id == 0 ? Method.POST : Method.PATCH;
            var request = new RestRequest(resource, method, DataFormat.Json);
            request.AddJsonBody(contact);
            var response = RestClient.Execute<Contact>(request);
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }

            return response.Data;
        }

        public Task DeleteContactAsync(Contact contact)
        {
            return Task.Run(() => DeleteContact(contact));
        }

        public void DeleteContact(Contact contact)
        {
            var resource = $"/sms/contacts/{contact.Id}/";
            var request = new RestRequest(resource, Method.DELETE, DataFormat.Json);
            var response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }
        }

        public Task UploadPhotoAsync(Contact contact, string avatar)
        {
            return Task.Run(() => UploadPhoto(contact, avatar));
        }

        public void UploadPhoto(Contact contact, string avatar)
        {
            var ext = Path.GetExtension(avatar);
            var resource = $"/sms/upload/{contact.Id}/{Path.GetFileName(avatar)}/";
            var request = new RestRequest(resource, Method.POST, DataFormat.Json);
            request.AddFile("Avatar", avatar, $"image/{ext.Trim(".".ToCharArray())}");
            var response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }
        }

        public async Task DeleteConversationAsync(Conversation conversation)
        {
            await Task.Run(() => DeleteConversation(conversation));
        }

        public void DeleteConversation(Conversation conversation)
        {
            var resource = $"/sms/conversations/{conversation.Id}/";
            var request = new RestRequest(resource, Method.DELETE);
            var response = RestClient.Execute(request);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                var message = $"{response.ErrorMessage} | {response.ErrorException?.Message} | {response.Content}";
                Debug.WriteLine(message);
                throw new HttpRequestException(message);
            }
        }
    }


}
