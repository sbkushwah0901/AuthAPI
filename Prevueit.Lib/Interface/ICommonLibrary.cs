using System;
using System.Collections.Generic;
using System.Text;
using static Prevueit.Lib.Enum;

namespace Prevueit.Lib.Interface
{
    public interface ICommonLibrary
    {
        void sendMail(string toEmail, EnumEmailType enumEmailType, string token, string fromUserName = "", int fromUserId = 0, int iUserId = 0, string fileName = "", string fileSize = "", string fileExpiryDate = "", int iFileId = 0);
        string generateToken(string email, int userId, bool isProfileChanged);
    }
}
