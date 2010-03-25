﻿using System;
using Syncless.Helper;

namespace Syncless.CompareAndSync.Exceptions
{
    public class MoveFolderException : ApplicationException
    {
        public MoveFolderException(Exception innerException)
            : base(ErrorMessage.CAS_UNABLE_TO_MOVE_FOLDER_EXCEPTION, innerException)
        {
        }
    }
}
