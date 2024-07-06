using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GmailInwardService
{
    internal class Program
    {
        private static UserCredential Login(string clientId,string clientSecret, string[] scopes)
        {
            var clientSecrets = new ClientSecrets()
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };
            return GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecrets, scopes, "user", CancellationToken.None).Result;
        }
        static void Main(string[] args)
        {
            var googleClientId = "ClientId";
            var googleClientSecret = "Clinet Secret Key";
            string[] scopes = new string[] { GmailService.Scope.GmailReadonly,GmailService.Scope.GmailModify };
            var credentials = Login(googleClientId, googleClientSecret, scopes);
            using (var gmailService = new GmailService(new BaseClientService.Initializer() { HttpClientInitializer = credentials }))
            {
                var profile = gmailService.Users.GetProfile("me").Execute();
                Console.WriteLine(profile.EmailAddress);
                //Console.ReadLine();

                var inboxlistRequest = gmailService.Users.Messages.List("me");
                inboxlistRequest.Q = "is:unread";
                inboxlistRequest.LabelIds = new List<string> { "INBOX", "UNREAD" };
                inboxlistRequest.IncludeSpamTrash = false;
                
                //get our emails   
                var emailListResponse = inboxlistRequest.Execute();
                if (emailListResponse != null && emailListResponse.Messages != null)
                {
                    //loop through each email and get what fields you want...   
                    int i = 0;
                    foreach (var email in emailListResponse.Messages)
                    {
                        var emailInfoRequest = gmailService.Users.Messages.Get("me",email.Id);
                        var emailInfoResponse = emailInfoRequest.Execute();
                        if (emailInfoResponse != null)
                        {
                            String from = "";
                            String date = "";
                            String subject = "";
                            //loop through the headers to get from,date,subject, body  
                            foreach (var mParts in emailInfoResponse.Payload.Headers)
                            {
                                if (mParts.Name == "Date")
                                {
                                    date = mParts.Value;
                                }
                                else if (mParts.Name == "From")
                                {
                                    from = mParts.Value;
                                }
                                else if (mParts.Name == "Subject")
                                {
                                    subject = mParts.Value;
                                }
                                if (date != "" && from != "" && emailInfoResponse.Payload!=null && emailInfoResponse.Payload.Parts!=null)
                                {
                                    foreach (MessagePart p in emailInfoResponse.Payload.Parts)
                                    {
                                        if (p.MimeType == "text/html")
                                        {
                                            byte[] data = FromBase64ForUrlString(p.Body.Data);
                                            string decodedString = Encoding.UTF8.GetString(data);
                                        }
                                    }
                                }
                               
                            }
                            
                            Console.WriteLine($"{i++} : {from} - {date} -  {subject}");
                            //var modifyRequest = new ModifyMessageRequest()
                            //{
                            //    AddLabelIds = null,
                            //    RemoveLabelIds = new List<string> { "UNREAD" }
                            //};                           
                            //gmailService.Users.Messages.Modify(modifyRequest, "me", email.Id).Execute();
                        }
                    }
                }
            }
            Console.ReadLine();
        }
        public static byte[] FromBase64ForUrlString(string base64ForUrlInput)
        {
            int padChars = (base64ForUrlInput.Length % 4) == 0 ? 0 : (4 - (base64ForUrlInput.Length % 4));
            StringBuilder result = new StringBuilder(base64ForUrlInput, base64ForUrlInput.Length + padChars);
            result.Append(String.Empty.PadRight(padChars, '='));
            result.Replace('-', '+');
            result.Replace('_', '/');
            return Convert.FromBase64String(result.ToString());
        }
    }
}
