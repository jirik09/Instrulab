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


// Communication constatnts ===================================================
#define COMM_BUFFER_SIZE 512
#define UART_SPEED 230400

// Scope constatnts ===================================================
#define MAX_SAMPLING_FREQ 2000000 //smps
#define MAX_ADC_CHANNELS 3

#define MAX_SCOPE_BUFF_SIZE 101000 //in bytes

#define	ADC_DATA_DEPTH 4096


#define MAX_GENERATING_FREQ 2000000 //smps
#define MAX_DAC_CHANNELS 2
#define MAX_GENERATOR_BUFF_SIZE 4000
#define	DAC_DATA_DEPTH 4096



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
