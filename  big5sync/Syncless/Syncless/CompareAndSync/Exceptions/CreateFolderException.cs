﻿using System;
using Syncless.Helper;

namespace Syncless.CompareAndSync.Exceptions
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateFolderException : ApplicationException
    {
        public CreateFolderException(Exception innerException)
            : base(ErrorMessage.CAS_UNABLE_TO_CREATE_FOLDER_EXCEPTION, innerException)
        {
        }
    }
}
