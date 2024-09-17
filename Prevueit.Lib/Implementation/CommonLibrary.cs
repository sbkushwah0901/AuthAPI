using Microsoft.IdentityModel.Tokens;
using Prevueit.Db.Models;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Models.Shared;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using static Prevueit.Lib.Enum;

using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Prevueit.Lib.Implementation
{
    public class CommonLibrary : ICommonLibrary
    {
        #region Variable Declartion
        private readonly AppSettingsModel m_appSettingsModel;
        private readonly prevuitContext _dbContext;


        #endregion

        #region constructor
        public CommonLibrary(prevuitContext dbContext, AppSettingsModel appSettingsModel)
        {
            m_appSettingsModel = appSettingsModel;
            _dbContext = dbContext;

        }
        #endregion
        public void sendMail(string toEmail, EnumEmailType enumEmailType, string token, string fromUserName = "",
                            int fromUserId = 0, int iUserId = 0, string fileName = "", string fileSize = "", string fileExpiryDate = "", int iFileId=0)
        {
            //Send mail start
            string body = string.Empty;
            string subject = string.Empty;

            Notification notification = new Notification();

            var validToken = _dbContext.UserInfo.Where(x => x.UserEmail == toEmail).Select(x => x.Token).FirstOrDefault();
            token = validToken;
            switch (enumEmailType)
            {
                case EnumEmailType.registerUser:
                    using (StreamReader reader = (System.IO.File.OpenText("NewUserRegister.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token + "&page=0");
                    subject = "Prevueit Sign Up";

                    //send notfication
                    notification.Title = "Prevuit Sign Up";
                    notification.Description = "You are registered successfully";
                    break;
                case EnumEmailType.loginUser:
                    using (StreamReader reader = (System.IO.File.OpenText("UserLogin.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token + "&page=0");
                    subject = "Prevueit Login";

                    //send notfication
                    notification.Title = "Prevuit Login";
                    notification.Description = "You are logged in successfully";
                    break;
                case EnumEmailType.fileShare:
                    using (StreamReader reader = (System.IO.File.OpenText("FileShare.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{{FromUserName}}", fromUserName);
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token + "&page=2");
                    body = body.Replace("{{FileSize}}", fileSize);
                    body = body.Replace("{{FileExpiryDate}}", fileExpiryDate);
                    subject = "Prevueit File Shared";

                    //send notfication
                    notification.Title = "File Share";
                    notification.Description = "File has been shared";
                    break;
                case EnumEmailType.folderShare:
                    using (StreamReader reader = (System.IO.File.OpenText("FolderShare.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{{FromUserName}}", fromUserName);
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token + "&page=2");
                    body = body.Replace("{{FileSize}}", fileSize);
                    body = body.Replace("{{fileCount}}", fileExpiryDate);
                    subject = "Prevueit File Shared";

                    //send notfication
                    notification.Title = "File Share";
                    notification.Description = "File has been shared";
                    break;
                case EnumEmailType.viewVideo:
                    using (StreamReader reader = (System.IO.File.OpenText("CommonEmailTemplate.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token);
                    body = body.Replace("{{message1}}", fromUserName + " has view file content on Prevueit.");
                    body = body.Replace("{{message2}}", "");
                    subject = "Prevueit Content View";

                    //send notfication
                    notification.Title = "Content View";
                    notification.Description = "Content has been viewed";
                    break;
                case EnumEmailType.reviewContent:
                    using (StreamReader reader = (System.IO.File.OpenText("CommonEmailTemplate.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token);
                    body = body.Replace("{{message1}}", fromUserName + " has reviewed content on Prevueit");
                    body = body.Replace("{{message2}}", "");
                    subject = "Prevueit Review Content";

                    //send notfication
                    notification.Title = "Review Content";
                    notification.Description = "Content had been reviewed";
                    break;
                case EnumEmailType.downloadContent:
                    using (StreamReader reader = (System.IO.File.OpenText("CommonEmailTemplate.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token);
                    body = body.Replace("{{message1}}", fromUserName + " has downloaded file from Prevueit.");
                    body = body.Replace("{{message2}}", "");
                    subject = "Prevueit File Download";

                    //send notfication
                    notification.Title = "File Download";
                    notification.Description = "File has been downloaded";
                    break;
                case EnumEmailType.storageLimit:
                    using (StreamReader reader = (System.IO.File.OpenText("CommonEmailTemplate.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token);
                    body = body.Replace("{{message1}}", fromUserName + ", you have reached 80% storage limit on Prevueit.");
                    body = body.Replace("{{message2}}", "");
                    subject = "Prevueit Storage Limit";

                    //send notfication
                    notification.Title = "Storage Limit";
                    notification.Description = "80% storage has been used";
                    break;
                case EnumEmailType.addComment:
                    using (StreamReader reader = (System.IO.File.OpenText("comment-email.html")))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{loginurl}", m_appSettingsModel.Email.VerifyUrl.ToString() + token + "&page=3&fileid=" + iFileId);
                    body = body.Replace("{{FromUserName}}", fromUserName);
                    body = body.Replace("{{fileName}}", fileName);
                    subject = "Comment Added";

                    //send notfication
                    notification.Title = "Comment";
                    notification.Description = "Comment added on file";
                    break;
                default:
                    break;

            }
            notification.IUserId = iUserId;
            notification.FromUserName = fromUserName;
            notification.FromUserId = fromUserId;
            notification.CreateDate = DateTime.Now;
            notification.IsActive = true;

            //msg.From = new MailAddress(m_appSettingsModel.Email.FromEmailId.ToString());
            //msg.To.Add(toEmail);

            //msg.Body = body;
            //msg.IsBodyHtml = true;
            //SmtpClient client = new SmtpClient();
            //client.UseDefaultCredentials = false;
            //client.Host = "smtpout.secureserver.net";
            //client.Port = 80;
            //client.EnableSsl = false;
            //client.DeliveryMethod = SmtpDeliveryMethod.Network;
            //client.Credentials = new System.Net.NetworkCredential(m_appSettingsModel.Email.FromEmailId.ToString(), m_appSettingsModel.Email.FromEmailPassword.ToString());
            try
            {
                SendmailFromSendInBlue(toEmail, subject, body);
                //client.Send(msg);

                _dbContext.Notification.Add(notification);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void SendmailFromSendInBlue(string toEmail, string emailSubject, string emailBody)
        {
            
            if (!Configuration.Default.ApiKey.Any())
            {
                Configuration.Default.ApiKey.Add("api-key", m_appSettingsModel.Email.SendInBlueAPIKey.ToString());
            }
            var apiInstance = new TransactionalEmailsApi();
            string SenderName = "Bharatransfer";
            string SenderEmail = m_appSettingsModel.Email.FromEmailId.ToString();
            SendSmtpEmailSender Email = new SendSmtpEmailSender(SenderName, SenderEmail);

            string ToEmail = toEmail;
            string ToName = "User";
            SendSmtpEmailTo smtpEmailTo = new SendSmtpEmailTo(ToEmail, ToName);
            List<SendSmtpEmailTo> To = new List<SendSmtpEmailTo>();
            To.Add(smtpEmailTo);

            string HtmlContent = emailBody;
            string TextContent = null;
            string Subject = "{{params.subject}}";

            JObject Headers = new JObject();
            Headers.Add("api-key", m_appSettingsModel.Email.SendInBlueAPIKey.ToString());
            long? TemplateId = null;
            JObject Params = new JObject();
            Params.Add("subject", emailSubject);

            SendSmtpEmailTo1 smtpEmailTo1 = new SendSmtpEmailTo1(ToEmail, ToName);
            List<SendSmtpEmailTo1> To1 = new List<SendSmtpEmailTo1>();
            To1.Add(smtpEmailTo1);
            Dictionary<string, object> _parmas = new Dictionary<string, object>();
            _parmas.Add("params", Params);

            SendSmtpEmailMessageVersions messageVersion = new SendSmtpEmailMessageVersions(To1, _parmas, null, null, null, Subject);
            List<SendSmtpEmailMessageVersions> messageVersiopns = new List<SendSmtpEmailMessageVersions>();
            messageVersiopns.Add(messageVersion);

            var sendSmtpEmail = new SendSmtpEmail(Email, To, null, null, HtmlContent, TextContent, Subject, null, null, Headers, TemplateId, Params, messageVersiopns, null);
            CreateSmtpEmail result = apiInstance.SendTransacEmail(sendSmtpEmail);

        }
        public string generateToken(string email, int userId, bool isProfileChanged)
        {
            string access_token = "";
            try
            {
                var strSecretKey = m_appSettingsModel.Jwt.Key;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(strSecretKey);
                var expiry = DateTime.Now.AddDays(15);

                var iUserInfoId = userId;
                var strEnumRole = "User";
                var strEmailAddress = email;
                var strLoginDisplayName = "UserName";
                var strProfileChanged = Convert.ToString(isProfileChanged).ToLower();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ApplicationConstants.UserId, iUserInfoId.ToString()),
                        new Claim(ApplicationConstants.UserName, strLoginDisplayName),
                        new Claim(ApplicationConstants.UserRole, strEnumRole),
                        new Claim(ApplicationConstants.isProfileChanged, strProfileChanged),
                        new Claim(ApplicationConstants.UserEmail, email)
                    }),
                    Expires = expiry,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = m_appSettingsModel.Jwt.Issuer,
                    IssuedAt = DateTime.Now,
                    //Claims = new Dictionary<string, object>(new KeyValuePair<string, object>[]
                    //{
                    //    new KeyValuePair<string, object>(ApplicationConstants.UserId, iUserInfoId.ToString()),
                    //    new KeyValuePair<string, object>(ApplicationConstants.UserName, strLoginDisplayName),
                    //    new KeyValuePair<string, object>(ApplicationConstants.UserRole, strEnumRole),
                    //    new KeyValuePair<string, object>(ApplicationConstants.UserEmail, strEmailAddress)
                    //})

                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                access_token = tokenHandler.WriteToken(token);

            }
            catch (Exception ex)
            {
                //throw ex;
            }

            return access_token;
        }
    }
}
