/*
  *****************************************************************************
  * @file    scope.c
  * @author  Y3288231
  * @date    Dec 15, 2014
  * @brief   This file contains oscilloscope functions
  ***************************************************************************** 
*/ 

// Includes ===================================================================
#include "cmsis_os.h"
#include "mcu_config.h"
#include "comms.h"
#include "generator.h"
#include "dac.h"
#include "tim.h"


// External variables definitions =============================================
xQueueHandle generatorMessageQueue;
uint8_t validateGenBuffUsage(void);
static volatile generatorTypeDef generator;
uint16_t blindValue=0;

uint16_t generatorBuffer[MAX_GENERATOR_BUFF_SIZE/2]; 

// Function definitions =======================================================
/**
  * @brief  Oscilloscope task function.
  * task is getting messages from other tasks and takes care about oscilloscope functions
  * @param  Task handler, parameters pointer
  * @retval None
  */
//portTASK_FUNCTION(vScopeTask, pvParameters){	
void GeneratorTask(void const *argument){
	
	generatorMessageQueue = xQueueCreate(10, 20);
	generatorSetDefault();
	char message[20];
	
	while(1){
		xQueueReceive(generatorMessageQueue, message, portMAX_DELAY);
		if(message[0]=='1'){

		}else if(message[0]=='2'){

		}else if(message[0]=='3'){ //invalidate
			if(generator.state==GENERATOR_IDLE){
				genInit();
			}
		}else if(message[0]=='4'){ //start
			if(generator.state==GENERATOR_IDLE){
				GeneratingEnable();
				generator.state=GENERATOR_RUN;
			}

		}else if(message[0]=='5'){ //stop
			if(generator.state==GENERATOR_RUN){
				GeneratingDisable();
				generator.state=GENERATOR_IDLE;
			}
		}

	}
}


/**
  * @brief  Oscilloscope set Default values
  * @param  None
  * @retval None
  */
void generatorSetDefault(void){
	generator.bufferMemory=generatorBuffer;
	generator.generatingFrequency=DEFAULT_GENERATING_FREQ;
	generator.numOfChannles=1;
	generator.oneChanSamples=MAX_GENERATOR_BUFF_SIZE/2;
	generator.pChanMem[0]=generatorBuffer;
	generator.state=GENERATOR_IDLE;
	generator.DAC_res=DAC_DATA_DEPTH;
}

void genInit(void){
	for(uint8_t i = 0;i<MAX_DAC_CHANNELS;i++){
		if(generator.numOfChannles>i){
			DAC_DMA_Reconfig(i,(uint32_t *)generator.pChanMem[i], generator.oneChanSamples);
		}else{
			DAC_DMA_Reconfig(i,(uint32_t *)&blindValue, 1);
		}
	}
	TIM_Reconfig(generator.generatingFrequency,&htim6);
}


uint8_t genSetData(uint16_t index,uint8_t length,uint8_t chan){
	uint8_t result = 1;
	if(generator.state==GENERATOR_IDLE && length<=generator.oneChanSamples && generator.numOfChannles<=chan){
		commBufferReadNBytes((uint8_t *)generator.pChanMem[chan-1]+index,length);
		result = 0;
	}
	return result;
}

uint8_t genSetFrequency(uint32_t freq){
	uint8_t result = 1;
	if(generator.state==GENERATOR_IDLE && freq<=MAX_GENERATING_FREQ){
		generator.generatingFrequency=freq;
		xQueueSendToBack(generatorMessageQueue, "3Invalidate", portMAX_DELAY);
		result=0;
	}
	return result;
}

uint8_t genSetLength(uint32_t length){
	uint8_t result=1;
	if(generator.state==GENERATOR_IDLE){
		uint32_t smpTmp=generator.oneChanSamples;
		generator.oneChanSamples=length;
		if(validateGenBuffUsage()){
			generator.oneChanSamples=smpTmp;
		}else{
			result=0;
		}
		xQueueSendToBack(generatorMessageQueue, "3Invalidate", portMAX_DELAY);
	}
	return result;
}



uint8_t genSetNumOfChannels(uint8_t chan){
	uint8_t result=1;
	uint8_t chanTmp=generator.numOfChannles;
	if(generator.state==GENERATOR_IDLE){
		if(chan<=MAX_DAC_CHANNELS){
			generator.numOfChannles=chan;
			if(validateGenBuffUsage()){
				generator.numOfChannles = chanTmp;
			}else{
				for(uint8_t i=0;i<chan;i++){
					generator.pChanMem[i]=(uint16_t *)&generatorBuffer[i*generator.oneChanSamples];
				}
				result=0;
			}
			xQueueSendToBack(generatorMessageQueue, "3Invalidate", portMAX_DELAY);
		}
	}
	return result;
}


/**
  * @brief 	Checks if scope settings doesn't exceed memory
  * @param  None
  * @retval err/ok
  */
uint8_t validateGenBuffUsage(){
	uint8_t result=1;
	uint32_t data_len=generator.oneChanSamples;
	if(generator.DAC_res>256){
		data_len=data_len*2;
	}
	data_len=data_len*generator.numOfChannles;
	if(data_len<=MAX_GENERATOR_BUFF_SIZE){
		result=0;
	}
	return result;
}


/**
  * @brief  Start scope sampling
  * @param  None
  * @retval None
  */
void genStart(void){
	xQueueSendToBack(generatorMessageQueue, "4Start", portMAX_DELAY);
}

/**
  * @brief  Stop scope sampling
  * @param  None
  * @retval None
  */
void genStop(void){
	xQueueSendToBack(generatorMessageQueue, "5Stop", portMAX_DELAY);
}


