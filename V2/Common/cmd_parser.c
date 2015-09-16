/*
  *****************************************************************************
  * @file    cmd_parser.c
  * @author  Y3288231
  * @date    Dec 16, 2014
  * @brief   This file contains functions for parsing commands
  ***************************************************************************** 
*/ 

// Includes ===================================================================
#include "cmsis_os.h"
#include "mcu_config.h"
#include "cmd_parser.h"
#include "commands.h"
#include "comms.h"
#include "scope.h"
#include "generator.h"
#include "adc.h"

// External variables definitions =============================================
xQueueHandle cmdParserMessageQueue;
command parseScopeCmd(void);
command parseGeneratorCmd(void);
command giveNextCmd(void);
// Function definitions =======================================================

/**
  * @brief  Command parser task function.
  * @param  Task handler, parameters pointer
  * @retval None
  */
	//portTASK_FUNCTION(vCmdParserTask, pvParameters) {
void CmdParserTask(void const *argument){
	cmdParserMessageQueue = xQueueCreate(10, 20);
	uint8_t message[20];
	uint8_t cmdIn[4];
	uint8_t chr;
	uint8_t byteRead;
	command tempCmd;
	while(1){
		xQueueReceive(cmdParserMessageQueue, message, portMAX_DELAY);
		///commsSendString("PARS_Run\r\n");

		if(message[0] == '1'){//parsing of command
			do{
				cmdIn[0] = cmdIn[1];
				cmdIn[1] = cmdIn[2];
				cmdIn[2] = cmdIn[3];
				cmdIn[3] = chr;
				byteRead = commBufferReadByte(&chr);
			}while(byteRead==0 && chr != ':' && chr != ';');
			
			switch (BUILD_CMD(cmdIn)){
				case CMD_IDN: //send IDN
					///ommsSendString("PARS_IDN\r\n");
					///xQueueSendToBack (messageQueue, STR_IDN, portMAX_DELAY);
					xQueueSendToBack (messageQueue, IDN_STRING, portMAX_DELAY);
					
				break;
				case CMD_SCOPE: //parse scope command
					///commsSendString("PARS_Scope\r\n");
					tempCmd = parseScopeCmd();
					if(tempCmd == CMD_END){
						xQueueSendToBack(messageQueue, STR_ACK, portMAX_DELAY);
					}else{
						xQueueSendToBack(messageQueue, STR_ERR, portMAX_DELAY);
					}
				break;
					
				case CMD_GENERATOR: //parse scope command
					///commsSendString("PARS_Scope\r\n");
					tempCmd = parseGeneratorCmd();
					if(tempCmd == CMD_END){
						xQueueSendToBack(messageQueue, STR_ACK, portMAX_DELAY);
					}else{
						xQueueSendToBack(messageQueue, STR_ERR, portMAX_DELAY);
					}
				break;
				default:
						xQueueSendToBack(messageQueue, STR_ERR, portMAX_DELAY);
			}	
		}
	}
}

