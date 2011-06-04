using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace CodeOrganizer
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
                mPane = mOutputWin.OutputWindowPanes.Item("Helper Output");
            }
            catch (Exception)
            {
                mPane = mOutputWin.OutputWindowPanes.Add("Helper Output");
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

            mOutputWin.Parent.Activate();
            mPane.Activate();
            mPane.OutputString(Environment.NewLine + "=================================..:: " + oMessage + " ::.=================================" + Environment.NewLine);
        }

        public void CloseLog()
        {
        }
    }
}
