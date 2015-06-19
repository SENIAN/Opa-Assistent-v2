using System;
using System.Linq;
using System.Windows;
using Microsoft.Speech.Recognition;
using OpaAssistent;

namespace WpfApplication1
{
    /// <summary>
    ///     Interaction logic for Login.xaml
    /// </summary>
    /// 
    
    
    public partial class Login : Window 
    {
        MainWindow m = new MainWindow();
       
        
        public Login()
        {
            InitializeComponent();
           
        }

        private void setVisibleAdmin(object sender, RoutedEventArgs e)
        {
            AdminGrid.Visibility = Visibility.Visible;
        }

        private void setVisibleUser(object sender, RoutedEventArgs e)
        {
            AdminGrid.Visibility = Visibility.Visible;
        }

        private void CheckLoginCreds(object sender, RoutedEventArgs e)
        {
            var user = Username2.Text;
            var psswd = Passbox2.Password;
            var db = new OPAEntities();
            var person = db.Persoon.Where(x => x.Loginnaam == user).ToList();

            try
            {
                var loginnaam = person.First().Loginnaam;
                var pss = person.First().Wachtwoord;
                if (user == loginnaam && psswd == pss)
                {
                    var mainwindow = new MainWindow();
                    mainwindow.Show();
                    Close();
                }
                else
                {
                    MessageBox.Show("Ongeldig gebruikersnaam of wachtwoord");
                }
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Ongeldig gebruikersnaam of wachtwoord");
            }
            {
            }
        }

        public void SpeechRecognized(object  sender, SpeechRecognizedEventArgs e)
        {
           const double ConfidenceThreshold = 0.3;
            // Spraak command action.
            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                { 
                    case "KINECT LOGIN":
                        MessageBox.Show("U wordt ingelogd");
                        var m1 = new MainWindow();
                        m1.Show();
                        Close();
                        break;
                    case "KINECT FAMILIE":
                        Console.WriteLine("Sessie familie wordt gestart een ogenblik geduld aub...");
                        var m2 = new MainWindow();
                        m2.Show();
                        m.StartFamilySession();
                        m.SendTokenMessage();      
                        break;
                    case "KINECT AFSLUITEN":
                        Console.WriteLine("Programma wordt afgesloten");
                        CloseWindow();
                        break;
                    case "KINECT HELP":
                        m.StartSupportSession();
                        break;
                }
            }
        }

        private void WindowLoaded1(object sender, RoutedEventArgs e)
        {
            m.WindowLoaded(sender, e);
        }

        public void CloseWindow()
        {
            this.LoginGrid.Visibility = Visibility.Hidden;
        }
    }
}