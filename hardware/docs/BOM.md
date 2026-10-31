# BOM: ESP32-WROOM-32E-N4 USB-C UART Board

One row per refdes. Value = component value only (drawn on sheet). Prose in Rationale. Every MPN UNVERIFIED until datasheet-checked on this machine’s libraries.

| Refdes | Value | Footprint | MPN | Rationale |
|--------|-------|-----------|-----|-----------|
| U1 | ESP32-WROOM-32E-N4 | RF_Module:ESP32-WROOM-32E | ESP32-WROOM-32E-N4 UNVERIFIED | Product MCU+antenna; symbol RF_Module:ESP32-WROOM-32 (discrete GND 1/15/38/39 match footprint pads); VDD on +3V3 only |
| U2 | CH340C | Package_SO:SOIC-16_3.9x9.9mm_P1.27mm | CH340C UNVERIFIED | USB-UART from VBUS only (VCC=VBUS, V3 bypassed); Iq off when USB unplugged — holds power.ch340_from_vbus_only and ≤50 µA sleep |
| U3 | LM66100DCK | Package_TO_SOT_SMD:SOT-363_SC-70-6 | LM66100DCKR UNVERIFIED | BAT+→+3V3 ideal-diode reverse protection; typ Iq ~0.2 µA / reverse leakage ≪50 µA; ~CE tied GND (always on); ST left open (intentional — no pullup Iq) |
| J1 | USB4105-03-A | Connector_USB:USB_C_Receptacle_GCT_USB4105-xx-A_16P_TopMnt_Horizontal | USB4105-03-A UNVERIFIED | USB2 16P mid-mount sink; D+/D− to CH340; VBUS to U2 only — no feed to +3V3 |
| SW1 | PTS810 | Button_Switch_SMD:SW_SPST_PTS810 | PTS810 UNVERIFIED | RESET momentary → EN; 4.2×3.2 mm body, about 2.5 mm tall, on the top |
| SW2 | PTS810 | Button_Switch_SMD:SW_SPST_PTS810 | PTS810 UNVERIFIED | BOOT momentary → GPIO0 strap |
| Q1 | 2N7002 | Package_TO_SOT_SMD:SOT-23 | 2N7002 UNVERIFIED | Auto-reset: DTR drives EN low; off-state Igss negligible vs sleep |
| Q2 | 2N7002 | Package_TO_SOT_SMD:SOT-23 | 2N7002 UNVERIFIED | Auto-boot: RTS drives IO0 low; off-state leakage ≪50 µA |
| R1 | 5.1k | Resistor_SMD:R_0402_1005Metric | RC0402FR-075K1L UNVERIFIED | CC1→GND sink advertisement (usb.cc_sink_advertisement) |
| R2 | 5.1k | Resistor_SMD:R_0402_1005Metric | RC0402FR-075K1L UNVERIFIED | CC2→GND sink advertisement |
| R3 | 10k | Resistor_SMD:R_0402_1005Metric | RC0402FR-0710KL UNVERIFIED | EN pullup to +3V3; classic ESP32 reset; no DC path when EN idle-high |
| R4 | 10k | Resistor_SMD:R_0402_1005Metric | RC0402FR-0710KL UNVERIFIED | IO0 pullup to +3V3; boot strap default high (SPI boot) |
| R5 | 10k | Resistor_SMD:R_0402_1005Metric | RC0402FR-0710KL UNVERIFIED | Q1 gate series from ~DTR; limits gate charge from VBUS-domain CH340 |
| R6 | 10k | Resistor_SMD:R_0402_1005Metric | RC0402FR-0710KL UNVERIFIED | Q2 gate series from ~RTS |
| R7 | 470 | Resistor_SMD:R_0402_1005Metric | RC0402FR-07470RL UNVERIFIED | TXD series CH340→ESP32 RXD0; limits backfeed into unpowered CH340 when USB out |
| R8 | 470 | Resistor_SMD:R_0402_1005Metric | RC0402FR-07470RL UNVERIFIED | RXD series ESP32 TXD0→CH340; same unpowered-bridge protection |
| C1 | 10uF | Capacitor_SMD:C_0402_1005Metric | CL05A106MQ5NUN UNVERIFIED | +3V3 bulk at WROOM per Espressif |
| C2 | 100nF | Capacitor_SMD:C_0402_1005Metric | CL05B104KO5NNNC UNVERIFIED | +3V3 HF decouple at WROOM |
| C3 | 100nF | Capacitor_SMD:C_0402_1005Metric | CL05B104KO5NNNC UNVERIFIED | CH340 VCC (VBUS) bypass — VBUS domain only |
| C4 | 100nF | Capacitor_SMD:C_0402_1005Metric | CL05B104KO5NNNC UNVERIFIED | CH340 V3 bypass per datasheet |
| C5 | 100nF | Capacitor_SMD:C_0402_1005Metric | CL05B104KO5NNNC UNVERIFIED | LM66100 VOUT/+3V3 local bypass |
| TP1 | BAT+ | TestPoint:TestPoint_Pad_D1.5mm | — | External 3.3 V LiPo module input pad (hand-solder) |
| TP2 | BAT- | TestPoint:TestPoint_Pad_D1.5mm | — | Battery return / GND pad |
| TP3 | GPIO4 | TestPoint:TestPoint_Pad_D1.5mm | — | Sliding-switch sense pad; switch not on BOM (intentional) |
| TP4 | GPIO16 | TestPoint:TestPoint_Pad_D1.5mm | — | Tamagotchi Paradise UART pad |
| TP5 | GPIO17 | TestPoint:TestPoint_Pad_D1.5mm | — | Tamagotchi Paradise UART pad |
| TP6 | GND | TestPoint:TestPoint_Pad_D1.5mm | — | Common GND pad for external wiring |

## Symbol lib_ids (stage 4)

- U1 → RF_Module:ESP32-WROOM-32 (footprint RF_Module:ESP32-WROOM-32E; discrete GND pins match pads)
- U2 → Interface_USB:CH340C
- U3 → Power_Management:LM66100DCK
- J1 → Connector:USB_C_Receptacle_USB2.0_16P
- SW1, SW2 → Switch:SW_Push
- Q1, Q2 → Transistor_FET:2N7002
- R1–R8 → Device:R
- C1–C5 → Device:C
- TP1–TP6 → Connector:TestPoint

## Sleep / Iq budget (USB unplugged)

| Contributor | Typ / notes | vs ≤50 µA |
|-------------|-------------|-----------|
| U1 deep-sleep | module-dominated | primary consumer |
| U3 LM66100 | ~0.2 µA Iq, reverse block | OK |
| U2 CH340C | unpowered (VBUS absent) | 0 on BAT |
| R3/R4 pullups | no DC when EN/IO0 high | OK |
| Q1/Q2 | off Igss nA-class | OK |
| C*, TP* | no Iq | OK |

**Intentional absences:** no LiPo charger/fuel-gauge/protection IC; no sliding-switch footprint; no VBUS→+3V3 regulator; no always-on LED; LM66100 ST no pullup; no CE resistor (~CE hard-tied GND).
