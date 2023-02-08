#ifndef USB2CAN_USB_TRANSFER_H
#define USB2CAN_USB_TRANSFER_H
#include "stm32f1xx_hal.h"

#pragma once

/* define commands for USB-CDC custom communication protocol */
#define USB_CMD_START_MON 0x01U
#define USB_CMD_STOP_MON 0x02U
#define USB_CMD_HANDSHAKE 0x66U
#define USB_CMD_ENABLE_TERMINATOR 0x06U
#define USB_CMD_GETINFO 0x07U
#define USB_CMD_HELLO 0x0AU

/* CAN relate USB defines */
#define USB_CMD_CAN_ERROR 0x40U
#define USB_CMD_CAN_FRAME 0x41U

typedef struct
{
    uint8_t cmd;
    uint8_t data[32];
}USB_Data;

void USB_FuncTransfer(uint8_t command, uint8_t* data, uint8_t dataSize);


#endif //USB2CAN_USB_TRANSFER_H
