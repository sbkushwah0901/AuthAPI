using Prevueit.Db.Models;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using static Prevueit.Lib.Enum;

namespace Prevueit.Lib.Implementation
{
    public class UserLibrary : IUserLibrary
    {
        #region Variable Declartion
        private readonly prevuitContext _dbContext;
        private readonly AppSettingsModel m_appSettingsModel;
        private readonly ICommonLibrary _commonLibrary;
        #endregion

        #region constructor
        public UserLibrary(prevuitContext dbContext, AppSettingsModel appSettingsModel, ICommonLibrary commonLibrary)
        {
            _dbContext = dbContext;
            m_appSettingsModel = appSettingsModel;
            _commonLibrary = commonLibrary;
        }
        #endregion

        public ResponseModel<bool> UpdateUser(UserModel userModel)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                if (userModel != null)
                {
                    UserInfo existUser = _dbContext.UserInfo.Where(x => x.IUserInfoId == userModel.IUserInfoId && x.IsActive == true).FirstOrDefault();
                    if (existUser != null)
                    {
                        existUser.FirstName = userModel.FirstName;
                        existUser.LastName = userModel.LastName;
                        existUser.PhoneNumber = userModel.PhoneNumber;
                        existUser.ProfilePicUrl = userModel.ProfilePicURL;
                        existUser.IsProfileChanged = userModel.IsProfileChanged;
                        existUser.Remarks = userModel.Remarks;
                        _dbContext.UserInfo.Update(existUser);
                        response.Message = "User updated successfully.";
                    }
                    _dbContext.SaveChanges();

                    var token = _commonLibrary.generateToken(existUser.UserEmail, Convert.ToInt16(existUser.IUserInfoId), Convert.ToBoolean(existUser.IsProfileChanged));
                    response.Message = token;
                    response.ResponseData = true;
                    response.IsSuccess = true;

                }
                else
                {
                    response.ResponseData = false;
                    response.IsSuccess = false;
                    response.Message = "Null reference";
                }

            }
            catch (Exception ex)
            {
                response.ResponseData = false;
                response.IsSuccess = false;
                response.Message = ex.Message;

            }

