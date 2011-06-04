using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Security;
using System.Runtime.ExceptionServices;
using System.Diagnostics;

namespace BlackSEOps
{
    class wininetWrapper
    {
        private Int32 iProxyPos;

        public struct Struct_INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        };

        public struct Struct_Internet_Option_Suppress_Behavior
        {
            public int dwBehaviorToSuppress;
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        public Boolean RefreshIEProxySettings()
        {
            System.Windows.Forms.WebBrowser oBrowser = new System.Windows.Forms.WebBrowser();
            oBrowser.ScriptErrorsSuppressed = true;
            CountedWait oCWTimer = new CountedWait(ref oBrowser, 4000);
            while (true)
            {
                try
                {
                    const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 95;
                    const int INTERNET_OPTION_PROXY = 38;
                    const int INTERNET_OPEN_TYPE_PROXY = 3;

                    Struct_INTERNET_PROXY_INFO struct_IPI;
                    if (iProxyPos >= BlackSEOps.Properties.Settings.Default.Proxies.Count)
                        iProxyPos = 0;
                    String sProxy = BlackSEOps.Properties.Settings.Default.Proxies[iProxyPos];
                    iProxyPos++;
                    // Filling in structure 
                    struct_IPI.dwAccessType = INTERNET_OPEN_TYPE_PROXY;
                    struct_IPI.proxy = Marshal.StringToHGlobalAnsi(sProxy);
                    struct_IPI.proxyBypass = Marshal.StringToHGlobalAnsi("local");

                    // Allocating memory 
                    IntPtr intptrStruct = Marshal.AllocCoTaskMem(Marshal.SizeOf(struct_IPI));

                    // Converting structure to IntPtr 
                    Marshal.StructureToPtr(struct_IPI, intptrStruct, true);

                    InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY, intptrStruct, Marshal.SizeOf(struct_IPI));
                    Marshal.FreeHGlobal(struct_IPI.proxy);
                    Marshal.FreeHGlobal(struct_IPI.proxyBypass);
                    Marshal.FreeCoTaskMem(intptrStruct);
                    InternetSetOption(IntPtr.Zero, INTERNET_OPTION_PROXY_SETTINGS_CHANGED, IntPtr.Zero, 0);

                    oBrowser.Navigate("http://kreuzerkrieg.dyndns.info/SiteMap.xml");
                    if (oCWTimer.Wait(15))
                    {
                        return true;
                    }
                    else
                    {
                        //System.Console.WriteLine("Proxy \"" + sProxy + "\" appears to be dead trying next one.");
                    }
                }
                catch (UnauthorizedAccessException uex)
                {
                    throw new UnauthorizedAccessException("RefreshIEProxySettings thrown an exception", uex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("RefreshIEProxySettings has thrown an exception", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("RefreshIEProxySettings thrown an exception", ex);
                }
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        public bool ClearIESession()
        {
            bool bRetVal = false;
            try
            {
                const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
                const int INTERNET_OPTION_RESET_URLCACHE_SESSION = 60;
                const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;
                const int INTERNET_SUPPRESS_COOKIE_PERSIST = 3;
                bRetVal = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
                bRetVal &= InternetSetOption(IntPtr.Zero, INTERNET_OPTION_RESET_URLCACHE_SESSION, IntPtr.Zero, 0);

                Struct_Internet_Option_Suppress_Behavior struct_IOSB;
                struct_IOSB.dwBehaviorToSuppress = INTERNET_SUPPRESS_COOKIE_PERSIST;
                // Allocating memory 
                IntPtr intptrStruct = Marshal.AllocCoTaskMem(Marshal.SizeOf(struct_IOSB));

                // Converting structure to IntPtr 
                Marshal.StructureToPtr(struct_IOSB, intptrStruct, true);
                bRetVal &= InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, intptrStruct, Marshal.SizeOf(struct_IOSB));
                Marshal.FreeCoTaskMem(intptrStruct);
            }
            catch (UnauthorizedAccessException uex)
            {
                throw new UnauthorizedAccessException("ClearIESession thrown an exception", uex);
            }
            catch (AccessViolationException ex)
            {
                throw new AccessViolationException("ClearIESession has thrown an exception", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("ClearIESession thrown an exception", ex);
            }
            return bRetVal;
        }
    }

    class SiteCrawler
    {
        private wininetWrapper oWININET;
        public SiteCrawler(wininetWrapper oWinInet)
        {
            oWININET = oWinInet;
        }

        public Dictionary<String, HashSet<String>> getSiteText(ref System.Windows.Forms.WebBrowser oBrowser, String sURL)
        {
            Dictionary<String, HashSet<String>> oSiteText = new Dictionary<string, HashSet<string>>();
            XmlReader oReader = new XmlTextReader(sURL);
            WebDocumentLoader oDocLoader = new WebDocumentLoader(oWININET);
            while (oReader.ReadState != ReadState.EndOfFile)
            {
                if (oReader.ReadToFollowing("url"))
                {
                    if (oReader.ReadToFollowing("loc"))
                    {
                        String sPageUrl = oReader.ReadElementContentAsString();
                        HtmlDocument oDocument = oDocLoader.loadDocument(ref oBrowser, sPageUrl);
                        HashSet<String> oPageText = retrievePageText(oDocument);
                        oSiteText.Add(sPageUrl, oPageText);
                    }
                }
            }
            return oSiteText;
        }

        private HashSet<String> retrievePageText(HtmlDocument oDocument)
        {
            HashSet<String> sWords = new HashSet<string>();
            HtmlElementCollection oElements = oDocument.All;
            String sText = "";
            for (int i = 0; i < oElements.Count; i++)
            {
                HtmlElement oElement = oElements[i];
                sText += " " + oElement.InnerText;
            }
            string[] sTokens = sText.Split(null);
            for (int i = 0; i < sTokens.Length; i++)
            {
                Regex oRegexp = new Regex(@"[A-Za-z]+[A-Za-z0-9]*");
                Match oResults = oRegexp.Match(sTokens[i]);
                if (oResults.Success)
                {
                    for (int j = 0; j < oResults.Captures.Count; j++)
                    {
                        String sToken = oResults.Captures[j].Value.ToUpperInvariant();
                        if (sToken.Length > 2 && !sWords.Contains(sToken))
                        {
                            sWords.Add(sToken);
                        }
                    }
                }
            }
            return sWords;
        }
    }

    class WebDocumentLoader
    {
        private wininetWrapper oWININET;

        public WebDocumentLoader(wininetWrapper oWinInet)
        {
            oWININET = oWinInet;
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        public HtmlDocument loadDocument(ref System.Windows.Forms.WebBrowser oBrowser, String sURL)
        {
            CountedWait oCWTimer = new CountedWait(ref oBrowser, 3000);
            while (true)
            {
                try
                {
                    oBrowser.Navigate(sURL);
                    if (oCWTimer.Wait(10))
                    {
                        return oBrowser.Document;
                    }
                    else
                    {
                        //System.Console.WriteLine("Proxy timeout, switching to next one.");
                        oWININET.RefreshIEProxySettings();
                    }
                }
                catch (UnauthorizedAccessException uex)
                {
                    throw new UnauthorizedAccessException("..::" + this.GetType().Name + "::..loadDocument has thrown an exception.", uex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("..::" + this.GetType().Name + "::..loadDocument has thrown an exception.", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..loadDocument thrown an exception, reason:  " + ex.Message + "\n" + ex.StackTrace + ", switching proxy");
                    oWININET.RefreshIEProxySettings();
                }
            }
        }
    }

    class TimedWait
    {
        private Boolean bWaitEnded = false;
        System.Timers.Timer oTimer;

        public TimedWait(Int32 iMilliseconds)
        {
            oTimer = new System.Timers.Timer(iMilliseconds);
        }

        public void Wait()
        {
            bWaitEnded = false;
            oTimer.Elapsed += OnTimedWaitEvent;
            oTimer.Start();
            while (!bWaitEnded)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
            oTimer.Stop();
            oTimer.Elapsed -= OnTimedWaitEvent;
        }

        private void OnTimedWaitEvent(object source, ElapsedEventArgs e)
        {
            bWaitEnded = true;
        }
    }

    class CountedWait
    {
        private WebBrowser Browser;
        private System.Timers.Timer oTimer;
        private Boolean bDocumentLoaded;
        private Int32 iCycles;
        public CountedWait(ref WebBrowser oBrowser, Int32 iMilliseconds)
        {
            oTimer = new System.Timers.Timer(iMilliseconds);
            Browser = oBrowser;
            bDocumentLoaded = false;
        }
        ~CountedWait()
        {
            if (!Browser.IsDisposed)
            {
                Browser.DocumentCompleted -= cwDocumentOnLoad;
                Browser.NewWindow -= cwOpenNewWindow;
            }
            oTimer.Elapsed -= cwOnTimedWaitEvent;
        }

        public Boolean Wait(Int32 CyclesToWait)
        {
            iCycles = 0;
            bDocumentLoaded = false;
            oTimer.Elapsed += cwOnTimedWaitEvent;
            oTimer.Start();
            Browser.DocumentCompleted += cwDocumentOnLoad;
            Browser.NewWindow += cwOpenNewWindow;
            while (!bDocumentLoaded && iCycles < CyclesToWait)
            {
                Application.DoEvents();
            }
            oTimer.Elapsed -= cwOnTimedWaitEvent;
            oTimer.Stop();
            Browser.DocumentCompleted -= cwDocumentOnLoad;
            Browser.NewWindow -= cwOpenNewWindow;
            return bDocumentLoaded;
        }

        protected void cwDocumentOnLoad(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            bDocumentLoaded = true;
            return;
        }

        private void cwOnTimedWaitEvent(object source, ElapsedEventArgs e)
        {
            iCycles++;
            return;
        }

        protected void cwOpenNewWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            return;
        }
    }

    abstract class FatherOfBlackSEOps
    {
        protected Boolean bDocumentLoaded = false;
        protected Boolean bSiteFound = false;
        protected Int32 iTokens = 2;
        protected Int32 iCounter;
        protected String sSiteToNavigate;
        protected Int32 iErrorCount = 10;

        protected wininetWrapper wininet;
        protected Random oRandomizer;
        protected WebDocumentLoader oDocLoader;

        public FatherOfBlackSEOps()
        {
            wininet = new wininetWrapper();
            oRandomizer = new Random();
            oDocLoader = new WebDocumentLoader(wininet);
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        public void StartAbuse(object arg)
        {
            Int32 iErrCount = 0;
            try
            {
                HashSet<String> oPageText = (HashSet<String>)arg;
                Int32 iStartI = (Int32)BlackSEOps.Properties.Settings.Default["iCount4" + this.GetType().Name];
                Int32 iStartJ = (Int32)BlackSEOps.Properties.Settings.Default["jCount4" + this.GetType().Name];
                System.Windows.Forms.WebBrowser oBrowser = new System.Windows.Forms.WebBrowser();

                for (int i = iStartI; i < oPageText.Count - iTokens; i++)
                {
                    try
                    {
                        bDocumentLoaded = false;
                        String sWords = "";
                        for (int l = 0; l < iTokens; l++)
                        {
                            sWords += " " + oPageText.ToArray<String>()[i + l];
                        }
                        String sBuzzWord = (String)BlackSEOps.Properties.Settings.Default["BuzzWords"];
                        String sQuery = (sWords.ToLowerInvariant() + " " + sBuzzWord).Trim();

                        wininet.ClearIESession();
                        wininet.RefreshIEProxySettings();

                        oBrowser = new System.Windows.Forms.WebBrowser();
                        GC.Collect();
                        oBrowser.ScriptErrorsSuppressed = true;
                        oBrowser.DocumentCompleted += DocumentOnLoad;
                        oBrowser.NewWindow += OpenNewWindow;
                        Go2FormNSubmitQuery(ref oBrowser, sQuery);

                        ProcessResultPage(ref oBrowser, sQuery);

                        oBrowser.DocumentCompleted -= DocumentOnLoad;
                        oBrowser.NewWindow -= OpenNewWindow;
                        if (!bSiteFound)
                        {
                            System.Console.WriteLine("..::" + this.GetType().Name + "::..Query \"" + sQuery + "\" NOT found!");
                        }

                        // Successful cycle, we count only sequences of errors
                        iErrCount = 0;
                    }
                    catch (AccessViolationException ex)
                    {
                        System.Console.WriteLine("..::" + this.GetType().Name + "::.. StartAbuse() has thrown an exception. Reason: " + ex.Message + "\n" + ex.StackTrace + "\nStarting over...");
                        //die in solitude
                    }
                    catch (UnauthorizedAccessException uex)
                    {
                        //clean and start over
                        System.Console.WriteLine("..::" + this.GetType().Name + "::.. StartAbuse() has thrown an exception. Reason: " + uex.Message + "\n" + uex.StackTrace + "\nStarting over...");
                    }
                    catch (Exception ex)
                    {
                        //die in solitude
                        System.Console.WriteLine("..::" + this.GetType().Name + "::.. StartAbuse() has thrown an exception. Reason: " + ex.Message + "\n" + ex.StackTrace + "\nStarting thread...");
                    }
                    finally
                    {
                        iErrCount++;
                        if (!oBrowser.IsDisposed)
                        {
                            oBrowser.DocumentCompleted -= DocumentOnLoad;
                            oBrowser.NewWindow -= OpenNewWindow;
                            oBrowser.Dispose();
                        }
                        if (iErrCount > iErrorCount)
                            throw new Exception("Too much errors for one thread, aborting...");
                    }
                }
            }
            catch (AccessViolationException ex)
            {
                //die in solitude
                System.Console.WriteLine("..::" + this.GetType().Name + "::.. abuse has thrown an exception. Reason: " + ex.Message + "\n" + ex.StackTrace + "\nAborting thread...");
                return;
            }
            catch (Exception ex)
            {
                //die in solitude
                System.Console.WriteLine("..::" + this.GetType().Name + "::.. abuse has thrown an exception. Reason: " + ex.Message + "\n" + ex.StackTrace + "\nAborting thread...");
                return;
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        protected void NavigateToSite(HtmlElement oLink, ref WebBrowser oBrowser)
        {
            CountedWait oCWTimer = new CountedWait(ref oBrowser, 3000);
            while (true)
            {
                try
                {
                    sSiteToNavigate = oLink.GetAttribute("href");
                    oLink.InvokeMember("click");
                    if (oCWTimer.Wait(10))
                    {
                        bSiteFound = true;
                        TimedWait oTimedWait = new TimedWait(oRandomizer.Next(10000, 30000));
                        oTimedWait.Wait();
                        return;
                    }
                    else
                    {
                        //System.Console.WriteLine("Proxy timeout, switching to next one.");
                        wininet.RefreshIEProxySettings();
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("..::" + this.GetType().Name + "::..NavigateToSite thrown an exception", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("..::" + this.GetType().Name + "::..NavigateToSite thrown an exception", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..NavigateToSite thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        protected void GoToNextPage(HtmlElement oLink, ref WebBrowser oBrowser)
        {
            CountedWait oCWTimer = new CountedWait(ref oBrowser, 3000);
            while (true)
            {
                try
                {
                    // Below line added to rise UnauthorizedAccessException in case something went wrong
                    String sHREF = oLink.GetAttribute("href");

                    oLink.InvokeMember("click");
                    if (oCWTimer.Wait(10))
                    {
                        TimedWait oTimedWait = new TimedWait(oRandomizer.Next(8000, 14000));
                        oTimedWait.Wait();
                        return;
                    }
                    else
                    {
                        //System.Console.WriteLine("Proxy timeout, switching to next one.");
                        wininet.RefreshIEProxySettings();
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to HTML properties, aborting operation", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("Access Violation, aborting operation", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..GoToNextPage thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
            }
        }

        protected void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            iCounter++;
        }

        protected void DocumentOnLoad(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            bDocumentLoaded = true;
        }

        protected abstract void ProcessResultPage(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery);

        protected abstract void Go2FormNSubmitQuery(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery);

        protected void OpenNewWindow(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.WebBrowser oTmpBrowser = (System.Windows.Forms.WebBrowser)sender;
            oDocLoader.loadDocument(ref oTmpBrowser, sSiteToNavigate);
            e.Cancel = true;

            bSiteFound = true;
            TimedWait oTimer = new TimedWait(oRandomizer.Next(10000, 30000));
            oTimer.Wait();
            return;
        }

        protected void __Go2FormNSubmitQuery(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery, String sUrl, String sInputId, String sBtnId)
        {
            CountedWait oCWTimer = new CountedWait(ref oBrowser, 3000);
            Int32 iErrorCount = 0;
            while (true)
            {
                try
                {
                    HtmlDocument oDocument = oDocLoader.loadDocument(ref oBrowser, sUrl);
                    HtmlElement oQueryInput = oDocument.GetElementById(sInputId);
                    HtmlElement oQuerySubmit = oDocument.GetElementById(sBtnId);
                    if (oQueryInput == null || oQuerySubmit == null)
                    {
                        throw new Exception("Submition form elements not found, retrying");
                    }
                    oQueryInput.InvokeMember("click");
                    oQueryInput.InvokeMember("focus");
                    oQueryInput.SetAttribute("value", sQuery);
                    oQuerySubmit.InvokeMember("click");

                    if (oCWTimer.Wait(10))
                    {
                        TimedWait oTimedWait = new TimedWait(oRandomizer.Next(10000, 30000));
                        oTimedWait.Wait();
                        return;
                    }
                    else
                    {
                        //System.Console.WriteLine("Proxy timeout, switching to next one.");
                        wininet.RefreshIEProxySettings();
                    }
                    iErrorCount = 0;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to HTML properties, aborting operation", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("Access Violation, aborting operation", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..__Go2FormNSubmitQuery thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
                finally
                {
                    iErrorCount++;
                    if (iErrorCount > 10)
                    {
                        throw new Exception("Too much errors in __Go2FormNSubmitQuery, aborting...");
                    }
                }
            }
        }
    }

    class BingSEO : FatherOfBlackSEOps
    {
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void ProcessResultPage(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            Boolean bProcessNextPage = true;
            bSiteFound = false;
            int iStartPage = 10;
            while (bProcessNextPage && !bSiteFound)
            {
                try
                {
                    bProcessNextPage = false;
                    HtmlDocument oHTMLDoc = oBrowser.Document;

                    HtmlElementCollection oLinks = oHTMLDoc.GetElementsByTagName("a");

                    for (int k = 0; k < oLinks.Count; k++)
                    {
                        HtmlElement oLink = oLinks[k];
                        String sHREF = oLink.GetAttribute("href");
                        if (sHREF.Contains("kreuzerkrieg.dyndns.info"))
                        {
                            if (oLink.InnerText.Contains("Zaslavsky"))
                            {
                                System.Console.WriteLine("..::" + this.GetType().Name + "::..Query \"" + sQuery + "\" found, loading page");
                                NavigateToSite(oLink, ref oBrowser);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Link inner text: " + oLink.InnerText);
                            }
                        }

                        if (!bSiteFound &&
                            sHREF.Contains(sQuery.Replace(" ", "+")) &&
                            sHREF.Contains("first=" + (iStartPage + 1).ToString()))
                        {
                            GoToNextPage(oLink, ref oBrowser);
                            break;
                        }
                    }
                    iStartPage += 10;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to HTML properties, aborting operation", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("Access Violation, aborting operation", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..ProcessResultPage thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void Go2FormNSubmitQuery(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            __Go2FormNSubmitQuery(ref oBrowser, sQuery, "http://www.bing.com/", "sb_form_q", "sb_form_go");
        }
    }

    class YahooSEO : FatherOfBlackSEOps
    {
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void ProcessResultPage(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            Boolean bProcessNextPage = true;
            bSiteFound = false;
            int iStartPage = 10;
            while (bProcessNextPage && !bSiteFound)
            {
                try
                {
                    bProcessNextPage = false;
                    HtmlDocument oHTMLDoc = oBrowser.Document;

                    HtmlElementCollection oLinks = oHTMLDoc.GetElementsByTagName("a");

                    for (int k = 0; k < oLinks.Count; k++)
                    {
                        HtmlElement oLink = oLinks[k];
                        String sHREF = oLink.GetAttribute("href");
                        if (sHREF.Contains("kreuzerkrieg.dyndns.info"))
                        {
                            if (oLink.InnerText.Contains("Zaslavsky"))
                            {
                                NavigateToSite(oLink, ref oBrowser);
                                break;
                            }
                        }

                        if (!bSiteFound &&
                            sHREF.Contains(sQuery.Replace(" ", "%2b")) &&
                            sHREF.Contains("pstart=1%26b=" + (iStartPage + 1).ToString()))
                        {
                            GoToNextPage(oLink, ref oBrowser);
                            break;
                        }
                    }
                    iStartPage += 10;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to HTML properties, aborting operation", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("Access Violation, aborting operation", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..ProcessResultPage thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void Go2FormNSubmitQuery(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            __Go2FormNSubmitQuery(ref oBrowser, sQuery, "http://www.yahoo.com/", "p", "search-submit");
        }
    }

    class GoogleSEO : FatherOfBlackSEOps
    {
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void ProcessResultPage(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            Boolean bProcessNextPage = true;
            bSiteFound = false;
            int iStartPage = 10;
            while (bProcessNextPage && !bSiteFound)
            {
                try
                {
                    bProcessNextPage = false;
                    HtmlDocument oHTMLDoc = oBrowser.Document;

                    HtmlElementCollection oLinks = oHTMLDoc.GetElementsByTagName("a");

                    for (int k = 0; k < oLinks.Count; k++)
                    {
                        HtmlElement oLink = oLinks[k];
                        String sHREF = oLink.GetAttribute("href");
                        if (sHREF.Contains("kreuzerkrieg.dyndns.info"))
                        {
                            if (oLink.InnerText.Contains("Zaslavsky"))
                            {
                                NavigateToSite(oLink, ref oBrowser);
                                break;
                            }
                        }

                        if (!bSiteFound &&
                            sHREF.Contains(sQuery.Replace(" ", "+")) &&
                            sHREF.Contains("start=" + iStartPage.ToString()))
                        {
                            GoToNextPage(oLink, ref oBrowser);
                            break;
                        }
                    }
                    iStartPage += 10;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to HTML properties, aborting operation", ex);
                }
                catch (AccessViolationException ex)
                {
                    throw new AccessViolationException("Access Violation, aborting operation", ex);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("..::" + this.GetType().Name + "::..ProcessResultPage thrown an exception, reason:  " + ex.Message + ", switching proxy");
                    wininet.RefreshIEProxySettings();
                }
            }
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        override protected void Go2FormNSubmitQuery(ref System.Windows.Forms.WebBrowser oBrowser, String sQuery)
        {
            __Go2FormNSubmitQuery(ref oBrowser, sQuery, "http://www.google.com/", "q", "btnG");
        }
    }

    class Program
    {
        [STAThread]
        [HandleProcessCorruptedStateExceptions]
        [SecurityCriticalAttribute]
        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            try
            {
                Boolean bShallProceed = false;
                wininetWrapper wininet = new wininetWrapper();
                String sStartPage = BlackSEOps.Properties.Settings.Default["sPage4GoogleSEO"].ToString();

                System.Windows.Forms.WebBrowser oBrowser = new System.Windows.Forms.WebBrowser();
                oBrowser.ScriptErrorsSuppressed = true;
                SiteCrawler oSiteCrawler = new SiteCrawler(wininet);
                Dictionary<String, HashSet<String>> oSiteText = oSiteCrawler.getSiteText(ref oBrowser, "http://kreuzerkrieg.dyndns.info/SiteMap.xml");
                foreach (String sKey in oSiteText.Keys)
                {
                    YahooSEO oYahooAbuser = new YahooSEO();
                    GoogleSEO oGoogleAbuser = new GoogleSEO();
                    BingSEO oBingAbuser = new BingSEO();
                    Thread oYahooThread = new Thread(oYahooAbuser.StartAbuse);
                    Thread oGoogleThread = new Thread(oGoogleAbuser.StartAbuse);
                    Thread oBingThread = new Thread(oBingAbuser.StartAbuse);

                    try
                    {
                        oBrowser.Dispose();
                        if (String.IsNullOrEmpty(sStartPage) || sKey == sStartPage)
                            bShallProceed = true;
                        if (!bShallProceed)
                            continue;
                        BlackSEOps.Properties.Settings.Default["sPage4GoogleSEO"] = sKey;

                        System.Console.WriteLine("Bumping page: " + sKey);

                        oYahooThread.SetApartmentState(ApartmentState.STA);
                        oYahooThread.Priority = ThreadPriority.BelowNormal;
                        oYahooThread.Name = "YAhOOViOlAtOR";
                        oYahooThread.Start(oSiteText[sKey]);
                        Thread.Sleep(100);
                        oGoogleThread.SetApartmentState(ApartmentState.STA);
                        oGoogleThread.Priority = ThreadPriority.BelowNormal;
                        oGoogleThread.Name = "GOOGlEViOlAtOR";
                        oGoogleThread.Start(oSiteText[sKey]);
                        Thread.Sleep(100);
                        oBingThread.SetApartmentState(ApartmentState.STA);
                        oBingThread.Priority = ThreadPriority.BelowNormal;
                        oBingThread.Name = "BiNGViOlAtOR";
                        oBingThread.Start(oSiteText[sKey]);
                        Thread.Sleep(100);
                        while (!oYahooThread.IsAlive ||
                            !oGoogleThread.IsAlive ||
                            !oBingThread.IsAlive) ;

                        oYahooThread.Join();
                        oGoogleThread.Join();
                        oBingThread.Join();
                        BlackSEOps.Properties.Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("Main has thrown an exception. Reason: " + ex.Message);
                        oYahooThread.Abort();
                        oGoogleThread.Abort();
                        oBingThread.Abort();
                        System.Console.WriteLine("Worker threads aborted, starting over...");
                    }
                }
            }
            catch (AccessViolationException ex)
            {
                System.Console.WriteLine("Main has thrown an exception. Reason: " + ex.Message);
                //die in solitude
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Main has thrown an exception. Reason: " + ex.Message);
                //die in solitude
            }
        }
    }
}
