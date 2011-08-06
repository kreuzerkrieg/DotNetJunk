using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace CPPHelper
{
    class OutputWindowLogger : Logger
    {
        private DTE2 mApplication;
        private OutputWindow mOutputWin;
        private OutputWindowPane mPane;
        public OutputWindowLogger(DTE2 oApplication)
        {
            mApplication = oApplication;
        }
        public void OpenLog()
        {
            mOutputWin = mApplication.ToolWindows.OutputWindow;
            try
            {
                mPane = mOutputWin.OutputWindowPanes.Item("C++ Helper Output");
            }
            catch (Exception)
            {
                mPane = mOutputWin.OutputWindowPanes.Add("C++ Helper Output");
            }
        }

        public void PrintMessage(Object oMessage)
        {
            
            mOutputWin.Parent.Activate();
            mPane.Activate();
            mPane.OutputString(oMessage + Environment.NewLine);
        }

        public void PrintHeaderMessage(Object oMessage)
        {
            PrintMessage(Environment.NewLine + "=================================..:: " + oMessage + " ::.=================================" + Environment.NewLine);
        }

        public void CloseLog()
        {
        }
    }
}
