using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace PhotoUploaderUpload
{
    public sealed class ActualUploader : IBackgroundTask
    {
        public ActualUploader()
        {
        }


        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("YUUUUUU-Huuuuuuu");
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode("I am here"));
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(toastXml));
        }

    }
}
