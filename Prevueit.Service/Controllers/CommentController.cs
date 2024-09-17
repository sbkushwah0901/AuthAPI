
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
    [Route("api/prevueit/comment")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentLibrary _commentLibrary;
        public CommentController(ICommentLibrary commentLibrary)
        {
            _commentLibrary = commentLibrary;
        }
        [Route("UploadComment")]
        [HttpPost]
        public async Task<ResponseModel<string>> UploadComment()
        {
            var files = Request.Form.Files;
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                var response = await _commentLibrary.UploadComment(files);
                return response;
            }
            else
            {
                return new ResponseModel<string>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }
        [Route("AddUpdateComment")]
        [HttpPost]
        public ResponseModel<bool> AddUpdateComment(Comment objComment)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                var response = _commentLibrary.AddUpdateComment(objComment);
                return response;
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }


        [Route("RemoveComment/{iCommentId:int}")]
        [HttpGet]
        public async Task<ResponseModel<bool>> RemoveComment(int iCommentId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return await _commentLibrary.RemoveComment(iCommentId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetCommentsByFileId/{iFileId:int}")]
        [HttpGet]
        public ResponseModel<List<CommentModel>> GetCommentsByFileId(int iFileId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _commentLibrary.GetCommentsByFileId(iFileId);
            }
            else
            {
                return new ResponseModel<List<CommentModel>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("LikeComment/{iCommentId:int}")]
        [HttpGet]
        public ResponseModel<int> LikeComment(int iCommentId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _commentLibrary.LikeComment(iCommentId);
            }
            else
            {
                return new ResponseModel<int>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

    }
}