            return response;
        }

        public ResponseModel<string> EmailSignUp(SignUpReq obj)
        {
            ResponseModel<string> response = new ResponseModel<string>();
            try
            {
                var existUser = _dbContext.UserInfo.Where(x => x.UserEmail == obj.UserEmail && x.IsActive == true).FirstOrDefault();
                if (existUser == null)
                {
                    UserInfo user = new UserInfo()
                    {
                        FirstName = "",
                        LastName = "",
                        UserEmail = obj.UserEmail,
                        TempName = "Guest User",
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsPermanentyBlock = false
                    };
                    _dbContext.UserInfo.Add(user);
                    _dbContext.SaveChanges();

                    var token = _commonLibrary.generateToken(user.UserEmail, Convert.ToInt16(user.IUserInfoId), Convert.ToBoolean(user.IsProfileChanged));
                    user.Token = token;
                    _dbContext.UserInfo.Update(user);
                    _dbContext.SaveChanges();

                    _commonLibrary.sendMail(obj.UserEmail, EnumEmailType.registerUser, token, user.TempName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                    response.Message = "User Registration Successfull";
                    response.ResponseData = token;
                }
                else
                {
                    var token = _commonLibrary.generateToken(existUser.UserEmail, Convert.ToInt16(existUser.IUserInfoId), Convert.ToBoolean(existUser.IsProfileChanged));
                    existUser.Token = token;
                    _dbContext.UserInfo.Update(existUser);
                    _dbContext.SaveChanges();

                    var userName = existUser.FirstName != null ? existUser.FirstName + " " + existUser.LastName : existUser.TempName;
                    _commonLibrary.sendMail(obj.UserEmail, EnumEmailType.loginUser, token, userName, Convert.ToInt16(existUser.IUserInfoId), Convert.ToInt32(existUser.IUserInfoId));

                    var isInValidToken = _isEmptyOrInvalid(existUser.Token);
                    if (!isInValidToken)
                    {
                        response.ResponseData = existUser.Token;
                        response.Message = "User login Successfull";

                    }
                }

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return response;
        }

        public ResponseModel<bool> UserLogin(UserLogin userModel)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                var validUser = _dbContext.UserInfo.Where(x => x.Token == userModel.Token).FirstOrDefault();
                var isInValidToken = _isEmptyOrInvalid(userModel.Token);
                if (validUser != null && !isInValidToken)
                {
                    validUser.IsVerified = true;
                    _dbContext.UserInfo.Update(validUser);
                    _dbContext.SaveChanges();
                    response.Message = "Login Successfully.";

                    //check storage for user
                    long usedByes = 0;
                    long gb = 1024 * 1024 * 1024;
                    long remainingBytes = 5 * gb;
                    long limitBytes = 4 * gb;
                    var files = _dbContext.FileStorage.Where(x => x.IUserInfoId == validUser.IUserInfoId).ToList();
                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            usedByes = Convert.ToInt64(usedByes + Convert.ToInt64(file.FileSize));
                        }
                    }
                    remainingBytes = remainingBytes - usedByes;
                    if (remainingBytes <= limitBytes)
                    {
                        var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == validUser.IUserInfoId).FirstOrDefault();
                        string userName = user != null ? user.FirstName != null ? user.FirstName + " " + user.LastName : user.TempName : "";
                        _commonLibrary.sendMail(user.UserEmail, EnumEmailType.storageLimit, "", userName, Convert.ToInt16(user.IUserInfoId), Convert.ToInt16(user.IUserInfoId));
                    }

                }
                else
                {
                    response.Message = "Invalid Token.";
                }
                response.IsSuccess = true;
                response.ResponseData = true;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Message = ex.Message;
            }

            return response;
        }

        public ResponseModel<UserModel> GetUserById(int iUserId)
        {
            ResponseModel<UserModel> response = new ResponseModel<UserModel>();
            try
            {
                var user = _dbContext.UserInfo.Where(x => x.IUserInfoId == iUserId && x.IsActive == true).FirstOrDefault();
                if (user != null)
                {
                    var files = _dbContext.FileStorage.Where(x => x.IUserInfoId == user.IUserInfoId).ToList();
                    long TotalSpace = 0;
                    if (files.Count > 0)
                    {
                        files.ForEach(x => { TotalSpace = TotalSpace + Convert.ToInt64(x.FileSize); });
                    }

                    var obj = new UserModel()
                    {
                        IUserInfoId = iUserId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        UserEmail = user.UserEmail,
                        TempName = user.TempName,
                        PhoneNumber = user.PhoneNumber,
                        IsVerified = user.IsVerified,
                        ProfilePicURL = user.ProfilePicUrl,
                        IsProfileChanged = Convert.ToBoolean(user.IsProfileChanged),
                        IsPaidUser = Convert.ToBoolean(user.IsPaidUser),
                        UserType = user.IsPaidUser != null && user.IsPaidUser == true ? "Paid user" : "Free user",
                        ExpiryDate = Convert.ToDateTime(user.ExpiryDate),
                        CreatedDate = Convert.ToDateTime(user.CreatedDate),
                        TotalSpaceUsed = Convert.ToString(TotalSpace),
                    };
                    response.ResponseData = obj;
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Message = ex.Message;
            }

            return response;
        }

        public ResponseModel<FileSizePerUser> GetFileSizeByUserId(int iUserId)
        {
            ResponseModel<FileSizePerUser> response = new ResponseModel<FileSizePerUser>();
            try
            {
                var obj = _dbContext.UserInfo.Where(x => x.IUserInfoId == iUserId && x.IsActive == true).FirstOrDefault();
                if (obj != null)
                {
                    //_commonLibrary.sendMail(obj.UserEmail, EnumEmailType.loginUser, obj.Token);
                }


                var usedByes = 0;
                long gb = 1024 * 1024 * 1024;
                long remainingBytes = 5 * gb;
                long totalBytes = 5 * gb;

                var files = _dbContext.FileStorage.Where(x => x.IUserInfoId == iUserId).ToList();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        usedByes = (int)(usedByes + Convert.ToInt64(file.FileSize));
                    }
                }
                remainingBytes = remainingBytes - usedByes;

                response.ResponseData = new FileSizePerUser()
                {
                    TotalBytes = Convert.ToString(totalBytes),
                    UsedBytes = Convert.ToString(usedByes),
                    RemainingBytes = Convert.ToString(remainingBytes)
                };
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel<string>> UploadProfilePic(dynamic files)
        {
            ResponseModel<string> res = new ResponseModel<string>();
            try
            {
                string path = "";
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                            var objName = Convert.ToString(fileName).Split(".");
                            fileName = "ProfilePic" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + objName[1];
                            string storageAccountConnectionString = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                            string containerName = "prevueit-profilepic";
                            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
                            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);
                            await Container.CreateIfNotExistsAsync();
                            CloudBlockBlob blob = Container.GetBlockBlobReference(fileName);
                            HashSet<string> blocklist = new HashSet<string>();
                            path = containerName + "/" + fileName;
                            const int pageSizeInBytes = 10485760;
                            long prevLastByte = 0;
                            long bytesRemain = file.Length;

                            byte[] bytes;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var fileStream = file.OpenReadStream();
                                await fileStream.CopyToAsync(ms);
                                bytes = ms.ToArray();
                            }

                            // Upload each piece
                            do
                            {
                                long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
                                byte[] bytesToSend = new byte[bytesToCopy];

                                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
                                prevLastByte += bytesToCopy;
                                bytesRemain -= bytesToCopy;

                                //create blockId
                                string blockId = Guid.NewGuid().ToString();
                                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));
                                blob.Properties.ContentType = file.ContentType;
                                await blob.PutBlockAsync(
                                    base64BlockId,
                                    new MemoryStream(bytesToSend, true),
                                    null
                                    );

                                blocklist.Add(base64BlockId);

                            } while (bytesRemain > 0);

                            //post blocklist
                            await blob.PutBlockListAsync(blocklist);

                        }
                    }

                }

                res.ResponseData = m_appSettingsModel.AzureStorage.AzureFileURL.ToString() + path;
                res.IsSuccess = true;
                res.Message = "Profile picture uploaded successfully.";
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.ResponseData = "";
                res.StatusCode = HttpStatusCode.InternalServerError;
            }

            return res;
        }

        public ResponseModel<bool> SendNotification(Notification notification)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                notification.CreateDate = DateTime.Now;
                notification.IsActive = true;

                _dbContext.Notification.Add(notification);
                _dbContext.SaveChanges();

                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public ResponseModel<List<NotificationModel>> GetNotificationByUserId(int iUserId)
        {
            ResponseModel<List<NotificationModel>> res = new ResponseModel<List<NotificationModel>>();
            res.ResponseData = new List<NotificationModel>();
            try
            {
                var maxDate = DateTime.Now.AddDays(-7);
                var notifications = _dbContext.Notification.Where(x => 
                                                                    x.IUserId == iUserId 
                                                                    && x.CreateDate >= maxDate
                                                                    ).OrderByDescending(x => x.CreateDate).ToList();
                if (notifications.Count > 0)
                {
                    foreach (var notification in notifications)
                    {
                        var userProfilePic = _dbContext.UserInfo.Where(x => x.IUserInfoId == notification.FromUserId)
                                                                 .Select(x => x.ProfilePicUrl).FirstOrDefault();   
                        NotificationModel notificationModel = new NotificationModel()
                        {
                            INotificationId = Convert.ToInt16(notification.INotificationId),
                            IUserId = Convert.ToInt16(notification.IUserId),
                            ProfilePicUrl = userProfilePic,
                            UserName = notification.FromUserName,
                            Title = notification.Title,
                            Description = notification.Description,
                            IsActive = Convert.ToBoolean(notification.IsActive),
                            CreateDate = Convert.ToDateTime(notification.CreateDate)
                        };
                        res.ResponseData.Add(notificationModel);
                    }
                }
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }
        public ResponseModel<bool> ReadNotification(int iNotificationId)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            try
            {
                var notification = _dbContext.Notification.Where(x => x.INotificationId == iNotificationId).FirstOrDefault();
                if(notification != null)
                {
                    notification.IsActive = false;
                    _dbContext.Notification.Update(notification);
                    _dbContext.SaveChanges();
                }
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public ResponseModel<int> GetNewNotification(int iUserId)
        {
            ResponseModel<int> res = new ResponseModel<int>();
            try
            {
                var maxDate = DateTime.Now.AddDays(-7);
                var notifications = _dbContext.Notification.Where(x =>
                                                                    x.IUserId == iUserId
                                                                    && x.CreateDate >= maxDate
                                                                    && x.IsActive == true
                                                                    ).ToList();
                if (notifications.Count > 0)
                {
                    res.ResponseData = notifications.Count;
                }
                else
                {
                    res.ResponseData = 0;
                }
                res.IsSuccess = true;
                res.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                res.IsSuccess = false;
                res.Message = ex.Message;
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public bool _isEmptyOrInvalid(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            var jwtToken = new JwtSecurityToken(token);
            return (jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow);
        }
        public int GenerateOTP(int n)
        {
            int m = (int)Math.Pow(10, n - 1);
            return m + new Random().Next(9 * m);
        }

    }

}
