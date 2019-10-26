using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MsgNThen.Adapter
{
    public class FormFileCollection : List<IFormFile>, IFormFileCollection, IReadOnlyList<IFormFile>, IEnumerable<IFormFile>, IEnumerable, IReadOnlyCollection<IFormFile>
    {
        public IFormFile this[string name]
        {
            get
            {
                return this.GetFile(name);
            }
        }

        public IFormFile GetFile(string name)
        {
            foreach (IFormFile formFile in (List<IFormFile>)this)
            {
                if (string.Equals(name, formFile.Name, StringComparison.OrdinalIgnoreCase))
                    return formFile;
            }
            return (IFormFile)null;
        }

        public IReadOnlyList<IFormFile> GetFiles(string name)
        {
            List<IFormFile> formFileList = new List<IFormFile>();
            foreach (IFormFile formFile in (List<IFormFile>)this)
            {
                if (string.Equals(name, formFile.Name, StringComparison.OrdinalIgnoreCase))
                    formFileList.Add(formFile);
            }
            return (IReadOnlyList<IFormFile>)formFileList;
        }
    }
}