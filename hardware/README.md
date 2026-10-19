# Tama Bridge PCB: ESP-32S

Module: ESP-32S (same pinout as ESP32-WROOM-32, 18 × 25.5 mm castellation). Firmware: verified `firmware/tama_bridge/` (IO16 = Tama RX, IO17 = Tama TX).

## Dimensions

- PCB: 20.0 × 37.6 mm. Enclosure inner limit is 20.5 × 38 mm, leaving about 0.2–0.25 mm per side.
- Antenna is at one end of the board, flush with the edge. No copper under the antenna (roughly the first 7 mm).
- Module body is 18 mm wide; board is 20 mm wide, leaving about 0.5 mm of pad outside the castellation for soldering.

## Pads (rear edge, left to right)

| Silkscreen | Function |
|---|---|
| TX RX G | J2, CH340. Board TX → CH340 RX, board RX → CH340 TX, common ground. Do not power from the CH340. |
| G RX TX | J3, Tamagotchi. RX → Tama RX (IO17 via 330 Ω), TX ← Tama TX (IO16 via 330 Ω). |
| SW G | J4, mode switch. SW shorted to G = BLE; open = SPP. |
| + − | J1, 3.3 V power. Put your own power switch in series on the positive rail. No reverse-polarity protection. |

Front: RST (SW1, EN) and BOOT (SW2, IO0). Back silkscreen: UART, TMGC, MODE, BAT.

Flash: hold BOOT, press RST once, release BOOT.

## Parts

SMD BOM / CPL are in `hardware/kicad/tama_bridge_esp32s/fab/`. U1 (ESP-32S) is listed; you can remove it manually when ordering from JLCPCB and solder it yourself. J1–J4 are wire pads only and are not in the BOM.

- U1 ESP-32S, LCSC C277944 (JLC stock may be 0; buy a pin-compatible ESP-32S / ESP-WROOM-32 yourself)
- Remaining 0603 / 0805 parts and two TS-1088 buttons: see BOM

Arduino: Board = ESP32 Dev Module. 4 MB flash is enough.

Gerber: `hardware/kicad/tama_bridge_esp32s/fab/tama_bridge_esp32s_gerber.zip`
