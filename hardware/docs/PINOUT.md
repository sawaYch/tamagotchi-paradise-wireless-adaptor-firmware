# PINOUT: ESP32-WROOM-32E-N4 USB-C UART Board

Authoritative net ↔ pin map from `schematic.intent.json` (stage 4). Symbol for U1 is `RF_Module:ESP32-WROOM-32` (discrete GND pins 1/15/38/39 match footprint `RF_Module:ESP32-WROOM-32E`).

## Power

| Net | Pins | Rationale |
|-----|------|-----------|
| BAT+ | TP1.1, U3.1 (VIN) | External 3.3 V LiPo module input → LM66100 |
| +3V3 | U3.6 (VOUT), U1.2 (VDD), C1.1, C2.1, C5.1, R3.1, R4.1 | BAT-derived MCU rail only; no VBUS feed |
| GND | TP2.1, TP6.1, U3.2, U3.3 (~CE), U1.1, U1.15, U1.38, U1.39, U2.1, J1.A1/A12/B1/B12/SH, C1–C5.2, R1.2, R2.2, SW1.2, SW2.2 | Common return; ~CE hard-tied GND (always on, no CE resistor Iq) |
| VBUS | J1.A4/A9/B4/B9, U2.16 (VCC), C3.1 | USB domain only — powers CH340 (`power.ch340_from_vbus_only`) |
| CH340_V3 | U2.4, C4.1 | CH340 internal 3.3 V bypass node |

## USB-C (J1)

| Net | Pins | Rationale |
|-----|------|-----------|
| USB_CC1 | J1.A5, R1.1 | 5.1 kΩ → GND sink advertisement |
| USB_CC2 | J1.B5, R2.1 | 5.1 kΩ → GND sink advertisement |
| USB_D+ | J1.A6, J1.B6, U2.5 (UD+) | USB2 D+ to CH340; pair suffix so D+/D− read as one interface |
| USB_D- | J1.A7, J1.B7, U2.6 (UD−) | USB2 D− to CH340 |

## UART0 programming (CH340 ↔ ESP32)

| Net | Pins | Rationale |
|-----|------|-----------|
| UART_TXD | U2.2 (TXD), R7.1 | CH340 TX → series R |
| UART_RXD | R7.2, U1.34 (RXD0/IO3) | Into ESP32 RXD0; 470 Ω limits unpowered-bridge backfeed |
| UART_ESP_TX | U1.35 (TXD0/IO1), R8.1 | ESP32 TX → series R; UART_ prefix with the programming pair |
| UART_CH_RX | R8.2, U2.3 (RXD) | Into CH340 RX; same 470 Ω unpowered-bridge limit |

## Reset / boot strapping

| Net | Pins | Rationale |
|-----|------|-----------|
| EN | U1.3, R3.2, SW1.1, Q1.3 (D) | RESET + auto-reset; R3 10k pullup to +3V3 |
| IO0 | U1.25, R4.2, SW2.1, Q2.3 (D) | BOOT strap; R4 10k pullup default SPI boot |
| USB_DTR | U2.13 (~DTR), R6.1, Q1.2 (S) | ~DTR to Q1 source and, through R6, to Q2 gate |
| USB_RTS | U2.14 (~RTS), R5.1, Q2.2 (S) | ~RTS to Q2 source and, through R5, to Q1 gate |
| Q1_G | R5.2, Q1.1 (G) | Gate series from ~RTS |
| Q2_G | R6.2, Q2.1 (G) | Gate series from ~DTR |

## External pads

| Net | Pins | Rationale |
|-----|------|-----------|
| GPIO4 | U1.26, TP3.1 | Sliding-switch sense pad (switch not on BOM) |
| GPIO16 | U1.27, TP4.1 | Tamagotchi Paradise UART |
| GPIO17 | U1.28, TP5.1 | Tamagotchi Paradise UART |

## Intentional no-connects

| Pins | Why absence is intentional |
|------|----------------------------|
| U3.4 (NC), U3.5 (ST) | ST open — no pullup Iq vs ≤50 µA sleep |
| U2.7, U2.8 (NC); U2.9–12 (~CTS/~DSR/~RI/~DCD); U2.15 (R232) | Unused CH340 modem/RS232 pins |
| J1.A8, J1.B8 (SBU1/SBU2) | USB2-only; SBU unused |
| U1.4–14, U1.16–24, U1.29–33, U1.36–37 | Unused WROOM GPIOs/flash straps not brought out |
