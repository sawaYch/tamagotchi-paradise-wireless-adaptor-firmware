# Hardware

The adaptor is one board: [`esp32-wroom-32e-n4-single`](esp32-wroom-32e-n4-single/).

Open [`esp32-wroom-32e-n4-single.kicad_pro`](esp32-wroom-32e-n4-single/esp32-wroom-32e-n4-single.kicad_pro) in KiCad.

## Board

| | |
| --- | --- |
| Module | ESP32-WROOM-32E-N4 |
| Layers | 2 |
| Copper | 1 oz |
| Thickness | 1.6 mm |
| Outline | 20.0 × 31.35 mm |
| Fabrication | JLCPCB |

The board edge runs from y = 6.25 mm to y = 37.6 mm. The module antenna sits in the 6.25 mm outside that edge, so the assembled length is about 37.6 mm.

USB-C (HRO TYPE-C-31-M-12) is a device (sink). VBUS powers the CH340X only. The ESP32 runs from BAT+ through an LM66100 ideal diode. BAT+ is 3.3 V from an external LiPo module, so USB and the battery can be connected at the same time without tying VBUS to the battery rail.

## Hand-solder pads

The pads are 1.0 mm and sit on the back edge. Left to right below is KiCad’s coordinate order, with x = 0 at the GPIO16 end. Back silkscreen reads **TX, RX, GND, SW, −, +**. Those silk names are the external wire: TX is the Tamagotchi TX line, which is the ESP32 RX pin.

| Silk | Ref | Net | Connect |
| --- | --- | --- | --- |
| TX | TP4 | GPIO16 | Tamagotchi UART TX (ESP32 RX) |
| RX | TP5 | GPIO17 | Tamagotchi UART RX (ESP32 TX) |
| GND | TP6 | GND | Tamagotchi ground |
| SW | TP3 | GPIO4 | Mode switch to GND. Open = SPP, shorted = BLE |
| − | TP2 | GND | Battery return (BAT−) |
| + | TP1 | BAT+ | 3.3 V from the external LiPo module |

UART is 460800 baud.

SW1 is RESET (EN). SW2 is BOOT (IO0).

## Flashing

Plug in USB-C. The CH340X shows up as a USB–UART. In Arduino, select **ESP32 Dev Module**. 4 MB flash is enough.

1. Hold **BOOT** (SW2).
2. Press and release **RESET** (SW1).
3. Release **BOOT**.
4. Upload [`firmware/tama_bridge/tama_bridge.ino`](../firmware/tama_bridge/tama_bridge.ino).

R9 (4.7 kΩ) between CH340X pins 5 and 6 selects push-pull DTR, idle high, for the auto-reset pair Q1 and Q2.

The firmware also drives GPIO2. This board does not bring GPIO2 out to a pad or an LED.

## Fabrication files

All order files are in [`esp32-wroom-32e-n4-single/fab/`](esp32-wroom-32e-n4-single/fab/).

| File | Use |
| --- | --- |
| `esp32-wroom-32e-n4-single-gerbers.zip` | Gerbers and drills for JLCPCB |
| `gerbers/` | The same plot, unpacked |
| `jlc_bom.csv` | JLCPCB assembly BOM |
| `jlc_cpl.csv` | JLCPCB pick-and-place |
| `bom-kicad.csv` | KiCad BOM export |
| `pos-raw.csv`, `esp32-wroom-32e-n4-single-all-pos.csv` | Position exports |

U1 (ESP32-WROOM-32E-N4) and U2 (CH340X) have no LCSC number in `jlc_bom.csv`. Fill those in before ordering assembly. The USB-C datasheet is [`datasheets/TYPE-C-31-M-12.pdf`](esp32-wroom-32e-n4-single/datasheets/TYPE-C-31-M-12.pdf).

## BOM

| Ref | Value | MPN | Notes |
| --- | --- | --- | --- |
| U1 | ESP32-WROOM-32E-N4 | ESP32-WROOM-32E-N4 | LCSC number not filled |
| U2 | CH340X | CH340X | USB–UART, powered from VBUS. LCSC number not filled |
| U3 | LM66100DCK | LM66100DCKT | BAT+ to +3V3 ideal diode. LCSC C2832141 |
| J1 | USB-C | TYPE-C-31-M-12 | HRO. LCSC C165948 |
| SW1, SW2 | Tactile | B3U-1000P | RESET and BOOT. LCSC C231329 |
| Q1, Q2 | 2N7002 | 2N7002 | Auto-reset and auto-boot |
| R1, R2 | 5.1 kΩ | 0402WGF5101TCE | USB CC sink resistors |
| R3–R6 | 10 kΩ | 0402WGF1002TCE | EN, IO0, and MOSFET gates |
| R7, R8 | 470 Ω | RC0402FR-07470RL | UART series resistors |
| R9 | 4.7 kΩ | 0402WGF4701TCE | CH340X DTR mode |
| C1 | 10 µF | CL05A106MQ5NUNC | +3V3 bulk |
| C2–C5 | 100 nF | CL05B104KO5NNNC | Bypass |
