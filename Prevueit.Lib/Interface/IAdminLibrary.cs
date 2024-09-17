using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Interface
{
    public interface IAdminLibrary
    {
        ResponseModel<int> GetAdminOTP(string mobileNumber);
        ResponseModel<bool> VerifyAdmin(string mobileNumber, int otp);
        ResponseModel<AllCountModel> GetAllCount();
        ResponseModel<List<UserModel>> GetUserByDate(DateRequestModel reqModel);
        ResponseModel<List<UserModel>> GetAllUsers();
        ResponseModel<bool> BlockUser(BlockUserReqModel reqModel);
        ResponseModel<bool> CreateUser(UserModel reqModel);
        ResponseModel<bool> UpdateSpaceConfig(SpaceConfigModel reqModel);
        ResponseModel<List<SpaceConfigModel>> GetSpaceConfiguration();
    }
}
