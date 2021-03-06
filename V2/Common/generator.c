/*
  *****************************************************************************
  * @file    scope.c
  * @author  Y3288231
  * @date    Dec 15, 2014
  * @brief   This file contains oscilloscope functions
  ***************************************************************************** 
*/ 

// Includes ===================================================================
	#ifdef USE_GEN
#include "cmsis_os.h"
#include "mcu_config.h"
#include "comms.h"
#include "generator.h"
#include "dac.h"
#include "tim.h"


// External variables definitions =============================================
xQueueHandle generatorMessageQueue;
uint8_t validateGenBuffUsage(void);
void clearGenBuffer(void);
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
	generatorMessageQueue = xQueueCreate(5, 20);
	generatorSetDefault();
	char message[20];
	
	while(1){
		xQueueReceive(generatorMessageQueue, message, portMAX_DELAY);
		if(message[0]=='1'){

		}else if(message[0]=='2'){

		}else if(message[0]=='3'){ //invalidate
			if(generator.state==GENERATOR_IDLE){
				
			}
		}else if(message[0]=='4'){ //start
			if(generator.state==GENERATOR_IDLE){
				genInit();
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
	for(uint8_t i = 0;i<MAX_DAC_CHANNELS;i++){
		generator.generatingFrequency[i]=DEFAULT_GENERATING_FREQ;
		generator.realGenFrequency[i]=DEFAULT_GENERATING_FREQ;
	}
	
	generator.numOfChannles=1;
	generator.maxOneChanSamples=MAX_GENERATOR_BUFF_SIZE/2;
	generator.oneChanSamples[0]=MAX_GENERATOR_BUFF_SIZE/2;
	generator.pChanMem[0]=generatorBuffer;
	generator.state=GENERATOR_IDLE;
	generator.DAC_res=DAC_DATA_DEPTH;
}

void genInit(void){
	for(uint8_t i = 0;i<MAX_DAC_CHANNELS;i++){
		TIM_Reconfig_gen(generator.generatingFrequency[i],i,0);
		if(generator.numOfChannles>i){
			DAC_DMA_Reconfig(i,(uint32_t *)generator.pChanMem[i], generator.oneChanSamples[i]);
		}else{
			DAC_DMA_Reconfig(i,(uint32_t *)&blindValue, 1);
		}
	}
}


uint8_t genSetData(uint16_t index,uint8_t length,uint8_t chan){
	uint8_t result = GEN_INVALID_STATE;
	if(generator.state==GENERATOR_IDLE ){
		if ((index*2+length)/2<=generator.oneChanSamples[chan-1] && generator.numOfChannles>=chan){
			if(commBufferReadNBytes((uint8_t *)generator.pChanMem[chan-1]+index*2,length)==length && commBufferReadByte(&result)==0 && result==';'){
				result = 0;
				xQueueSendToBack(generatorMessageQueue, "3Invalidate", portMAX_DELAY);
			}else{
			result = GEN_INVALID_DATA;
			}
		}else{
			result = GEN_OUT_OF_MEMORY;
		}
	}
	return result;
}

uint8_t genSetFrequency(uint32_t freq,uint8_t chan){
	uint8_t result = GEN_TO_HIGH_FREQ;
	uint32_t realFreq;
	if(freq<=MAX_GENERATING_FREQ){
		generator.generatingFrequency[chan-1] = freq;
		result = TIM_Reconfig_gen(generator.generatingFrequency[chan-1],chan-1,&realFreq);
		generator.realGenFrequency[chan-1] = realFreq;
	}
	return result;
}

void genSendRealSamplingFreq(void){
	xQueueSendToBack(messageQueue, "2SendGenFreq", portMAX_DELAY);
}

void genDataOKSendNext(void){
	xQueueSendToBack(messageQueue, "7GenNext", portMAX_DELAY);
}

void genStatusOK(void){
	xQueueSendToBack(messageQueue, "8GenOK", portMAX_DELAY);
}


uint32_t genGetRealSmplFreq(uint8_t chan){
	return generator.realGenFrequency[chan-1];
}

uint8_t genSetLength(uint32_t length,uint8_t chan){
	uint8_t result=GEN_INVALID_STATE;
	if(generator.state==GENERATOR_IDLE){
		uint32_t smpTmp=generator.maxOneChanSamples;
		if(length<=generator.maxOneChanSamples){
			generator.oneChanSamples[chan-1]=length;
			clearGenBuffer();
			result=0;
		}else{
			result = GEN_BUFFER_SIZE_ERR;
		}
		xQueueSendToBack(generatorMessageQueue, "3Invalidate", portMAX_DELAY);
	}
	return result;
}



uint8_t genSetNumOfChannels(uint8_t chan){
	uint8_t result=GEN_INVALID_STATE;
	uint8_t chanTmp=generator.numOfChannles;
	if(generator.state==GENERATOR_IDLE){
		if(chan<=MAX_DAC_CHANNELS){
			generator.numOfChannles=chan;
			generator.maxOneChanSamples=MAX_GENERATOR_BUFF_SIZE/2/chan;
			for(uint8_t i=0;i<chan;i++){
				generator.pChanMem[i]=(uint16_t *)&generatorBuffer[i*generator.maxOneChanSamples];
			}
			result=0;
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
	uint32_t data_len=generator.maxOneChanSamples;
	if(generator.DAC_res>8){
		data_len=data_len*2;
	}
	data_len=data_len*generator.numOfChannles;
	if(data_len<=MAX_GENERATOR_BUFF_SIZE){
		result=0;
	}
	return result;
}

/**
  * @brief 	Clears generator buffer
  * @param  None
  * @retval None
  */
void clearGenBuffer(void){
	for(uint32_t i=0;i<MAX_GENERATOR_BUFF_SIZE/2;i++){
		generatorBuffer[i]=0;
	}
}



void genSetOutputBuffer(void){
	DACSetOutputBuffer();
}

void genUnsetOutputBuffer(void){
	DACUnsetOutputBuffer();
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

	#endif //USE_GEN

