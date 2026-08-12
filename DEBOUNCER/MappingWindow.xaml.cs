using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace JoystickMapperUI
{
    public partial class MappingWindow : Window
    {
        public ControllerConfig Config { get; private set; }

        public MappingWindow(ControllerConfig currentConfig)
        {
            InitializeComponent();
            Config = currentConfig;

            // Load existing Button Indexes to display on TextBoxes
            LoadConfigToUI();
        }

        private void LoadConfigToUI()
        {
            TxtA.Text = Config.BtnA.ToString();
            TxtB.Text = Config.BtnB.ToString();
            TxtX.Text = Config.BtnX.ToString();
            TxtY.Text = Config.BtnY.ToString();
            TxtLB.Text = Config.BtnLB.ToString();
            TxtRB.Text = Config.BtnRB.ToString();
            TxtLT.Text = Config.BtnLT.ToString();
            TxtRT.Text = Config.BtnRT.ToString();
            TxtBack.Text = Config.BtnBack.ToString();
            TxtStart.Text = Config.BtnStart.ToString();
            TxtL3.Text = Config.BtnL3.ToString();
            TxtR3.Text = Config.BtnR3.ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Save all Index values back to the Config Object
            Config.BtnA = ParseIndex(TxtA.Text, 0);
            Config.BtnB = ParseIndex(TxtB.Text, 1);
            Config.BtnX = ParseIndex(TxtX.Text, 2);
            Config.BtnY = ParseIndex(TxtY.Text, 3);
            Config.BtnLB = ParseIndex(TxtLB.Text, 4);
            Config.BtnRB = ParseIndex(TxtRB.Text, 5);
            Config.BtnLT = ParseIndex(TxtLT.Text, 6);
            Config.BtnRT = ParseIndex(TxtRT.Text, 7);
            Config.BtnBack = ParseIndex(TxtBack.Text, 8);
            Config.BtnStart = ParseIndex(TxtStart.Text, 9);
            Config.BtnL3 = ParseIndex(TxtL3.Text, 10);
            Config.BtnR3 = ParseIndex(TxtR3.Text, 11);

            DialogResult = true;
            Close();
        }

        private int ParseIndex(string input, int defaultValue)
        {
            if (int.TryParse(input, out int result) && result >= 0)
            {
                return result;
            }
            return defaultValue;
        }

        // Restrict input to numeric characters only
        private void TxtNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}