/**
  * @brief  Scope command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseScopeCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	//try to parse command while buffer is not empty 

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_SCOPE_TRIG_MODE://set trigger mode
				cmdIn = giveNextCmd();
				if(isScopeTrigMode(cmdIn)){
					if(cmdIn == CMD_MODE_NORMAL){
						scopeSetTriggerMode(TRIG_NORMAL);
					}else if(cmdIn == CMD_MODE_AUTO){
						scopeSetTriggerMode(TRIG_AUTO);
					}else if(cmdIn == CMD_MODE_SINGLE){
						scopeSetTriggerMode(TRIG_SINGLE);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
			case CMD_SCOPE_TRIG_EDGE: //set trigger edge
				cmdIn = giveNextCmd();
				if(isScopeTrigEdge(cmdIn)){
					if(cmdIn == CMD_EDGE_RISING){
						scopeSetTriggerEdge(EDGE_RISING);
					}else if(cmdIn == CMD_EDGE_FALLING){
						scopeSetTriggerEdge(EDGE_FALLING);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;		
				
			case CMD_SCOPE_CHANNELS: //set number of channels
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=scopeSetNumOfChannels(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=scopeSetNumOfChannels(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=scopeSetNumOfChannels(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=scopeSetNumOfChannels(4);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
			case CMD_SCOPE_TRIG_CHANNEL: //set trigger channel
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=scopeSetTrigChannel(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=scopeSetTrigChannel(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=scopeSetTrigChannel(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=scopeSetTrigChannel(4);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
				//setting of data resolution not used
////			case CMD_SCOPE_DATA_DEPTH: //set data bit depth
////				cmdIn = giveNextCmd();
////				if(isScopeDataDepth(cmdIn)){
////					if(cmdIn == CMD_DATA_DEPTH_12B){
////						error=scopeSetDataDepth(ADC_12B);
////					}else if(cmdIn == CMD_DATA_DEPTH_10B){
////						error=scopeSetDataDepth(ADC_10B);
////					}else if(cmdIn == CMD_DATA_DEPTH_8B){
////						error=scopeSetDataDepth(ADC_8B);
////					}else if(cmdIn == CMD_DATA_DEPTH_6B){
////						error=scopeSetDataDepth(ADC_6B);
////					}
////				}else{
////					cmdIn = CMD_ERR;
////				}
////			break;
				
			case CMD_SCOPE_SAMPLING_FREQ: //set sampling frequency
				cmdIn = giveNextCmd();
				if(isScopeFreq(cmdIn)){
					if(cmdIn == CMD_FREQ_1K){
						error=scopeSetSamplingFreq(1000);
					}else if(cmdIn == CMD_FREQ_2K){
						error=scopeSetSamplingFreq(2000);
					}else if(cmdIn == CMD_FREQ_5K){
						error=scopeSetSamplingFreq(5000);
					}else if(cmdIn == CMD_FREQ_10K){
						error=scopeSetSamplingFreq(10000);
					}else if(cmdIn == CMD_FREQ_20K){
						error=scopeSetSamplingFreq(20000);
					}else if(cmdIn == CMD_FREQ_50K){
						error=scopeSetSamplingFreq(50000);
					}else if(cmdIn == CMD_FREQ_100K){
						error=scopeSetSamplingFreq(100000);
					}else if(cmdIn == CMD_FREQ_200K){
						error=scopeSetSamplingFreq(200000);
					}else if(cmdIn == CMD_FREQ_500K){
						error=scopeSetSamplingFreq(500000);
					}else if(cmdIn == CMD_FREQ_1M){
						error=scopeSetSamplingFreq(1000000);
					}else if(cmdIn == CMD_FREQ_2M){
						error=scopeSetSamplingFreq(2000000);
					}else if(cmdIn == CMD_FREQ_5M){
						error=scopeSetSamplingFreq(5000000);
					}else if(cmdIn == CMD_FREQ_10M){
						error=scopeSetSamplingFreq(10000000);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
						
			case CMD_SCOPE_TRIG_LEVEL: //set trigger level
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					scopeSetTrigLevel((uint16_t)cmdIn);
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
			case CMD_SCOPE_PRETRIGGER: //set prettriger
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					scopeSetPretrigger((uint16_t)cmdIn);
				}else{
					cmdIn = CMD_ERR;
				}
			break;	
				
			case CMD_SCOPE_DATA_LENGTH: //set trigger edge
				cmdIn = giveNextCmd();
				if(isScopeNumOfSamples(cmdIn)){
					if(cmdIn == CMD_SAMPLES_200){
						error=scopeSetNumOfSamples(200);
					}else if(cmdIn == CMD_SAMPLES_500){
						error=scopeSetNumOfSamples(500);
					}else if(cmdIn == CMD_SAMPLES_1K){
						error=scopeSetNumOfSamples(1000);
					}else if(cmdIn == CMD_SAMPLES_2K){
						error=scopeSetNumOfSamples(2000);
					}else if(cmdIn == CMD_SAMPLES_5K){
						error=scopeSetNumOfSamples(5000);
					}else if(cmdIn == CMD_SAMPLES_10K){
						error=scopeSetNumOfSamples(10000);
					}else if(cmdIn == CMD_SAMPLES_20K){
						error=scopeSetNumOfSamples(20000);
					}else if(cmdIn == CMD_SAMPLES_50K){
						error=scopeSetNumOfSamples(50000);
					}else if(cmdIn == CMD_SAMPLES_100K){
						error=scopeSetNumOfSamples(100000);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
			case CMD_SCOPE_START: //start sampling
				scopeStart();
			break;	
			
			case CMD_SCOPE_STOP: //stop sampling
				scopeStop();
			break;	
			
			case CMD_SCOPE_NEXT: //restart sampling
				scopeRestart();
			break;	
				
			case CMD_END:break;
			default:
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error==1){
		cmdIn=CMD_ERR;
	}
return cmdIn;
}





/**
  * @brief  Scope command parse function 
  * @param  None
  * @retval Command ACK or ERR
  */
