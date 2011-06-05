using System;
using System.Collections.Generic;
using System.Text;

namespace CodeOrganizer
{
    public interface Logger
    {
        void OpenLog();
        void PrintMessage(Object oMessage);
        void PrintHeaderMessage(Object oMessage);
        void CloseLog();
    }
}
