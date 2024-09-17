using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authorization;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using Prevueit.Db.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Prevueit.Service.Controllers
{
    [Route("api/prevueit/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminLibrary _adminLibrary;
        public AdminController(IAdminLibrary adminLibrary)
        {
            _adminLibrary = adminLibrary;
        }

        [Route("AdminLogin/{mobileNumber}")]
        [HttpGet]
        public ResponseModel<int> AdminLogin(string mobileNumber)
        {
                return _adminLibrary.GetAdminOTP(mobileNumber);
        }

        [Route("VerifyAdmin/{mobileNumber}/{otp:int}")]
        [HttpGet]
        public ResponseModel<bool> VerifyAdmin(string mobileNumber, int otp)
        {
            return _adminLibrary.VerifyAdmin(mobileNumber, otp);
        }

        [Route("GetAllCount")]
        [HttpGet]
        public ResponseModel<AllCountModel> GetAllCount()
        {
            return _adminLibrary.GetAllCount();
        }

        [Route("GetUserByDate")]
        [HttpPost]
        public ResponseModel<List<UserModel>> GetUserByDate(DateRequestModel reqModel)
        {
            return _adminLibrary.GetUserByDate(reqModel);
        }

        [Route("GetAllUsers")]
        [HttpGet]
        public ResponseModel<List<UserModel>> GetAllUsers()
        {
            return _adminLibrary.GetAllUsers();
        }

        [Route("BlockUser")]
        [HttpPost]
        public ResponseModel<bool> BlockUser(BlockUserReqModel reqModel)
        {
            return _adminLibrary.BlockUser(reqModel);
        }

        [Route("CreateUser")]
        [HttpPost]
        public ResponseModel<bool> CreateUser(UserModel reqModel)
        {
            return _adminLibrary.CreateUser(reqModel);
        }

        [Route("UpdateSpaceConfig")]
        [HttpPost]
        public ResponseModel<bool> UpdateSpaceConfig(SpaceConfigModel reqModel)
        {
            return _adminLibrary.UpdateSpaceConfig(reqModel);
        }

        [Route("GetSpaceConfiguration")]
        [HttpGet]
        public ResponseModel<List<SpaceConfigModel>> GetSpaceConfiguration()
        {
            return _adminLibrary.GetSpaceConfiguration();
        }

    }
}
