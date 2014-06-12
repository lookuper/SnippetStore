using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using SnippetStore.BusinessLogic;
using GoogleFile = Google.Apis.Drive.v2.Data.File;

namespace SnippetStore
{
    public sealed class GoogleDriveStorage
    {
        private String FolderMimeType = "application/vnd.google-apps.folder";
        private String FileMimeType = "text/plain";
        private GoogleFile _lookuperFolderLink;
        private GoogleFile LookuperFolderLink
        {
            get
            {
                if (_lookuperFolderLink == null)
                {
                    var allFiles = GetAllNotTrashedFiles();

                    var lookuperFolder = allFiles
                        .FirstOrDefault(item => item.Title.Equals(StoreFolderName, StringComparison.OrdinalIgnoreCase));

                    _lookuperFolderLink = lookuperFolder;
                }

                return _lookuperFolderLink;
            }
        }

        public String ClientId { get; private set; }
        public String ClientSecret { get; private set; }
        public UserCredential UserCredentials { get; private set; }
        public String StoreFolderName { get { return "SnippetStoreFolder"; } }
        public DriveService Service
        {
            get
            {
                var secrects = new ClientSecrets
                {
                    ClientId = this.ClientId,
                    ClientSecret = this.ClientSecret,
                };

                UserCredentials = GoogleWebAuthorizationBroker.AuthorizeAsync(secrects, new[] { DriveService.Scope.Drive }, "user", CancellationToken.None).Result;

                return new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = UserCredentials,
                    ApplicationName = "SkySnippetStore",
                });
            }
        }

        public GoogleDriveStorage(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public IList<GoogleFile> GetAllNotTrashedFiles()
        {
            var request = Service.Files.List();
            request.Q = "trashed=false";

            return request.Execute().Items;
        }

        public IList<GoogleFile> GetLookuperFiles()
        {
            var allFiles = GetAllNotTrashedFiles();
            var children = GetNotTrashedChildrens(LookuperFolderLink.Id);

            var lookuperFiles = from file in allFiles
                       join child in children 
                             on file.Id equals child.Id
                       select file;

            return lookuperFiles.ToList();
        }

        public IList<ChildReference> GetNotTrashedChildrens(string parentId)
        {
            var request = Service.Children.List(parentId);
            request.Q = "trashed=false";

            return request.Execute().Items;
        }

        public void CreateInfustructure()
        {
            if (LookuperFolderLink == null)
            {
                CreateFolder(StoreFolderName);
            }
        }

        private void CreateFolder(string folderName)
        {
            var fileOrFolderToCreate = new Google.Apis.Drive.v2.Data.File
            {
                Title = folderName,
                MimeType = FolderMimeType,
            };

            Service.Files.Insert(fileOrFolderToCreate).Execute();
        }

        public void CreateFile(string fileName, string content)
        {
            if (LookuperFolderLink == null)
                CreateFolder(StoreFolderName);

            var parentRef = new ParentReference { Id = LookuperFolderLink.Id };

            var fileToCreate = new GoogleFile
            {
                Title = fileName,
                MimeType = FileMimeType,
                FileExtension = "csharp",
                Parents = new[] { parentRef },
            };

            var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var responce = Service.Files.Insert(fileToCreate, contentStream, FileMimeType).Upload();
            UpdateETags();
        }

        public void Update(Snippet snippet)
        {
            var allFiles = GetAllNotTrashedFiles();
            var fileToUpdate = allFiles.FirstOrDefault(file => file.Title.Equals(snippet.Name, StringComparison.OrdinalIgnoreCase));

            if (fileToUpdate == null)
            {
                CreateFile(snippet.Name, snippet.Content);
                return;
            }

            var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(snippet.Content));
            var update = Service.Files.Update(fileToUpdate, fileToUpdate.Id, contentStream, FileMimeType);
            update.NewRevision = true;

            update.UploadAsync();
            UpdateETags();
        }

        public void RemoveSnippet(Snippet snippet)
        {
            var allFiles = GetAllNotTrashedFiles();
            var fileToRemove = allFiles.FirstOrDefault(file => file.Title.Equals(snippet.Name, StringComparison.OrdinalIgnoreCase));

            if (fileToRemove == null)
                return;

            Service.Files.Delete(fileToRemove.Id)
                .ExecuteAsync();

            UpdateETags();
        }

        public void UploadFiles(List<Snippet> fileList)
        {
            foreach (var file in fileList)
            {
                CreateFile(file.Name, file.Content);
            }
        }

        private void UpdateETags()
        {
            SnippetStore.Properties.Settings.Default.ETags = String.Empty;
            ETagsCheck();
        }

        private string ETagsCheck()
        {
            if (String.IsNullOrEmpty(SnippetStore.Properties.Settings.Default.ETags))
            {
                var allFiles = GetAllNotTrashedFiles();
                StringBuilder toSerialization = new StringBuilder();

                foreach (var file in allFiles)
                {
                    toSerialization.AppendFormat("{0}:{1},", file.Title, file.ETag);
                }

                SnippetStore.Properties.Settings.Default.ETags = toSerialization.ToString();
            }

            return SnippetStore.Properties.Settings.Default.ETags;
        }

        public List<GoogleFile> GetUpdateFilesFromGoogleDrive()
        {
            var originalFiles = ETagsCheck();
            var allFiles = GetAllNotTrashedFiles();
            var updatedFiles = new List<GoogleFile>();

            foreach (var bucket in originalFiles.Split(','))
            {
                if (String.IsNullOrEmpty(bucket))
                    continue;

                var token = bucket.Split(':');
                var name = token.ElementAtOrDefault(0);
                var etag = token.ElementAtOrDefault(1);

                var gFile = allFiles.FirstOrDefault(item => item.Title.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && !item.MimeType.Equals(FolderMimeType));

                if (gFile != null)
                {
                    if (!gFile.ETag.Equals(etag, StringComparison.OrdinalIgnoreCase))
                        updatedFiles.Add(gFile);
                }
            }

            if (updatedFiles.Count != 0)
                UpdateETags();

            return updatedFiles;
        }

        public IList<Snippet> DownloadFiles(List<GoogleFile> files)
        {
            var localService = Service;
            var snippets = new List<Snippet>();

            foreach (var file in files)
            {
                var stream = localService.HttpClient.GetStreamAsync(file.DownloadUrl);
                var content = new System.IO.StreamReader(stream.Result).ReadToEnd();

                var snippet = new Snippet
                {
                    Name = file.Title,
                    Content = content,
                };

                snippets.Add(snippet);
            }
            return snippets;
        }

        private IList<Snippet> DownloadFilesFromLookuperFolder()
        {
            var localService = Service;

            var allFiles = GetAllNotTrashedFiles();
            var avaliableSnippets = GetNotTrashedChildrens(LookuperFolderLink.Id);

            var filesToDownload = new List<GoogleFile>();
            var snippets = new List<Snippet>();

            foreach (var snippetFile in avaliableSnippets)
            {
                var mappedFile = allFiles.FirstOrDefault(item => item.Id == snippetFile.Id);

                if (mappedFile == null || mappedFile.DownloadUrl == null)
                    continue;

                var stream = localService.HttpClient.GetStreamAsync(mappedFile.DownloadUrl);
                var content = new System.IO.StreamReader(stream.Result).ReadToEnd();

                var snippet = new Snippet
                {
                    Name = mappedFile.Title,
                    Content = content,
                };

                snippets.Add(snippet);
            }

            return snippets;
        }
    }
}
