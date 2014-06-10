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

        public GoogleDriveStorage()
        {
            ClientId = "929945743179-l1ut4m2r3untdolncrgu6tf6vlsccjcl.apps.googleusercontent.com";
            ClientSecret = "W7iNoZwX1Wl4WS4HBP5B3akU";
        }

        public GoogleDriveStorage(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = ClientSecret;
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
        }

        public void Update(Snippet snippet)
        {
            var allFiles = GetAllNotTrashedFiles();
            var fileToUpdate = allFiles.FirstOrDefault(file => file.Title.Equals(snippet.Name, StringComparison.OrdinalIgnoreCase));

            var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(snippet.Content));
            var update = Service.Files.Update(fileToUpdate, fileToUpdate.Id, contentStream, FileMimeType);
            update.NewRevision = true;

            update.UploadAsync();
        }

        public void RemoveSnippet(Snippet snippet)
        {
            var allFiles = GetAllNotTrashedFiles();
            var fileToRemove = allFiles.FirstOrDefault(file => file.Title.Equals(snippet.Name, StringComparison.OrdinalIgnoreCase));

            Service.Files.Delete(fileToRemove.Id).Execute();
        }

        public void UploadFiles(List<Snippet> fileList)
        {
            foreach (var file in fileList)
            {
                CreateFile(file.Name, file.Content);
            }
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
