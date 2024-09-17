using Microsoft.AspNetCore.Http;
using Prevueit.Lib.Models.Shared;
using System;
using System.IdentityModel.Tokens.Jwt;


namespace Prevueit.Service
{
    public static class CommonFunctions
    {
        public static ResponseModel<bool> isValidToken(string token)
        {
            ResponseModel<bool> res = new ResponseModel<bool>();
            if (string.IsNullOrEmpty(token))
            {
                res.IsSuccess = false;
                res.ResponseData = false;
                res.Message = "Invalid Token.";

                return res;
            }

            var jwtToken = new JwtSecurityToken(token);
            res.ResponseData = !((jwtToken == null) || (jwtToken.ValidFrom > DateTime.UtcNow) || (jwtToken.ValidTo < DateTime.UtcNow));
            res.IsSuccess = true;

            return res;
        }

      
    }
}
