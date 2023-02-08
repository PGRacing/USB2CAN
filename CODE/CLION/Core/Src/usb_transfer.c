//
// Created by Karol on 08.02.2023.
//

#include "usb_transfer.h"
#include "usbd_cdc_if.h"

void USB_FuncTransfer(uint8_t command, uint8_t* data, uint8_t dataSize)
{
    USB_Data usbData;
    usbData.cmd = command;

    memset(usbData.data, 0x00, sizeof(usbData.data));
    memcpy(usbData.data, data, dataSize);

    CDC_Transmit_FS((uint8_t*)&usbData, dataSize + 1);
}