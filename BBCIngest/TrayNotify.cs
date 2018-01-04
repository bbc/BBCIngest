using System;
using System.ComponentModel;
using System.Windows.Forms;
using Ingest;

namespace BBCIngest
{
    class TrayNotify
    {
        private bool showicon = false;
        NotifyIcon notifyIcon;
        private class MyContainer : IContainer
        {
            private ComponentCollection components;
            public MyContainer()
            {
                components = new ComponentCollection(new IComponent[] { });
            }
            public ComponentCollection Components
            {
                get
                {
                    return components;
                }
            }

            public void Add(IComponent component)
            {
            }

            public void Add(IComponent component, string name)
            {
            }

            public void Remove(IComponent component)
            {
            }

            public void Dispose()
            {
            }
        }

        public TrayNotify(FetchAndPublish fetcher)
        {
            MyContainer components = new MyContainer();
            notifyIcon = new NotifyIcon(components)
            {
                Icon = Properties.Resources.main,
                Text = "BBC Ingest",
                BalloonTipText = "BBC Ingest",
                Visible = showicon
            };
            fetcher.listenForTerseMessages(new TerseMessageDelegate(terse));
            fetcher.listenForChattyMessages(new ChattyMessageDelegate(chatty));
            fetcher.listenForEditionStatus(new ShowEditionStatusDelegate(chatty));
        }

        public void terse(string s)
        {
            notifyIcon.BalloonTipText = s;
            notifyIcon.ShowBalloonTip(1000);
        }

        public void chatty(string s)
        {
            // be terse
        }
    }
}
