/*
  *****************************************************************************
  * @file    generator.h
  * @author  Y3288231
  * @date    Dec 15, 2014
  * @brief   This file contains definitions and prototypes of oscilloscope functions
  ***************************************************************************** 
*/ 
#ifndef GENERATOR_H_
#define GENERATOR_H_

#define DEFAULT_GENERATING_FREQ 1000


typedef enum{
	GENERATOR_IDLE = 0,
	GENERATOR_RUN
}generatorState;

typedef struct{
	uint16_t *bufferMemory;		
	uint32_t generatingFrequency; 	
	generatorState state;	
	uint8_t numOfChannles;
	uint16_t *pChanMem[4];
	uint32_t oneChanSamples;
	uint16_t DAC_res;
}generatorTypeDef;


void GeneratorTask(void const *argument);
void generatorSetDefault(void);
void genInit(void);
uint8_t genSetData(uint16_t index,uint8_t length,uint8_t chan);
uint8_t genSetFrequency(uint32_t freq);
uint8_t genSetLength(uint32_t length);
uint8_t genSetNumOfChannels(uint8_t chan);
void genStart(void);
void genStop(void);




#endif /* GENERATOR_H_ */




