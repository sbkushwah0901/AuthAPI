using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Prevueit.Lib.Interface;
using Prevueit.Lib.Model;
using Prevueit.Lib.Models.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Prevueit.Service.Controllers
{
    [Route("api/prevueit/user")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileLibrary _fileLibrary;
        private readonly AppSettingsModel m_appSettingsModel;

        public FileController(IFileLibrary fileLibrary, AppSettingsModel appSettingsModel)
        {
            _fileLibrary = fileLibrary;
            m_appSettingsModel = appSettingsModel;
        }

        [Route("UploadFiles")]
        [HttpPost]
        public ResponseModel<bool> UploadFiles(FileUploadReqModel lstFile)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                var response = _fileLibrary.UploadFiles(lstFile);
                return response;
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)]
        [Route("Upload")]
        [HttpPost]
        public async Task<ResponseModel<string>> Upload()
        {
            var res = new ResponseModel<string>();
            try
            {
                var files = Request.Form.Files;

                string path = "";
                string originalName = "";
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.ToString().Trim('"');
                            originalName = Convert.ToString(fileName);
                            var objName = Convert.ToString(fileName).Split(".");
                            fileName = "File" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + objName[1];
                            string storageAccountConnectionString = m_appSettingsModel.AzureStorage.storageAccountConnectionString.ToString();
                            string containerName = m_appSettingsModel.AzureStorage.ContainerName.ToString();
                            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
                            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);
                            await Container.CreateIfNotExistsAsync();
                            CloudBlockBlob blob = Container.GetBlockBlobReference(fileName);
                            HashSet<string> blocklist = new HashSet<string>();
                            path = containerName + "/" + fileName;
                            const int pageSizeInBytes = 104857600;
                            long prevLastByte = 0;
                            long bytesRemain = file.Length;

                            byte[] bytes;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var fileStream = file.OpenReadStream();
                                await fileStream.CopyToAsync(ms);
                                bytes = ms.ToArray();
                            }
                            var contentType = "";
                            if (Convert.ToString(file.ContentType).Contains("audio"))
                            {
                                contentType = "video/mp4";
                            }
                            else
                            {
                                contentType = Convert.ToString(file.ContentType);
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

                                blob.Properties.ContentType = contentType;
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
                res.Message = originalName;
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


        [Route("UpdateFileDetail")]
        [HttpPost]
        public ResponseModel<bool> UpdateFileDetail(FileUploadReqModel fileModel)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                var response = _fileLibrary.UpdateFileDetail(fileModel);
                return response;
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetFileDetailsId/{iFileId:int}")]
        [HttpGet]
        public ResponseModel<FileModel> GetFileDetailsId(int iFileId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetFileDetailsId(iFileId);
            }
            else
            {
                return new ResponseModel<FileModel>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("UpdateReviewedStatus/{iFileId:int}/{userEmail}/")]
        [HttpGet]
        public ResponseModel<bool> UpdateReviewedStatus(int iFileId, string userEmail)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.UpdateReviewedStatus(iFileId, userEmail);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetFileSharedUserEmail/{userEmail}/{fileType}")]
        [HttpGet]
        public ResponseModel<List<FileModel>> GetFileSharedUserId(string userEmail, string fileType)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetFileSharedUserId(userEmail, fileType);
            }
            else
            {
                return new ResponseModel<List<FileModel>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }


        [Route("GetFilesByUploadedUserId/{fileType}/{iUserId:int}/{iFolderId:int}")]
        [HttpGet]
        public ResponseModel<List<FileModel>> GetFilesByUploadedUserId(string fileType, int iUserId, int iFolderId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetFilesByUploadedUserId(iUserId, fileType, iFolderId);
            }
            else
            {
                return new ResponseModel<List<FileModel>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetSentFilesByUserId/{iUserId:int}")]
        [HttpGet]
        public ResponseModel<List<FileModel>> GetSentFilesByUserId(int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetSentFilesByUserId(iUserId);
            }
            else
            {
                return new ResponseModel<List<FileModel>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("DeleteFile/{iFileId:int}")]
        [HttpGet]
        public async Task<ResponseModel<bool>> DeleteFile(int iFileId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return await _fileLibrary.DeleteFile(iFileId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("AddSharedUser")]
        [HttpPost]
        public ResponseModel<bool> AddSharedUser(AddUserToFileModel reqModel)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.AddSharedUser(reqModel);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("RemoveSharedUser/{iFileId:int}/{iFolderId:int}/{userEmails}")]
        [HttpGet]
        public ResponseModel<bool> RemoveSharedUser(int iFileId, int iFolderId, string userEmails)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.RemoveSharedUser(iFileId, iFolderId, userEmails);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("IncrementViewCount/{iFileId:int}")]
        [HttpGet]
        public ResponseModel<bool> IncrementViewCount(int iFileId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.IncrementViewCount(iFileId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }


        [Route("SendEmail")]
        [HttpPost]
        public ResponseModel<bool> SendEmail(EmailReqModel reqModel)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);

            if (resToken.ResponseData)
            {
                reqModel.Token = bearer_token;
                return _fileLibrary.SendEmail(reqModel);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("AddRemoveFavourites/{iFileFolderId:int}/{isFavourite}/{isFolder}")]
        [HttpGet]
        public ResponseModel<bool> AddRemoveFavourites(int iFileFolderId, bool isFavourite, bool isFolder)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.AddRemoveFavourites(iFileFolderId, isFavourite, isFolder);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("RenameFileFolder/{iFileFolderId:int}/{newName}/{isFolder}")]
        [HttpGet]
        public ResponseModel<bool> RenameFileFolder(int iFileFolderId, string newName, bool isFolder)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.RenameFileFolder(iFileFolderId, newName, isFolder);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }


        [Route("CreateFolder")]
        [HttpPost]
        public ResponseModel<int> CreateFolder(FolderDetail reqModel)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.CreateFolder(reqModel);
            }
            else
            {
                return new ResponseModel<int>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("MoveToFolder/{iFolderId:int}/{iFileId:int}")]
        [HttpGet]
        public ResponseModel<bool> MoveToFolder(int iFolderId, int iFileId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.MoveToFolder(iFolderId, iFileId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetFoldersByUserId/{iUserInfoId:int}")]
        [HttpGet]
        public ResponseModel<List<FolderDetail>> GetFoldersByUserId(int iUserInfoId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetFoldersByUserId(iUserInfoId);
            }
            else
            {
                return new ResponseModel<List<FolderDetail>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("DeleteFolder/{iFolderId:int}")]
        [HttpGet]
        public async Task<ResponseModel<bool>> DeleteFolder(int iFolderId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return await _fileLibrary.DeleteFolder(iFolderId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("FetFolderDetailWithFiles/{iFolderId:int}")]
        [HttpGet]
        public ResponseModel<FolderWithFileModel> FetFolderDetailWithFiles( int iFolderId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.FetFolderDetailWithFiles(iFolderId);
            }
            else
            {
                return new ResponseModel<FolderWithFileModel>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("DuplicateFile/{iFileId:int}/{iUserId:int}")]
        [HttpGet]
        public async Task<ResponseModel<bool>> DuplicateFile(int iFileId, int iUserId)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return await _fileLibrary.DuplicateFile(iFileId, iUserId);
            }
            else
            {
                return new ResponseModel<bool>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }

        [Route("GetSearchFileLists/{fileType}/{iUserId:int}/{userEmail}")]
        [HttpGet]
        public ResponseModel<List<SearchFiles>> GetSearchFileLists(string fileType, int iUserId, string userEmail)
        {
            var bearer_token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            var resToken = CommonFunctions.isValidToken(bearer_token);
            if (resToken.ResponseData)
            {
                return _fileLibrary.GetSearchFileLists(fileType, iUserId, userEmail);
            }
            else
            {
                return new ResponseModel<List<SearchFiles>>() { IsSuccess = false, Message = "Invalid Token", StatusCode = System.Net.HttpStatusCode.Unauthorized };
            }
        }
    }
}
