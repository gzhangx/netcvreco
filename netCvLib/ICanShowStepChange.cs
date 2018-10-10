﻿using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netCvLib
{
    public interface ICanShowStepChange
    {
        Mat ShowAllStepChange(DiffVect vect);
    }
}
