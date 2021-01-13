using System.IO;
using System.IO.Compression;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NeoRTS
{
    namespace EditorTools
    {
        public class PostBuildProcessorUpload : IPostprocessBuildWithReport
        {

            public int callbackOrder => 0;

            public void OnPostprocessBuild(BuildReport report)
            {
                string zipFileName = report.summary.outputPath.Replace("/NeoRTS.exe", "") + "/../NeoRTS-Client.zip";

                if (File.Exists(zipFileName))
                {
                    File.Delete(zipFileName);
                }

                string directory = report.summary.outputPath.Replace("NeoRTS.exe", "");
                ZipFile.CreateFromDirectory(directory, directory + "../NeoRTS-Client.zip");

                GoogleDriveExtension.GoogleDriveCommunication.UploadLatestClientBuild();
            }
        }
    }
}