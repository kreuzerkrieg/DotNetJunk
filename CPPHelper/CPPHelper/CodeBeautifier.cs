using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace CPPHelper
{
    class CodeBeautifier
    {
        private Logger mLogger;
        public CodeBeautifier(Logger logger)
        {
            mLogger = logger;
        }
        internal void Beautify(ProjectItem oItem)
        {
            TextSelection selection = (TextSelection)oItem.Document.Selection;
            selection.SelectAll();
            String source = selection.Text;
            AStyleInterface AStyle = new AStyleInterface();
            String formattedSource = AStyle.FormatSource(source, "mode=c -A1 -C -w -f -p -U -xd -j -k1 -W1");
            if (formattedSource == String.Empty)
            {
                throw new Exception("Cannot format " + oItem.Name);
            }
            selection.Insert(formattedSource);
            oItem.Document.Save();
        }
    }
}
