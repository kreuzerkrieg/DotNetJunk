using System;
using System.Collections.Generic;
using System.Text;

namespace CPPHelper
{
    public interface Logger
    {
        void OpenLog();
        void PrintMessage(Object oMessage);
        void PrintHeaderMessage(Object oMessage);
        void CloseLog();
    }
}
