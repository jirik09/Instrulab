/*
  *****************************************************************************
  * @file    mcu_config.h
  * @author  Y3288231
  * @date    jan 15, 2014
  * @brief   Hardware abstraction for communication
  ***************************************************************************** 
*/ 
#ifndef STM32F4_CONFIG_H_
#define STM32F4_CONFIG_H_

#include "stm32f4xx_hal.h"
#include "usb_device.h"
#include "math.h"
#include "err_list.h"

#define IDN_STRING "STM32F4-Discovery FW1.00" //max 30 chars
#define MCU "STM32F407VG"

// Communication constatnts ===================================================
#define COMM_BUFFER_SIZE 512
#define UART_SPEED 230400

#define USART_GPIO GPIOA
#define USART_TX GPIO_PIN_2
#define USART_RX GPIO_PIN_3
#define USART_CFG_STR "TX-PA2 RX-PA3"

#define USE_USB
#define USB_CFG_STR "DP-PA12 DM-PA11"

// Scope constatnts ===================================================
#define MAX_SAMPLING_FREQ 2000000 //smps
#define MAX_ADC_CHANNELS 3

#define MAX_SCOPE_BUFF_SIZE 5000 //in bytes
#define SCOPE_BUFFER_MARGIN 100
#define SCOPE_CFG_STR "CH1-PC1 CH2-PC2 CH3-PC3" //TODO definice dole prepsat tak aby byly pouzity


#define MAX_GENERATING_FREQ 2000000 //smps
#define MAX_DAC_CHANNELS 2
#define MAX_GENERATOR_BUFF_SIZE 2000
#define	DAC_DATA_DEPTH 12
#define GEN_CFG_STR "CH1-PA4 CH2-PA5"


/* Definition of ADC and DMA for channel 1 */
#define ADC_CH_1  ADC1
#define ADC_GPIO_CH_1  GPIOC
#define ADC_PIN_CH_1  GPIO_PIN_0
#define ADC_CHANNEL_CH_1  ADC_CHANNEL_10
#define ADC_DMA_CHANNEL_CH_1  DMA_CHANNEL_0
#define ADC_DMA_STREAM_CH_1  DMA2_Stream0

/* Definition of ADC and DMA for channel 2 */
#define ADC_CH_2  ADC2
#define ADC_GPIO_CH_2  GPIOC
#define ADC_PIN_CH_2  GPIO_PIN_1
#define ADC_CHANNEL_CH_2  ADC_CHANNEL_11
#define ADC_DMA_CHANNEL_CH_2  DMA_CHANNEL_1
#define ADC_DMA_STREAM_CH_2  DMA2_Stream2 

/* Definition of ADC and DMA for channel 3 */
#define ADC_CH_3  ADC3
#define ADC_GPIO_CH_3  GPIOC
#define ADC_PIN_CH_3  GPIO_PIN_2
#define ADC_CHANNEL_CH_3  ADC_CHANNEL_12
#define ADC_DMA_CHANNEL_CH_3  DMA_CHANNEL_2
#define ADC_DMA_STREAM_CH_3  DMA2_Stream1

/* Definition of ADC and DMA for channel 4 */
#define ADC_CH_4  0
#define ADC_GPIO_CH_4  0
#define ADC_PIN_CH_4  0
#define ADC_CHANNEL_CH_4  0
#define ADC_DMA_CHANNEL_CH_4  0
#define ADC_DMA_STREAM_CH_4  0 




#endif /* STM32F4_CONFIG_H_ */