command parseGeneratorCmd(void){
	command cmdIn=CMD_ERR;
	uint8_t error=0;
	uint16_t index;
	uint8_t length,chan;

		do{ 
		cmdIn = giveNextCmd();
		switch(cmdIn){
			case CMD_GEN_DATA://set data
				cmdIn = giveNextCmd();
				index=cmdIn;
				length=cmdIn>>16;
				chan=cmdIn>>24;
				if(getBytesAvailable()<length*2){
					error=1;
				}else{
					error=genSetData(index*2,length*2,chan);
				}
			break;
				
			case CMD_GEN_SAMPLING_FREQ: //set sampling freq
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					error=genSetFrequency(cmdIn);
				}else{
					cmdIn = CMD_ERR;
				}
			break;	
				
			case CMD_GEN_DATA_LENGTH: //set data length
				cmdIn = giveNextCmd();
				if(cmdIn != CMD_END && cmdIn != CMD_ERR){
					error=genSetLength(cmdIn);
				}else{
					cmdIn = CMD_ERR;
				}
			break;	
			
			case CMD_GEN_CHANNELS: //set number of channels
				cmdIn = giveNextCmd();
				if(isChannel(cmdIn)){
					if(cmdIn == CMD_CHANNELS_1){
						error=genSetNumOfChannels(1);
					}else if(cmdIn == CMD_CHANNELS_2){
						error=genSetNumOfChannels(2);
					}else if(cmdIn == CMD_CHANNELS_3){
						error=genSetNumOfChannels(3);
					}else if(cmdIn == CMD_CHANNELS_4){
						error=genSetNumOfChannels(4);
					}
				}else{
					cmdIn = CMD_ERR;
				}
			break;
				
				
			case CMD_GEN_START: //start sampling
				genStart();
			break;	
			
			case CMD_GEN_STOP: //stop sampling
				genStop();
			break;	
				
			case CMD_END:break;
			default:
				cmdIn = CMD_ERR;
			break;
		}
	}while(cmdIn != CMD_END && cmdIn != CMD_ERR && error==0);
	if(error==1){
		cmdIn=CMD_ERR;
	}
return cmdIn;
}

/**
  * @brief  Read command from input buffer 
  * @param  None
  * @retval Command
  */
command giveNextCmd(void){
	uint8_t cmdNext[5];
	uint8_t bytesRead = commBufferReadNBytes(cmdNext, 5);
	if(bytesRead >= 4){
		return BUILD_CMD(cmdNext);
	}else if(bytesRead == 0){
		return CMD_END;
	}else{
		return CMD_ERR;
	}
} 
