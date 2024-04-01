using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RegMaster
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private List<RegCode> regCodes;
        private Random random = new Random();
        public MainWindow()
        {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "RegMaster.RegCodes.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                regCodes = JsonConvert.DeserializeObject<List<RegCode>>(json);
            }
            var savedTheme = Properties.Settings.Default.Theme;
            if (savedTheme == "Dark")
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                ChangeThemeButton.Content = new SymbolIcon(SymbolRegular.WeatherMoon16);
            }
            else if (savedTheme == "Light")
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
                ChangeThemeButton.Content = new SymbolIcon(SymbolRegular.WeatherSunny16);
            }
            RegCodeTextBox.Text = regCodes[random.Next(regCodes.Count)].Name;
            RegCountTextBlock.Text = $"Reg Count : {regCodes.Count}";
            MatchingNamesListBox.ItemsSource = regCodes.Select(r => r.Name);
        }

        private void RegCodeTextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            var searchTerm = RegCodeTextBox.Text;

            if (string.IsNullOrEmpty(searchTerm))
            {
                MatchingNamesListBox.ItemsSource = regCodes.Select(r => r.Name);
                return;
            }

            var regex = new Regex(Regex.Escape(searchTerm), RegexOptions.IgnoreCase);

            var matchingRegCodes = regCodes.Where(r => regex.IsMatch(r.Name)).ToList();

            MatchingNamesListBox.ItemsSource = matchingRegCodes.Select(r => r.Name);

            if (matchingRegCodes.Count == 0)
            {
                MatchingNamesListBox.SelectedIndex = -1;
            }
            else
            {
                MatchingNamesListBox.SelectedIndex = 0;
            }
        }


        private void DisplayRegCode(RegCode regCode)
        {
            if (RevertToggleSwitch.IsChecked == true)
            {
                RegCodeDisplayTextBox.Text = regCode.RevertCode;
            }
            else
            {
                RegCodeDisplayTextBox.Text = regCode.Code;
            }
        }

        private void MatchingNamesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedName = MatchingNamesListBox.SelectedItem as string;
            var regCode = regCodes.FirstOrDefault(r => r.Name == selectedName);
            if (regCode != null)
            {
                DisplayRegCode(regCode);
            }
        }

        private void RevertToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            MatchingNamesListBox_SelectionChanged(MatchingNamesListBox, null);
        }

        private void RevertToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            MatchingNamesListBox_SelectionChanged(MatchingNamesListBox, null);
        }
        private void SaveReg_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

            saveFileDialog.DefaultExt = ".reg";
            saveFileDialog.Filter = "Registry Files (*.reg)|*.reg";

            saveFileDialog.InitialDirectory = Environment.CurrentDirectory;

            var regCodeText = RegCodeDisplayTextBox.Text;
            var lines = regCodeText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var lineWithFileName = lines.FirstOrDefault(line => line.Contains("; --"));
            var fileName = lineWithFileName != null && lineWithFileName.Contains("; --")
                ? lineWithFileName.Split(new[] { "; --" }, StringSplitOptions.None)[1]
                : string.Empty;

            if (!string.IsNullOrEmpty(fileName))
            {
                saveFileDialog.FileName = fileName;
            }

            Nullable<bool> result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string filename = saveFileDialog.FileName;

                File.WriteAllText(filename, RegCodeDisplayTextBox.Text);
            }
        }
        private void ChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            var currentTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();

            if (currentTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark)
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
                Properties.Settings.Default.Theme = "Light";
                ChangeThemeButton.Content = new SymbolIcon(SymbolRegular.WeatherSunny16);
            }
            else if (currentTheme == Wpf.Ui.Appearance.ApplicationTheme.Light)
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                Properties.Settings.Default.Theme = "Dark";
                ChangeThemeButton.Content = new SymbolIcon(SymbolRegular.WeatherMoon16);
            }

            Properties.Settings.Default.Save();
        }
        private void SaveAndRunReg_Click(object sender, RoutedEventArgs e)
        {
            string tempPath = Path.GetTempPath();
            string fileName = Path.Combine(tempPath, Guid.NewGuid().ToString() + ".reg");
            File.WriteAllText(fileName, RegCodeDisplayTextBox.Text);
            Process regeditProcess = Process.Start("regedit.exe", "/s \"" + fileName + "\"");
            regeditProcess.WaitForExit();
            if (regeditProcess.ExitCode == 0)
            {
                System.Windows.MessageBox.Show("Successful");
            }
            else
            {
                System.Windows.MessageBox.Show("Operation failed 😔. Error Code : " + regeditProcess.ExitCode);
            }
            File.Delete(fileName);
        }

        private void BuyMeCoffee_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.buymeacoffee.com/berkayay");
        }
    }
    public class RegCode
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string RevertCode { get; set; }
    }
}
