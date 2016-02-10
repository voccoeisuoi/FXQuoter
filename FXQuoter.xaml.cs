using System;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using System.Net;
using System.IO;
using System.Xml.XPath;
using System.Xml;

namespace FXQuoter
{
    /// <summary>
    /// Logica di interazione per FxQuoter.xaml
    /// </summary>
    public partial class FXQ : Window, INotifyPropertyChanged
    {
        private AutoResetEvent m_stopEvent = new AutoResetEvent(false);

        #region Constructor

        public FXQ()
        {
            InitializeComponent();

            (new Thread(QuotesDownloader_Thread)).Start();
        }

        #endregion

        #region Properties

        private string m_error;
        public string ERROR
        {
            get { return m_error; }
            set
            {
                m_error = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ERROR"));
            }
        }

        private string m_bid;
        public string Bid
        {
            get { return m_bid; }
            set
            {
                m_bid = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Bid"));
            }
        }

        private string m_ask;
        public string Ask
        {
            get { return m_ask; }
            set
            {
                m_ask = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Ask"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        #endregion

        #region Thread

        private void QuotesDownloader_Thread()
        {
            while (true)
            {
                using (WebClient client = new WebClient())
                {
                    string URL = "http://tools.fxdd.com/tools/pricefeedgenerator/pricefeed.xml";
                    WebRequest request = WebRequest.Create(URL);
                    Stream response = null;

                    try
                    {
                        response = request.GetResponse().GetResponseStream();
                    }
                    catch (Exception)
                    {
                        this.Bid = "CONN";
                        this.Ask = "ERR";
                    }

                    if (response != null)
                    {
                        this.UpdateFromXML(response);
                    }

                }

                if (m_stopEvent.WaitOne(10000))
                {
                    break;
                }
            }
        }

        private void UpdateFromXML(System.IO.Stream response)
        {
            if (response.CanRead == false)
            {
                this.ERROR = "ERROR";
                return;
            }

            XPathDocument xDoc = null;

            try
            {
                xDoc = new XPathDocument(response);
            }
            catch (XmlException exc)
            {
                System.Diagnostics.Trace.Write("XmlException: " + exc.Message);
                return;
            }
            catch (WebException exc)
            {
                System.Diagnostics.Trace.Write("WebException: " + exc.Message);
                return;
            }

            XPathNavigator nav = xDoc.CreateNavigator();
            XmlNamespaceManager nsMan = new XmlNamespaceManager(nav.NameTable);

            XPathNodeIterator it = nav.Select("//Result[pair/text()='EUR/USD']", nsMan);
            if (it.MoveNext())
            {
                it.Current.MoveToChild("bid", string.Empty);

                string[] splittedBid = it.Current.Value.Split('.');
                this.Bid = splittedBid[1];

                it.Current.MoveToParent();
                it.Current.MoveToChild("ask", string.Empty);

                string[] splittedAsk = it.Current.Value.Split('.');
                this.Ask = splittedAsk[1];
            }
        }

        #endregion

        #region Events

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_stopEvent.Set();
        }

        #endregion
    }
}
