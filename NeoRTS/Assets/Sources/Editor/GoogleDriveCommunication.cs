
using Google.Apis.Services;
using Google.Apis.Discovery.v1;
using UnityEngine;
using Google.Apis.Discovery.v1.Data;
using UnityEditor;
using Google.Apis.Drive.v3;
using System.IO;
using Google.Apis.Upload;
using Google.Apis.Auth.OAuth2;
using System.Threading;

namespace NeoRTS
{
    namespace EditorTools
    {

        namespace GoogleDriveExtension
        {
            public static class GoogleDriveCommunication
            {
                static private DriveService m_googleDriveService;
                static private UserCredential m_credentials;

                static async public void InitializeDriveService()
                {
                    string path;

                    if (PlayerPrefs.HasKey("GOOGLE_SECRETS_PATH"))
                    {
                        path = PlayerPrefs.GetString("GOOGLE_SECRETS_PATH");
                    }
                    else
                        path = EditorUtility.OpenFilePanel("Credentials file", Application.dataPath, "json");

                    if (path.Length == 0) return;

                    FileStream credentialsFileStream = File.OpenRead(path);

                    m_credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(credentialsFileStream, new[]
                    {
                        DriveService.Scope.Drive
                    }, "Marc Jacob", CancellationToken.None);


                    m_googleDriveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = m_credentials,
                        ApplicationName = "Neo RTS"
                    });

                    PlayerPrefs.SetString("GOOGLE_SECRETS_PATH", path);
                }

                static async public void UploadFile(string path, string uploadName, string contentType)
                {
                    FileStream file = File.OpenRead(path);
                    if (file.CanRead == false) throw new System.Exception("Error : Cannot read file.");

                    var filesListRequest = m_googleDriveService.Files.List();
                    var filesList = filesListRequest.Execute();

                    string fileID = "";
                    foreach(var f in filesList.Files)
                    {
                        if (f.Name == uploadName)
                        {
                            fileID = f.Id;
                        }
                    }

                    if (fileID.Length == 0)
                    {
                        Debug.LogError("FILE NOT FOUND ONLINE.");
                        return;
                    }

                    var upload = m_googleDriveService.Files.Update(new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = uploadName,
                    },
                    fileID,
                    file,
                    contentType);

                    upload.ProgressChanged += (progress) => OnUploadProgress(progress, file.Length);
                    upload.ResponseReceived += OnUploadResponseReceived;

                    var task = upload.UploadAsync();
                    task.ContinueWith((t) => { file.Dispose(); });
                }

                static private void OnUploadProgress(IUploadProgress uploadProgress, long bytesToUpload)
                {
                    switch(uploadProgress.Status)
                    {
                        case (UploadStatus.Uploading):
                            Debug.Log("Uploading... Progress : " + uploadProgress.BytesSent + " / " + bytesToUpload);
                            break;
                        case (UploadStatus.Failed):
                            Debug.Log("Upload failed." + uploadProgress.Exception);
                            break;
                    }
                }

                static private void OnUploadResponseReceived(Google.Apis.Drive.v3.Data.File file)
                {
                    Debug.Log("File '" + file.Name + "' uploaded successfully.");
                }

                [MenuItem("Tools/Google Drive Extension/Upload Latest Client Build")]
                static public void UploadLatestClientBuild()
                {
                    if (m_googleDriveService == null) InitializeDriveService();
                    UploadFile(Application.dataPath + "/../Builds/NeoRTS-Client.zip", "NeoRTS.zip", "application/file");
                }
            }
        }
    }
}