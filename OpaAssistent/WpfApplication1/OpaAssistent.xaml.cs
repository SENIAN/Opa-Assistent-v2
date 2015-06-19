using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using WpfApplication1;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace OpaAssistent
{
    public partial class MainWindow : Window
    {
        private readonly string url = "http://145.24.200.15/tokengenerator/script.php?id=1";
        private string familie;
        private KinectSensor sensor;
        private SpeechRecognitionEngine speechEngine;
        private string support;
        MediaElement em = new MediaElement();
        ChromiumWebBrowser wbv = new ChromiumWebBrowser();


        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("Initialiseer applicatie.");
            
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (var recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) &&
                    "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        public void WindowLoaded(object sender, RoutedEventArgs e)
        {
          
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }

            if (null != sensor)
            {
                try
                {
                    // Start the sensor!
                    sensor.Start();
                }
                catch (IOException)
                {
                    // Some other application is streaming from the same Kinect sensor
                    sensor = null;
                }
            }

            if (null == sensor)
            {
                Console.WriteLine("No speechrecognizer");
                return;
            }

            var ri = GetKinectRecognizer();

            if (null != ri)
            {
                speechEngine = new SpeechRecognitionEngine(ri.Id);

                var choice = new Choices();
                choice.Add(new SemanticResultValue("kinect familie", "KINECT FAMILIE"));
                choice.Add(new SemanticResultValue("kinect login", "KINECT LOGIN"));
                choice.Add(new SemanticResultValue("kinect afsluiten", "KINECT AFSLUITEN"));
                choice.Add(new SemanticResultValue("kinect help", "KINECT HELP" ));
  

                var gb = new GrammarBuilder {Culture = ri.Culture};
                gb.Append(choice);

                var g = new Grammar(gb);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(WpfApplication1.Properties.Resources.SpeechGrammar)))
                {
                    //var g = new Grammar(memoryStream);
                    speechEngine.LoadGrammar(g);
                }

                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(),
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                Console.WriteLine("No SpeechRecognizer");
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != sensor)
            {
                sensor.AudioSource.Stop();

                sensor.Stop();
                sensor = null;
            }

            if (null != speechEngine)
            {
                speechEngine.SpeechRecognized -= SpeechRecognized;
                speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                speechEngine.RecognizeAsyncStop();
            }
        }

        public void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double ConfidenceThreshold = 0.3;
            // Spraak command action.
            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "KINECT FAMILIE":
                        Console.WriteLine("Sessie familie wordt gestart een ogenblik geduld aub...");
                        var mainwindow = new MainWindow();
                        mainwindow.Show();
                        StartFamilySession();
                        SendTokenMessage();
                        
                        break;
                    case "KINECT AFSLUITEN":
                        Console.WriteLine("Programma wordt afgesloten");
                        Close();
                        break;
                    case "KINECT HELP":
                        StartSupportSession();
                        break;
                }
            }
        }

        public void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //Als het niet herkent wordt;
            Console.WriteLine("Ik heb u niet verstaan");
        }

        
        public void SendTokenMessage()
        {
            Console.WriteLine("Sending Token Message");
            var db = new OPAEntities();
            var person = db.Persoon.Where(x => x.Naam == "Mohammed").ToList();
            var ltn = person.First().E_mail;
            var to = ltn;
            var from = "sensobilityserver@gmail.com";
            var psswd = (string) Application.Current.Resources["psswd"];
            var Onderwerp = "Opa Session Stream";
            var Tekst = ("OPA vraagt u deel te nemen aan een sessie klik op de volgende url om " +
                         "hier deel aan te " +
                         "nemen" + "   " + url);
            var SmtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from, psswd)
            };
            using (var message = new MailMessage(from, to)
            {
                Subject = Onderwerp,
                Body = Tekst
            })
            {
                SmtpClient.Send(message);
            }
        }
        /*
        public void SendRequest()
        {
            var db = new OPAEntities();
            var person = db.Persoon.Where(x => x.Naam == "Serhildan").ToList();
            var ltn = person.First().Telefoonnummer.ToString();
            var phone = ltn;
            var text =
                "OPA+stream+wenst+een+sessie+met+u+te+starten+klik+op+de+onderstaande+link+http%3A%2F%2Flocalhost%2Enl";
            var password = "qwaszx1";
            String smsurl = "http://145.24.224.41:57251/send.html?smsto=" + "0" + phone + "&" + "smsbody=" + text + "+" +
                         "&" + "smstype=" + "sms";
            Console.WriteLine(smsurl);
        }
        */
        public void StartFamilySession(object sender, RoutedEventArgs e)
        {
       
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
            {
                em.LoadedBehavior = MediaState.Close;
                em.UnloadedBehavior = MediaState.Close;
                MainGrid.Children.Remove(em);
                StartFamilySession();
                Console.WriteLine("Starting family Session");
                SendTokenMessage();
            });
        }
        /*
        void ProgressDialogTestDefault()
        {
            ProgressDialogResult result = ProgressDialog.Execute(this, "Loading data...", (bw, we) =>
            {

                Thread.Sleep(4000);

            });

            if (result.OperationFailed)
                MessageBox.Show("ProgressDialog failed.");
            else
                MessageBox.Show("ProgressDialog successfully executed.");
        }
        */
        public void GetWebStream()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) delegate
            {
                Console.WriteLine("Start webstream");
                var settings = new BrowserSettings();
                var cefSettings = new CefSettings();
                cefSettings.CefCommandLineArgs.Add("enable-media-stream", "enable-media-stream");
                Cef.Initialize(cefSettings);

                wbv.BrowserSettings.JavascriptDisabled = false;
                wbv.Width = this.ActualWidth - 500;
                wbv.Height = this.ActualHeight;
                wbv.VerticalAlignment = VerticalAlignment.Top;
                wbv.HorizontalAlignment = HorizontalAlignment.Left;
                wbv.Margin = new Thickness(10, 10, 0, 0);
                MainGrid.Children.Add(wbv);
                wbv.Address = url;

            });
        }

        public void StartExternalSession(object sender, RoutedEventArgs e)
        {
        }

        public void StartSupportSession(object sender, RoutedEventArgs e)
        {
            StartSupportSession();
        }

        public void StartSupportSession()
        {
            MainGrid.Children.Remove(wbv);
            em.Source = new Uri("http://clips.vorwaerts-gmbh.de/big_buck_bunny.mp4");
            em.Height = 600;
            em.Width = 800;
            em.Margin = new Thickness(-100, -100, 300, -100);
            em.LoadedBehavior = MediaState.Manual;
            em.UnloadedBehavior = MediaState.Manual;
            em.Play();
            MainGrid.Children.Add(em);
            Console.WriteLine("start support session");  
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            em.LoadedBehavior = MediaState.Close;
            em.UnloadedBehavior = MediaState.Close;
            Close();
        }

        public void StartFamilySession()
        {
            GetWebStream();
        }

    }
}