﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Syncless.Monitor;
using Syncless.CompareAndSync;
namespace Syncless.Core
{
    public interface IMonitorControllerInterface
    {
        void HandleFileChange(FileChangeEvent fe);
        void HandleFolderChange(FolderChangeEvent fe);
        void HandleDriveChange(DriveChangeEvent dce);
    }
}
