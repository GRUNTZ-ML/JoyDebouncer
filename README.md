# JoyDebouncer
<img width="530" height="571" alt="image" src="https://github.com/user-attachments/assets/91ac7190-dcf0-4414-b116-d9a58dda9cd5" />

> **Note from the Creator:**  
> This project was created from the perspective of a **regular user without a computer science background or professional coding experience**, created solely to solve a personal controller issue. All code in this project was developed, written, and refined with the help of **AI (Google Gemini)** to achieve the desired features.  
>  
> *This is a personal hobby project. Pull Requests and Issue reports are not actively reviewed or maintained.*

**JoyDebouncer** is a Windows utility designed to fix button chattering (double input) issues—a common problem with older or worn-out game controllers when replacement rubber pads or repair parts are unavailable.

---

## 🛠️ How It Works

JoyDebouncer captures raw inputs from your physical controller (DirectInput), filters out rapid, unintentional duplicate presses (Debounce), and forwards the cleaned input to a **Virtual Xbox 360 Controller**. Games and applications only read from this virtual controller.
---

## ✨ Features

### 🎯 Main Feature
* **Debounce Interval:** Set the suppression window for duplicate inputs in milliseconds (ms). The default is `50 ms`.  
  * *Example:* If your physical button registers multiple presses within 50 ms, JoyDebouncer considers it a hardware glitch/double input and passes only a single press to the virtual controller.

### ⚙️ Additional Features
* **Select Target Joystick:** Choose specific physical joysticks directly from a dropdown menu. This solves issues where legacy apps only bind to Device #1, preventing input conflicts when multiple controllers are connected.
* **Button Mapping:** Map individual physical buttons (DirectInput) to match standard Xbox 360 virtual layout. Great for 3rd-party gamepads with non-standard button indexes.
* **Inner / Outer Deadzone Configuration:** Customize analog stick inner and outer deadzones for the virtual controller. Ideal for controllers lacking vendor software support. *(Can be toggled On/Off)*.
* **High Performance Mode:** Boosts analog responsiveness for ultra-smooth movement.
  * *Benchmark Results (Measured via GamepadLa on a 1000Hz Polling Rate controller):*
    * **Normal Mode:** Polling rate ~600–700Hz (~2–3% CPU usage).
    * **High Performance Mode:** Polling rate up to ~900–1000Hz (~8–9% CPU usage on Ryzen 5 7600).
* **Profile Management:** Save, load, and manage custom configurations for different gamepads or game genres.

---

## 📋 Prerequisites

To ensure proper functionality without input conflicts, please install the following required software:

1. **[ViGEmBus Driver](https://github.com/nefarius/ViGEmBus):** Required to create and manage the Virtual Xbox 360 Controller in Windows.
2. **[HidHide](https://github.com/nefarius/HidHide):** Hides your physical controller from Windows, Steam, and games to prevent **Double Controller Input**.
3. **Gamepad running in DirectInput (DInput) mode:**  
   > ⚠️ **Important Limitation:** Controllers running in **XInput mode cannot be completely hidden from Steam via HidHide**. You **must** set your physical controller to **DirectInput (DInput)** mode for device hiding to work correctly.

---

## 📖 Step-by-Step Guide

### Step 1: HidHide Setup (Hide Physical Controller)
1. Install both **ViGEmBus Driver** and **HidHide**.
2. Connect your controller and switch it to **DirectInput (DInput)** mode.
3. Open **HidHide Configuration Client**.
4. Locate your physical controller in the list, check it so a **Red Lock icon** 🔒 appears, and check **Enable device hiding** (bottom-left corner).
5. Unplug/turn off your controller, then reconnect it for the hiding rule to apply.
6. Go to the **Applications** tab in HidHide and add `JoyDebouncer.exe` to allow JoyDebouncer access to the physical controller. *(Alternatively, use the Inverse cloak option and select the apps you wish to hide instead).*
7. **Verification:** Open Steam -> *Settings* -> *Controller*. If your physical controller name does **not** appear, it is successfully hidden!

### Step 2: Configuring JoyDebouncer
1. Launch **JoyDebouncer** and select your physical controller from the dropdown menu.
2. **Button Mapping:** Map your physical button numbers to the virtual Xbox layout.  
   * *Tip:* Open [Gamepad Tester](https://gamepad-tester.com/) in a browser to easily identify your controller's button index numbers.
3. **Optional Settings:**
   * Enable Deadzone configuration and adjust the inner/outer sliders if needed.
   * Enable **High Performance Mode** if you require maximum stick responsiveness.
4. **Saving Profiles:**
   * Click **SAVE** to apply and save your profile. Unsaved changes will not persist.
   * Use **NEW** to create a profile or **DEL** to delete unwanted profiles.
5. **Start Engine:**
   * Click **START**. The virtual Xbox 360 controller will be created.
   * Test inputs on [Gamepad Tester](https://gamepad-tester.com/). Verify that the Xbox 360 virtual controller responds smoothly without double inputs.
   * Click **STOP** when you are finished using the program.
