using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib
{
    public class Enum
    {
        public enum EnumFileType
        {
            video,
            audio,
            document,
            image,
            other
        }

        public enum EnumEmailType
        {
            registerUser,
            loginUser,
            fileShare,
            viewVideo,
            reviewContent,
            downloadContent,
            storageLimit,
            addComment,
            folderShare
        }
    }
}
