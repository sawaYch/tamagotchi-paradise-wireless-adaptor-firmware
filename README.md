# Tamagotchi Paradise Wireless Adaptor

A Bluetooth transparent serial bridge for **Tamagotchi Paradise**. It lets PC and mobile tools such as [**TamaHome**](https://bbamorachi.us/tamahome-toolkit-windows/), [**BBSite**](https://tools.bbamorachi.us/), [**Paramix Studio**](https://mameise.github.io/Paramix-Studio/), and similar UART-based apps talk to the device wirelessly over Bluetooth instead of a wired USB–UART cable.

Built around an **ESP32-WROOM-32E-N4** module, the adaptor sits between the host and Tamagotchi Paradise and forwards bytes in both directions at 460800 baud.

## Rev 1.0

<img src="./docs/20261126_191917.jpg" width="320" />
<img src="./docs/20261126_191859.jpg" width="320" />
<img src="./docs/20261127_042046.jpg" width="320" />
<img src="./docs/20261127_082405.jpg" width="320" />

_The silk print on the back TX RX means connect to TMGC's TX RX. It is not TX RX of the ESP32. Wireless adaptor case rev 1.0 still in early prototyping phase._

## Why this exists

Official and community tools expect a serial (UART / Web Serial) connection. That works well on desktop with a cable, but:

- Many platforms only expose Bluetooth reliably (especially mobile)
- Some browsers have Web Bluetooth but not Web Serial
- A small wireless bridge is easier to leave connected to the Tamagotchi

This project provides the hardware, firmware, and (for web tools) a BLE Web Serial shim so the same tools can work across **Windows, macOS, Linux, Android, and iOS**.

## How it works

```text
Host app (TamaHome / BBSite / …)
        │
        │  Bluetooth Classic SPP  or  BLE
        ▼
   ESP32-WROOM-32E-N4 bridge board
        │
        │  UART @ 460800 (GPIO16 RX / GPIO17 TX)
        ▼
  Tamagotchi Paradise
```

A mode switch on the board selects the radio link:

| Mode                               | Switch            | Typical use                          |
| ---------------------------------- | ----------------- | ------------------------------------ |
| **SPP** (Classic Bluetooth serial) | SW open           | Desktop apps, Chrome, Android Chrome |
| **BLE**                            | SW shorted to GND | BLE only for iOS Bluefy BBSite       |

Device names:

- SPP: `Tamagotchi Paradise Bridge SPP`
- BLE: `Tamagotchi Paradise Bridge`

## Repository layout

| Path                                                                         | Contents                                                                          |
| ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| [`firmware/tama_bridge/`](firmware/tama_bridge/)                             | Arduino / ESP32 firmware (SPP + BLE bridge)                                       |
| [`hardware/esp32-wroom-32e-n4-single/`](hardware/esp32-wroom-32e-n4-single/) | KiCad project for the ESP32-WROOM-32E-N4 board, plus Gerbers, BOM, and CPL        |
| [`hardware/README.md`](hardware/README.md)                                   | Pad map, flashing, and fabrication notes                                          |
| [`worker/`](worker/)                                                         | Cloudflare Worker that proxies web tools and injects a BLE Web Serial shim        |
| [`tools/ble-loopback/`](tools/ble-loopback/)                                 | Optional local BLE throughput / shim test pages (dev tools for protocol analysis) |
| [`tools/TamaHomeLaunch/`](tools/TamaHomeLaunch/)                             | Windows launcher so stock TamaHome can open the Bluetooth COM port at 460800      |

The adaptor also needs a case and a rechargeable LiPo battery circuit. See https://github.com/sawaYch/tamagotchi-paradise-uart-adaptor/tree/main/models/wireless

## Firmware

Sketch: [`firmware/tama_bridge/tama_bridge.ino`](firmware/tama_bridge/tama_bridge.ino)

- Arduino board: **ESP32 Dev Module** (4 MB flash is enough)
- Pins: UART2 on **IO16** (RX from Tamagotchi), **IO17** (TX to Tamagotchi); mode on **IO4**. Firmware also drives **IO2**, which this board does not bring out to a pad or LED.
- Build options in `build_opt.h` raise NimBLE buffers / MTU for better BLE throughput

Flash over the board’s USB-C port (onboard CH340X): hold **BOOT**, press **RST**, release **BOOT**, then upload. See [`hardware/README.md`](hardware/README.md).

## Hardware

One board, [`hardware/esp32-wroom-32e-n4-single/`](hardware/esp32-wroom-32e-n4-single/): a 2-layer, 20.0 × 31.35 mm PCB (1.6 mm, 1 oz). The ESP32-WROOM-32E-N4 antenna extends about 6.25 mm past the board edge. The module runs from an external 3.3 V LiPo regulator through an LM66100 ideal diode. USB-C powers a CH340X for programming and does not feed the battery rail.

Hand-solder pads on the back edge:

- GPIO16 / GPIO17 — Tamagotchi UART at 460800 baud
- GPIO4 — mode switch to GND
- BAT+ / BAT− — 3.3 V from the external battery module

Pad map, BOM, and Gerbers: [`hardware/README.md`](hardware/README.md).

## Web tools (BLE shim)

Desktop Web Serial apps usually need Classic serial or SPP. On platforms where only BLE is available, [`worker/`](worker/) runs a Cloudflare Worker that:

1. Proxies the upstream tools site
2. Injects [`worker/public/__tama/shim.js`](worker/public/__tama/shim.js), which implements `navigator.serial` on top of Web Bluetooth NUS

That lets browser-based tools reuse their existing Web Serial code path over BLE.

## Platform notes

| Platform | Typical path                             | Application                                                                                                           |
| -------- | ---------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Windows  | SPP                                      | TamaHome through [TamaHomeLaunch](#windows-tamahome-tamahomelaunch) ✅ <br/> bbamorachi.us ✅ <br/> Paramix Studio ✅ |
| MacOS    | SPP                                      | TamaHome ✅ <br/> bbamorachi.us ✅ <br/> Paramix Studio ✅                                                            |
| Linux    | SPP                                        | \*untested                                                                                                            |
| Android  | SPP via Chrome in Desktop site mode      | bbamorachi.us ✅ <br/> Paramix Studio ✅                                                                              |
| iOS      | BLE via Bluefy web browser | bbamorachi.us ✅ <br/> Paramix Studio ✅                                                                              |

Exact app support depends on each tool (TamaHome, BBSite, etc.). The adaptor only provides a transparent UART bridge.

## Windows TamaHome (TamaHomeLaunch)

Stock TamaHome on Windows cannot open this adaptor's Bluetooth serial port. [`tools/TamaHomeLaunch`](tools/TamaHomeLaunch) starts TamaHome and skips that one check in memory. It does not edit TamaHome's files. Browser tools such as BBSite do not need it: Chrome Web Serial does not have this check.

### Why it is needed

TamaHome talks to Tamagotchi Paradise at **460800** baud. This firmware keeps that rate on the UART between the ESP32 and the Tamagotchi (GPIO16 / GPIO17). Bluetooth SPP only carries the bytes. The baud number on the Windows COM port does not set the radio speed, and the Microsoft Bluetooth serial driver accepts `460800`.

The failure happens before the driver is asked. Windows reports the Bluetooth COM port's maximum baud as **115200**. TamaHome uses .NET `System.IO.Ports`, which reads that maximum and throws:

```text
The maximum baud rate for the device is 115200 (Parameter 'baudRate')
```

A wired CP2102, CH340, or FTDI port reports a maximum high enough for 460800, so the same TamaHome build works on a USB–UART cable and stops on Bluetooth.

Editing `System.IO.Ports.dll` inside the TamaHome folder removes the check, but a TamaHome update replaces that file. The launcher leaves the install untouched. Each time it starts TamaHome, it loads a small startup hook (`TamaHomeBaudHook.dll`) that skips the maximum-baud test in that process only.

### Build

TamaHome already requires the .NET 8 desktop runtime. From this repository:

```text
dotnet publish tools/TamaHomeLaunch/TamaHomeLaunch.csproj -c Release
```

The program is:

```text
tools/TamaHomeLaunch/bin/Release/net8.0/publish/TamaHomeLaunch.exe
```

`TamaHomeBaudHook.dll` is written next to that exe. Keep those files together. Point a shortcut at `TamaHomeLaunch.exe`, not at `TamaParadiseTool.exe`.

### Use

1. Pair **Tamagotchi Paradise Bridge SPP** in Windows. Leave the adaptor's mode switch open (SPP). Shorting the switch to GND selects BLE, which TamaHome does not use.
2. Start **TamaHomeLaunch.exe**. It runs `E:\TamaParadiseTool\TamaParadiseTool.exe`. If TamaHome is installed somewhere else, pass that path:

   ```text
   TamaHomeLaunch.exe "D:\Apps\TamaParadiseTool\TamaParadiseTool.exe"
   ```

3. In TamaHome, connect to the **Outgoing** Bluetooth COM port.

Windows creates two ports, both often named "Standard Serial over Bluetooth link". Only the Outgoing port connects to the adaptor. The Incoming port is for a remote device calling this PC. This adaptor is the server, so it never calls in. The Incoming port still opens, then every write waits until it times out. Cancelling that wait can freeze TamaHome. In Device Manager, the Outgoing port's device id contains the adaptor's Bluetooth address. The Incoming port's address is all zeros.

If the launcher cannot find TamaHome or the hook DLL, it shows a message box. A startup log is appended to `%LocalAppData%\TamaHomeLaunch\hook.log`. A normal start records that two baud checks were skipped.

## Extra Setup on iOS

Since iOS only support BLE, it does not create a serial COM port in web browser like Chrome. You need to install Bluefy (free on iOS App Store):

- https://bluefy.app/
- https://apps.apple.com/us/app/bluefy-web-ble-browser/id1492822055

### Highly suggest you to create a iOS ShortCut to visit the proxied Tamagotchi Web Tools:

1. Open "Shortcuts"  
   <img src="./docs/IMG_0879.PNG" width="450">

2. Click "All Shortcuts"  
   <img src="./docs/IMG_0880.PNG" width="450">

3. Click "+" sign to add new shortcut  
   <img src="./docs/IMG_0881.PNG" width="450">

4. Select "Open URL" (Not Open App)  
   <img src="./docs/IMG_0882.PNG" width="450">

5. Fill-in url "bluefy://{your hosted proxy website for tmgc web tools}"

6. Done. Now you click the newly added shortcut on Home Screen should open Bluefy with TMGC Web Tools. Toggle the wireless adaptor to BLE mode, and the on the web tools click "Connect USB", you should see the BLE adaptor device. Connect to it, then plug the wireless adaptor to your Tamagotchi Paradise, you should be able to transfer Gift, Lab items, etc.

## License

This project is licensed under the [MIT License](LICENSE).
