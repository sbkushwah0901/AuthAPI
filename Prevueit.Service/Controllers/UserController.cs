
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authorization;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;
using Prevueit.Db.Models;
using System.Collections.Generic;

namespace Prevueit.Service.Controllers
{
    [Route("api/prevueit/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserLibrary _userLibrary;
        public UserController(IUserLibrary userLibrary)
        {
            _userLibrary = userLibrary;
        }

        [AllowAnonymous]
        [Route("UpdateUser")]
        [HttpPost]
        public ResponseModel<bool> UpdateUser(UserModel userModel)
        {
            dynamic response ;
            try
            {
                var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                var resToken = CommonFunctions.isValidToken(bearer_token);
                if (resToken.ResponseData)
                {
                    response = _userLibrary.UpdateUser(userModel);
                }
                else
                {
                    return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
                }
            }
            catch (Exception)
            {
                response = false;
            }
            return response;
        }

        [AllowAnonymous]
        [Route("UpdateExternalUser")]
        [HttpPost]
        public ResponseModel<bool> UpdateExternalUser(UserModel userModel)
        {
            dynamic response;
            try
            {
                response = _userLibrary.UpdateUser(userModel);
            }
            catch (Exception)
            {
                response = false;
            }
            return response;
        }

        [Route("EmailSignUp")]
        [HttpPost]
        public ResponseModel<string> EmailSignUp(SignUpReq obj)
        {
            return _userLibrary.EmailSignUp(obj);
        }

        [Route("UserLogin")]
        [HttpPost]
        public ResponseModel<bool> UserLogin(UserLogin userModel)
        {
            dynamic response ;
            try
            {
                response = _userLibrary.UserLogin(userModel);
            }
            catch (Exception)
            {
                response = false;
            }
            return response;
        }
        
        [Route("GetUserById/{iUserId:int}")]
        [HttpGet]
        public ResponseModel<UserModel> GetUserById(int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if(resToken.ResponseData)
            {
                return _userLibrary.GetUserById(iUserId);
            }
            else
            {
                return new ResponseModel<UserModel>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
            
        }

        [Route("GetFileSizeByUserId/{iUserId:int}")]
        [HttpGet]
        public ResponseModel<FileSizePerUser> GetFileSizeByUserId(int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _userLibrary.GetFileSizeByUserId(iUserId);
            }
            else
            {
                return new ResponseModel<FileSizePerUser>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }


        [DisableRequestSizeLimit]
        [Route("UploadProfilePic")]
        [HttpPost]
        public async Task<ResponseModel<string>> UploadProfilePic()
        {
            var files = Request.Form.Files;
            var response = await _userLibrary.UploadProfilePic(files);
            return response;
        }

        [Route("SendNotification")]
        [HttpPost]
        public ResponseModel<bool> SendNotification(Notification notification)
        {
            dynamic response;
            try
            {
                var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                var resToken = CommonFunctions.isValidToken(bearer_token);
                if (resToken.ResponseData)
                {
                    return _userLibrary.SendNotification(notification);
                }
                else
                {
                    return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
                }
                
            }
            catch (Exception)
            {
                response = false;
            }
            return response;
        }

        [Route("GetNotificationByUserId/{iUserId:int}")]
        [HttpGet]
        public ResponseModel<List<NotificationModel>> GetNotificationByUserId(int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _userLibrary.GetNotificationByUserId(iUserId);
            }
            else
            {
                return new ResponseModel<List<NotificationModel>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("ReadNotification/{iNotificationId:int}")]
        [HttpGet]
        public ResponseModel<bool> ReadNotification(int iNotificationId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _userLibrary.ReadNotification(iNotificationId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetNewNotification/{iUserId:int}")]
        [HttpGet]
        public ResponseModel<int> GetNewNotification(int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _userLibrary.GetNewNotification(iUserId);
            }
            else
            {
                return new ResponseModel<int>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }
    }
}
