using Prevueit.Db.Models;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Prevueit.Lib.Interface
{
    public interface IUserLibrary
    {
        ResponseModel<bool> UpdateUser(UserModel userModel);
        ResponseModel<string> EmailSignUp(SignUpReq obj);
        ResponseModel<bool> UserLogin(UserLogin userModel);
        ResponseModel<UserModel> GetUserById(int iUserId);
        ResponseModel<FileSizePerUser> GetFileSizeByUserId(int iUserId);
        Task<ResponseModel<string>> UploadProfilePic(dynamic files);
        ResponseModel<bool> SendNotification(Notification notification);
        ResponseModel<List<NotificationModel>> GetNotificationByUserId(int iUserId);
        ResponseModel<bool> ReadNotification(int iNotificationId);
        ResponseModel<int> GetNewNotification(int iUserId);
    }
}
