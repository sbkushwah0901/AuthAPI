using Prevueit.Db.Models;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Http;
using static Prevueit.Lib.Enum;
using System.IO;
using System.Threading.Tasks;


namespace Prevueit.Lib.Implementation
{
    public class AdminLibrary : IAdminLibrary
    {
        #region Variable Declartion
        private readonly prevuitContext _dbContext;
        private readonly AppSettingsModel m_appSettingsModel;
        private readonly ICommonLibrary _commonLibrary;

        #endregion

        #region constructor
        public AdminLibrary(prevuitContext dbContext, AppSettingsModel appSettingsModel, ICommonLibrary commonLibrary)
        {
            _dbContext = dbContext;
            m_appSettingsModel = appSettingsModel;
            _commonLibrary = commonLibrary;
        }
        #endregion

        public ResponseModel<int> GetAdminOTP(string mobileNumber)
        {
            ResponseModel<int> response = new ResponseModel<int>();
            try
            {
                var admin = _dbContext.Admin.Where(x => x.MobileNumber == mobileNumber).FirstOrDefault();
                if (admin != null)
                {
                    var otp = GenerateOTP(6);
                    admin.Otp = otp;

                    _dbContext.Admin.Update(admin);
                    _dbContext.SaveChanges();

                    //Send Msg Code here
                    response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = "Admin detail not found.";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<bool> VerifyAdmin(string mobileNumber, int otp)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                var admin = _dbContext.Admin.Where(x => x.MobileNumber == mobileNumber && x.Otp == otp).FirstOrDefault();
                if (admin != null)
                {
                    admin.Otp = 0;
                    admin.LastLoginDate = DateTime.Now;

                    _dbContext.Admin.Update(admin);
                    _dbContext.SaveChanges();
                    response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = "OTP does not match.";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<AllCountModel> GetAllCount()
        {
            ResponseModel<AllCountModel> response = new ResponseModel<AllCountModel>();
            try
            {
                var users = _dbContext.UserInfo.Where(x => x.IsActive == true && x.IsPermanentyBlock == false).ToList();
                Int64 totalUsedSpace = 0;
                Int64 freeUserSpace = 0;
                Int64 paidUserSpace = 0;

                var fileList = _dbContext.FileStorage.ToList();

                foreach (var user in users)
                {
                    var files = fileList.Where(x => x.IUserInfoId == user.IUserInfoId).ToList();
                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            if (user.IsPaidUser == true)
                            {
                                paidUserSpace = Convert.ToInt64(paidUserSpace + Convert.ToInt64(file.FileSize));
                            }
                            else
                            {
                                freeUserSpace = Convert.ToInt64(freeUserSpace + Convert.ToInt64(file.FileSize));
                            }
                        }
                    }
                }
                totalUsedSpace = paidUserSpace + freeUserSpace;

                response.ResponseData = new AllCountModel()
                {
                    TotalUserCount = users.Count(),
                    FreelUserCount = users.Where(x => x.IsPaidUser == false || x.IsPaidUser == null).Count(),
                    PaidUserCount = users.Where(x => x.IsPaidUser == true).Count(),
                    TotalSpaceUsed = Convert.ToString(totalUsedSpace),
                    FreeUserSpace = Convert.ToString(freeUserSpace),
                    PaidUserSpace = Convert.ToString(paidUserSpace),
                    TodayFreeUser = users.Where(x => x.IsPaidUser != true && Convert.ToDateTime(x.CreatedDate).Date == DateTime.Now.Date).Count(),
                    TodayPaidUser = users.Where(x => x.IsPaidUser == true && Convert.ToDateTime(x.CreatedDate).Date == DateTime.Now.Date).Count(),
                };
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<List<UserModel>> GetUserByDate(DateRequestModel reqModel)
        {
            ResponseModel<List<UserModel>> response = new ResponseModel<List<UserModel>>();
            try
            {
                response.ResponseData = new List<UserModel>();
                var users = _dbContext.UserInfo.AsEnumerable().Where(x => 
                                                            x.IsActive == true && x.IsPermanentyBlock == false &&
                                                            Convert.ToDateTime(x.CreatedDate).Date >= reqModel.StartDate.Date &&
                                                            Convert.ToDateTime(x.CreatedDate).Date <= reqModel.EndDate.Date).ToList();
                var lstFiles = _dbContext.FileStorage.ToList();
                
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        var files = lstFiles.Where(x => x.IUserInfoId == user.IUserInfoId).ToList();
                        long TotalSpace = 0;
                        if (files.Count > 0)
                        {

                            files.ForEach(x => { TotalSpace = TotalSpace + Convert.ToInt64(x.FileSize); });
                        }

                        UserModel u = new UserModel()
                        {
                            IUserInfoId = Convert.ToInt16(user.IUserInfoId),
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            UserEmail = user.UserEmail,
                            TempName = user.TempName,
                            PhoneNumber = user.PhoneNumber,
                            IsVerified = user.IsVerified,
                            ProfilePicURL = user.ProfilePicUrl,
                            IsProfileChanged = Convert.ToBoolean(user.IsProfileChanged),
                            IsPaidUser = Convert.ToBoolean(user.IsPaidUser),
                            UserType = user.IsPaidUser == true ? "Paid user" : "Free user",
                            ExpiryDate = Convert.ToDateTime(user.ExpiryDate),
                            CreatedDate = Convert.ToDateTime(user.CreatedDate),
                            TotalSpaceUsed = Convert.ToString(TotalSpace),
                        };
                        response.ResponseData.Add(u);
                    }
                }
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<List<UserModel>> GetAllUsers()
        {
            ResponseModel<List<UserModel>> response = new ResponseModel<List<UserModel>>();
            try
            {
                response.ResponseData = new List<UserModel>();
                var users = _dbContext.UserInfo.Where(x => x.IsActive == true && x.IsPermanentyBlock == false).ToList();

                var lstFiles = _dbContext.FileStorage.ToList();
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        var files = lstFiles.Where(x => x.IUserInfoId == user.IUserInfoId).ToList();
                        long TotalSpace = 0;
                        if(files.Count > 0)
                        {

                            files.ForEach(x => { TotalSpace = TotalSpace + Convert.ToInt64(x.FileSize); });
                        }
                        UserModel u = new UserModel()
                        {
                            IUserInfoId = Convert.ToInt16(user.IUserInfoId),
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            UserEmail = user.UserEmail,
                            TempName = user.TempName,
                            PhoneNumber = user.PhoneNumber,
                            IsVerified = user.IsVerified,
                            ProfilePicURL = user.ProfilePicUrl,
                            IsProfileChanged = Convert.ToBoolean(user.IsProfileChanged),
                            IsPaidUser = Convert.ToBoolean(user.IsPaidUser),
                            UserType = user.IsPaidUser == true ? "Paid user" : "Free user",
                            ExpiryDate = Convert.ToDateTime(user.ExpiryDate),
                            CreatedDate = Convert.ToDateTime(user.CreatedDate),
                            TotalSpaceUsed = Convert.ToString(TotalSpace),
                        };
                        response.ResponseData.Add(u);
                    }
                }
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }
        public ResponseModel<bool> BlockUser(BlockUserReqModel reqModel)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                var user = _dbContext.UserInfo.Where(x => x.UserEmail == reqModel.UserEmail).FirstOrDefault();
                if (user != null)
                {
                    if (reqModel.IsPermanentlyBlock == true)
                    {
                        user.IsPermanentyBlock = true;
                        user.BlockDate = DateTime.Now;
                    }
                    user.IsActive = false;
                    _dbContext.UserInfo.Update(user);
                    _dbContext.SaveChanges();
                    response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
                    response.Message = "User E-mail Blocked successfully";
                }
                else
                {
                    response.IsSuccess = false; response.StatusCode = HttpStatusCode.NotFound;
                    response.Message = "User with '" + reqModel.UserEmail + "' Not Found";
                }
                
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<bool> CreateUser(UserModel reqModel)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                var existUser = _dbContext.UserInfo.Where(x => x.UserEmail == reqModel.UserEmail && x.IsActive == true).FirstOrDefault();
                if(existUser != null)
                {
                    var token = _commonLibrary.generateToken(existUser.UserEmail, Convert.ToInt16(existUser.IUserInfoId), Convert.ToBoolean(existUser.IsProfileChanged));
                    existUser.Token = token;
                    response.IsSuccess = false; 
                    response.StatusCode = HttpStatusCode.Ambiguous;
                    response.Message = "User with e-mail '" + reqModel.UserEmail + "' has already been registered";
                    return response;
                }
                UserInfo user = new UserInfo()
                {
                    FirstName = reqModel.FirstName,
                    LastName = reqModel.LastName,
                    UserEmail = reqModel.UserEmail,
                    TempName = reqModel.TempName,
                    PhoneNumber = reqModel.PhoneNumber,
                    IsActive = true,
                    IsVerified = reqModel.IsVerified,
                    CreatedDate = DateTime.Now,
                    IsPaidUser = reqModel.IsProfileChanged,
                    //ExpiryDate = reqModel.ExpiryDate,
                    Remarks = reqModel.Remarks,
                    IsPermanentyBlock = false
                };
                _dbContext.UserInfo.Add(user);
                _dbContext.SaveChanges();
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
                response.Message = "User Registration Successfull";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<bool> UpdateSpaceConfig(SpaceConfigModel reqModel)
        {
            ResponseModel<bool> response = new ResponseModel<bool>();
            try
            {
                var config = _dbContext.SpaceConfiguration.Where(x => x.ISpaceConfigurationId == reqModel.iSpaceConfigId).FirstOrDefault();
                if (config != null)
                {
                    config.PerFileUploadLimit = reqModel.PerFileUploadLimit;
                    config.TotalSpaceAllowed = reqModel.TotalSpaceAllowed;
                    //config.ExpiryDate = reqModel.ExpiryDate;
                    _dbContext.SpaceConfiguration.Update(config);
                    _dbContext.SaveChanges();
                }
                response.ResponseData = true;
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public ResponseModel<List<SpaceConfigModel>> GetSpaceConfiguration()
        {
            ResponseModel<List<SpaceConfigModel>> response = new ResponseModel<List<SpaceConfigModel>>();
            response.ResponseData = new List<SpaceConfigModel>();
            try
            {
                var configList = _dbContext.SpaceConfiguration.ToList();
                var users = _dbContext.UserInfo.Where(x => x.IsActive == true && x.IsPermanentyBlock == false).ToList();
                foreach (var config in configList)
                {
                    int count = 0;
                    if(config.UserType == "Free User")
                    {
                        count = users.Where(x => x.IsPaidUser != true).Count();
                    }
                    else
                    {
                        count = users.Where(x => x.IsPaidUser == true).Count();
                    }
                    var configModel = new SpaceConfigModel()
                    {
                        iSpaceConfigId = Convert.ToInt16(config.ISpaceConfigurationId),
                        UserType = config.UserType,
                        PerFileUploadLimit = config.PerFileUploadLimit,
                        TotalSpaceAllowed = config.TotalSpaceAllowed,
                        ExpiryDate = Convert.ToDateTime(config.ExpiryDate),
                        UserCount = count
                    };
                    response.ResponseData.Add(configModel);
                }
                response.IsSuccess = true; response.StatusCode = HttpStatusCode.OK;

            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public int GenerateOTP(int n)
        {
            int m = (int)Math.Pow(10, n - 1);
            return m + new Random().Next(9 * m);
        }
    }
}
