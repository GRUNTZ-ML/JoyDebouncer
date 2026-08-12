using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.DirectInput;

namespace JoystickMapperUI
{
    // =========================================================
    // Joystick data class for populating the ComboBox
    // =========================================================
    public class JoystickItem
    {
        public string Name { get; set; }
        public Guid InstanceGuid { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    // =========================================================
    // Config management class including Mapping and Deadzone properties
    // =========================================================
    public class ControllerConfig
    {
        public string ProfileName { get; set; } = "Default";
        public int DebounceMs { get; set; } = 50;
        public bool IsHighPerformance { get; set; } = false;

        // --- Deadzone Configuration Properties ---
        public bool UseCustomDz { get; set; } = false;
        public double LeftInnerDz { get; set; } = 5.0;
        public double LeftOuterDz { get; set; } = 100.0;
        public double RightInnerDz { get; set; } = 5.0;
        public double RightOuterDz { get; set; } = 100.0;

        // Button Indexes (Default Standard DirectInput)
        public int BtnA { get; set; } = 0;
        public int BtnB { get; set; } = 1;
        public int BtnX { get; set; } = 2;
        public int BtnY { get; set; } = 3;
        public int BtnLB { get; set; } = 4;
        public int BtnRB { get; set; } = 5;
        public int BtnLT { get; set; } = 6; // Digital LT button
        public int BtnRT { get; set; } = 7; // Digital RT button
        public int BtnShare { get; set; } = 8; // Share / Capture button
        public int BtnBack { get; set; } = 8;  // Default Select/Back button index set to 8
        public int BtnStart { get; set; } = 9;
        public int BtnL3 { get; set; } = 10;
        public int BtnR3 { get; set; } = 11;

        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public static ControllerConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return new ControllerConfig();
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ControllerConfig>(json) ?? new ControllerConfig();
        }
    }

    public class DebounceButton
    {
        private bool _currentState = false;
        private DateTime _lastDebounceTime = DateTime.MinValue;
        private readonly TimeSpan _debounceInterval;

        public DebounceButton(int debounceMilliseconds = 50)
        {
            _debounceInterval = TimeSpan.FromMilliseconds(debounceMilliseconds);
        }

        public bool Update(bool rawState)
        {
            DateTime now = DateTime.Now;
            if (rawState != _currentState)
            {
                if ((now - _lastDebounceTime) > _debounceInterval)
                {
                    _currentState = rawState;
                    _lastDebounceTime = now;
                }
            }
            return _currentState;
        }
    }

    public partial class MainWindow : Window
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        private CancellationTokenSource _cts;
        private bool _isRunning = false;
        private ControllerConfig _currentConfig = new ControllerConfig();

        private string ProfileFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");

        public MainWindow()
        {
            InitializeComponent();

            if (!Directory.Exists(ProfileFolder))
            {
                Directory.CreateDirectory(ProfileFolder);
            }

            RefreshProfileList();
            RefreshJoystickList();
        }

        // =========================================================
        // Method to scan for physical joysticks and populate the ComboBox
        // =========================================================
        private void RefreshJoystickList()
        {
            CmbJoysticks.Items.Clear();
            try
            {
                using (var directInput = new DirectInput())
                {
                    List<DeviceInstance> foundDevices = new List<DeviceInstance>();
                    foundDevices.AddRange(directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices));
                    foundDevices.AddRange(directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices));

                    foreach (var deviceInstance in foundDevices)
                    {
                        if (!CmbJoysticks.Items.Cast<JoystickItem>().Any(x => x.InstanceGuid == deviceInstance.InstanceGuid))
                        {
                            CmbJoysticks.Items.Add(new JoystickItem
                            {
                                Name = deviceInstance.InstanceName ?? "Generic Gamepad",
                                InstanceGuid = deviceInstance.InstanceGuid
                            });
                        }
                    }
                }

