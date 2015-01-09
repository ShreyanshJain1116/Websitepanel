﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using WebsitePanel.WebDav.Core.Client;
using WebsitePanel.WebDavPortal.Config;
using WebsitePanel.WebDavPortal.Exceptions;
using WebsitePanel.Portal;
using WebsitePanel.Providers.OS;
using Ninject;
using WebsitePanel.WebDavPortal.DependencyInjection;

namespace WebsitePanel.WebDavPortal.Models
{
    public class WebDavManager : IWebDavManager
    {
        private readonly WebDavSession _webDavSession = new WebDavSession();
        private IList<SystemFile> _rootFolders;
        private int _itemId;
        private IFolder _currentFolder;
        private string _organizationName;
        private string _webDavRootPath;
        private bool _isRoot = true;

        public string RootPath 
        {
            get { return _webDavRootPath; }
        }

        public string OrganizationName
        {
            get { return _organizationName; }
        }

        public WebDavManager(NetworkCredential credential, int itemId)
        {
            _webDavSession.Credentials = credential;
            _itemId = itemId;
            IKernel _kernel = new StandardKernel(new NinjectSettings { AllowNullInjection = true }, new WebDavExplorerAppModule());
            var accountModel = _kernel.Get<AccountModel>();
            _rootFolders = ConnectToWebDavServer(accountModel.UserName);

            if (_rootFolders.Any())
            {
                var folder = _rootFolders.First();
                var uri = new Uri(folder.Url);
                _webDavRootPath = uri.Scheme + "://" + uri.Host + uri.Segments[0] + uri.Segments[1];
                _organizationName = uri.Segments[1].Trim('/');
            }
        }

        public void OpenFolder(string pathPart)
        {
            if (string.IsNullOrWhiteSpace(pathPart))
            {
                _isRoot = true;
                return;
            }
            _isRoot = false;
            _currentFolder = _webDavSession.OpenFolder(_webDavRootPath + pathPart);
        }

        public IEnumerable<IHierarchyItem> GetChildren()
        {
            IHierarchyItem[] children;

            if (_isRoot)
            {
                children = _rootFolders.Select(x => new WebDavHierarchyItem {Href = new Uri(x.Url), ItemType = ItemType.Folder}).ToArray();
            }
            else
	        {
                children = _currentFolder.GetChildren();
	        }

            List<IHierarchyItem> sortedChildren = children.Where(x => x.ItemType == ItemType.Folder).OrderBy(x => x.DisplayName).ToList();
            sortedChildren.AddRange(children.Where(x => x.ItemType != ItemType.Folder).OrderBy(x => x.DisplayName));

            return sortedChildren;
        }

        public bool IsFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) | _currentFolder == null)
                return false;

            try
            {
                IResource resource = _currentFolder.GetResource(fileName);
                //Stream stream = resource.GetReadStream();
                return true;
            }
            catch (InvalidOperationException)
            {
            }
            return false;
        }

        public byte[] GetFileBytes(string fileName)
        {
            try
            {
                IResource resource = _currentFolder.GetResource(fileName);
                Stream stream = resource.GetReadStream();
                byte[] fileBytes = ReadFully(stream);
                return fileBytes;
            }
            catch (InvalidOperationException exception)
            {
                throw new ResourceNotFoundException("Resource not found", exception);
            }
        }

        public string GetFileUrl(string fileName)
        {
            try
            {
                IResource resource = _currentFolder.GetResource(fileName);
                return resource.Href.ToString();
            }
            catch (InvalidOperationException exception)
            {
                throw new ResourceNotFoundException("Resource not found", exception);
            }
        }

        private IList<SystemFile> ConnectToWebDavServer(string userName)
        {
            var rootFolders = new List<SystemFile>();
            foreach (var folder in ES.Services.EnterpriseStorage.GetEnterpriseFolders(_itemId))
            {
                var permissions = ES.Services.EnterpriseStorage.GetEnterpriseFolderPermissions(_itemId, folder.Name);
                if (permissions.Any(x => x.DisplayName == userName))
                    rootFolders.Add(folder);
            }
            return rootFolders;
        }

        private byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16*1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }
    }
}