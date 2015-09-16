/*
  *****************************************************************************
  * @file    comms_hal.c
  * @author  Y3288231
  * @date    jan 15, 2014
  * @brief   Hardware abstraction for communication
  ***************************************************************************** 
*/ 

// Includes ===================================================================
#include "cmsis_os.h"
#include "usbd_cdc_if.h"
#include "mcu_config.h"
#include "comms.h"
#include "comms_hal.h"
#include "adc.h"
#include "usart.h"



// External variables definitions =============================================

// Function prototypes ========================================================
void commHalInit(void){
	/* init code for USB_DEVICE */
  MX_USB_DEVICE_Init();
/////	USART2Init();
/////	USART2RxIRQInit();
}

void commsSend(uint8_t chr){
	if (hUsbDeviceFS.dev_state == USBD_STATE_CONFIGURED){	
		while(CDC_Transmit_FS(&chr,1)!=USBD_OK){
		//////taskYIELD();
		}
	}else{
		UARTsendChar(chr);
	}
}

void commsSendBuff(uint8_t *buff, uint16_t len){
	if (hUsbDeviceFS.dev_state == USBD_STATE_CONFIGURED){	
		while(CDC_Transmit_FS(buff,len)!=USBD_OK){
		//////taskYIELD();
		}
	}else{
		UARTsendBuff((char *)buff,len);
	}
}
void commsSendString(char *chr){
	uint32_t i = 0;
	char * tmp=chr;
	while(*(tmp++)){i++;}
	if (hUsbDeviceFS.dev_state == USBD_STATE_CONFIGURED){	
		while(CDC_Transmit_FS((uint8_t*)chr,i)!=USBD_OK){
			/////taskYIELD();
		}
	}else{
		UARTsendBuff(chr,i);
	}

}

void commsRecieveUSB(uint8_t chr){
	if (hUsbDeviceFS.dev_state == USBD_STATE_CONFIGURED){	
		commInputByte(chr);
	}
}

void commsRecieveUART(uint8_t chr){
	if (hUsbDeviceFS.dev_state != USBD_STATE_CONFIGURED){	
		commInputByte(chr);
	}
}