                if (CmbJoysticks.Items.Count > 0)
                {
                    CmbJoysticks.SelectedIndex = 0;
                }
                else
                {
                    CmbJoysticks.Items.Add(new JoystickItem { Name = "No Gamepad Found", InstanceGuid = Guid.Empty });
                    CmbJoysticks.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning joysticks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshJoysticks_Click(object sender, RoutedEventArgs e)
        {
            RefreshJoystickList();
        }

        private void RefreshProfileList(string selectProfileName = null)
        {
            CmbProfiles.Items.Clear();

            if (Directory.Exists(ProfileFolder))
            {
                var files = Directory.GetFiles(ProfileFolder, "*.json");
                foreach (var file in files)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    CmbProfiles.Items.Add(profileName);
                }
            }

            if (CmbProfiles.Items.Count == 0)
            {
                CmbProfiles.Items.Add("Default");
            }

            if (!string.IsNullOrEmpty(selectProfileName) && CmbProfiles.Items.Contains(selectProfileName))
            {
                CmbProfiles.SelectedItem = selectProfileName;
            }
            else
            {
                CmbProfiles.SelectedIndex = 0;
            }

            LoadSelectedProfile(showNotification: false);
        }

        private void LoadSelectedProfile(bool showNotification)
        {
            string profileName = CmbProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName) && CmbProfiles.SelectedItem != null)
            {
                profileName = CmbProfiles.SelectedItem.ToString();
            }

            if (string.IsNullOrEmpty(profileName)) return;

            string safeFileName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(ProfileFolder, $"{safeFileName}.json");

            if (File.Exists(filePath))
            {
                _currentConfig = ControllerConfig.LoadFromFile(filePath);
            }
            else
            {
                _currentConfig = new ControllerConfig { ProfileName = profileName };
            }

            // Display Config values on UI
            TxtDebounceMs.Text = _currentConfig.DebounceMs.ToString();
            ChkHighPerformance.IsChecked = _currentConfig.IsHighPerformance;

            ChkUseCustomDz.IsChecked = _currentConfig.UseCustomDz;
            SliderLeftInner.Value = _currentConfig.LeftInnerDz;
            SliderLeftOuter.Value = _currentConfig.LeftOuterDz;
            SliderRightInner.Value = _currentConfig.RightInnerDz;
            SliderRightOuter.Value = _currentConfig.RightOuterDz;

