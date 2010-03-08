﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Syncless.Core
{
    public interface IOriginsFinder
    {
        List<string> GetOrigins(string path);
    }
}