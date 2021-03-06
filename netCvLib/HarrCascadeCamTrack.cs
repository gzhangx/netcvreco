﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace netCvLib
{
    public class HarrCascadeCamTrack : ICamTrackable
    {
        GZHarC haar = new GZHarC();
        System.Drawing.Rectangle[] results;
        System.Drawing.Rectangle result = new System.Drawing.Rectangle();
        public void CamTracking(Mat curImg, VidLoc.RealTimeTrackLoc realTimeTrack, PreVidStream vidProvider, IDriver driver, BreakDiffDebugReporter debugReporter)
        {
            debugReporter.ReportInProcessing(true);
            results = haar.Detect(curImg);
            result.Width = 0;
            result.Height = 0;
            if (results != null && results.Length > 0)
            {
                results = results.OrderByDescending(r => r.Width * r.Height).ToArray();
                result = results[0];
            }
            debugReporter.ReportInProcessing(false);
            realTimeTrack.CurPos = 0;            
            DiffVect vect = new DiffVect();
            vect.Vector = realTimeTrack.vect;
            debugReporter.ReportStepChanges(new StepChangeReporter(curImg, results, result), vect);            
            if (result.Width == 0)
            {
                //debugReporter.InfoReport($"NA");
                driver.Stop();
            }
            else
            {                
                double diff = (curImg.Width / 2) - ((result.X + result.Width) / 2);
                //debugReporter.InfoReport($"{(diff > 0? "L":"R") } diff {diff.ToString("0.0")} imw ${((result.X + result.Width) / 2)}");
                realTimeTrack.vect = new DiffVector(diff, 0, 0);
                driver.Track(realTimeTrack);
            }            
        }

        
    }

    class StepChangeReporter : ICanShowStepChange
    {
        Mat input;
        System.Drawing.Rectangle[] results;
        System.Drawing.Rectangle result;
        public StepChangeReporter(Mat origImg, System.Drawing.Rectangle[] ress, System.Drawing.Rectangle res)
        {
            input = origImg.Clone();
            results = ress;
            result = res;
        }
        public Mat ShowAllStepChange(DiffVect vect)
        {            
            foreach (var rect in results)
            {
                CvInvoke.Rectangle(input, rect, new MCvScalar(100));
            }
            if (result.Width != 0)
            {
                CvInvoke.Rectangle(input, result, new MCvScalar(200));
            }
            return input;
        }
    }
}