            if (showNotification)
            {
                MessageBox.Show($"Profile \"{profileName}\" loaded successfully!", "Profile Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedProfile(showNotification: true);
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            int counter = 1;
            string newProfileName;
            string filePath;

            do
            {
                newProfileName = $"Profile {counter}";
                string safeFileName = string.Join("_", newProfileName.Split(Path.GetInvalidFileNameChars()));
                filePath = Path.Combine(ProfileFolder, $"{safeFileName}.json");
                counter++;
            }
            while (File.Exists(filePath));

            _currentConfig = new ControllerConfig { ProfileName = newProfileName };
            _currentConfig.SaveToFile(filePath);

            RefreshProfileList(newProfileName);

            CmbProfiles.Text = newProfileName;
            TxtDebounceMs.Text = _currentConfig.DebounceMs.ToString();
            ChkHighPerformance.IsChecked = _currentConfig.IsHighPerformance;

            ChkUseCustomDz.IsChecked = _currentConfig.UseCustomDz;
            SliderLeftInner.Value = _currentConfig.LeftInnerDz;
            SliderLeftOuter.Value = _currentConfig.LeftOuterDz;
            SliderRightInner.Value = _currentConfig.RightInnerDz;
            SliderRightOuter.Value = _currentConfig.RightOuterDz;

            MessageBox.Show($"Created and loaded new profile \"{newProfileName}\".", "Profile Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string profileName = CmbProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string safeFileName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(ProfileFolder, $"{safeFileName}.json");

            _currentConfig.ProfileName = profileName;

            if (int.TryParse(TxtDebounceMs.Text, out int ms))
            {
                _currentConfig.DebounceMs = ms;
            }
            _currentConfig.IsHighPerformance = ChkHighPerformance.IsChecked ?? false;

            // Save Deadzone values from UI to Config
            _currentConfig.UseCustomDz = ChkUseCustomDz.IsChecked ?? false;
            _currentConfig.LeftInnerDz = SliderLeftInner.Value;
            _currentConfig.LeftOuterDz = SliderLeftOuter.Value;
            _currentConfig.RightInnerDz = SliderRightInner.Value;
            _currentConfig.RightOuterDz = SliderRightOuter.Value;

            _currentConfig.SaveToFile(filePath);

            MessageBox.Show($"Profile \"{profileName}\" saved successfully!", "Profile Saved", MessageBoxButton.OK, MessageBoxImage.Information);

            RefreshProfileList(profileName);
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            string profileName = CmbProfiles.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please select a profile to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string safeFileName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(ProfileFolder, $"{safeFileName}.json");

            var result = MessageBox.Show($"Are you sure you want to delete profile \"{profileName}\"?",
                                       "Confirm Delete",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    MessageBox.Show($"Profile \"{profileName}\" has been deleted.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshProfileList();
            }
        }

        private void BtnOpenMapping_Click(object sender, RoutedEventArgs e)
        {
            MappingWindow mappingWin = new MappingWindow(_currentConfig) { Owner = this };
            if (mappingWin.ShowDialog() == true)
            {
                _currentConfig = mappingWin.Config;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            if (CmbJoysticks.SelectedItem is not JoystickItem selectedJoystick || selectedJoystick.InstanceGuid == Guid.Empty)
            {
                MessageBox.Show("Please select a valid physical controller first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtDebounceMs.Text, out int debounceMs) || debounceMs < 0)
            {
                debounceMs = 50;
                TxtDebounceMs.Text = "50";
            }

            Guid targetGuid = selectedJoystick.InstanceGuid;

            _cts = new CancellationTokenSource();
            _isRunning = true;

            bool isHighPerformance = ChkHighPerformance.IsChecked ?? true;

            // Lock UI when execution starts
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            ChkHighPerformance.IsEnabled = false;
            TxtDebounceMs.IsEnabled = false;
            CmbProfiles.IsEnabled = false;
            BtnLoadProfile.IsEnabled = false;
            BtnNewProfile.IsEnabled = false;
            BtnSaveProfile.IsEnabled = false;
            BtnDeleteProfile.IsEnabled = false;
            CmbJoysticks.IsEnabled = false;
            BtnRefreshJoysticks.IsEnabled = false;
            ChkUseCustomDz.IsEnabled = false;
            SliderLeftInner.IsEnabled = false;
            SliderLeftOuter.IsEnabled = false;
            SliderRightInner.IsEnabled = false;
            SliderRightOuter.IsEnabled = false;

            if (FindName("BtnOpenMapping") is Button btnMapping)
            {
                btnMapping.IsEnabled = false;
            }

            Task.Run(() => WorkerLoop(_cts.Token, isHighPerformance, debounceMs, _currentConfig, targetGuid));
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopWorker();
        }

        private void StopWorker()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _isRunning = false;

            // Restore UI state when stopped
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            ChkHighPerformance.IsEnabled = true;
            TxtDebounceMs.IsEnabled = true;
            CmbProfiles.IsEnabled = true;
            BtnLoadProfile.IsEnabled = true;
            BtnNewProfile.IsEnabled = true;
            BtnSaveProfile.IsEnabled = true;
            BtnDeleteProfile.IsEnabled = true;
            CmbJoysticks.IsEnabled = true;
            BtnRefreshJoysticks.IsEnabled = true;
            ChkUseCustomDz.IsEnabled = true;
            SliderLeftInner.IsEnabled = true;
            SliderLeftOuter.IsEnabled = true;
            SliderRightInner.IsEnabled = true;
            SliderRightOuter.IsEnabled = true;

            if (FindName("BtnOpenMapping") is Button btnMapping)
            {
                btnMapping.IsEnabled = true;
            }

            UpdateStatus(TxtPhysicalStatus, "Not Connected", Colors.Red);
            UpdateStatus(TxtVirtualStatus, "Disconnected", Colors.Red);
        }

        private short ApplyDeadzone(short rawValue, double innerPercent, double outerPercent)
        {
            // rawValue is in range -32768 to 32767
            double normalized = rawValue / 32768.0; // -1.0 to 1.0 (approximate)
            double absVal = Math.Abs(normalized);

            double inner = innerPercent / 100.0;
            double outer = outerPercent / 100.0;

            if (absVal <= inner)
            {
                return 0;
            }

            if (absVal >= outer)
            {
                return (short)(normalized > 0 ? 32767 : -32768);
            }

            // Rescale values between Inner and Outer to full range
            double scaled = (absVal - inner) / (outer - inner);
            scaled = Math.Clamp(scaled, 0.0, 1.0);

            int result = (int)(scaled * 32767.0);
            return (short)(normalized > 0 ? result : -result);
        }

        private void WorkerLoop(CancellationToken token, bool isHighPerformance, int debounceMs, ControllerConfig config, Guid targetGuid)
        {
            TimeBeginPeriod(1);

            DirectInput directInput = null;
            Joystick joystick = null;
            ViGEmClient vigem = null;
            IXbox360Controller virtualController = null;

            try
            {
                directInput = new DirectInput();

                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                {
                    if (deviceInstance.InstanceGuid == targetGuid)
                    {
                        joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                        break;
                    }
                }

                if (joystick == null)
                {
                    foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    {
                        if (deviceInstance.InstanceGuid == targetGuid)
                        {
                            joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                            break;
                        }
                    }
                }

                if (joystick == null)
                {
                    UpdateStatus(TxtPhysicalStatus, "Selected Device Not Found", Colors.Red);
                    Dispatcher.Invoke(() => StopWorker());
                    return;
                }

                joystick.Properties.BufferSize = 256;
                joystick.Acquire();

                string deviceName = joystick.Information.InstanceName ?? "Generic Gamepad";
                UpdateStatus(TxtPhysicalStatus, $"Connected: {deviceName}", Colors.Green);

                vigem = new ViGEmClient();
                virtualController = vigem.CreateXbox360Controller();
                virtualController.Connect();
                UpdateStatus(TxtVirtualStatus, "Connected (Virtual Xbox 360)", Colors.Green);

                // Debounce Variables
                DebounceButton btnA = new DebounceButton(debounceMs);
                DebounceButton btnB = new DebounceButton(debounceMs);
                DebounceButton btnX = new DebounceButton(debounceMs);
                DebounceButton btnY = new DebounceButton(debounceMs);
                DebounceButton btnLB = new DebounceButton(debounceMs);
                DebounceButton btnRB = new DebounceButton(debounceMs);
                DebounceButton btnLT = new DebounceButton(debounceMs);
                DebounceButton btnRT = new DebounceButton(debounceMs);
                DebounceButton btnBack = new DebounceButton(debounceMs);
                DebounceButton btnStart = new DebounceButton(debounceMs);
                DebounceButton btnL3 = new DebounceButton(debounceMs);
                DebounceButton btnR3 = new DebounceButton(debounceMs);
                DebounceButton btnUp = new DebounceButton(debounceMs);
                DebounceButton btnDown = new DebounceButton(debounceMs);
                DebounceButton btnLeft = new DebounceButton(debounceMs);
                DebounceButton btnRight = new DebounceButton(debounceMs);

                short lastX = 0, lastY = 0, lastZ = 0, lastRZ = 0;
                bool toggleBit = false;

                Stopwatch sw = Stopwatch.StartNew();
                double targetIntervalTicks = Stopwatch.Frequency / 1000.0;
                double nextTargetTicks = sw.ElapsedTicks + targetIntervalTicks;

                int sleepDuration = isHighPerformance ? 0 : 1;

                while (!token.IsCancellationRequested)
                {
                    joystick.Poll();
                    JoystickState state = joystick.GetCurrentState();
                    bool[] raw = state.Buttons;

                    short currentX = (short)(state.X - 32768);
                    short currentY = (short)(32767 - state.Y);
                    short currentZ = (short)(state.Z - 32768);
                    short currentRZ = (short)(32767 - state.RotationZ);

                    // Apply Custom Deadzone if enabled
                    if (config.UseCustomDz)
                    {
                        currentX = ApplyDeadzone(currentX, config.LeftInnerDz, config.LeftOuterDz);
                        currentY = ApplyDeadzone(currentY, config.LeftInnerDz, config.LeftOuterDz);
                        currentZ = ApplyDeadzone(currentZ, config.RightInnerDz, config.RightOuterDz);
                        currentRZ = ApplyDeadzone(currentRZ, config.RightInnerDz, config.RightOuterDz);
                    }

                    toggleBit = !toggleBit;
                    short sendX = currentX;
                    if (currentX == lastX && currentY == lastY && currentZ == lastZ && currentRZ == lastRZ)
                    {
                        sendX = (short)Math.Clamp(currentX + (toggleBit ? 1 : -1), -32768, 32767);
                    }
                    lastX = currentX; lastY = currentY; lastZ = currentZ; lastRZ = currentRZ;

                    virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, sendX);
                    virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, currentY);
                    virtualController.SetAxisValue(Xbox360Axis.RightThumbX, currentZ);
                    virtualController.SetAxisValue(Xbox360Axis.RightThumbY, currentRZ);

                    // --- Digital Triggers ---
                    bool isLtPressed = btnLT.Update(GetSafeButton(raw, config.BtnLT));
                    bool isRtPressed = btnRT.Update(GetSafeButton(raw, config.BtnRT));

                    virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(isLtPressed ? 255 : 0));
                    virtualController.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(isRtPressed ? 255 : 0));

                    // Buttons Mapping
                    virtualController.SetButtonState(Xbox360Button.A, btnA.Update(GetSafeButton(raw, config.BtnA)));
                    virtualController.SetButtonState(Xbox360Button.B, btnB.Update(GetSafeButton(raw, config.BtnB)));
                    virtualController.SetButtonState(Xbox360Button.X, btnX.Update(GetSafeButton(raw, config.BtnX)));
                    virtualController.SetButtonState(Xbox360Button.Y, btnY.Update(GetSafeButton(raw, config.BtnY)));
                    virtualController.SetButtonState(Xbox360Button.LeftShoulder, btnLB.Update(GetSafeButton(raw, config.BtnLB)));
                    virtualController.SetButtonState(Xbox360Button.RightShoulder, btnRB.Update(GetSafeButton(raw, config.BtnRB)));

                    bool isSelectPressed = GetSafeButton(raw, config.BtnBack);
                    virtualController.SetButtonState(Xbox360Button.Back, btnBack.Update(isSelectPressed));

                    virtualController.SetButtonState(Xbox360Button.Start, btnStart.Update(GetSafeButton(raw, config.BtnStart)));
                    virtualController.SetButtonState(Xbox360Button.LeftThumb, btnL3.Update(GetSafeButton(raw, config.BtnL3)));
                    virtualController.SetButtonState(Xbox360Button.RightThumb, btnR3.Update(GetSafeButton(raw, config.BtnR3)));

                    // D-Pad
                    int[] pov = state.PointOfViewControllers;
                    if (pov.Length > 0)
                    {
                        int povVal = pov[0];
                        virtualController.SetButtonState(Xbox360Button.Up, btnUp.Update(povVal == 0));
                        virtualController.SetButtonState(Xbox360Button.Right, btnRight.Update(povVal == 9000));
                        virtualController.SetButtonState(Xbox360Button.Down, btnDown.Update(povVal == 18000));
                        virtualController.SetButtonState(Xbox360Button.Left, btnLeft.Update(povVal == 27000));
                    }

                    virtualController.SubmitReport();

                    // Loop Wait
                    while (!token.IsCancellationRequested)
                    {
                        double remainingMs = (nextTargetTicks - sw.ElapsedTicks) * 1000.0 / Stopwatch.Frequency;
                        if (remainingMs <= 0) break;

                        if (remainingMs > 0.8) Thread.Sleep(sleepDuration);
                        else if (remainingMs > 0.2) Thread.SpinWait(300);
                        else Thread.SpinWait(10);
                    }

                    long currentTicks = sw.ElapsedTicks;
                    if (currentTicks > nextTargetTicks + targetIntervalTicks)
                        nextTargetTicks = currentTicks + targetIntervalTicks;
                    else
                        nextTargetTicks += targetIntervalTicks;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(TxtPhysicalStatus, $"Error: {ex.Message}", Colors.Red);
            }
            finally
            {
                virtualController?.Disconnect();
                joystick?.Unacquire();
                joystick?.Dispose();
                directInput?.Dispose();

                TimeEndPeriod(1);
            }
        }

        private bool GetSafeButton(bool[] raw, int index)
        {
            if (index < 0 || index >= raw.Length) return false;
            return raw[index];
        }

        private void UpdateStatus(System.Windows.Controls.TextBlock textBlock, string text, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                textBlock.Text = text;
                textBlock.Foreground = new SolidColorBrush(color);
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopWorker();
        }

        private void TxtDebounceMs_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}